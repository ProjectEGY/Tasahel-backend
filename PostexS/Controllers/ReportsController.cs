using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using PostexS.Interfaces;
using PostexS.Models.Enums;
using PostexS.Services;
using PostexS.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using PostexS.Models.ViewModels;
using System.Threading.Tasks;
using OrderStatus = PostexS.Models.Enums.OrderStatus;

namespace PostexS.Controllers
{
    [Authorize(Roles = "Admin,HighAdmin,TrustAdmin")]
    public class ReportsController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IWalletService _walletService;
        private readonly IGeneric<Wallet> _wallet;

        public ReportsController(IOrderService orderService, IWalletService walletService, IGeneric<Wallet> wallet)
        {
            _orderService = orderService;
            _walletService = walletService;
            _wallet = wallet;
        }

        #region ExportOrders
        [HttpGet]
        public ActionResult ExportOrders()
        {
            TimeZoneInfo egyptZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            DateTime nowInEgypt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptZone);

            var model = new ReportVM
            {
                fromDate = nowInEgypt.Date,
                toDate = nowInEgypt.Date.AddDays(1).AddSeconds(-1)
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult ExportOrders(ReportVM model)
        {
            TimeZoneInfo egyptZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");

            model.fromDate = TimeZoneInfo.ConvertTimeFromUtc(model.fromDate, egyptZone).Date;
            model.toDate = TimeZoneInfo.ConvertTimeFromUtc(model.toDate, egyptZone).Date.AddDays(1).AddSeconds(-1);

            var orders = _orderService.GetList(x => x.CreateOn >= model.fromDate && x.CreateOn <= model.toDate).ToList();

            using (var workbook = new XLWorkbook())
            {
                var orderTable = OrdersExport(orders);
                AddWorksheet(workbook, "الطلبات", orderTable);

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;

                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "الطلبات.xlsx");
                }
            }
        }
        #endregion

        #region ExportWallets
        [HttpGet]
        public ActionResult ExportWallets()
        {
            TimeZoneInfo egyptZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            DateTime nowInEgypt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptZone);

            var model = new ReportVM
            {
                fromDate = nowInEgypt.Date,
                toDate = nowInEgypt.Date.AddDays(1).AddSeconds(-1)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> ExportWallets(ReportVM model)
        {
            TimeZoneInfo egyptZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            model.fromDate = TimeZoneInfo.ConvertTimeFromUtc(model.fromDate, egyptZone);
            model.toDate = TimeZoneInfo.ConvertTimeFromUtc(model.toDate, egyptZone);

            using (var workbook = new XLWorkbook())
            {
                var wallets = _walletService.GetWalletOrdersList(x => x.CreateOn >= model.fromDate && x.CreateOn <= model.toDate).ToList();
                wallets = wallets.Where(x => x.CompletedOn >= model.fromDate && x.CompletedOn <= model.toDate).ToList();
                var walletTable = await CompletedOrdersExport(wallets);
                AddWorksheet(workbook, "طلبات التسويات", walletTable);

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;

                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "الطلبات_التي_تم_تسويتها.xlsx");
                }
            }
        }
        #endregion

        #region ExportUnsettledOrders
        [HttpGet]
        public ActionResult ExportUnsettledOrders()
        {
            TimeZoneInfo egyptZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            DateTime nowInEgypt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptZone);

            var model = new ReportVM
            {
                fromDate = nowInEgypt.Date,
                toDate = nowInEgypt.Date.AddDays(1).AddSeconds(-1)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> ExportUnsettledOrders(ReportVM model)
        {
            TimeZoneInfo egyptZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            model.fromDate = TimeZoneInfo.ConvertTimeFromUtc(model.fromDate, egyptZone);
            model.toDate = TimeZoneInfo.ConvertTimeFromUtc(model.toDate, egyptZone);

            using (var workbook = new XLWorkbook())
            {
                var unsettledOrders = _orderService.GetList(x => x.OrderCompleted == OrderCompleted.NOK &&
                                                                x.Status != OrderStatus.Completed &&
                                                                x.Status != OrderStatus.Returned &&
                                                                x.Status != OrderStatus.PartialReturned &&
                                                                x.Finished &&
                                                                x.CreateOn >= model.fromDate && x.CreateOn <= model.toDate).ToList();
                var unsettledTable = await FinishedOrdersExport(unsettledOrders);
                string sheetName = "طلبات تم تقفيلها ولم يتم تسويتها";
                sheetName = sheetName.Length > 31 ? sheetName.Substring(0, 31) : sheetName;
                AddWorksheet(workbook, sheetName, unsettledTable);

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;

                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "الطلبات_تم_تقفيلها_ولم_يتم_تسويتها.xlsx");
                }
            }
        }
        #endregion

        private void AddWorksheet(XLWorkbook workbook, string sheetName, DataTable data)
        {
            var worksheet = workbook.Worksheets.Add(sheetName);
            worksheet.Cell(1, 1).InsertTable(data);
        }

        public DataTable OrdersExport(List<Order> orders)
        {
            DataTable dt = new DataTable("Report");
            dt.Columns.AddRange(new DataColumn[] {
                new DataColumn("الحاله"), new DataColumn("التسويه"), new DataColumn("رقم الطلب"), new DataColumn("التاريخ"),
                new DataColumn("الراسل"), new DataColumn("رقم تليفون الراسل"), new DataColumn("المرسل إليه"), new DataColumn("رقم تليفون المرسل إليه"),
                new DataColumn("العنوان"), new DataColumn("المطلوب دفعه"), new DataColumn("تم دفعه"), new DataColumn("المندوب"),
                new DataColumn("الملاحظات"), new DataColumn("ملاحظات المندوب")
            });

            foreach (var item in orders)
            {
                string status = GetStatus(item.Status);
                string complete = item.OrderCompleted == OrderCompleted.NOK ? "لم يتم تسويتها" : "تم تسويتها";
                string driverNotes = item.OrderNotes.Any() ? item.OrderNotes.OrderBy(x => x.Id).Last().Content : "";
                string deliveryName = item.Delivery?.Name ?? "";
                string formattedDateTime = TimeZoneInfo.ConvertTimeFromUtc(item.CreateOn, TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time")).ToString("dd MMM, yyyy - hh:mm tt");

                dt.Rows.Add(status, complete, item.Code, formattedDateTime, item.Client.Name, item.Client.PhoneNumber, item.ClientName, item.ClientPhone,
                            item.AddressCity + " - " + item.Address, item.TotalCost, item.ArrivedCost, deliveryName, item.Notes, driverNotes);
            }
            return dt;
        }

        public async Task<DataTable> FinishedOrdersExport(List<Order> orders)
        {
            DataTable dt = new DataTable("Report");
            dt.Columns.AddRange(new DataColumn[] {
               new DataColumn("كود الطلب"), new DataColumn("الحاله"), new DataColumn("رقم عملية التقفيله"), new DataColumn("تاريخ التقفيله"), new DataColumn("قام بالتقفيله"),
                new DataColumn("الراسل"), new DataColumn("رقم تليفون الراسل"), new DataColumn("المرسل إليه"), new DataColumn("رقم تليفون المرسل إليه"),
                new DataColumn("العنوان"), new DataColumn("المطلوب دفعه"), new DataColumn("تم دفعه"), new DataColumn("المندوب"),
                new DataColumn("الملاحظات"), new DataColumn("ملاحظات المندوب")
            });

            foreach (var item in orders)
            {
                string status = GetStatus(item.Status);
                string driverNotes = item.OrderNotes != null ? item.OrderNotes.Any() ? item.OrderNotes.OrderBy(x => x.Id).Last().Content : "" : "";
                string deliveryName = item.Delivery?.Name ?? "";
                var Data = await _wallet.GetObja(x => x.Id == item.WalletId, "User");
                string formattedFinishedDateTime = Data != null ? TimeZoneInfo.ConvertTimeFromUtc(Data.CreateOn, TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time")).ToString("dd MMM, yyyy - hh:mm tt") : "";
                string FinishedName = Data?.User?.Name ?? "";

                dt.Rows.Add(item.Code, status, item.WalletId, formattedFinishedDateTime, FinishedName, item.Client.Name, item.Client.PhoneNumber, item.ClientName, item.ClientPhone,
                            item.AddressCity + " - " + item.Address, item.TotalCost, item.ArrivedCost, deliveryName, item.Notes, driverNotes);
            }
            return dt;
        }

        public async Task<DataTable> CompletedOrdersExport(List<Order> orders)
        {
            DataTable dt = new DataTable("Report");
            dt.Columns.AddRange(new DataColumn[] {
               new DataColumn("كود الطلب"), new DataColumn("الحاله"), new DataColumn("التسويه"), new DataColumn("رقم عملية التسوية"), new DataColumn("تاريخ التسوية"), new DataColumn("قام بالتسوية"),
                new DataColumn("الراسل"), new DataColumn("رقم تليفون الراسل"), new DataColumn("المرسل إليه"), new DataColumn("رقم تليفون المرسل إليه"),
                new DataColumn("العنوان"), new DataColumn("تم تسديده"), new DataColumn("نسبة الراسل"), new DataColumn("المندوب"),
                new DataColumn("الملاحظات"), new DataColumn("ملاحظات المندوب")
            });

            foreach (var item in orders)
            {
                string status = GetStatus(item.Status);
                string complete = item.OrderCompleted == OrderCompleted.NOK ? "لم يتم تسويتها" : "تم تسويتها";
                string driverNotes = item.OrderNotes != null ? item.OrderNotes.Any() ? item.OrderNotes.OrderBy(x => x.Id).Last().Content : "" : "";
                string deliveryName = item.Delivery?.Name ?? "";
                string formattedCompletedDateTime = item.CompletedOn.HasValue ? TimeZoneInfo.ConvertTimeFromUtc(item.CompletedOn.Value, TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time")).ToString("dd MMM, yyyy - hh:mm tt") : "";
                string CompletedName = "";
                var Data = await _wallet.GetObja(x => x.Id == item.CompletedId, "Complete_User");
                if (Data != null)
                    CompletedName = Data.Complete_User?.Name ?? "";

                dt.Rows.Add(item.Code, status, complete, item.CompletedId, formattedCompletedDateTime, CompletedName, item.Client.Name, item.Client.PhoneNumber, item.ClientName, item.ClientPhone,
                            item.AddressCity + " - " + item.Address, item.ArrivedCost, item.ClientCost, deliveryName, item.Notes, driverNotes);
            }
            return dt;
        }

        public string GetStatus(OrderStatus Status)
        {
            if (Status == OrderStatus.Placed) return "جديد";
            if (Status == OrderStatus.PartialReturned) return "مرتجع جزئي";
            if (Status == OrderStatus.Returned) return "مرتجع كامل";
            if (Status == OrderStatus.PartialDelivered) return "تم التوصيل جزئي";
            if (Status == OrderStatus.Assigned) return "جارى التوصيل";
            if (Status == OrderStatus.Delivered) return "تم التوصيل";
            if (Status == OrderStatus.Rejected) return "مرفوض";
            if (Status == OrderStatus.Waiting) return "مؤجل";
            if (Status == OrderStatus.Completed) return "تم تسويته";
            return "";
        }
    }
}
