using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using PostexS.Helper;
using PostexS.Interfaces;
using PostexS.Models.Data;
using PostexS.Models.Domain;
using PostexS.Models.Dtos;
using PostexS.Models.Enums;
using PostexS.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static PostexS.Helper.ExportToExcel;
using ZXing;
using ZXing.Common;
using System.Drawing.Imaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Bibliography;
using System.Drawing.Printing;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Transactions;
using System.ComponentModel;
using System.Web;
using PostexS.Models.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using System.Data;

namespace PostexS.Controllers
{
    [Authorize(Roles = "Admin,HighAdmin,Accountant,Client,Driver,TrustAdmin,TrackingAdmin")]
    public class OrdersController : Controller
    {
        private readonly IGeneric<ApplicationUser> _users;
        private readonly IGeneric<Order> _orders;
        private readonly IGeneric<OrderOperationHistory> _Histories;
        private readonly ICRUD<OrderOperationHistory> _CRUDHistory;
        private readonly IOrderService _orderService;
        private readonly IGeneric<OrderNotes> _notes;
        private readonly IGeneric<Branch> _branch;
        private readonly ICRUD<Order> _CRUD;
        private readonly IGeneric<OrderTransferrHistory> _TransferHistories;
        private readonly IGeneric<Wallet> _wallet;
        private readonly UserManager<ApplicationUser> _userManger;
        private readonly IGeneric<OrderNotes> _orderNotes;
        private readonly List<Order> order1 = new List<Order>();
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IGeneric<Notification> _notification;
        private IGeneric<DeviceTokens> _pushNotification;
        private readonly IWapilotService _wapilotService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IGeneric<CourierOrderSheet> _courierOrderSheet;
        private readonly IGeneric<CourierOrderSheetItem> _courierOrderSheetItem;
        private readonly FirebaseMessagingService _firebaseService;
        private static readonly HashSet<string> SenderTransferAuthorizedEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Admin@Tasahel.com",
            "elyaskee@Tasahel.com"
        };
        private const string SenderTransferAuthorizedUserId = "9897454b-add0-45ef-ad3b-53027814ede";
        public OrdersController(IGeneric<ApplicationUser> users, IGeneric<Order> orders, IGeneric<OrderOperationHistory> histories
            , ICRUD<Order> CRUD, ICRUD<OrderOperationHistory> CRUDhistory, IGeneric<OrderNotes> notes, IGeneric<Branch> branch,
            IOrderService orderService, IGeneric<OrderTransferrHistory> TransferHistories, IGeneric<DeviceTokens> pushNotification, IGeneric<Notification> notification, IWebHostEnvironment webHostEnvironment, UserManager<ApplicationUser> userManger, IGeneric<OrderNotes> OrderNotes, IGeneric<Wallet> wallet, IWapilotService wapilotService, IHttpClientFactory httpClientFactory, IServiceScopeFactory serviceScopeFactory,
            IGeneric<CourierOrderSheet> courierOrderSheet, IGeneric<CourierOrderSheetItem> courierOrderSheetItem, FirebaseMessagingService firebaseService)
        {
            _orderService = orderService;
            _users = users;
            _orders = orders;
            _Histories = histories;
            _CRUDHistory = CRUDhistory;
            _notes = notes;
            _CRUD = CRUD;
            _branch = branch;
            _TransferHistories = TransferHistories;
            _wallet = wallet;
            _userManger = userManger;
            _webHostEnvironment = webHostEnvironment;
            _orderNotes = OrderNotes;

            _notification = notification;
            _pushNotification = pushNotification;
            _wapilotService = wapilotService;
            _httpClientFactory = httpClientFactory;
            _serviceScopeFactory = serviceScopeFactory;
            _courierOrderSheet = courierOrderSheet;
            _courierOrderSheetItem = courierOrderSheetItem;
            _firebaseService = firebaseService;
        }

        [Authorize(Roles = "Admin,HighAdmin,Accountant,Client,TrustAdmin,TrackingAdmin")]
        public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 50,
            string q = "all", long BranchId = -1)
        {
            //1  القاهره
            //2  المنصورة
            //3  طنطا
            //4  اسكندريه
            //5  بورسعيد
            //6  دمياط
            //7  التجمع

            //var ord = _orderService.GetList(x => x.Code == "461530").First();
            //if (ord.Client.BranchId == 1 && ord.BranchId == 2 && ord.PreviousBranchId == null && ord.TransferredConfirmed == false)
            //{
            //    ord.PreviousBranchId = ord.Client.BranchId;
            //    await _orders.Update(ord);
            //    await _CRUD.Update(ord.Id);
            //}




            //var DATE = DateTime.UtcNow.AddMonths(-1);

            //var orders = _orderService.GetList(x => x.CreateOn > DATE && x.Client.BranchId != x.BranchId && x.PreviousBranchId == null &&
            //(x.Status == OrderStatus.Returned_And_Paid_DeliveryCost || x.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender) && x.ReturnedOrderCompleted == OrderCompleted.NOK).ToList();


            //foreach (var order in orders)
            //{
            //    order.PreviousBranchId = order.Client.BranchId;
            //    await _orders.Update(order);
            //    await _CRUD.Update(order.Id);
            //}


            //Expression<Func<Wallet, bool>> filter = null;
            //DateTime specificDate = DateTime.Parse("2023-12-31 00:00:57.9029664");
            //filter = x => (/*x.TransactionType == TransactionType.OrderReturnedComplete ||*/ x.TransactionType == TransactionType.OrderComplete && x.CreateOn >= specificDate && x.Complete_UserId == null);
            //Func<IQueryable<Wallet>, IOrderedQueryable<Wallet>> orderBy = o => o.OrderByDescending(c => c.Id);

            //var wallets = _wallet.GetAllAsIQueryable(filter, orderBy, "ActualUser,Orders").ToList();
            //foreach (var item in wallets)
            //{
            //    var firstOrder = _orders.GetAllAsIQueryable(x => x.CompletedId == item.Id && x.OrderOperationHistoryId != null, null, null).FirstOrDefault();
            //    if (firstOrder != null)
            //    {
            //        var OperationHistoryId = firstOrder.OrderOperationHistoryId;
            //        if (OperationHistoryId != null)
            //        {
            //            var history = await _Histories.GetObj(x => x.Id == OperationHistoryId);
            //            if (history != null)
            //            {
            //                if (history.Complete_UserId != null)
            //                {
            //                    var AccountantID = history.Complete_UserId;
            //                    item.Complete_UserId = AccountantID;
            //                    await _wallet.Update(item);
            //                }
            //            }
            //        }
            //    }

            //}
            // === كود صيانة البيانات القديمة — تم تعطيله لأنه كان يشتغل مع كل فتح صفحة ===
            // === لو محتاج تشغله تاني، شغله مرة واحدة من endpoint منفصل ===
            // var orders = _orderService.GetList(x => x.CreateOn > new DateTime(2024, 10, 1, 0, 0, 0, 0) && x.Client.BranchId != x.BranchId && x.PreviousBranchId == null && x.TransferredConfirmed == false && !x.IsDeleted).ToList();
            // foreach (var order in orders) { order.PreviousBranchId = order.Client.BranchId; await _orders.Update(order); await _CRUD.Update(order.Id); }
            // var ordersWithoutCode = _orderService.GetList(x => x.Code == null && x.Status != OrderStatus.PartialReturned && !x.IsDeleted).ToList();
            // foreach (var order in ordersWithoutCode) { order.Code = "Tas" + order.Id.ToString(); order.BarcodeImage = getBarcode(order.Code); await _orders.Update(order); await _CRUD.Update(order.Id); }
            // var ordersWithoutHistory = _orderService.GetList(x => x.OrderOperationHistoryId == null && x.CreateOn > new DateTime(2023, 12, 28, 0, 0, 0, 0)).ToList();
            // foreach (var order in ordersWithoutHistory.OrderByDescending(x => x.CreateOn)) { ... }
            // var ordersWithoutBarCode = _orderService.GetList(x => x.BarcodeImage == null && !x.Code.StartsWith("R") && !x.IsDeleted && x.Status != OrderStatus.PartialReturned && x.Status != OrderStatus.Delivered && x.Status != OrderStatus.Rejected && x.CreateOn > new DateTime(2023, 12, 30, 0, 0, 0, 0)).ToList();
            // foreach (var order in ordersWithoutBarCode.OrderByDescending(x => x.CreateOn).ToList()) { order.BarcodeImage = getBarcode(order.Code); await _orders.Update(order); await _CRUD.Update(order.Id); }

            //////////
            if (User.IsInRole("Client"))
            {
                ViewBag.ClientId = _userManger.GetUserId(User);
            }
            ViewBag.IsAdmin = false;
            if (User.IsInRole("Admin"))
            {
                ViewBag.IsAdmin = true;
            }
            if (User.IsInRole("HighAdmin") || User.IsInRole("Accountant") || User.IsInRole("TrackingAdmin"))
            {
                var user = await _users.GetObj(x => x.Id == _userManger.GetUserId(User));
                BranchId = user.BranchId;
            }
            ViewBag.Drivers = _users.Get(x => x.UserType == UserType.Client && (x.BranchId == BranchId || BranchId == -1)).ToList();
            ViewBag.Branchs = _branch.Get(x => !x.IsDeleted).ToList();
            ViewBag.BranchId = BranchId;
            ViewBag.q = q;

            return View(await GetPagedListItems("", pageNumber, pageSize, q, BranchId));
        }

        [Authorize(Roles = "Admin,HighAdmin,Accountant,Client,TrustAdmin,TrackingAdmin")]
        public async Task<IActionResult> CreateOrderWebview(string UserId)
        {
            if (!await _users.IsExist(x => x.Id == UserId && x.UserType == UserType.Client))
                return BadRequest("هذا الراسل غير موجود");
            var user = _users.Get(x => x.Id == UserId).First();
            ViewBag.ClientId = UserId;
            ViewBag.Note = user.OrdersGeneralNote != null ? user.OrdersGeneralNote : "";
            ViewBag.Branchs = _branch.Get(x => !x.IsDeleted).ToList();
            return View();
        }
        public async Task<string> ClientNotes(string id)
        {
            if (!await _users.IsExist(x => x.Id == id))
            {
                return "";
            }
            var user = _users.Get(x => x.Id == id).First();
            return user.OrdersGeneralNote != null ? user.OrdersGeneralNote : "";
        }
        public async Task<IActionResult> Copy(long id)
        {
            if (!await _orders.IsExist(x => x.Id == id && !x.IsDeleted))
            {
                return NotFound("هذا الطلب غير موجود او محذوف");
            }

            return PartialView(_orderService.Get(x => x.Id == id));
        }
        public async Task<IActionResult> history(long id)
        {
            if (!await _orders.IsExist(x => x.Id == id))
            {
                return NotFound("هذا الطلب غير موجود او محذوف");
            }
            var transferrHistories = await GetTransferHistory(id);
            ViewBag.transferrHistories = transferrHistories;
            return PartialView(_orderService.GetOrderHistory(x => x.OrderId == id));
        }
        private async Task<List<TransferrHistory>> GetTransferHistory(long OrderId)
        {
            List<TransferrHistory> transferrHistories = new List<TransferrHistory>();

            var transfers = _TransferHistories.Get(x => x.OrderId == OrderId).OrderBy(a => a.Id).ToList();

            ApplicationUser Transfer_User;
            ApplicationUser AcceptTransfer_User;
            Branch FromBranch;
            Branch ToBranch;
            foreach (var transfer in transfers)
            {
                AcceptTransfer_User = new ApplicationUser();
                FromBranch = new Branch();
                ToBranch = new Branch();

                Transfer_User = await _users.GetObj(x => x.Id == transfer.Transfer_UserId);
                if (transfer.AcceptTransfer_UserId != null)
                    AcceptTransfer_User = await _users.GetObj(x => x.Id == transfer.AcceptTransfer_UserId);
                FromBranch = _branch.Get(x => x.Id == transfer.FromBranchId).First();
                ToBranch = _branch.Get(x => x.Id == transfer.ToBranchId).First();

                transferrHistories.Add(new TransferrHistory()
                {
                    Transfer_UserName = Transfer_User.Name,
                    TransferDate = transfer.CreateOn,
                    AcceptTransfer_UserName = (transfer.AcceptTransfer_UserId != null && AcceptTransfer_User != null) ? AcceptTransfer_User.Name : null,
                    AcceptTransferDate = transfer.AcceptTransferDate,
                    TransferCancel = transfer.TransferCancel,
                    FromBranchName = FromBranch.Name,
                    ToBranchName = ToBranch.Name,
                });
            }
            return transferrHistories;
        }
        [AllowAnonymous]
        public IActionResult UsersWebView(string id, string type, DateTime? FilterTime, DateTime? FilterTimeTo,
            string state = "", string taswya = "")
        {
            ViewBag.UserId = id;
            ViewBag.FilterTime = FilterTime;
            ViewBag.FilterType = FilterTimeTo;
            ViewBag.typ = type;
            if (state == "" && taswya == "")
            {
                if (type == "c")
                {
                    ViewBag.type = 0;
                    ViewBag.typ = "c";
                    return View(_orderService.GetList(x => x.ClientId == id && !x.IsDeleted &&
                                                           (!FilterTime.HasValue || DateTime.Compare(FilterTime.Value.ToUniversalTime(),
                                                               x.CreateOn) <= 0) &&
                                                           (!FilterTimeTo.HasValue || DateTime.Compare(FilterTimeTo.Value.ToUniversalTime(),
                                                               x.CreateOn) >= 0)).ToList());
                }
                else
                {
                    ViewBag.typ = "d";
                    ViewBag.type = 1;
                    return View(_orderService.GetList(x => x.DeliveryId == id && !x.IsDeleted &&
                                                           (!FilterTime.HasValue || DateTime.Compare(FilterTime.Value.ToUniversalTime(),
                                                               x.CreateOn) <= 0) &&
                                                           (!FilterTimeTo.HasValue || DateTime.Compare(FilterTimeTo.Value.ToUniversalTime(),
                                                               x.CreateOn) >= 0)
                    ).ToList());
                }
            }
            else
            {
                if (type == "c")

                {
                    ViewBag.UserId = id;
                    ViewBag.type = 0;
                    ViewBag.typ = "c";
                    if (taswya == "completed")
                    {
                        if (state == "gdeda")
                        {
                            return View(_orderService.GetList(x =>
                                x.ClientId == id && !x.IsDeleted && x.Status == OrderStatus.Placed &&
                                x.OrderCompleted == OrderCompleted.OK).ToList());
                        }

                        if (state == "garya")
                        {
                            return View(_orderService.GetList(x =>
                                x.ClientId == id && !x.IsDeleted && x.Status == OrderStatus.Assigned &&
                                x.OrderCompleted == OrderCompleted.OK).ToList());
                        }

                        if (state == "wasalet")
                        {
                            return View(_orderService.GetList(x =>
                                x.ClientId == id && !x.IsDeleted && x.Status == OrderStatus.Delivered &&
                                x.OrderCompleted == OrderCompleted.OK).ToList());
                        }

                        if (state == "mo2gl")
                        {
                            return View(_orderService.GetList(x =>
                                x.ClientId == id && !x.IsDeleted && x.Status == OrderStatus.Waiting &&
                                x.OrderCompleted == OrderCompleted.OK).ToList());
                        }

                        if (state == "closed")
                        {
                            return View(_orderService.GetList(x =>
                                x.ClientId == id && !x.IsDeleted && x.Status != OrderStatus.Completed && x.Finished &&
                                x.OrderCompleted == OrderCompleted.OK).ToList());
                        }

                        if (state == "refused")
                        {
                            return View(_orderService.GetList(
                                x => x.ClientId == id && !x.IsDeleted && x.OrderCompleted == OrderCompleted.OK &&
                                     x.Status == OrderStatus.Rejected
                            ).ToList());
                        }

                        if (state == "deleted")
                        {
                            ViewBag.type = 0;
                            ViewBag.typ = "c";
                            return View(_orderService.GetList(
                                x => x.ClientId == id && x.IsDeleted && x.OrderCompleted == OrderCompleted.OK
                            ).ToList());
                        }

                        if (state == "mo3l2")
                        {
                            ViewBag.type = 0;
                            ViewBag.typ = "c";
                            return View(_orderService.GetList(
                                x => x.ClientId == id && !x.IsDeleted && x.Status == OrderStatus.Placed && x.Pending &&
                                     x.OrderCompleted == OrderCompleted.OK
                            ).ToList());
                        }
                    }
                    else if (taswya == "uncompleted")
                    {
                        if (state == "gdeda")
                        {
                            return View(_orderService.GetList(x =>
                                x.ClientId == id && !x.IsDeleted && x.Status == OrderStatus.Placed &&
                                x.OrderCompleted == OrderCompleted.NOK).ToList());
                        }

                        if (state == "garya")
                        {
                            return View(_orderService.GetList(x =>
                                x.ClientId == id && !x.IsDeleted && x.Status == OrderStatus.Assigned &&
                                x.OrderCompleted == OrderCompleted.NOK).ToList());
                        }

                        if (state == "wasalet")
                        {
                            return View(_orderService.GetList(x =>
                                x.ClientId == id && !x.IsDeleted && x.Status == OrderStatus.Delivered &&
                                x.OrderCompleted == OrderCompleted.NOK).ToList());
                        }

                        if (state == "mo2gl")
                        {
                            return View(_orderService.GetList(x =>
                                x.ClientId == id && !x.IsDeleted && x.Status == OrderStatus.Waiting &&
                                x.OrderCompleted == OrderCompleted.NOK).ToList());
                        }

                        if (state == "closed")
                        {
                            return View(_orderService.GetList(x =>
                                x.ClientId == id && !x.IsDeleted && x.Status != OrderStatus.Completed && x.Finished &&
                                x.OrderCompleted == OrderCompleted.NOK).ToList());
                        }

                        if (state == "refused")
                        {
                            return View(_orderService.GetList(
                                x => x.ClientId == id && !x.IsDeleted && x.OrderCompleted == OrderCompleted.NOK &&
                                     x.Status == OrderStatus.Rejected
                            ).ToList());
                        }

                        if (state == "deleted")
                        {
                            ViewBag.type = 0;
                            ViewBag.typ = "c";
                            return View(_orderService.GetList(
                                x => x.ClientId == id && x.IsDeleted && x.OrderCompleted == OrderCompleted.NOK
                            ).ToList());
                        }

                        if (state == "mo3l2")
                        {
                            ViewBag.type = 0;
                            ViewBag.typ = "c";
                            return View(_orderService.GetList(
                                x => x.ClientId == id && !x.IsDeleted && x.Status == OrderStatus.Placed && x.Pending &&
                                     x.OrderCompleted == OrderCompleted.NOK
                            ).ToList());
                        }
                    }
                }
                else
                {
                    ViewBag.UserId = id;
                    ViewBag.type = 0;
                    ViewBag.typ = "d";
                    if (taswya == "completed")
                    {
                        if (state == "gdeda")
                        {
                            return View(_orderService.GetList(x =>
                                x.DeliveryId == id && !x.IsDeleted && x.Status == OrderStatus.Placed &&
                                x.OrderCompleted == OrderCompleted.OK).ToList());
                        }

                        if (state == "garya")
                        {
                            return View(_orderService.GetList(x =>
                                x.DeliveryId == id && !x.IsDeleted && x.Status == OrderStatus.Assigned &&
                                x.OrderCompleted == OrderCompleted.OK).ToList());
                        }

                        if (state == "wasalet")
                        {
                            return View(_orderService.GetList(x =>
                                x.DeliveryId == id && !x.IsDeleted && x.Status == OrderStatus.Delivered &&
                                x.OrderCompleted == OrderCompleted.OK).ToList());
                        }

                        if (state == "mo2gl")
                        {
                            return View(_orderService.GetList(x =>
                                x.DeliveryId == id && !x.IsDeleted && x.Status == OrderStatus.Waiting &&
                                x.OrderCompleted == OrderCompleted.OK).ToList());
                        }

                        if (state == "closed")
                        {
                            return View(_orderService.GetList(x =>
                                x.DeliveryId == id && !x.IsDeleted && x.Status != OrderStatus.Completed && x.Finished &&
                                x.OrderCompleted == OrderCompleted.OK).ToList());
                        }

                        if (state == "refused")
                        {
                            return View(_orderService.GetList(
                                x => x.DeliveryId == id && !x.IsDeleted && x.OrderCompleted == OrderCompleted.OK &&
                                     x.Status == OrderStatus.Rejected
                            ).ToList());
                        }

                        if (state == "deleted")
                        {
                            ViewBag.type = 0;
                            ViewBag.typ = "c";
                            return View(_orderService.GetList(
                                x => x.DeliveryId == id && x.IsDeleted && x.OrderCompleted == OrderCompleted.OK
                            ).ToList());
                        }

                        if (state == "mo3l2")
                        {
                            ViewBag.type = 0;
                            ViewBag.typ = "c";
                            return View(_orderService.GetList(
                                x => x.DeliveryId == id && !x.IsDeleted && x.Status == OrderStatus.Placed &&
                                     x.Pending && x.OrderCompleted == OrderCompleted.OK
                            ).ToList());
                        }
                    }
                    else if (taswya == "uncompleted")
                    {
                        if (state == "gdeda")
                        {
                            return View(_orderService.GetList(x =>
                                x.DeliveryId == id && !x.IsDeleted && x.Status == OrderStatus.Placed &&
                                x.OrderCompleted == OrderCompleted.NOK).ToList());
                        }

                        if (state == "garya")
                        {
                            return View(_orderService.GetList(x =>
                                x.DeliveryId == id && !x.IsDeleted && x.Status == OrderStatus.Assigned &&
                                x.OrderCompleted == OrderCompleted.NOK).ToList());
                        }

                        if (state == "wasalet")
                        {
                            return View(_orderService.GetList(x =>
                                x.DeliveryId == id && !x.IsDeleted && x.Status == OrderStatus.Delivered &&
                                x.OrderCompleted == OrderCompleted.NOK).ToList());
                        }

                        if (state == "mo2gl")
                        {
                            return View(_orderService.GetList(x =>
                                x.DeliveryId == id && !x.IsDeleted && x.Status == OrderStatus.Waiting &&
                                x.OrderCompleted == OrderCompleted.NOK).ToList());
                        }

                        if (state == "closed")
                        {
                            return View(_orderService.GetList(x =>
                                x.DeliveryId == id && !x.IsDeleted && x.Status != OrderStatus.Completed && x.Finished &&
                                x.OrderCompleted == OrderCompleted.NOK).ToList());
                        }

                        if (state == "refused")
                        {
                            return View(_orderService.GetList(
                                x => x.DeliveryId == id && !x.IsDeleted && x.OrderCompleted == OrderCompleted.NOK &&
                                     x.Status == OrderStatus.Rejected
                            ).ToList());
                        }

                        if (state == "deleted")
                        {
                            ViewBag.type = 0;
                            ViewBag.typ = "c";
                            return View(_orderService.GetList(
                                x => x.DeliveryId == id && x.IsDeleted && x.OrderCompleted == OrderCompleted.NOK
                            ).ToList());
                        }

                        if (state == "mo3l2")
                        {
                            ViewBag.type = 0;
                            ViewBag.typ = "c";
                            return View(_orderService.GetList(
                                x => x.DeliveryId == id && !x.IsDeleted && x.Status == OrderStatus.Placed &&
                                     x.Pending && x.OrderCompleted == OrderCompleted.NOK
                            ).ToList());
                        }


                        if (state == "all")
                        {
                            ViewBag.type = 0;
                            ViewBag.typ = "c";
                            return View(_orderService.GetList(
                                x => x.DeliveryId == id && !x.IsDeleted &&
                                     x.Pending && x.OrderCompleted == OrderCompleted.NOK
                            ).ToList());
                        }
                    }
                }
            }

            return View();
        }
        //[AllowAnonymous]
        [Authorize]
        public IActionResult Users(string id, string type, DateTime? FilterTime, DateTime? FilterTimeTo,
    string state = "", string taswya = "", int page = 1, int pageSize = 100, string searchTerm = "")
        {
            int retryCount = 0;
            while (retryCount < 3) // محاولة 3 مرات
            {
                try
                {
                    if (User.IsInRole("Client"))
                    {
                        id = _userManger.GetUserId(User);
                    }

                    ViewBag.UserId = id;
                    ViewBag.FilterTime = FilterTime;
                    ViewBag.FilterTimeTo = FilterTimeTo;
                    ViewBag.typ = type;

                    IQueryable<Order> baseQuery = null;
                    IQueryable<Order> query = null;

                    if (string.IsNullOrWhiteSpace(state) && string.IsNullOrWhiteSpace(taswya))
                    {
                        if (type == "c")
                        {
                            ViewBag.type = 0;
                            ViewBag.typ = "c";
                            Expression<Func<Order, bool>> filter = x => x.ClientId == id && !x.IsDeleted &&
                                   (!FilterTime.HasValue || x.CreateOn >= FilterTime.Value.ToUniversalTime()) &&
                                   (!FilterTimeTo.HasValue || x.CreateOn <= FilterTimeTo.Value.ToUniversalTime());
                            baseQuery = _orderService.GetBaseQuery(filter).OrderByDescending(x => x.CreateOn);
                            query = _orderService.GetQueryableListLight(filter).OrderByDescending(x => x.CreateOn);
                        }
                        else
                        {
                            ViewBag.typ = "d";
                            ViewBag.type = 1;
                            Expression<Func<Order, bool>> filter = x => x.DeliveryId == id && !x.IsDeleted &&
                                   (!FilterTime.HasValue || x.CreateOn >= FilterTime.Value.ToUniversalTime()) &&
                                   (!FilterTimeTo.HasValue || x.CreateOn <= FilterTimeTo.Value.ToUniversalTime());
                            baseQuery = _orderService.GetBaseQuery(filter).OrderByDescending(x => x.CreateOn);
                            query = _orderService.GetQueryableListLight(filter).OrderByDescending(x => x.CreateOn);
                        }
                    }
                    else
                    {
                        if (type == "c")
                        {
                            ViewBag.UserId = id;
                            ViewBag.type = 0;
                            ViewBag.typ = "c";

                            if (taswya == "completed")
                            {
                                query = ApplyClientStateFilter(id, state, OrderCompleted.OK);
                            }
                            else if (taswya == "uncompleted")
                            {
                                query = ApplyClientStateFilter(id, state, OrderCompleted.NOK);
                            }
                            else if (taswya == "all")
                            {
                                query = ApplyClientStateFilter(id, state, null);
                            }
                        }
                        else
                        {
                            ViewBag.UserId = id;
                            ViewBag.type = 0;
                            ViewBag.typ = "d";

                            if (taswya == "completed")
                            {
                                query = ApplyDeliveryStateFilter(id, state, OrderCompleted.OK);
                            }
                            else if (taswya == "uncompleted")
                            {
                                query = ApplyDeliveryStateFilter(id, state, OrderCompleted.NOK);
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        Expression<Func<Order, bool>> searchFilter = x =>
                            x.Code.Contains(searchTerm) ||
                            x.ClientName.Contains(searchTerm) ||
                            x.Address.Contains(searchTerm) ||
                            x.Notes.Contains(searchTerm);

                        query = query?.Where(searchFilter);
                        if (baseQuery != null)
                            baseQuery = baseQuery.Where(searchFilter);
                    }

                    var countQuery = baseQuery ?? query;
                    var totalItems = countQuery?.Count() ?? 0;
                    var totalPages = pageSize > 0 ? (int)Math.Ceiling(totalItems / (double)pageSize) : 1;

                    if (pageSize <= 0) pageSize = Math.Min(totalItems, 500);

                    var pagedItems = query?.AsNoTracking()
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToList() ?? new List<Order>();

                    ViewBag.CurrentPage = page;
                    ViewBag.PageSize = pageSize;
                    ViewBag.TotalPages = totalPages;
                    ViewBag.TotalItems = totalItems;

                    return View(pagedItems);
                }
                catch (SqlException ex) when (ex.Number == 1205) // Deadlock
                {
                    retryCount++;
                    if (retryCount == 3)
                    {
                        TempData["Error"] = "تعذر إكمال العملية بسبب مشكلة مؤقتة، يرجى المحاولة لاحقاً";
                        return RedirectToAction("Index");
                    }
                    Thread.Sleep(100 * retryCount); // انتظر قبل إعادة المحاولة
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "حدث خطأ غير متوقع";
                    return RedirectToAction("Index");
                }
            }
            return RedirectToAction("Index");
        }

        [Authorize]
        public IActionResult UserOrdersNew(string id, string type = "c")
        {
            if (User.IsInRole("Client"))
            {
                id = _userManger.GetUserId(User);
            }
            ViewBag.UserId = id;
            ViewBag.typ = type;
            return View();
        }

        [Authorize]
        [HttpGet]
        public IActionResult UserOrdersApi(string id, string type = "c", string state = "", string taswya = "",
            DateTime? filterTime = null, DateTime? filterTimeTo = null,
            string searchTerm = "", int page = 1, int pageSize = 50, bool includeStats = false)
        {
            if (User.IsInRole("Client"))
            {
                id = _userManger.GetUserId(User);
            }
            if (string.IsNullOrEmpty(id))
                return Json(new { error = "معرف المستخدم مطلوب" });

            if (pageSize <= 0) pageSize = 50;
            if (pageSize > 1000) pageSize = 1000;
            if (page < 1) page = 1;

            IQueryable<Order> baseQuery;

            if (!string.IsNullOrWhiteSpace(state) || !string.IsNullOrWhiteSpace(taswya))
            {
                if (type == "c")
                {
                    OrderCompleted? comp = null;
                    if (taswya == "completed") comp = OrderCompleted.OK;
                    else if (taswya == "uncompleted") comp = OrderCompleted.NOK;
                    baseQuery = ApplyClientStateFilterBase(id, state, comp);
                }
                else
                {
                    OrderCompleted? comp = null;
                    if (taswya == "completed") comp = OrderCompleted.OK;
                    else if (taswya == "uncompleted") comp = OrderCompleted.NOK;
                    baseQuery = ApplyDeliveryStateFilterBase(id, state, comp);
                }
            }
            else
            {
                Expression<Func<Order, bool>> filter;
                if (type == "c")
                    filter = x => x.ClientId == id && !x.IsDeleted &&
                        (!filterTime.HasValue || x.CreateOn >= filterTime.Value.ToUniversalTime()) &&
                        (!filterTimeTo.HasValue || x.CreateOn <= filterTimeTo.Value.ToUniversalTime());
                else
                    filter = x => x.DeliveryId == id && !x.IsDeleted &&
                        (!filterTime.HasValue || x.CreateOn >= filterTime.Value.ToUniversalTime()) &&
                        (!filterTimeTo.HasValue || x.CreateOn <= filterTimeTo.Value.ToUniversalTime());

                baseQuery = _orderService.GetBaseQuery(filter).OrderByDescending(x => x.CreateOn);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                baseQuery = baseQuery.Where(x =>
                    x.Code.Contains(searchTerm) ||
                    x.ClientName.Contains(searchTerm) ||
                    x.Address.Contains(searchTerm) ||
                    x.Notes.Contains(searchTerm));
            }

            var totalItems = baseQuery.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var items = baseQuery.AsNoTracking()
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new UserOrderItemVM
                {
                    Id = o.Id,
                    Code = o.Code,
                    CreateOn = o.CreateOn,
                    ClientName = o.ClientName,
                    Address = o.Address,
                    Notes = o.Notes,
                    TotalCost = o.TotalCost,
                    ArrivedCost = o.ArrivedCost,
                    Status = o.Status,
                    OrderCompleted = o.OrderCompleted,
                    CompletedOn = o.CompletedOn,
                    SenderName = o.Client.Name,
                    DeliveryName = o.Delivery != null ? o.Delivery.Name : null,
                    DeliveryPhone = o.Delivery != null ? o.Delivery.PhoneNumber : null,
                    LastNote = o.OrderNotes.OrderByDescending(n => n.Id).Select(n => n.Content).FirstOrDefault()
                })
                .ToList();

            var filteredStats = new UserOrdersStatsVM
            {
                Total = totalItems,
                Placed = baseQuery.Count(x => x.Status == OrderStatus.Placed),
                Assigned = baseQuery.Count(x => x.Status == OrderStatus.Assigned),
                Delivered = baseQuery.Count(x => x.Status == OrderStatus.Delivered || x.Status == OrderStatus.Delivered_With_Edit_Price || x.Status == OrderStatus.PartialDelivered),
                Returned = baseQuery.Count(x => x.Status == OrderStatus.Returned || x.Status == OrderStatus.PartialReturned || x.Status == OrderStatus.Returned_And_Paid_DeliveryCost || x.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender),
                Rejected = baseQuery.Count(x => x.Status == OrderStatus.Rejected),
                Waiting = baseQuery.Count(x => x.Status == OrderStatus.Waiting),
                Completed = baseQuery.Count(x => x.OrderCompleted == OrderCompleted.OK),
                TotalArrivedCost = baseQuery.Any() ? baseQuery.Sum(x => x.ArrivedCost) : 0
            };

            var response = new UserOrdersApiResponse
            {
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = page,
                PageSize = pageSize,
                Items = items,
                FilteredStats = filteredStats
            };

            if (includeStats)
            {
                IQueryable<Order> globalQuery;
                if (type == "c")
                    globalQuery = _orderService.GetBaseQuery(x => x.ClientId == id && !x.IsDeleted);
                else
                    globalQuery = _orderService.GetBaseQuery(x => x.DeliveryId == id && !x.IsDeleted);

                response.GlobalStats = new UserOrdersStatsVM
                {
                    Total = globalQuery.Count(),
                    Placed = globalQuery.Count(x => x.Status == OrderStatus.Placed),
                    Assigned = globalQuery.Count(x => x.Status == OrderStatus.Assigned),
                    Delivered = globalQuery.Count(x => x.Status == OrderStatus.Delivered || x.Status == OrderStatus.Delivered_With_Edit_Price || x.Status == OrderStatus.PartialDelivered),
                    Returned = globalQuery.Count(x => x.Status == OrderStatus.Returned || x.Status == OrderStatus.PartialReturned || x.Status == OrderStatus.Returned_And_Paid_DeliveryCost || x.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender),
                    Rejected = globalQuery.Count(x => x.Status == OrderStatus.Rejected),
                    Waiting = globalQuery.Count(x => x.Status == OrderStatus.Waiting),
                    Completed = globalQuery.Count(x => x.OrderCompleted == OrderCompleted.OK),
                    TotalArrivedCost = globalQuery.Sum(x => x.ArrivedCost)
                };
            }

            return Json(response);
        }

        [Authorize]
        [HttpGet]
        public IActionResult UserOrdersAllIds(string id, string type = "c", string state = "", string taswya = "",
            DateTime? filterTime = null, DateTime? filterTimeTo = null, string searchTerm = "")
        {
            if (User.IsInRole("Client"))
                id = _userManger.GetUserId(User);

            if (string.IsNullOrEmpty(id))
                return Json(new { ids = new List<long>() });

            IQueryable<Order> baseQuery;

            if (!string.IsNullOrWhiteSpace(state) || !string.IsNullOrWhiteSpace(taswya))
            {
                OrderCompleted? comp = null;
                if (taswya == "completed") comp = OrderCompleted.OK;
                else if (taswya == "uncompleted") comp = OrderCompleted.NOK;

                baseQuery = type == "c"
                    ? ApplyClientStateFilterBase(id, state, comp)
                    : ApplyDeliveryStateFilterBase(id, state, comp);
            }
            else
            {
                Expression<Func<Order, bool>> filter;
                if (type == "c")
                    filter = x => x.ClientId == id && !x.IsDeleted &&
                        (!filterTime.HasValue || x.CreateOn >= filterTime.Value.ToUniversalTime()) &&
                        (!filterTimeTo.HasValue || x.CreateOn <= filterTimeTo.Value.ToUniversalTime());
                else
                    filter = x => x.DeliveryId == id && !x.IsDeleted &&
                        (!filterTime.HasValue || x.CreateOn >= filterTime.Value.ToUniversalTime()) &&
                        (!filterTimeTo.HasValue || x.CreateOn <= filterTimeTo.Value.ToUniversalTime());

                baseQuery = _orderService.GetBaseQuery(filter);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                baseQuery = baseQuery.Where(x =>
                    x.Code.Contains(searchTerm) ||
                    x.ClientName.Contains(searchTerm) ||
                    x.Address.Contains(searchTerm) ||
                    x.Notes.Contains(searchTerm));
            }

            var ids = baseQuery.AsNoTracking().Select(x => x.Id).ToList();
            return Json(new { ids = ids, count = ids.Count });
        }

        private IQueryable<Order> ApplyClientStateFilterBase(string id, string state, OrderCompleted? completedStatus)
        {
            IQueryable<Order> query = _orderService.GetBaseQuery(x => x.ClientId == id);
            if (completedStatus.HasValue)
                query = query.Where(x => x.OrderCompleted == completedStatus.Value);

            switch (state)
            {
                case "gdeda": return query.Where(x => !x.IsDeleted && x.Status == OrderStatus.Placed).OrderByDescending(x => x.CreateOn);
                case "garya": return query.Where(x => !x.IsDeleted && x.Status == OrderStatus.Assigned).OrderByDescending(x => x.CreateOn);
                case "wasalet": return query.Where(x => !x.IsDeleted && (x.Status == OrderStatus.Delivered || x.Status == OrderStatus.Delivered_With_Edit_Price || x.Status == OrderStatus.PartialDelivered)).OrderByDescending(x => x.CreateOn);
                case "wasaleteditprice": return query.Where(x => !x.IsDeleted && x.Status == OrderStatus.Delivered_With_Edit_Price).OrderByDescending(x => x.CreateOn);
                case "mo2gl": return query.Where(x => !x.IsDeleted && x.Status == OrderStatus.Waiting).OrderByDescending(x => x.CreateOn);
                case "closed": return query.Where(x => !x.IsDeleted && x.Status != OrderStatus.Completed && x.Finished).OrderByDescending(x => x.CreateOn);
                case "returned": return query.Where(x => !x.IsDeleted && (x.Status == OrderStatus.Returned || x.Status == OrderStatus.Returned_And_Paid_DeliveryCost || x.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender || x.Status == OrderStatus.PartialReturned)).OrderByDescending(x => x.CreateOn);
                case "refused": return query.Where(x => !x.IsDeleted && x.Status == OrderStatus.Rejected).OrderByDescending(x => x.CreateOn);
                case "deleted": return query.Where(x => x.IsDeleted).OrderByDescending(x => x.CreateOn);
                case "mo3l2": return query.Where(x => !x.IsDeleted && x.Status == OrderStatus.Placed && x.Pending).OrderByDescending(x => x.CreateOn);
                case "all": return query.Where(x => !x.IsDeleted).OrderByDescending(x => x.CreateOn);
                default: return query.OrderByDescending(x => x.CreateOn);
            }
        }

        private IQueryable<Order> ApplyDeliveryStateFilterBase(string id, string state, OrderCompleted? completedStatus)
        {
            IQueryable<Order> query = _orderService.GetBaseQuery(x => x.DeliveryId == id);
            if (completedStatus.HasValue)
                query = query.Where(x => x.OrderCompleted == completedStatus.Value);

            switch (state)
            {
                case "gdeda": return query.Where(x => !x.IsDeleted && x.Status == OrderStatus.Placed).OrderByDescending(x => x.CreateOn);
                case "garya": return query.Where(x => !x.IsDeleted && x.Status == OrderStatus.Assigned).OrderByDescending(x => x.CreateOn);
                case "wasalet": return query.Where(x => !x.IsDeleted && x.Status == OrderStatus.Delivered).OrderByDescending(x => x.CreateOn);
                case "mo2gl": return query.Where(x => !x.IsDeleted && x.Status == OrderStatus.Waiting).OrderByDescending(x => x.CreateOn);
                case "closed": return query.Where(x => !x.IsDeleted && x.Status != OrderStatus.Completed && x.Finished).OrderByDescending(x => x.CreateOn);
                case "returned": return query.Where(x => !x.IsDeleted && (x.Status == OrderStatus.Returned || x.Status == OrderStatus.PartialReturned)).OrderByDescending(x => x.CreateOn);
                case "refused": return query.Where(x => !x.IsDeleted && x.Status == OrderStatus.Rejected).OrderByDescending(x => x.CreateOn);
                case "deleted": return query.Where(x => x.IsDeleted).OrderByDescending(x => x.CreateOn);
                case "mo3l2": return query.Where(x => !x.IsDeleted && x.Status == OrderStatus.Placed && x.Pending).OrderByDescending(x => x.CreateOn);
                default: return query.OrderByDescending(x => x.CreateOn);
            }
        }

        private IQueryable<Order> ApplyClientStateFilter(string id, string state, OrderCompleted? completedStatus)
        {
            IQueryable<Order> query = _orderService.GetQueryableListLight(x => x.ClientId == id);

            if (completedStatus.HasValue)
            {
                query = query.Where(x => x.OrderCompleted == completedStatus.Value);
            }

            switch (state)
            {
                case "gdeda":
                    return query.Where(x => !x.IsDeleted && x.Status == OrderStatus.Placed)
                        .OrderByDescending(x => x.CreateOn);
                case "garya":
                    return query.Where(x => !x.IsDeleted && x.Status == OrderStatus.Assigned)
                        .OrderByDescending(x => x.CreateOn);
                case "wasalet":
                    return query.Where(x => !x.IsDeleted &&
                        (x.Status == OrderStatus.Delivered ||
                         x.Status == OrderStatus.Delivered_With_Edit_Price ||
                         x.Status == OrderStatus.PartialDelivered))
                        .OrderByDescending(x => x.CreateOn);
                case "mo2gl":
                    return query.Where(x => !x.IsDeleted && x.Status == OrderStatus.Waiting)
                        .OrderByDescending(x => x.CreateOn);
                case "closed":
                    return query.Where(x => !x.IsDeleted && x.Status != OrderStatus.Completed && x.Finished)
                        .OrderByDescending(x => x.CreateOn);
                case "returned":
                    return query.Where(x => !x.IsDeleted &&
                        (x.Status == OrderStatus.Returned ||
                         x.Status == OrderStatus.Returned_And_Paid_DeliveryCost ||
                         x.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender ||
                         x.Status == OrderStatus.PartialReturned))
                        .OrderByDescending(x => x.CreateOn);
                case "refused":
                    return query.Where(x => !x.IsDeleted && x.Status == OrderStatus.Rejected)
                        .OrderByDescending(x => x.CreateOn);
                case "deleted":
                    return query.Where(x => x.IsDeleted)
                        .OrderByDescending(x => x.CreateOn);
                case "mo3l2":
                    return query.Where(x => !x.IsDeleted && x.Status == OrderStatus.Placed && x.Pending)
                        .OrderByDescending(x => x.CreateOn);
                case "all":
                    return query.Where(x => !x.IsDeleted)
                        .OrderByDescending(x => x.CreateOn);
                default:
                    return query.OrderByDescending(x => x.CreateOn);
            }
        }

        private IQueryable<Order> ApplyDeliveryStateFilter(string id, string state, OrderCompleted? completedStatus)
        {
            IQueryable<Order> query = _orderService.GetQueryableListLight(x => x.DeliveryId == id);

            if (completedStatus.HasValue)
            {
                query = query.Where(x => x.OrderCompleted == completedStatus.Value);
            }

            switch (state)
            {
                case "gdeda":
                    return query.Where(x => !x.IsDeleted && x.Status == OrderStatus.Placed)
                        .OrderByDescending(x => x.CreateOn);
                case "garya":
                    return query.Where(x => !x.IsDeleted && x.Status == OrderStatus.Assigned)
                        .OrderByDescending(x => x.CreateOn);
                case "wasalet":
                    return query.Where(x => !x.IsDeleted && x.Status == OrderStatus.Delivered)
                        .OrderByDescending(x => x.CreateOn);
                case "mo2gl":
                    return query.Where(x => !x.IsDeleted && x.Status == OrderStatus.Waiting)
                        .OrderByDescending(x => x.CreateOn);
                case "closed":
                    return query.Where(x => !x.IsDeleted && x.Status != OrderStatus.Completed && x.Finished)
                        .OrderByDescending(x => x.CreateOn);
                case "returned":
                    return query.Where(x => !x.IsDeleted &&
                        (x.Status == OrderStatus.Returned || x.Status == OrderStatus.PartialReturned))
                        .OrderByDescending(x => x.CreateOn);
                case "refused":
                    return query.Where(x => !x.IsDeleted && x.Status == OrderStatus.Rejected)
                        .OrderByDescending(x => x.CreateOn);
                case "deleted":
                    return query.Where(x => x.IsDeleted)
                        .OrderByDescending(x => x.CreateOn);
                case "mo3l2":
                    return query.Where(x => !x.IsDeleted && x.Status == OrderStatus.Placed && x.Pending)
                        .OrderByDescending(x => x.CreateOn);
                default:
                    return query.OrderByDescending(x => x.CreateOn);
            }
        }

        #region LineExpress Orders
        [Authorize(Roles = "Admin,HighAdmin,Accountant,Client,Driver,TrustAdmin,TrackingAdmin")]
        public async Task<IActionResult> LineExpress(DateTime? fromDate = null, DateTime? toDate = null, int pageNumber = 1, int pageSize = 50)
        {
            // Set default dates (last 3 days)
            if (!fromDate.HasValue)
            {
                fromDate = DateTime.UtcNow.AddDays(-3).Date;
            }
            if (!toDate.HasValue)
            {
                toDate = DateTime.UtcNow.Date;
            }

            // Call API to get shipments
            var shipments = await GetShipmentsFromApi(fromDate.Value, toDate.Value, pageNumber, pageSize);

            ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
            ViewBag.PageSize = pageSize;
            ViewBag.PageNumber = pageNumber;

            // نحتفظ بعدد العناصر في الصفحة الحالية فقط
            ViewBag.HasNextPage = shipments.Count == pageSize;

            return View(shipments);
        }
        private async Task<List<Shipment>> GetShipmentsFromApi(DateTime fromDate, DateTime toDate, int pageNumber, int pageSize)
        {
            var apiUrl = "https://vsoftapi.com-eg.net/api/ClientUsers/V6/GetShipmentsPage";

            var requestData = new
            {
                fromDate = fromDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                toDate = toDate.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                pageParam = new
                {
                    pageSize = pageSize,
                    pageNumber = pageNumber
                }
            };

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new FlexibleDateTimeConverter() }
            };

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("CompanyID", "180109");
            client.DefaultRequestHeaders.Add("AccessToken", "44FFBD0A-99FD-4935-9BEB-379B0BA840DA");

            using var response = await client.PostAsJsonAsync(apiUrl, requestData);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse>(options);
                return result?.Shipments ?? new List<Shipment>();
            }

            return new List<Shipment>();
        }

        #endregion


        #region Archive
        [Authorize(Roles = "Admin,TrustAdmin,Accountant")]
        public async Task<IActionResult> Archive(string? AccountantId, DateTime? FilterTime, DateTime? FilterTimeTo, int pageNumber = 1, int pageSize = 50)
        {
            ViewBag.AccountantId = "0";
            if (User.IsInRole("Accountant"))
            {
                AccountantId = _userManger.GetUserId(User);
                ViewBag.AccountantId = AccountantId;
            }
            return View(await getorders("", pageNumber, pageSize, FilterTime, FilterTimeTo, AccountantId));
            // var temp = await PagedList<Wallet>.CreateAsync(archive, pageNumber, pageSize).Result;
            //return View(archive);
        }
        public async Task<PagedList<Wallet>> getorders(string searchStr, int pageNumber, int pageSize,
            DateTime? FilterTime, DateTime? FilterTimeTo, string? AccountantId)
        {

            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var now = DateTime.Now.ToUniversalTime();
            var today = TimeZoneInfo.ConvertTimeFromUtc(now, timeZoneInfo);
            if (searchStr != null)
                searchStr = searchStr.ToLower();
            Expression<Func<Wallet, bool>> filter = null;
            Func<IQueryable<Wallet>, IOrderedQueryable<Wallet>> orderBy = o => o.OrderByDescending(c => c.Id);
            if (FilterTime == null || FilterTimeTo == null)
            {
                filter = x => x.TransactionType == TransactionType.OrderComplete && !String.IsNullOrWhiteSpace(searchStr)
                && (AccountantId == "0" || x.Complete_UserId == AccountantId)
                && (x.Id.ToString().Contains(searchStr)
                                        || x.ActualUser.Name.Contains(searchStr)
                                        || x.Amount.ToString().Contains(searchStr));
            }
            else
            {
                filter = x => x.TransactionType == TransactionType.OrderComplete &&
             ((FilterTime.Value <= x.CreateOn)
             && (AccountantId == "0" || x.Complete_UserId == AccountantId)
                && (FilterTimeTo.Value >= x.CreateOn) &&
                      (String.IsNullOrWhiteSpace(searchStr) || (x.Id.ToString().Contains(searchStr)
                                        || x.ActualUser.Name.Contains(searchStr)
                                        || x.Amount.ToString().Contains(searchStr))));
            }

            ViewBag.Users = _users.Get(x => !x.IsDeleted && x.UserType == UserType.Accountant).ToList();
            ViewBag.PageStartRowNum = ((pageNumber - 1) * pageSize) + 1;
            return await PagedList<Wallet>.CreateAsync(
                _wallet.GetAllAsIQueryable(filter, orderBy, "ActualUser,Orders,Complete_User")
                , pageNumber, pageSize);
        }
        public async Task<IActionResult> GetItemsArchive(string searchStr, DateTime? FilterTime, DateTime? FilterTimeTo, string? AccountantId, int pageNumber = 1,
          int pageSize = 50)
        {
            ViewBag.searchStr = searchStr;
            ViewBag.FilterTime = FilterTime;
            ViewBag.FilterTimeTo = FilterTimeTo;
            ViewBag.Users = _users.Get(x => !x.IsDeleted && x.UserType == UserType.Accountant).ToList();
            if (User.IsInRole("Accountant"))
            {
                AccountantId = _userManger.GetUserId(User);
            }
            var archive = (await getorders(searchStr, pageNumber, pageSize, FilterTime, FilterTimeTo, AccountantId)).ToList();
            var walletIds = archive.Select(x => x.Id).ToList();
            var allOrders = _orders.Get(x => x.CompletedId.HasValue && walletIds.Contains(x.CompletedId.Value)).ToList();
            var ordersByWallet = allOrders.GroupBy(x => x.CompletedId.Value).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var item in archive)
            {
                item.Orders = ordersByWallet.ContainsKey(item.Id) ? ordersByWallet[item.Id] : new List<Order>();
            }
            return PartialView("_TableListArchive",
                archive);
        }

        public async Task<IActionResult> GetPaginationArchive(string? AccountantId, string searchStr, DateTime? FilterTime, DateTime? FilterTimeTo, int pageNumber = 1,
            int pageSize = 50)
        {
            if (User.IsInRole("Accountant"))
            {
                AccountantId = _userManger.GetUserId(User);
            }
            var model = PagedList<Wallet>.GetPaginationObject(
                await getorders(searchStr, pageNumber, pageSize, FilterTime, FilterTimeTo, AccountantId));
            model.GetItemsUrl = "/Orders/GetItemsArchive";
            model.GetPaginationUrl = "/Orders/GetPaginationArchive";
            return PartialView("_Pagination3", model);
        }
        #endregion

        #region Archive Returned
        [Authorize(Roles = "Admin,TrustAdmin,Accountant")]
        public async Task<IActionResult> ArchiveReturned(string? AccountantId, DateTime? FilterTime, DateTime? FilterTimeTo, int pageNumber = 1, int pageSize = 50)
        {
            ViewBag.AccountantId = "0";
            if (User.IsInRole("Accountant"))
            {
                AccountantId = _userManger.GetUserId(User);
                ViewBag.AccountantId = AccountantId;
            }
            ViewBag.Users = _users.Get(x => !x.IsDeleted && x.UserType == UserType.Accountant).ToList();
            return View(await getReturnedorders("", pageNumber, pageSize, FilterTime, FilterTimeTo, AccountantId));
        }
        public async Task<PagedList<Wallet>> getReturnedorders(string searchStr, int pageNumber, int pageSize,
            DateTime? FilterTime, DateTime? FilterTimeTo, string? AccountantId)
        {

            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var now = DateTime.Now.ToUniversalTime();
            var today = TimeZoneInfo.ConvertTimeFromUtc(now, timeZoneInfo);
            if (searchStr != null)
                searchStr = searchStr.ToLower();
            Expression<Func<Wallet, bool>> filter = null;
            Func<IQueryable<Wallet>, IOrderedQueryable<Wallet>> orderBy = o => o.OrderByDescending(c => c.Id);
            if (FilterTime == null || FilterTimeTo == null)
            {
                filter = x => x.TransactionType == TransactionType.OrderReturnedComplete && !String.IsNullOrWhiteSpace(searchStr)
                && (AccountantId == "0" || x.Complete_UserId == AccountantId)
                   && (x.Id.ToString().Contains(searchStr)
                                        || x.ActualUser.Name.Contains(searchStr)
                                        || x.Amount.ToString().Contains(searchStr));
            }
            else
            {
                filter = x => x.TransactionType == TransactionType.OrderReturnedComplete &&
             ((FilterTime.Value <= x.CreateOn)
             && (AccountantId == "0" || x.Complete_UserId == AccountantId)
                && (FilterTimeTo.Value >= x.CreateOn) &&
                      (String.IsNullOrWhiteSpace(searchStr) || (x.Id.ToString().Contains(searchStr)
                                        || x.ActualUser.Name.Contains(searchStr)
                                        || x.Amount.ToString().Contains(searchStr))));
            }

            ViewBag.Users = _users.Get(x => !x.IsDeleted && x.UserType == UserType.Accountant).ToList();
            ViewBag.PageStartRowNum = ((pageNumber - 1) * pageSize) + 1;
            return await PagedList<Wallet>.CreateAsync(
                _wallet.GetAllAsIQueryable(filter, orderBy, "ActualUser,Orders,Complete_User")
                , pageNumber, pageSize);
        }
        public async Task<IActionResult> GetItemsArchiveReturned(string searchStr, DateTime? FilterTime, DateTime? FilterTimeTo, string? AccountantId, int pageNumber = 1,
          int pageSize = 50)
        {
            ViewBag.searchStr = searchStr;
            ViewBag.FilterTime = FilterTime;
            ViewBag.FilterTimeTo = FilterTimeTo;
            ViewBag.Users = _users.Get(x => !x.IsDeleted && x.UserType == UserType.Accountant).ToList();
            if (User.IsInRole("Accountant"))
            {
                AccountantId = _userManger.GetUserId(User);
            }
            var archive = (await getReturnedorders(searchStr, pageNumber, pageSize, FilterTime, FilterTimeTo, AccountantId)).ToList();
            var returnedWalletIds = archive.Select(x => x.Id).ToList();
            var allReturnedOrders = _orders.Get(x => x.CompletedId.HasValue && returnedWalletIds.Contains(x.CompletedId.Value)).ToList();
            var returnedOrdersByWallet = allReturnedOrders.GroupBy(x => x.CompletedId.Value).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var item in archive)
            {
                item.Orders = returnedOrdersByWallet.ContainsKey(item.Id) ? returnedOrdersByWallet[item.Id] : new List<Order>();
            }
            return PartialView("_TableListArchiveReturned",
                archive);
        }

        public async Task<IActionResult> GetPaginationArchiveReturned(string? AccountantId, string searchStr, DateTime? FilterTime, DateTime? FilterTimeTo, int pageNumber = 1,
            int pageSize = 50)
        {
            if (User.IsInRole("Accountant"))
            {
                AccountantId = _userManger.GetUserId(User);
            }
            var model = PagedList<Wallet>.GetPaginationObject(
                await getReturnedorders(searchStr, pageNumber, pageSize, FilterTime, FilterTimeTo, AccountantId));
            model.GetItemsUrl = "/Orders/GetItemsArchiveReturned";
            model.GetPaginationUrl = "/Orders/GetPaginationArchiveReturned";
            return PartialView("_Pagination3", model);
        }
        #endregion

        public class PrintAllNewRequest
        {
            public List<long> Orders { get; set; }
            public string Driver { get; set; }
        }
        public IActionResult PrintSelectedOrders([FromBody] PrintAllNewRequest request)
        {
            //return RedirectToAction(nameof(PrintAllNew), new { Orders = request.Orders, Driver = request.Driver });
            var redirectUrl = Url.Action(nameof(PrintAllNew), new { Orders = request.Orders, Driver = request.Driver });
            return Json(new { redirectUrl });
        }
        public IActionResult PrintBarcodeSelectedOrders([FromBody] PrintAllNewRequest request)
        {
            //return RedirectToAction(nameof(PrintAllNew), new { Orders = request.Orders, Driver = request.Driver });
            var redirectUrl = Url.Action(nameof(PrintAllNewBarCode), new { Orders = request.Orders });
            return Json(new { redirectUrl });
        }

        [Authorize(Roles = "Admin,HighAdmin,Accountant,Client,TrustAdmin")]
        public async Task<IActionResult> PrintAllNew(List<long> Orders, string Driver = "")
        {
            long BranchId = 0;
            if (User.IsInRole("HighAdmin") || User.IsInRole("Accountant"))
            {
                var user = await _users.GetObj(x => x.Id == _userManger.GetUserId(User));
                BranchId = user.BranchId;
                if (Orders.Count > 0)
                {
                    List<Order> Selectedorders = new List<Order>();
                    foreach (var order in Orders)
                    {
                        Selectedorders.Add(_orderService.GetList(x => x.Id == order && x.BranchId == BranchId).FirstOrDefault());
                    }
                    return View(Selectedorders);
                }
                if (Driver != "")
                {
                    return View(_orderService.GetList(x => x.Status == OrderStatus.Assigned && x.BranchId == BranchId
                                                           && !x.Pending && !x.IsDeleted &&
                                                           (x.DeliveryId == "" ? true : x.DeliveryId == Driver)));
                }
                return View(_orderService.GetList(x => x.Status == OrderStatus.Placed && x.BranchId == BranchId
                                                  && !x.Pending && !x.IsDeleted));

            }
            if (User.IsInRole("Client"))
            {
                var user = await _users.GetObj(x => x.Id == _userManger.GetUserId(User));
                BranchId = user.BranchId;
                if (Orders.Count > 0)
                {
                    List<Order> Selectedorders = new List<Order>();
                    foreach (var order in Orders)
                    {
                        Selectedorders.Add(_orderService.GetList(x => x.Id == order && x.ClientId == user.Id).FirstOrDefault());
                    }
                    return View(Selectedorders);
                }
                return View(_orderService.GetList(x => x.Status == OrderStatus.Placed && x.ClientId == user.Id
                                                  && !x.Pending && !x.IsDeleted));

            }
            if (Orders.Count > 0)
            {
                List<Order> Selectedorders = new List<Order>();
                foreach (var order in Orders)
                {
                    Selectedorders.Add(_orderService.GetList(x => x.Id == order).FirstOrDefault());
                }
                return View(Selectedorders);
            }
            if (Driver != "")
            {
                return View(_orderService.GetList(x => x.Status == OrderStatus.Assigned
                                                       && !x.Pending && !x.IsDeleted &&
                                                       (x.DeliveryId == "" ? true : x.DeliveryId == Driver)));
            }

            return View(_orderService.GetList(x => x.Status == OrderStatus.Placed
                                                   && !x.Pending && !x.IsDeleted));
        }
        [Authorize(Roles = "Admin,HighAdmin,Accountant,Client,TrustAdmin")]
        public async Task<IActionResult> PrintAllNewBarCode(List<long> Orders, string Driver = "")
        {
            long BranchId = 0;
            if (User.IsInRole("HighAdmin") || User.IsInRole("Accountant"))
            {
                var user = await _users.GetObj(x => x.Id == _userManger.GetUserId(User));
                BranchId = user.BranchId;
                if (Orders.Count > 0)
                {
                    List<Order> Selectedorders = new List<Order>();
                    foreach (var id in Orders)
                    {
                        var order = _orderService.GetList(x => x.Id == id && x.BranchId == BranchId).FirstOrDefault();
                        if (order.Code.Contains('R'))
                        {
                            var code = order.Code.Replace("R", string.Empty);
                            var OriginalOrder = _orderService.GetList(x => x.Code == code).FirstOrDefault();
                            Selectedorders.Add(OriginalOrder);
                        }
                        else
                            Selectedorders.Add(order);
                    }
                    return View(Selectedorders);
                }
                return View(_orderService.GetList(x => x.Status == OrderStatus.Placed && x.BranchId == BranchId
                                                  && !x.Pending && !x.IsDeleted));

            }
            if (Orders.Count > 0)
            {
                List<Order> Selectedorders = new List<Order>();
                foreach (var id in Orders)
                {
                    var order = _orderService.GetList(x => x.Id == id).FirstOrDefault();
                    if (order.Code.Contains('R'))
                    {
                        var code = order.Code.Replace("R", string.Empty);
                        var OriginalOrder = _orderService.GetList(x => x.Code == code).FirstOrDefault();
                        Selectedorders.Add(OriginalOrder);
                    }
                    else
                        Selectedorders.Add(order);
                }
                return View(Selectedorders);
            }

            return View(_orderService.GetList(x => x.Status == OrderStatus.Placed
                                                   && !x.Pending && !x.IsDeleted));
        }
        [Authorize(Roles = "Admin,HighAdmin,Accountant,Client,TrustAdmin")]
        public IActionResult PrintClientOrders(string Id, string? message, string? searchCode, int page = 1, int pageSize = 100)
        {
            ViewBag.Id = Id;
            ViewBag.message = message;

            // قائمة المناديب لتحويل الطلبات
            var currentDriver = _users.Get(x => x.Id == Id).FirstOrDefault();
            if (currentDriver != null)
            {
                ViewBag.Drivers = _users.Get(x => !x.IsDeleted && x.UserType == UserType.Driver
                    && x.Id != Id && x.BranchId == currentDriver.BranchId).ToList();
            }
            else
            {
                ViewBag.Drivers = new List<ApplicationUser>();
            }

            var baseQuery = _orders.GetAllAsIQueryable()
                .Include(x => x.Client)
                .Include(x => x.Delivery)
                .Where(x => x.DeliveryId == Id
                           && x.Status == OrderStatus.Assigned
                           && !x.IsDeleted
                           && x.OrderCompleted == OrderCompleted.NOK
                           && !x.Finished
                           && (string.IsNullOrEmpty(searchCode) || EF.Functions.Like(x.Code, $"%{searchCode}%")));

            // حساب العدد الكلي دون تحميل البيانات
            var totalCount = baseQuery.Count();

            // جلب فقط بيانات الصفحة المطلوبة
            var pagedOrders = baseQuery
                .OrderBy(x => x.CreateOn)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.SearchCode = searchCode;

            return View(pagedOrders);
        }

        [Authorize(Roles = "Admin,HighAdmin,Accountant,LowAdmin,TrustAdmin")]
        [HttpPost]
        public async Task<IActionResult> TransferOrders(string CurrentDeliveryId, long[] OrdersIds, string NewDeliveryId)
        {
            List<long> OrdersTransferred = new List<long>();
            using (var scope = new TransactionScope(TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted, Timeout = TimeSpan.FromMinutes(10) },
                TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    var newDriver = await _users.GetSingle(x => x.Id == NewDeliveryId);
                    if (newDriver == null || newDriver.UserType != UserType.Driver)
                    {
                        return RedirectToAction(nameof(PrintClientOrders), new { Id = CurrentDeliveryId, message = "المندوب الجديد غير صالح" });
                    }

                    foreach (var orderId in OrdersIds)
                    {
                        var order = await _orders.GetSingle(x => x.Id == orderId, "Client,Branch");
                        if (IsValidOrderForTransfer(order, newDriver))
                        {
                            order.DeliveryId = NewDeliveryId;
                            order.LastUpdated = DateTime.Now.ToUniversalTime();
                            if (await _orders.Update(order))
                            {
                                OrdersTransferred.Add(orderId);
                                await UpdateOrderHistoryForTransfer(order.OrderOperationHistoryId);
                            }
                        }
                    }

                    scope.Complete();
                    if (OrdersTransferred.Count > 0)
                    {
                        string message = $"تم تحويل عدد {OrdersTransferred.Count} طلب إلى المندوب: {newDriver.Name}";
                        return RedirectToAction(nameof(PrintClientOrders), new { Id = CurrentDeliveryId, message });
                    }
                    else
                    {
                        return RedirectToAction(nameof(PrintClientOrders), new { Id = CurrentDeliveryId, message = "حدث خطأ أثناء التحويل" });
                    }
                }
                catch (Exception ex)
                {
                    return RedirectToAction(nameof(PrintClientOrders), new { Id = CurrentDeliveryId, message = "حدث خطأ أثناء التحويل" });
                }
            }
        }

        private async Task UpdateOrderHistoryForTransfer(long? orderHistoryId)
        {
            if (orderHistoryId.HasValue)
            {
                var history = await _Histories.GetObj(x => x.Id == orderHistoryId);
                if (history != null)
                {
                    history.Assign_To_Driver_UserId = _userManger.GetUserId(User);
                    history.Assign_To_DriverDate = DateTime.Now.ToUniversalTime();
                    await _Histories.Update(history);
                }
            }
        }

        private bool IsValidOrderForTransfer(Order order, ApplicationUser newDriver)
        {
            return order != null &&
                    !order.IsDeleted && !order.Pending &&
                   ((order.BranchId == newDriver.BranchId && order.Client.BranchId == newDriver.BranchId) ||
                    (order.BranchId == newDriver.BranchId && order.TransferredConfirmed));
        }

        [Authorize(Roles = "Admin,HighAdmin,Accountant,Client,TrustAdmin")]
        public IActionResult PrintExcelClientOrders(string Id)
        {
            return View(_orderService.GetList(x =>
                x.DeliveryId == Id && x.Status == OrderStatus.Assigned && !x.IsDeleted &&
                x.OrderCompleted == OrderCompleted.NOK && !x.Finished));
        }
        [Authorize(Roles = "Admin,HighAdmin,Accountant,Client,TrustAdmin")]
        public IActionResult PrintExcelClientNewOrders(string Id, List<long> OrdersPrint,
            bool showProductName = false, bool showSenderPhone = false, bool showSenderName = false,
            bool showOrderCost = false, bool showDeliveryFees = false, bool showClientCode = false)
        {
            ViewBag.showProductName = showProductName;
            ViewBag.showSenderPhone = showSenderPhone;
            ViewBag.showSenderName = showSenderName;
            ViewBag.showOrderCost = showOrderCost;
            ViewBag.showDeliveryFees = showDeliveryFees;
            ViewBag.showClientCode = showClientCode;
            return View(_orderService.GetList(x =>
                x.DeliveryId == Id && x.Status == OrderStatus.Assigned && !x.IsDeleted &&
                x.OrderCompleted == OrderCompleted.NOK && !x.Finished && OrdersPrint.Contains(x.Id)));
        }
        [Authorize(Roles = "Admin,HighAdmin,Accountant,Client,TrustAdmin,TrackingAdmin")]
        public async Task<IActionResult> Details(long id)
        {
            if (!await _orders.IsExist(x => x.Id == id && !x.IsDeleted))
            {
                return NotFound("هذا الطلب غير موجود او محذوف");
            }

            return View(_orders.GetAllAsIQueryable(x => x.Id == id, null, "OrderNotes,Client").FirstOrDefault());
        }
        [AllowAnonymous]
        public async Task<IActionResult> test(string code)
        {
            if (!await _orders.IsExist(x => x.Code == code && !x.IsDeleted))
            {
                return NotFound("هذا الطلب غير موجود او محذوف");
            }
            var model = _orders.GetAllAsIQueryable(x => x.Code == code, null, "OrderNotes,Delivery,Client,Client.Branch,PreviousBranch,Branch").FirstOrDefault();
            return View(model);
        }
        public IActionResult Tracking()
        {
            return View();
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult SearchOrder(string orderNumber)
        {
            try
            {
                //var order = _orders.GetAllAsIQueryable(x => x.Id == (orderNumber - 1000), null, "OrderNotes,Client,Delivery,Branch,OrderNotes").FirstOrDefault();
                var order = _orders.GetAllAsIQueryable(x => x.Code == orderNumber, null, "OrderNotes,Client,Delivery,Branch,OrderNotes").FirstOrDefault();
                if (order != null)
                {
                    return PartialView("_OrderDetailsPartial", order);
                }
                else
                {
                    return Content("هذا الطلب غير موجود أو محذوف");
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine(ex.Message);
                return Content("حدث خطأ أثناء معالجة الطلب");
            }
        }
        //[Authorize(Roles = "Admin,HighAdmin,Accountant,Client,TrustAdmin")]
        //public async Task<IActionResult> Detailsbarcode(byte[] barcode)
        //{
        //    if (!await _orders.IsExist(x => x.BarcodeImage == barcode && !x.IsDeleted))
        //    {
        //        return NotFound("هذا الطلب غير موجود او محذوف");
        //    }

        //    return View(_orders.GetAllAsIQueryable(x => x.BarcodeImage == barcode, null, "OrderNotes,Client").FirstOrDefault());
        //}


        [Authorize(Roles = "Admin,HighAdmin,Accountant,Client,TrustAdmin")]
        public IActionResult Print(List<long> orderIds, bool returned = false)
        {
            ViewBag.returned = returned;
            //var model = orderIds.Split(',').Select(long.Parse).ToList();
            var orders = _orderService.GetList(x => orderIds.Contains(x.Id)).ToList();
            return View(orders);
        }
        #region Complete Orders
        [Route("ALL-CompleteOrders")]
        [Authorize(Roles = "Admin,TrustAdmin,Accountant")]
        public async Task<IActionResult> AllComplete(List<string> UserIds)
        {
            bool auth = User.IsInRole("Client");
            bool authAccountant = User.IsInRole("Accountant");
            var user = new ApplicationUser();
            if (auth)
            {
                user = await _userManger.GetUserAsync(User);
            }
            var Accountant = new ApplicationUser();
            ViewBag.branchId = 0;
            if (authAccountant)
            {
                Accountant = await _userManger.GetUserAsync(User);
                ViewBag.branchId = Accountant.BranchId;
            }

            var orders = new List<Order>();
            if (UserIds.Count > 0)
                orders = _orderService.GetList(x => x.OrderCompleted == OrderCompleted.NOK &&
                                                    x.Status != OrderStatus.Completed &&
                                                    x.Status != OrderStatus.Returned &&
                                                    x.Status != OrderStatus.PartialReturned
                                                    && x.Finished
                                                    && !x.IsDeleted
                                                    && (UserIds.Contains(x.ClientId))).ToList();
            else if (auth)
                orders = _orderService.GetList(x => x.OrderCompleted == OrderCompleted.OK &&
                                                    x.Status != OrderStatus.Completed &&
                                                    x.Status != OrderStatus.Returned &&
                                                    x.Status != OrderStatus.PartialReturned
                                                    && x.Finished
                                                    && !x.IsDeleted && x.ClientId == user.Id).ToList();
            else
                orders = _orderService.GetList(x => x.OrderCompleted == OrderCompleted.NOK &&
                                                    x.Status != OrderStatus.Completed &&
                                                    x.Status != OrderStatus.Returned &&
                                                    x.Status != OrderStatus.PartialReturned
                                                    && x.Finished
                                                    && !x.IsDeleted).ToList();
            ViewBag.senders = _orderService.GetUsers(x => x.OrderCompleted == OrderCompleted.NOK &&
                                                          x.Status != OrderStatus.Completed &&
                                                          x.Status != OrderStatus.Returned &&
                                                          x.Status != OrderStatus.PartialReturned
                                                          && x.Finished
                                                          && !x.IsDeleted).ToList();
            return View(orders);
        }
        [Route("Complete-Orders")]
        [Authorize(Roles = "Admin,TrustAdmin,Accountant")]
        public async Task<IActionResult> Complete(string UserId)
        {
            bool auth = User.IsInRole("Client");
            bool authAccountant = User.IsInRole("Accountant");
            var user = new ApplicationUser();
            if (auth)
            {
                user = await _userManger.GetUserAsync(User);
            }
            var Accountant = new ApplicationUser();
            ViewBag.branchId = 0;
            if (authAccountant)
            {
                Accountant = await _userManger.GetUserAsync(User);
                ViewBag.branchId = Accountant.BranchId;
            }

            var orders = new List<Order>();
            if (UserId != null)
                orders = _orderService.GetList(x => x.OrderCompleted == OrderCompleted.NOK &&
                                                    x.Status != OrderStatus.Completed &&
                                                    x.Status != OrderStatus.Returned &&
                                                    x.Status != OrderStatus.PartialReturned
                                                    && x.Finished
                                                    && !x.IsDeleted
                                                    && (UserId == x.ClientId)).ToList();
            else if (auth)
                orders = _orderService.GetList(x => x.OrderCompleted == OrderCompleted.OK &&
                                                    x.Status != OrderStatus.Completed &&
                                                    x.Status != OrderStatus.Returned &&
                                                    x.Status != OrderStatus.PartialReturned
                                                    && x.Finished
                                                    && !x.IsDeleted && x.ClientId == user.Id).ToList();
            //else
            //    orders = _orderService.GetList(x => x.OrderCompleted == OrderCompleted.NOK &&
            //                                        x.Status != OrderStatus.Completed &&
            //                                        x.Status != OrderStatus.Returned &&
            //                                        x.Status != OrderStatus.PartialReturned
            //                                        && x.Finished
            //                                        && !x.IsDeleted).ToList();
            ViewBag.senders = _orderService.GetUsers(x => x.OrderCompleted == OrderCompleted.NOK &&
                                                          x.Status != OrderStatus.Completed &&
                                                    x.Status != OrderStatus.Returned &&
                                                    x.Status != OrderStatus.PartialReturned
                                                          && x.Finished
                                                          && !x.IsDeleted).ToList();
            return View(orders/*.Where(x => x.Status != OrderStatus.PartialReturned && x.Status != OrderStatus.Returned).ToList()*/);
        }
        [Route("Complete-Orders")]
        [Authorize(Roles = "Admin,TrustAdmin,Accountant")]
        [HttpPost]
        public async Task<IActionResult> Complete(List<long> OrderId, List<double> DeliveryCost)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required,
                                       new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted, Timeout = TimeSpan.FromMinutes(10) },
                                       TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    if (DeliveryCost.Any(cost => cost != -1))
                    {
                        double total = 0;
                        var admin = await _users.GetObj(x => x.Id == "9897454b-add0-45ef-ad3b-53027814ede7");
                        var orders = new List<long>();
                        string userid = " ";

                        // تجميع الطلبات الصالحة أولاً (لم يتم تسويتها + DeliveryCost != -1)
                        var validOrders = new List<(Order order, double deliveryCost)>();
                        for (int i = 0; i < OrderId.Count; i++)
                        {
                            if (DeliveryCost[i] != -1)
                            {
                                var order = await _orders.GetObj(x => x.Id == OrderId[i]);
                                if (order != null && order.OrderCompleted != OrderCompleted.OK)
                                {
                                    validOrders.Add((order, DeliveryCost[i]));
                                }
                            }
                        }

                        if (validOrders.Count == 0)
                        {
                            scope.Complete();
                            TempData["Error"] = "لا توجد طلبات صالحة للتسوية (ربما تم تسويتها بالفعل)";
                            return RedirectToAction(nameof(Complete));
                        }

                        // إنشاء Wallet فقط لو فيه طلبات فعلية
                        var wallet = new Wallet()
                        {
                            UserId = admin.Id,
                            Amount = total,
                            TransactionType = TransactionType.OrderComplete,
                            UserWalletLast = admin.Wallet,
                            Complete_UserId = _userManger.GetUserId(User),
                            AddedToAdminWallet = false
                        };
                        await _wallet.Add(wallet);

                        foreach (var (order, deliveryCost) in validOrders)
                        {
                            order.ClientCost = order.ArrivedCost - deliveryCost;
                            order.OrderCompleted = OrderCompleted.OK;
                            order.CompletedId = wallet.Id;
                            order.CompletedOn = DateTime.Now.ToUniversalTime();
                            if (userid != order.ClientId && userid != " ")
                                throw new Exception("فشل تسوية الطلبات.");
                            userid = order.ClientId;
                            total = total + (order.ArrivedCost - order.ClientCost - order.DeliveryCost);
                            order.LastUpdated = DateTime.Now.ToUniversalTime();
                            if (await _orders.Update(order))
                            {
                                if (order.OrderOperationHistoryId != null)
                                {
                                    var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
                                    history.Complete_UserId = _userManger.GetUserId(User);
                                    history.CompleteDate = DateTime.Now.ToUniversalTime();
                                    await _Histories.Update(history);
                                }
                            }
                            orders.Add(order.Id);
                        }

                        admin.Wallet = admin.Wallet + total;
                        if (!await _users.Update(admin))
                            throw new Exception("فشل تسوية الطلبات.");

                        wallet.Amount = total;
                        wallet.ActualUserId = userid;
                        wallet.AddedToAdminWallet = true;
                        await _wallet.Update(wallet);
                        scope.Complete();
                        return RedirectToAction(nameof(Print), new { orderIds = orders });
                    }
                    return RedirectToAction(nameof(Complete));

                }
                catch (Exception ex)
                {
                    return RedirectToAction(nameof(Complete));
                }
            }
        }
        #endregion

        #region Complete Returned Orders
        [Route("Store/Returned")]
        [Authorize(Roles = "Admin,HighAdmin,TrustAdmin,Accountant")]
        public async Task<IActionResult> AllReturnedComplete(List<string> UserIds, long branchId = -1, int pageNumber = 1, int pageSize = 50, bool loadAll = false)
        {
            bool authHighAdmin = User.IsInRole("HighAdmin");
            bool authHighAccountant = User.IsInRole("Accountant");
            var BranchAdmin = new ApplicationUser();
            ViewBag.branchId = branchId;
            ViewBag.Branchs = _branch.Get(x => !x.IsDeleted).ToList();
            if (authHighAdmin || authHighAccountant)
            {
                BranchAdmin = await _userManger.GetUserAsync(User);
                ViewBag.branchId = BranchAdmin.BranchId;
            }

            var query = _orderService.GetQueryableList(x =>
                ((x.OrderCompleted == OrderCompleted.NOK && (x.Status == OrderStatus.Returned ||
                    x.Status == OrderStatus.PartialReturned))
                || (x.ReturnedOrderCompleted == OrderCompleted.NOK && x.ReturnedFinished &&
                    (x.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender ||
                     x.Status == OrderStatus.Returned_And_Paid_DeliveryCost)))
                && !x.IsDeleted);

            if (authHighAdmin || authHighAccountant)
            {
                query = query.Where(x => x.BranchId == BranchAdmin.BranchId &&
                    (x.Client.BranchId == BranchAdmin.BranchId || x.TransferredConfirmed));
            }
            else if (branchId != -1)
            {
                query = query.Where(x => x.BranchId == branchId &&
                    (x.Client.BranchId == branchId || x.TransferredConfirmed));
            }

            if (UserIds != null && UserIds.Count > 0)
            {
                query = query.Where(x => UserIds.Contains(x.ClientId));
            }

            query = query.Include(x => x.Client).ThenInclude(c => c.Branch);

            var totalCount = await query.CountAsync();
            ViewBag.TotalCount = totalCount;
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.LoadAll = loadAll;

            List<Order> orders;
            if (loadAll)
            {
                orders = await query.OrderByDescending(x => x.Id).ToListAsync();
            }
            else
            {
                orders = await query.OrderByDescending(x => x.Id)
                    .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            }

            ViewBag.senders = _orderService.GetUsers(x => ((x.OrderCompleted == OrderCompleted.NOK && (x.Status == OrderStatus.Returned ||
                                                          x.Status == OrderStatus.PartialReturned))
                                                          || (x.ReturnedOrderCompleted == OrderCompleted.NOK && x.ReturnedFinished &&
                                                          (x.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender ||
                                                         x.Status == OrderStatus.Returned_And_Paid_DeliveryCost)))
                                                          && !x.IsDeleted).ToList();
            return View(orders);
        }
        [Authorize(Roles = "Admin,HighAdmin,TrustAdmin,Accountant")]
        [Route("Returned-Orders")]
        public async Task<IActionResult> ReturnedComplete(string UserId)
        {
            bool authAccountant = User.IsInRole("Accountant");
            bool authHighAdmin = User.IsInRole("HighAdmin");
            var user = new ApplicationUser();

            var Accountant = new ApplicationUser();
            ViewBag.branchId = 0;
            if (authAccountant || authHighAdmin)
            {
                Accountant = await _userManger.GetUserAsync(User);
                ViewBag.branchId = Accountant.BranchId;
            }

            var orders = new List<Order>();
            if (UserId != null)
                orders = _orderService.GetList(x => x.BranchId == x.Client.BranchId &&
                                                   ((x.OrderCompleted == OrderCompleted.NOK && (x.Status == OrderStatus.Returned ||
                                                          x.Status == OrderStatus.PartialReturned))
                                                          || (x.ReturnedOrderCompleted == OrderCompleted.NOK && x.ReturnedFinished &&
                                                          (x.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender ||
                                                         x.Status == OrderStatus.Returned_And_Paid_DeliveryCost)))
                                                    && x.Finished
                                                    && !x.IsDeleted
                                                    && (UserId == x.ClientId)).ToList();
            //else
            //    orders = _orderService.GetList(x => x.BranchId == x.Client.BranchId &&
            //                                        ((x.OrderCompleted == OrderCompleted.NOK && (x.Status == OrderStatus.Returned ||
            //                                              x.Status == OrderStatus.PartialReturned))
            //                                              || (x.ReturnedOrderCompleted == OrderCompleted.NOK &&
            //                                              (x.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender ||
            //                                             x.Status == OrderStatus.Returned_And_Paid_DeliveryCost)))
            //                                        && x.Finished
            //                                        && !x.IsDeleted).ToList();
            ViewBag.senders = _orderService.GetUsers(x => ((x.OrderCompleted == OrderCompleted.NOK && (x.Status == OrderStatus.Returned ||
                                                          x.Status == OrderStatus.PartialReturned))
                                                          || (x.ReturnedOrderCompleted == OrderCompleted.NOK && x.ReturnedFinished &&
                                                          (x.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender ||
                                                         x.Status == OrderStatus.Returned_And_Paid_DeliveryCost)))
                                                          && x.Finished
                                                          && !x.IsDeleted).ToList();
            return View(orders);
        }
        [Authorize(Roles = "Admin,HighAdmin,TrustAdmin,Accountant")]
        [Route("Returned-Orders-New")]
        public async Task<IActionResult> ReturnedCompleteNew(string UserId)
        {
            bool authAccountant = User.IsInRole("Accountant");
            bool authHighAdmin = User.IsInRole("HighAdmin");
            var user = new ApplicationUser();

            var Accountant = new ApplicationUser();
            ViewBag.branchId = 0;
            if (authAccountant || authHighAdmin)
            {
                Accountant = await _userManger.GetUserAsync(User);
                ViewBag.branchId = Accountant.BranchId;
            }

            var orders = new List<Order>();
            if (UserId != null)
                orders = _orderService.GetList(x => x.BranchId == x.Client.BranchId &&
                                                   ((x.OrderCompleted == OrderCompleted.NOK && (x.Status == OrderStatus.Returned ||
                                                          x.Status == OrderStatus.PartialReturned))
                                                          || (x.ReturnedOrderCompleted == OrderCompleted.NOK && x.ReturnedFinished &&
                                                          (x.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender ||
                                                         x.Status == OrderStatus.Returned_And_Paid_DeliveryCost)))
                                                    && x.Finished
                                                    && !x.IsDeleted
                                                    && (UserId == x.ClientId)).ToList();
            //else
            //    orders = _orderService.GetList(x => x.BranchId == x.Client.BranchId &&
            //                                        ((x.OrderCompleted == OrderCompleted.NOK && (x.Status == OrderStatus.Returned ||
            //                                              x.Status == OrderStatus.PartialReturned))
            //                                              || (x.ReturnedOrderCompleted == OrderCompleted.NOK &&
            //                                              (x.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender ||
            //                                             x.Status == OrderStatus.Returned_And_Paid_DeliveryCost)))
            //                                        && x.Finished
            //                                        && !x.IsDeleted).ToList();
            ViewBag.senders = _orderService.GetUsers(x => ((x.OrderCompleted == OrderCompleted.NOK && (x.Status == OrderStatus.Returned ||
                                                          x.Status == OrderStatus.PartialReturned))
                                                          || (x.ReturnedOrderCompleted == OrderCompleted.NOK && x.ReturnedFinished &&
                                                          (x.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender ||
                                                         x.Status == OrderStatus.Returned_And_Paid_DeliveryCost)))
                                                          && x.Finished
                                                          && !x.IsDeleted).ToList();
            return View(orders);
        }
        [Authorize(Roles = "Admin,HighAdmin,TrustAdmin,Accountant")]
        [Route("Returned-Orders")]
        [HttpPost]
        public async Task<IActionResult> ReturnedComplete(List<long> OrderId, List<bool> Recieve)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required,
                                       new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted, Timeout = TimeSpan.FromMinutes(10) },
                                       TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    if (Recieve.Any(r => r == true))
                    {
                        double total = 0;
                        var admin = await _users.GetObj(x => x.Id == "9897454b-add0-45ef-ad3b-53027814ede7");
                        var orders = new List<long>();
                        string userid = " ";

                        // تجميع الطلبات الصالحة أولاً
                        var validOrders = new List<(Order order, int index)>();
                        for (int i = 0; i < OrderId.Count; i++)
                        {
                            if (Recieve[i])
                            {
                                var order = await _orders.GetSingle(x => x.Id == OrderId[i], "Client,Branch");
                                if (order != null && order.BranchId == order.Client.BranchId)
                                {
                                    bool alreadySettled = (order.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender || order.Status == OrderStatus.Returned_And_Paid_DeliveryCost)
                                        ? order.ReturnedOrderCompleted == OrderCompleted.OK
                                        : order.OrderCompleted == OrderCompleted.OK;

                                    if (!alreadySettled)
                                        validOrders.Add((order, i));
                                }
                            }
                        }

                        if (validOrders.Count == 0)
                        {
                            scope.Complete();
                            TempData["Error"] = "لا توجد طلبات صالحة للتسوية (ربما تم تسويتها بالفعل)";
                            return RedirectToAction(nameof(ReturnedComplete));
                        }

                        var wallet = new Wallet()
                        {
                            UserId = admin.Id,
                            Amount = total,
                            TransactionType = TransactionType.OrderReturnedComplete,
                            UserWalletLast = admin.Wallet,
                            Complete_UserId = _userManger.GetUserId(User),
                            AddedToAdminWallet = false,
                        };
                        await _wallet.Add(wallet);

                        foreach (var (order, idx) in validOrders)
                        {
                            if (order.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender || order.Status == OrderStatus.Returned_And_Paid_DeliveryCost)
                            {
                                order.ReturnedOrderCompleted = OrderCompleted.OK;
                                order.ReturnedCompletedId = wallet.Id;
                            }
                            else
                            {
                                order.OrderCompleted = OrderCompleted.OK;
                                order.CompletedId = wallet.Id;
                            }
                            order.CompletedOn = DateTime.Now.ToUniversalTime();
                            if (userid != order.ClientId && userid != " ")
                                throw new Exception("فشل تسوية الطلبات.");
                            userid = order.ClientId;
                            if (order.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender || order.Status == OrderStatus.Returned_And_Paid_DeliveryCost)
                                total = total + (order.Cost);
                            else total = total + (order.TotalCost);
                            order.LastUpdated = DateTime.Now.ToUniversalTime();
                            if (await _orders.Update(order))
                            {
                                if (order.OrderOperationHistoryId != null)
                                {
                                    var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
                                    if (order.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender || order.Status == OrderStatus.Returned_And_Paid_DeliveryCost)
                                    {
                                        history.ReturnedComplete_UserId = _userManger.GetUserId(User);
                                        history.ReturnedCompleteDate = DateTime.Now.ToUniversalTime();
                                    }
                                    else
                                    {
                                        history.Complete_UserId = _userManger.GetUserId(User);
                                        history.CompleteDate = DateTime.Now.ToUniversalTime();
                                    }
                                    await _Histories.Update(history);
                                }
                            }
                            orders.Add(order.Id);
                        }

                        wallet.Amount = total;
                        wallet.ActualUserId = userid;
                        wallet.AddedToAdminWallet = true;
                        await _wallet.Update(wallet);
                        scope.Complete();
                        return RedirectToAction(nameof(Print), new { orderIds = orders, returned = true });
                    }
                    else
                    {
                        return RedirectToAction(nameof(ReturnedComplete));
                    }
                }
                catch (Exception ex)
                {
                    return RedirectToAction(nameof(ReturnedComplete));
                }
            }
        }
        #endregion

        #region Switch orders
        [Authorize(Roles = "Admin,HighAdmin,Accountant,TrustAdmin")]
        public async Task<IActionResult> Switch(List<long> OrderId, string delete, string newOrders = "0")
        {
            var temp = OrderId;
            if (newOrders == "1")
            {
                if (delete != "1")
                {
                    //اقبل
                    foreach (var id in OrderId)
                    {
                        if (!await _orders.IsExist(x => x.Id == id))
                        {
                            return BadRequest("هذه الطلب غير موجود");
                        }
                        else
                        {
                            var order = await _orders.GetObj(x => x.Id == id);
                            order.Pending = false;
                            order.LastUpdated = DateTime.Now.ToUniversalTime();
                            await _orders.Update(order);
                            if (order.OrderOperationHistoryId != null)
                            {
                                var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
                                history.Accept_UserId = _userManger.GetUserId(User);
                                history.AcceptDate = DateTime.Now.ToUniversalTime();
                                await _Histories.Update(history);
                                //await _CRUDHistory.Update(history.Id);
                            }

                        }
                    }
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    //رفض

                    foreach (var id in OrderId)
                    {
                        if (!await _orders.IsExist(x => x.Id == id))
                        {
                            return BadRequest("هذه الطلب غير موجوده");
                        }
                        else
                        {
                            var order = await _orders.GetObj(x => x.Id == id);
                            order.Status = OrderStatus.Rejected;
                            order.LastUpdated = DateTime.Now.ToUniversalTime();
                            await _orders.Update(order);
                            if (order.OrderOperationHistoryId != null)
                            {
                                var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
                                history.Reject_UserId = _userManger.GetUserId(User);
                                history.RejectDate = DateTime.Now.ToUniversalTime();
                                await _Histories.Update(history);
                                //await _CRUDHistory.Update(history.Id);
                            }

                        }
                    }
                    return RedirectToAction(nameof(Index), new { q = "rej" });
                }
            }
            else
            {
                if (delete != "1")
                {
                    // نطلع اكسيل شيت
                    List<Order> Orders = new List<Order>();
                    if (OrderId != null && OrderId.Count > 0)
                    {
                        for (var i = 0; i < OrderId.Count; i++)
                        {
                            var order = _orderService.GetList(x => x.Id == OrderId[i]).FirstOrDefault();
                            if (order != null)
                            {
                                Orders.Add(order);
                            }
                        }
                    }

                    var dt = ExcelExport.OrderExport(Orders);
                    using (XLWorkbook wb = new XLWorkbook())
                    {
                        wb.Worksheets.Add(dt);
                        using (MemoryStream stream = new MemoryStream())
                        {
                            wb.SaveAs(stream);

                            // التأكد من إعادة مؤشر تيار الذاكرة إلى البداية
                            stream.Position = 0;

                            // استخدام FileResult مع headers مناسبة
                            Response.Clear();
                            Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                            Response.Headers.Add("Content-Disposition", "attachment; filename=OrdersReport.xlsx");

                            // إرجاع البيانات كـ FileResult
                            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "OrdersReport.xlsx");
                        }
                    }

                }
                else
                {
                    if (User.IsInRole("Admin"))
                    {
                        foreach (var id in OrderId)
                        {
                            await _CRUD.ToggleDelete(id);
                            var order = _orders.Get(x => x.Id == id).First();

                            if (_orders.Get(x => x.Id == id).First().IsDeleted)
                            {
                                if (order.OrderOperationHistoryId != null)
                                {
                                    var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
                                    history.Delete_UserId = _userManger.GetUserId(User);
                                    history.DeleteDate = DateTime.Now.ToUniversalTime();
                                    await _Histories.Update(history);
                                }
                            }
                            else
                            {
                                if (order.OrderOperationHistoryId != null)
                                {
                                    var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
                                    history.Restore_UserId = _userManger.GetUserId(User);
                                    history.RestoreDate = DateTime.Now.ToUniversalTime();
                                    await _Histories.Update(history);
                                }
                            }
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }
            }

        }
        //[Authorize(Roles = "Admin,HighAdmin,Accountant,TrustAdmin")]
        //[HttpPost]
        //public async Task<IActionResult> Switch(List<long> OrderId, long BranchId = -1)
        //{
        //    if (BranchId == -1)
        //    {
        //        if (User.IsInRole("HighAdmin") || User.IsInRole("Accountant"))
        //        {
        //            var user = await _users.GetObj(x => x.Id == _userManger.GetUserId(User));
        //            //ViewBag.BranchId = user.BranchId;
        //            foreach (var item in OrderId)
        //            {
        //                var order = _orders.GetAllAsIQueryable(x => x.Id == item, null, "Client,Branch,PreviousBranch").First();
        //                if (order.Status == OrderStatus.Placed && order.DeliveryId == null && !order.IsDeleted
        //             && !order.Pending && order.BranchId == user.BranchId && (order.Client.BranchId == user.BranchId || order.TransferredConfirmed))
        //                {
        //                    order.BranchId = BranchId;
        //                    order.TransferredConfirmed = false;
        //                    order.LastUpdated = DateTime.Now.ToUniversalTime();
        //                    if (await _orders.Update(order))
        //                    {
        //                        if (order.OrderOperationHistoryId != null)
        //                        {
        //                            var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
        //                            history.Transfer_UserId = _userManger.GetUserId(User);
        //                            history.TransferDate = DateTime.Now.ToUniversalTime();
        //                            await _Histories.Update(history);
        //                            //                                    await _CRUDHistory.Update(history.Id);
        //                        }
        //                    }
        //                }
        //            }
        //            if (OrderId.Count() > 0)
        //            {
        //                await SendNotify(BranchId);
        //            }
        //            return RedirectToAction(nameof(Index));
        //        }
        //        //ViewBag.Branchs = _branch.Get(x => !x.IsDeleted).ToList();
        //        //ViewBag.OrderId = OrderId;
        //        //return View();
        //    }
        //    foreach (var item in OrderId)
        //    {
        //        var order = _orders.GetAllAsIQueryable(x => x.Id == item, null, "Client,Branch").First();
        //        if (order.Status == OrderStatus.Placed && order.DeliveryId == null && !order.IsDeleted && !order.Pending)
        //        {
        //            order.BranchId = BranchId;
        //            order.TransferredConfirmed = false;
        //            order.LastUpdated = DateTime.Now.ToUniversalTime();
        //            if (await _orders.Update(order))
        //            {
        //                if (order.OrderOperationHistoryId != null)
        //                {
        //                    var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
        //                    history.Transfer_UserId = _userManger.GetUserId(User);
        //                    history.TransferDate = DateTime.Now.ToUniversalTime();
        //                    await _Histories.Update(history);
        //                    //await _CRUDHistory.Update(history.Id);
        //                }
        //            }
        //        }
        //    }
        //    if (OrderId.Count() > 0)
        //    {
        //        await SendNotify(BranchId);
        //    }
        //    return RedirectToAction(nameof(Index));
        //}
        #endregion


        private async Task<bool> SendNotify(long BranchId, bool returned = false)
        {

            var BranchAdmins = _users.Get(x => x.UserType == UserType.HighAdmin
                 && x.BranchId == BranchId && !x.IsDeleted && !x.Branch.IsDeleted).ToList();

            var Title = $"طلبات جديده محولة للفرع";
            var Body = $"طلبات محوله في الطريق الي الفرع , تم تحويل طلبات جديده من فرع الي الفرع لديك , يرجي مراجعتها عند الوصول وتأكيد استلامها .";
            if (returned)
            {
                Title = $"مرتجعات جديده محولة للفرع";
                Body = $"مرتجعات محوله في الطريق الي الفرع , تم تحويل مرتجعات جديده من فرع الي الفرع لديك , يرجي مراجعتها عند الوصول وتأكيد استلامها .";
            }
            var send = new SendNotification(_pushNotification, _notification, _firebaseService.CaptainMessaging);
            foreach (var admin in BranchAdmins)
            {
                await send.SendToAllSpecificAndroidUserDevices(admin.Id, Title, Body, notificationType: "admin");
            }
            return true;
        }

        [Authorize(Roles = "Admin,TrustAdmin")]
        public async Task<IActionResult> EditComplete(List<string> UserIds)
        {
            var time = DateTime.Now.AddHours(-72).ToUniversalTime();
            bool auth = User.IsInRole("Client");
            string userId = null;
            if (auth)
            {
                userId = _userManger.GetUserId(User);
            }

            var orders = new List<Order>();
            if (UserIds.Count > 0)
                orders = _orderService.GetList(x => x.OrderCompleted == OrderCompleted.OK &&
                                                    x.Status == OrderStatus.Completed
                                                    && x.Finished
                                                    && !x.IsDeleted
                                                    && (UserIds.Contains(x.ClientId)) &&
                                                    (x.CompletedOn.Value.ToUniversalTime() >
                                                     DateTime.Now.AddHours(-72).ToUniversalTime())).ToList();
            else if (auth)
                orders = _orderService.GetList(x => x.OrderCompleted == OrderCompleted.OK &&
                                                    x.Status != OrderStatus.Completed
                                                    && x.Finished
                                                    && !x.IsDeleted && x.ClientId == userId).ToList();
            else
                orders = _orderService.GetList(x => x.OrderCompleted == OrderCompleted.OK &&
                                                    x.Status != OrderStatus.Completed
                                                    && x.Finished
                                                    && !x.IsDeleted && x.CompletedOn >= time).ToList();
            ViewBag.senders = _orderService.GetUsers(x => x.OrderCompleted == OrderCompleted.OK &&
                                                          x.Status != OrderStatus.Completed
                                                          && x.Finished
                                                          && !x.IsDeleted && x.CompletedOn >= time).ToList();
            return View(orders);
        }
        [Authorize(Roles = "Admin,TrustAdmin")]
        [HttpPost]
        public async Task<IActionResult> EditComplete(List<long> OrderId, List<double> DeliveryCost)
        {
            double total = 0;
            var user = await _users.GetObj(x => x.Id == "9897454b-add0-45ef-ad3b-53027814ede7");
            var orders = new List<long>();
            var wallet = new Wallet()
            {
                UserId = user.Id,
                Amount = total,
                TransactionType = TransactionType.OrderComplete,
                UserWalletLast = user.Wallet,
                AddedToAdminWallet = false
            };
            string userid = " ";
            await _wallet.Add(wallet);
            for (int i = 0; i < OrderId.Count; i++)
            {
                if (DeliveryCost[i] != -1)
                {
                    var order = await _orders.GetObj(x => x.Id == OrderId[i]);
                    order.ClientCost = DeliveryCost[i];
                    //order.Status = OrderStatus.Completed;
                    order.OrderCompleted = OrderCompleted.OK;
                    order.CompletedId = wallet.Id;
                    userid = order.ClientId;
                    total += (order.ArrivedCost - (order.ClientCost - DeliveryCost[i]));
                    user.Wallet += (order.ArrivedCost - (order.ClientCost - DeliveryCost[i]));
                    order.LastUpdated = DateTime.Now.ToUniversalTime();
                    if (await _orders.Update(order))
                    {
                        if (order.OrderOperationHistoryId != null)
                        {
                            var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
                            history.EditComplete_UserId = _userManger.GetUserId(User);
                            history.EditCompleteDate = DateTime.Now.ToUniversalTime();
                            await _Histories.Update(history);
                            // await _CRUDHistory.Update(history.Id);
                        }
                    }
                    await _users.Update(user);
                    orders.Add(order.Id);
                }
            }

            wallet.Amount = total;
            wallet.ActualUserId = userid;
            wallet.AddedToAdminWallet = true;
            await _wallet.Update(wallet);
            //var orderIdsString = string.Join(",", OrderId);
            return RedirectToAction(nameof(Print), new { orderIds = OrderId });

        }
        public async Task<IActionResult> CompletedWallet(string id)
        {
            if (User.IsInRole("Client"))
            {
                var user = await _userManger.GetUserAsync(User);
                return View(GetWallets(user.Id));
            }

            return View(GetWallets(id));
        }
        public async Task<IActionResult> CompletedReturnedWallet(string id)
        {
            if (User.IsInRole("Client"))
            {
                var user = await _userManger.GetUserAsync(User);
                return View(GetWallets(user.Id));
            }

            return View(GetWallets(id));
        }

        public IActionResult CompletedWalletView(string id)
        {
            return View(GetWallets(id));
        }

        private List<Wallet> GetWallets(string userId) => _wallet
            .Get(x => (x.TransactionType == TransactionType.OrderReturnedComplete || x.TransactionType == TransactionType.OrderComplete) && x.ActualUserId == userId)
            .OrderByDescending(x => x.Id).ToList();

        public async Task<IActionResult> PrintReceipt(long walletId)
        {
            var Data = await _wallet.GetObj(x => x.Id == walletId);
            var orders = GetWalletOrders(walletId);
            var user = await _users.GetObj(x => x.Id == Data.ActualUserId);

            Data.Orders = orders;
            Data.ActualUser = user;
            var model = new ReceiptViewModel
            {
                Code = Data.Id.ToString(),
                SenderName = Data.ActualUser.Name,
                TotalDeliveredOrders = Data.Orders.Count(),
                SettlementDate = Data.CreateOn,
                TotalAmount = Data.TransactionType == TransactionType.OrderComplete ? Data.Orders.Sum(x => x.ClientCost) : Data.TransactionType == TransactionType.OrderReturnedComplete ? Data.Orders.Sum(x => x.ReturnedCost).Value : 0,
                type = Data.TransactionType
            };
            return View(model);
        }
        public IActionResult WalletOrders(long walletId)
        {
            return View(GetWalletOrders(walletId));
        }
        public IActionResult WalletReturnedOrders(long walletId)
        {
            return View(GetWalletOrders(walletId));
        }

        public IActionResult WalletOrdersView(long walletId)
        {
            return View(GetWalletOrders(walletId));
        }

        private List<Order> GetWalletOrders(long walletId) =>
            _orderService.GetList(c => c.CompletedId == walletId || c.ReturnedCompletedId == walletId).ToList();

        #region Edit Order
        [Authorize(Roles = "Admin,TrustAdmin,HighAdmin,Accountant,TrackingAdmin")]
        public async Task<IActionResult> Edit(long id)
        {
            ViewBag.Branchs = _branch.Get(x => !x.IsDeleted).ToList();

            if (!await _orders.IsExist(x => x.Id == id))
            {
                return NotFound();
            }

            ViewBag.Title = "تعديل طلب";
            return View(_orders.Get(x => x.Id == id).First());
        }
        [Authorize(Roles = "Admin,HighAdmin,TrustAdmin,Accountant,TrackingAdmin")]
        [HttpPost]
        public async Task<IActionResult> Edit(Order model)
        {
            ViewBag.Branchs = _branch.Get(x => !x.IsDeleted).ToList();

            if (!ModelState.IsValid)
            {
                return BadRequest("حدث خطاء اثناء ادخال البيانات");
            }

            model.TotalCost = model.Cost + model.DeliveryFees;
            model.LastUpdated = DateTime.Now.ToUniversalTime();

            if (!await _orders.Update(model))
            {
                return BadRequest("من فضل حاول في وقتاً أخر");
            }

            await _CRUD.Update(model.Id);
            if (model.OrderOperationHistoryId != null)
            {
                var history = await _Histories.GetObj(x => x.Id == model.OrderOperationHistoryId);
                history.Edit_UserId = _userManger.GetUserId(User);
                history.EditDate = DateTime.Now.ToUniversalTime();
                await _Histories.Update(history);
                // await _CRUDHistory.Update(history.Id);
            }
            return RedirectToAction(nameof(Index));
        }
        #endregion

        #region Update Order Status
        [Authorize(Roles = "Admin,HighAdmin,Accountant,TrustAdmin")]
        public async Task<IActionResult> EditStatus(long id)
        {
            ViewBag.Branchs = _branch.Get(x => !x.IsDeleted).ToList();

            if (!await _orders.IsExist(x => x.Id == id && x.Status != OrderStatus.PartialReturned))
            {
                return NotFound();
            }

            ViewBag.Title = "تعديل حالة طلب";
            return View(_orders.Get(x => x.Id == id).First());
        }
        [Authorize(Roles = "Admin,HighAdmin,Accountant,TrustAdmin")]
        [HttpPost]
        public async Task<IActionResult> EditStatus(Order model, string NewNotes)
        {
            ViewBag.Branchs = _branch.Get(x => !x.IsDeleted).ToList();
            var order = _orders.Get(x => x.Id == model.Id).First();
            order.Status = OrderStatus.Placed;
            if (!string.IsNullOrWhiteSpace(NewNotes))
            {
                await _orderNotes.Add(new OrderNotes()
                {
                    Content = NewNotes,
                    OrderId = order.Id,
                    UserId = order.DeliveryId
                });
            }
            if (!ModelState.IsValid)
            {
                return BadRequest("حدث خطاء اثناء ادخال البيانات");
            }
            if (model.Status == OrderStatus.Placed)
            {

                order.DeliveryId = null;
                order.Finished = false;
                order.WalletId = null;
                order.CompletedId = null;
                order.OrderCompleted = OrderCompleted.NOK;
            }
            if (!await _orders.Update(order))
            {
                return BadRequest("من فضل حاول في وقتاً أخر");
            }

            await _CRUD.Update(order.Id);
            if (order.OrderOperationHistoryId != null)
            {
                var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
                history.Edit_UserId = _userManger.GetUserId(User);
                history.EditDate = DateTime.Now.ToUniversalTime();
                await _Histories.Update(history);
                // await _CRUDHistory.Update(history.Id);
            }
            return RedirectToAction(nameof(Index));
        }
        #endregion
        [Authorize(Roles = "Admin,TrustAdmin")]
        public async Task<IActionResult> EditBranch(long id)
        {
            if (!await _orders.IsExist(x => x.Id == id))
            {
                return NotFound();
            }
            var order = _orders.Get(x => x.Id == id).First();
            ViewBag.Branchs = _branch.Get(x => !x.IsDeleted && x.Id != order.BranchId).ToList();

            // if (User.IsInRole("HighAdmin")||User.IsInRole("Accountant"))
            //{
            //    var user = await _users.GetObj(x => x.Id == _userManger.GetUserId(User));
            //}
            if (order.Status == OrderStatus.Placed && order.DeliveryId == null && !order.IsDeleted && !order.Pending)
            {
                ViewBag.Title = "تعديل طلب";
                return View(_orders.Get(x => x.Id == id).First());
            }
            return RedirectToAction(nameof(Index));
        }
        [Authorize(Roles = "Admin,TrustAdmin")]
        [HttpPost]
        public async Task<IActionResult> EditBranch(Order order)
        {
            ViewBag.Branchs = _branch.Get(x => !x.IsDeleted).ToList();

            if (!ModelState.IsValid)
            {
                return BadRequest("حدث خطاء اثناء ادخال البيانات");
            }
            if (order.Status == OrderStatus.Placed && order.DeliveryId == null && !order.IsDeleted && !order.Pending)
            {
                if (order.BranchId != order.Client.BranchId)
                {
                    order.TransferredConfirmed = false;
                }
                if (!await _orders.Update(order))
                {
                    return BadRequest("من فضلك حاول في وقتاً أخر");
                }
                await _CRUD.Update(order.Id);
                if (order.OrderOperationHistoryId != null)
                {
                    var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
                    history.Transfer_UserId = _userManger.GetUserId(User);
                    history.TransferDate = DateTime.Now.ToUniversalTime();
                    await _Histories.Update(history);
                    //await _CRUDHistory.Update(history.Id);
                }
            }
            return RedirectToAction(nameof(Index));
        }
        [Authorize]
        public IActionResult Reviews(long id)
        {
            return View(_notes
                .GetAllAsIQueryable(x => x.OrderId == id && !x.IsDeleted, null, "Order,User,Order.Delivery").ToList());
        }
        [Authorize(Roles = "Admin,HighAdmin,Accountant,Client,TrustAdmin,TrackingAdmin")]
        [HttpPost]
        public async Task<IActionResult> Create(Order model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("حدث خطاء اثناء ادخال البيانات");
            }

            if (User.IsInRole("Client"))
                model.Pending = true;
            // استلام مخزن: لو الحساب اللي بيدخل الطلب مفعّل عليه التوجيه للمخزن، الطلب يروح لانتظار الاستلام.
            // بنستخدم Pending (عشان يتحجز زي أي طلب معلّق) + WarehousePending (مُميِّز يفصله عن موافقة الراسل).
            var creator = await _users.GetObj(x => x.Id == _userManger.GetUserId(User));
            if (creator != null && creator.OrdersGoToWarehousePending)
            {
                model.Pending = true;
                model.WarehousePending = true;
            }
            model.TotalCost = model.Cost + model.DeliveryFees;
            model.Status = OrderStatus.Placed;
            model.BranchId = (await _users.GetObj(x => x.Id == model.ClientId)).BranchId;
            if (!await _orders.Add(model))
            {
                return BadRequest("من فضلك حاول لاحقاً");
            }
            OrderOperationHistory history = new OrderOperationHistory()
            {
                OrderId = model.Id,
                Create_UserId = _userManger.GetUserId(User),
                CreateDate = model.CreateOn,
            };




            if (!await _Histories.Add(history))
            {
                if (!await _Histories.Add(history))
                {
                    return BadRequest("من فضلك حاول لاحقاً");
                }
            }
            model.OrderOperationHistoryId = history.Id;

            //string datetoday = DateTime.Now.ToString("ddMMyyyy");
            model.Code = "Tas" + /*datetoday +*/ model.Id.ToString();
            model.BarcodeImage = getBarcode(model.Code);

            model.LastUpdated = model.CreateOn;
            if (!await _orders.Update(model))
            {
                if (!await _orders.Update(model))
                {
                    return BadRequest("من فضل حاول في وقتاً أخر");
                }
            }

            await _CRUD.Update(model.Id);
            #region notification

            if (User.IsInRole("Client"))
            {
                var BranchAdmins = _users.Get(x => x.UserType == UserType.HighAdmin
                  && x.BranchId == model.BranchId && !x.IsDeleted && !x.Branch.IsDeleted).ToList();

                var id = _userManger.GetUserId(User);
                var user = _users.Get(x => x.Id == id).First();
                var Title = $"طلب جديد للراسل : {user.Name}";
                var Body = $"قام الراسل : {user.Name}  برفع طلب جديد , وكود الطلب هو : {model.Code}";

                var send = new SendNotification(_pushNotification, _notification, _firebaseService.CaptainMessaging);
                foreach (var admin in BranchAdmins)
                {
                    await send.SendToAllSpecificAndroidUserDevices(admin.Id, Title, Body, notificationType: "admin");
                }
            }

            #endregion

            return RedirectToAction(nameof(Index));
        }
        //[Authorize(Roles = "Admin,HighAdmin,Accountant,TrustAdmin")]
        [Authorize(Roles = "Admin")]
        [HttpDelete]
        public async Task<IActionResult> Delete(long id)
        {
            if (!await _orders.IsExist(x => x.Id == id))
            {
                return Json(new { success = false, message = "هذه الطلب غير موجوده" });
            }
            else
            {
                if (!await _CRUD.ToggleDelete(id))
                {
                    return Json(new { success = false, message = "حدث خطاء ما من فضلك حاول لاحقاً " });
                }

                var order = _orders.Get(x => x.Id == id).First();
                if (_orders.Get(x => x.Id == id).First().IsDeleted)
                {
                    if (order.OrderOperationHistoryId != null)
                    {
                        var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
                        history.Delete_UserId = _userManger.GetUserId(User);
                        history.DeleteDate = DateTime.Now.ToUniversalTime();
                        await _Histories.Update(history);
                    }
                    return Json(new
                    { success = true, message = "تم حذف الطلب بنجاح لاسترجاعة قم بالتوجهه الطلبات المحذوفة " });
                }
                else
                {
                    if (order.OrderOperationHistoryId != null)
                    {
                        var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
                        history.Restore_UserId = _userManger.GetUserId(User);
                        history.RestoreDate = DateTime.Now.ToUniversalTime();
                        await _Histories.Update(history);
                    }
                    return Json(new { success = true, message = "تم استراجاع الطلب بنجاح " });
                }
            }
        }
        [Authorize(Roles = "Admin,HighAdmin,Accountant,TrustAdmin")]
        public async Task<IActionResult> Accept(long id)
        {
            if (!await _orders.IsExist(x => x.Id == id))
            {
                return BadRequest("هذه الطلب غير موجوده");
            }
            else
            {
                var order = await _orders.GetObj(x => x.Id == id);
                order.Pending = false;
                order.LastUpdated = DateTime.Now.ToUniversalTime();

                await _orders.Update(order);
                if (order.OrderOperationHistoryId != null)
                {
                    var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
                    history.Accept_UserId = _userManger.GetUserId(User);
                    history.AcceptDate = DateTime.Now.ToUniversalTime();
                    await _Histories.Update(history);
                }

                return RedirectToAction(nameof(Index));
            }
        }
        public async Task<IActionResult> _TotalPrice(string searchStr, string q, long BranchId = -1)
        {
            bool auth = User.IsInRole("Client");
            string userId = null;
            if (auth)
            {
                userId = _userManger.GetUserId(User);
            }

            Expression<Func<Order, bool>> filter = null;


            if (!string.IsNullOrEmpty(searchStr))
                searchStr = searchStr.ToLower();
            if (q == "deleted")
            {
                filter = f => f.IsDeleted &&
                              ((string.IsNullOrEmpty(searchStr) ? true : f.ClientPhone.Contains(searchStr))
                               || (string.IsNullOrEmpty(searchStr) ? true : f.Code.Contains(searchStr)))
                              && (BranchId == -1
                                  ? true
                                  : f.BranchId == BranchId
                                    && (auth ? f.ClientId == userId && f.Status != OrderStatus.PartialReturned : true));
            }
            else if (q == "placed")
            {
                filter = f => f.Status == OrderStatus.Placed && !f.IsDeleted &&
                              !f.Pending &&
                              ((string.IsNullOrEmpty(searchStr) ? true : f.ClientPhone.Contains(searchStr))
                               || (string.IsNullOrEmpty(searchStr) ? true : f.Code.Contains(searchStr)))
                              && (BranchId == -1 ? true : f.BranchId == BranchId)
                              && (auth ? f.ClientId == userId && f.Status != OrderStatus.PartialReturned : true);
            }
            else if (q == "ass")
            {
                filter = f => f.Status == OrderStatus.Assigned && !f.IsDeleted &&
                              f.OrderCompleted == OrderCompleted.NOK && !f.Finished &&
                              ((string.IsNullOrEmpty(searchStr) ? true : f.ClientPhone.Contains(searchStr))
                               || (string.IsNullOrEmpty(searchStr) ? true : f.Code.Contains(searchStr)))
                              && (BranchId == -1 ? true : f.BranchId == BranchId)
                              && (auth ? f.ClientId == userId && f.Status != OrderStatus.PartialReturned : true);
            }
            else if (q == "wai")
            {
                filter = f => f.Status == OrderStatus.Waiting && !f.IsDeleted &&
                              f.OrderCompleted == OrderCompleted.NOK && !f.Finished &&
                              ((string.IsNullOrEmpty(searchStr) ? true : f.ClientPhone.Contains(searchStr))
                               || (string.IsNullOrEmpty(searchStr) ? true : f.Code.Contains(searchStr)))
                              && (BranchId == -1 ? true : f.BranchId == BranchId)
                              && (auth ? f.ClientId == userId && f.Status != OrderStatus.PartialReturned : true);
            }
            else if (q == "com")
            {
                filter = f => !f.IsDeleted && f.Finished &&
                              f.OrderCompleted == OrderCompleted.OK &&
                              ((string.IsNullOrEmpty(searchStr) ? true : f.ClientPhone.Contains(searchStr))
                               || (string.IsNullOrEmpty(searchStr) ? true : f.Code.Contains(searchStr)))
                              && (BranchId == -1 ? true : f.BranchId == BranchId)
                              && (auth ? f.ClientId == userId && f.Status != OrderStatus.PartialReturned : true);
            }
            else if (q == "fsh")
            {
                filter = f => f.Status != OrderStatus.Completed && f.Finished && !f.IsDeleted &&
                              f.OrderCompleted == OrderCompleted.NOK &&
                              ((string.IsNullOrEmpty(searchStr) ? true : f.ClientPhone.Contains(searchStr))
                               || (string.IsNullOrEmpty(searchStr) ? true : f.Code.Contains(searchStr)))
                              && (BranchId == -1 ? true : f.BranchId == BranchId)
                              && (auth ? f.ClientId == userId && f.Status != OrderStatus.PartialReturned : true);
            }
            else if (q == "pen")
            {
                filter = f => f.Status == OrderStatus.Placed && f.Pending &&
                              !f.IsDeleted &&
                              ((string.IsNullOrEmpty(searchStr) ? true : f.ClientPhone.Contains(searchStr))
                               || (string.IsNullOrEmpty(searchStr) ? true : f.Code.Contains(searchStr)))
                              && (BranchId == -1 ? true : f.BranchId == BranchId)
                              && (auth ? f.ClientId == userId && f.Status != OrderStatus.PartialReturned : true);
            }
            else if (q == "returned")
            {
                filter = f => (f.Status == OrderStatus.Returned || f.Status == OrderStatus.PartialReturned) &&
                              !f.IsDeleted &&
                              ((string.IsNullOrEmpty(searchStr) ? true : f.ClientPhone.Contains(searchStr))
                               || (string.IsNullOrEmpty(searchStr) ? true : f.Code.Contains(searchStr)))
                              && (BranchId == -1 ? true : f.BranchId == BranchId)
                              && (auth ? f.ClientId == userId && f.Status != OrderStatus.PartialReturned : true);
            }
            else if (q == "rej")
            {
                filter = f => f.Status != OrderStatus.Rejected && !f.IsDeleted &&
                              f.OrderCompleted == OrderCompleted.NOK && !f.Finished &&
                              ((string.IsNullOrEmpty(searchStr) ? true : f.ClientPhone.Contains(searchStr))
                               || (string.IsNullOrEmpty(searchStr) ? true : f.Code.Contains(searchStr)))
                              && (BranchId == -1 ? true : f.BranchId == BranchId)
                              && (auth ? f.ClientId == userId && f.Status != OrderStatus.PartialReturned : true);
            }
            else
            {
                filter = f => !f.IsDeleted &&
                              ((string.IsNullOrEmpty(searchStr) ? true : f.ClientPhone.Contains(searchStr))
                               || (string.IsNullOrEmpty(searchStr) ? true : f.Code.Contains(searchStr)))
                              && (BranchId == -1 ? true : f.BranchId == BranchId)
                              && (auth ? f.ClientId == userId && f.Status != OrderStatus.PartialReturned : true);
            }

            var orders = _orders.Get(filter).ToList();
            var totalPrice = orders.Sum(x => x.Cost + x.DeliveryCost);
            return PartialView(totalPrice);
        }
        public System.IO.Stream OutputStream { get; }
        public IActionResult ExportToExecl(List<long> OrdersId,
            bool showProductName = false, bool showSenderPhone = false, bool showSenderName = false,
            bool showOrderCost = false, bool showDeliveryFees = false, bool showClientCode = false,
            bool showStatus = false, bool useCustomColumns = false)
        {
            List<Order> Orders = new List<Order>();
            bool auth = User.IsInRole("Client");
            if (OrdersId != null)
            {
                for (var i = 0; i < OrdersId.Count; i++)
                {
                    if (auth)
                        Orders.Add(_orderService.GetList(x => x.Id == OrdersId[i] && x.ClientId == _userManger.GetUserId(User)).FirstOrDefault());
                    else
                        Orders.Add(_orderService.GetList(x => x.Id == OrdersId[i]).FirstOrDefault());
                }
            }

            DataTable dt;
            if (useCustomColumns)
            {
                var options = new ExcelOptionalColumns
                {
                    ShowProductName = showProductName,
                    ShowSenderPhone = showSenderPhone,
                    ShowSenderName = showSenderName,
                    ShowOrderCost = showOrderCost,
                    ShowDeliveryFees = showDeliveryFees,
                    ShowClientCode = showClientCode,
                    ShowStatus = showStatus
                };
                dt = ExcelExport.OrderExportWithOptions(Orders, options);
            }
            else
            {
                dt = ExcelExport.OrderExport(Orders);
            }

            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dt);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "OrdersReport-" + DateTime.Now.ToString("yyyy-MM-dd-HHmmss") + ".xlsx");
                }
            }
        }

        public async Task<IActionResult> ExportToExeclWallet(long userWallet,
            bool showProductName = false, bool showSenderPhone = false, bool showSenderName = false,
            bool showOrderCost = false, bool showDeliveryFees = false, bool showClientCode = false,
            bool useCustomColumns = false)
        {
            using (var workbook = new XLWorkbook())
            {
                var wallets = GetWalletOrders(userWallet);
                if (wallets.Count() > 0)
                {
                    var worksheet = workbook.Worksheets.Add("Orders");
                    var row = 2;

                    // Setting up header row with bold formatting
                    var headerRow = worksheet.Row(1);
                    headerRow.Style.Font.Bold = true;

                    // الأعمدة الأساسية
                    int col = 1;
                    headerRow.Cell(col++).Value = "حالة الطلب";
                    headerRow.Cell(col++).Value = "رقم الطلب";
                    headerRow.Cell(col++).Value = "التاريخ";
                    headerRow.Cell(col++).Value = "المرسل إليه";
                    headerRow.Cell(col++).Value = "تليفون المرسل إليه";
                    headerRow.Cell(col++).Value = "المحافظة";
                    headerRow.Cell(col++).Value = "العنوان";
                    headerRow.Cell(col++).Value = "المطلوب دفعه";
                    headerRow.Cell(col++).Value = "المندوب";
                    headerRow.Cell(col++).Value = "الملاحظات";

                    // الأعمدة الاختيارية
                    int colProductName = 0, colSenderPhone = 0, colSenderName = 0, colOrderCost = 0, colDeliveryFees = 0, colClientCode = 0;
                    if (!useCustomColumns || showProductName) { colProductName = col; headerRow.Cell(col++).Value = "اسم المنتج"; }
                    if (!useCustomColumns || showSenderPhone) { colSenderPhone = col; headerRow.Cell(col++).Value = "رقم تليفون الراسل"; }
                    if (!useCustomColumns || showSenderName) { colSenderName = col; headerRow.Cell(col++).Value = "اسم الراسل"; }
                    if (!useCustomColumns || showOrderCost) { colOrderCost = col; headerRow.Cell(col++).Value = "سعر الطلب"; }
                    if (!useCustomColumns || showDeliveryFees) { colDeliveryFees = col; headerRow.Cell(col++).Value = "سعر الشحن"; }
                    if (!useCustomColumns || showClientCode) { colClientCode = col; headerRow.Cell(col++).Value = "كود العميل"; }

                    // أعمدة المحفظة الإضافية
                    int colArrivedCost = col; headerRow.Cell(col++).Value = "تم تسديده";
                    int colClientCost = col; headerRow.Cell(col++).Value = "نسبة الراسل";
                    int colDriverNotes = col; headerRow.Cell(col++).Value = "ملاحظات المندوب";

                    foreach (var item in wallets)
                    {
                        string statusBadge = GetWalletStatusText(item);
                        string lastOrderNoteContent = "";

                        if (item.DeliveryId != null && item.OrderNotes.Count > 0 && !string.IsNullOrWhiteSpace(item.OrderNotes.OrderBy(x => x.Id).Last().Content))
                        {
                            lastOrderNoteContent = item.OrderNotes.OrderBy(x => x.Id).Last().Content;
                        }

                        string DeliveryName = item.DeliveryId != null && item.Delivery != null ? item.Delivery.Name : "";
                        var CreatedOn = TimeZoneInfo.ConvertTimeFromUtc(item.CreateOn, TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time"));

                        int c = 1;
                        worksheet.Cell(row, c++).Value = statusBadge;
                        worksheet.Cell(row, c++).Value = item.Code;
                        worksheet.Cell(row, c++).Value = CreatedOn.ToString("dd MMM, yyyy - hh:mm tt");
                        worksheet.Cell(row, c++).Value = item.ClientName;
                        worksheet.Cell(row, c++).Value = item.ClientPhone;
                        worksheet.Cell(row, c++).Value = item.AddressCity ?? "";
                        worksheet.Cell(row, c++).Value = item.Address ?? "";
                        worksheet.Cell(row, c++).Value = item.TotalCost;
                        worksheet.Cell(row, c++).Value = DeliveryName;
                        worksheet.Cell(row, c++).Value = item.Notes ?? "";

                        // الأعمدة الاختيارية (سعر الشحن في صفحة الطباعة = تم تسديده - نسبة الراسل، محسوب من القيم المحفوظة)
                        if (colProductName > 0) worksheet.Cell(row, colProductName).Value = "";
                        if (colSenderPhone > 0) worksheet.Cell(row, colSenderPhone).Value = item.Client?.PhoneNumber ?? "";
                        if (colSenderName > 0) worksheet.Cell(row, colSenderName).Value = item.Client?.Name ?? "";
                        if (colOrderCost > 0) worksheet.Cell(row, colOrderCost).Value = item.Cost;
                        if (colDeliveryFees > 0)
                            worksheet.Cell(row, colDeliveryFees).Value = item.OrderCompleted == OrderCompleted.OK ? (item.ArrivedCost - item.ClientCost) : item.DeliveryFees;
                        if (colClientCode > 0) worksheet.Cell(row, colClientCode).Value = item.ClientCode ?? "";

                        // أعمدة المحفظة: نسبة الراسل من DB، الباقي محسوب عند التصدير فقط
                        worksheet.Cell(row, colArrivedCost).Value = item.ArrivedCost;
                        worksheet.Cell(row, colClientCost).Value = item.ClientCost;
                        worksheet.Cell(row, colDriverNotes).Value = lastOrderNoteContent;

                        row++;
                    }

                    using (var memoryStream = new MemoryStream())
                    {
                        workbook.SaveAs(memoryStream);
                        return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "OrdersReport-" + DateTime.Now.ToString("yyyy-MM-dd-HHmmss") + ".xlsx");
                    }
                }

                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> ExportToExeclWalletForAdmins(long userWallet,
            bool showProductName = false, bool showSenderPhone = false, bool showSenderName = false,
            bool showOrderCost = false, bool showDeliveryFees = false, bool showClientCode = false,
            bool useCustomColumns = false)
        {
            using (var workbook = new XLWorkbook())
            {
                var wallets = GetWalletOrders(userWallet);
                if (wallets.Count() > 0)
                {
                    var worksheet = workbook.Worksheets.Add("Orders");
                    var row = 2;

                    // Setting up header row with bold formatting
                    var headerRow = worksheet.Row(1);
                    headerRow.Style.Font.Bold = true;

                    // الأعمدة الأساسية
                    int col = 1;
                    headerRow.Cell(col++).Value = "حالة الطلب";
                    headerRow.Cell(col++).Value = "رقم الطلب";
                    headerRow.Cell(col++).Value = "التاريخ";
                    headerRow.Cell(col++).Value = "المرسل إليه";
                    headerRow.Cell(col++).Value = "تليفون المرسل إليه";
                    headerRow.Cell(col++).Value = "المحافظة";
                    headerRow.Cell(col++).Value = "العنوان";
                    headerRow.Cell(col++).Value = "المطلوب دفعه";
                    headerRow.Cell(col++).Value = "المندوب";
                    headerRow.Cell(col++).Value = "الملاحظات";

                    // الأعمدة الاختيارية
                    int colProductName = 0, colSenderPhone = 0, colSenderName = 0, colOrderCost = 0, colDeliveryFeesOpt = 0, colClientCode = 0;
                    if (!useCustomColumns || showProductName) { colProductName = col; headerRow.Cell(col++).Value = "اسم المنتج"; }
                    if (!useCustomColumns || showSenderPhone) { colSenderPhone = col; headerRow.Cell(col++).Value = "رقم تليفون الراسل"; }
                    if (!useCustomColumns || showSenderName) { colSenderName = col; headerRow.Cell(col++).Value = "اسم الراسل"; }
                    if (!useCustomColumns || showOrderCost) { colOrderCost = col; headerRow.Cell(col++).Value = "سعر الطلب"; }
                    if (!useCustomColumns || showDeliveryFees) { colDeliveryFeesOpt = col; headerRow.Cell(col++).Value = "سعر الشحن"; }
                    if (!useCustomColumns || showClientCode) { colClientCode = col; headerRow.Cell(col++).Value = "كود العميل"; }

                    // أعمدة المحفظة الإضافية (للأدمن)
                    int colArrivedCost = col; headerRow.Cell(col++).Value = "تم تسديده";
                    int colClientCost = col; headerRow.Cell(col++).Value = "نسبة الراسل";
                    int colDriverNotes = col; headerRow.Cell(col++).Value = "ملاحظات المندوب";
                    int colDriverCommission = col; headerRow.Cell(col++).Value = "عمولة المندوب";
                    int colNetProfit = col; headerRow.Cell(col++).Value = "صافي الربح";

                    foreach (var item in wallets)
                    {
                        string statusBadge = GetWalletStatusText(item);
                        string lastOrderNoteContent = "";

                        if (item.DeliveryId != null && item.OrderNotes.Count > 0 && !string.IsNullOrWhiteSpace(item.OrderNotes.OrderBy(x => x.Id).Last().Content))
                        {
                            lastOrderNoteContent = item.OrderNotes.OrderBy(x => x.Id).Last().Content;
                        }

                        string DeliveryName = item.DeliveryId != null && item.Delivery != null ? item.Delivery.Name : "";
                        var CreatedOn = TimeZoneInfo.ConvertTimeFromUtc(item.CreateOn, TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time"));

                        int c = 1;
                        worksheet.Cell(row, c++).Value = statusBadge;
                        worksheet.Cell(row, c++).Value = item.Code;
                        worksheet.Cell(row, c++).Value = CreatedOn.ToString("dd MMM, yyyy - hh:mm tt");
                        worksheet.Cell(row, c++).Value = item.ClientName;
                        worksheet.Cell(row, c++).Value = item.ClientPhone;
                        worksheet.Cell(row, c++).Value = item.AddressCity ?? "";
                        worksheet.Cell(row, c++).Value = item.Address ?? "";
                        worksheet.Cell(row, c++).Value = item.TotalCost;
                        worksheet.Cell(row, c++).Value = DeliveryName;
                        worksheet.Cell(row, c++).Value = item.Notes ?? "";

                        // الأعمدة الاختيارية: نسبة الراسل من DB، تكلفة الشحن = تم تسديده - نسبة الراسل، والربح منها
                        if (colProductName > 0) worksheet.Cell(row, colProductName).Value = "";
                        if (colSenderPhone > 0) worksheet.Cell(row, colSenderPhone).Value = item.Client?.PhoneNumber ?? "";
                        if (colSenderName > 0) worksheet.Cell(row, colSenderName).Value = item.Client?.Name ?? "";
                        if (colOrderCost > 0) worksheet.Cell(row, colOrderCost).Value = item.Cost;
                        if (colDeliveryFeesOpt > 0)
                            worksheet.Cell(row, colDeliveryFeesOpt).Value = item.OrderCompleted == OrderCompleted.OK ? (item.ArrivedCost - item.ClientCost) : item.DeliveryFees;
                        if (colClientCode > 0) worksheet.Cell(row, colClientCode).Value = item.ClientCode ?? "";

                        // أعمدة المحفظة: نسبة الراسل من DB، تكلفة الشحن والربح محسوبان عند التصدير فقط
                        worksheet.Cell(row, colArrivedCost).Value = item.ArrivedCost;
                        worksheet.Cell(row, colClientCost).Value = item.ClientCost;
                        worksheet.Cell(row, colDriverNotes).Value = lastOrderNoteContent;
                        worksheet.Cell(row, colDriverCommission).Value = item.DeliveryCost;
                        double calculatedDeliveryFees = item.OrderCompleted == OrderCompleted.OK ? (item.ArrivedCost - item.ClientCost) : item.DeliveryFees;
                        double netProfit = calculatedDeliveryFees - item.DeliveryCost;
                        worksheet.Cell(row, colNetProfit).Value = netProfit;
                        row++;
                    }

                    using (var memoryStream = new MemoryStream())
                    {
                        workbook.SaveAs(memoryStream);
                        return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "OrdersReport-" + DateTime.Now.ToString("yyyy-MM-dd-HHmmss") + ".xlsx");
                    }
                }

                return RedirectToAction("Index");
            }
        }

        /// <summary>
        /// دالة مساعدة لتحويل حالة الطلب لنص عربي (تُستخدم في تصدير المحفظة)
        /// </summary>
        private string GetWalletStatusText(Order item)
        {
            if (item.IsDeleted) return "محذوف";
            return item.Status switch
            {
                OrderStatus.Placed => "جديد",
                OrderStatus.Assigned => "جارى التوصيل",
                OrderStatus.Delivered => "تم التوصيل",
                OrderStatus.PartialDelivered => "تم التوصيل جزئي",
                OrderStatus.PartialReturned => "مرتجع جزئي",
                OrderStatus.Rejected => "مرفوض",
                OrderStatus.Waiting => "مؤجل",
                OrderStatus.Completed => "تم تسويته",
                OrderStatus.Returned_And_DeliveryCost_On_Sender => "مرتجع وشحن على الراسل",
                OrderStatus.Returned_And_Paid_DeliveryCost => "مرتجع ودفع شحن",
                OrderStatus.Delivered_With_Edit_Price => "تم التوصيل مع تعديل السعر",
                OrderStatus.Returned => "مرتجع كامل",
                _ => ""
            };
        }

        public async Task<IActionResult> FinishOrder(DriverSubmitOrderDto model)
        {
            // تحسين الأداء: جلب الطلب مع بيانات المندوب في استعلام واحد بدلاً من 3 استعلامات منفصلة
            var order = await _orders.GetSingle(
                x => x.Id == model.OrderId && !x.IsDeleted,
                includeProperties: "Delivery");

            if (order == null)
            {
                return BadRequest("لا يوجد طلب ");
            }

            var image = HttpContext.Request.Form.Files.FirstOrDefault(f => f.Name == "OrderImage");

            string Returned_Image = "";
            if (image != null)
            {
                Returned_Image = await MediaControl.Upload(FilePath.OrderReturns, image, _webHostEnvironment);
            }
            //عشان نشيلها من اي تقفيله قبل كده
            order.WalletId = null;
            ///

            var user = order.Delivery; // استخدام البيانات المحملة مسبقاً بدلاً من استعلام جديد
            switch (model.Status)
            {
                case OrderStatus.Delivered:
                    order.ArrivedCost = order.TotalCost;
                    order.DeliveryCost = user.DeliveryCost != null ? user.DeliveryCost.Value : 0;
                    order.Status = model.Status;
                    await _orderNotes.Add(new OrderNotes()
                    {
                        Content = model.Note,
                        OrderId = order.Id,
                        UserId = order.DeliveryId
                    });
                    break;
                case OrderStatus.Delivered_With_Edit_Price:
                    order.ArrivedCost = model.Paid;
                    order.DeliveryCost = user.DeliveryCost != null ? user.DeliveryCost.Value : 0;
                    order.Status = model.Status;
                    await _orderNotes.Add(new OrderNotes()
                    {
                        Content = model.Note,
                        OrderId = order.Id,
                        UserId = order.DeliveryId
                    });
                    break;
                case OrderStatus.Returned_And_DeliveryCost_On_Sender:
                case OrderStatus.Returned_And_Paid_DeliveryCost when model.Paid == 0:
                    order.ArrivedCost = 0;
                    order.DeliveryCost = user.DeliveryCost != null ? user.DeliveryCost.Value : 0;
                    order.Status = model.Status;
                    order.ReturnedCost = order.Cost;
                    await _orderNotes.Add(new OrderNotes()
                    {
                        Content = model.Note,
                        OrderId = order.Id,
                        UserId = order.DeliveryId
                    });
                    break;
                case OrderStatus.Returned_And_Paid_DeliveryCost:
                    order.ArrivedCost = model.Paid;
                    order.DeliveryCost = user.DeliveryCost != null ? user.DeliveryCost.Value : 0;
                    order.Status = model.Status;
                    order.ReturnedCost = order.Cost;
                    await _orderNotes.Add(new OrderNotes()
                    {
                        Content = model.Note,
                        OrderId = order.Id,
                        UserId = order.DeliveryId
                    });
                    break;
                case OrderStatus.PartialDelivered:
                    order.Status = model.Status;
                    order.DeliveryCost = user.DeliveryCost != null ? user.DeliveryCost.Value : 0;
                    order.ArrivedCost = model.Paid;
                    await _orderNotes.Add(new OrderNotes()
                    {
                        Content = model.Note,
                        OrderId = order.Id,
                        UserId = order.DeliveryId
                    });
                    Order PartialReturned;
                    //هنعمل طلب عكسي مرتجع جزئي بنفس الكود + R
                    if (!await _orders.IsExist(x => x.Code == 'R' + order.Code && !x.IsDeleted))
                    {
                        PartialReturned = new Order()
                        {
                            Code = 'R' + order.Code,
                            Notes = order.Notes,
                            AddressCity = order.AddressCity,
                            Address = order.Address,
                            ClientCode = order.ClientCode,
                            ClientName = order.ClientName,
                            ClientPhone = order.ClientPhone,
                            ClientSecondaryPhone = order.ClientSecondaryPhone,
                            Cost = order.Cost,
                            DeliveryFees = order.DeliveryFees,
                            TotalCost = order.TotalCost,
                            Pending = order.Pending,
                            TransferredConfirmed = order.TransferredConfirmed,
                            PendingReturnTransferrConfirmed = order.PendingReturnTransferrConfirmed,
                            ArrivedCost = order.ArrivedCost,
                            DeliveryCost = 0,
                            ReturnedCost = order.TotalCost - order.ArrivedCost,
                            Finished = order.Finished,
                            Status = OrderStatus.PartialReturned,
                            Returned_Image = Returned_Image,
                            OrderCompleted = order.OrderCompleted,
                            ClientId = order.ClientId,
                            LastUpdated = DateTime.Now.ToUniversalTime(),
                            WalletId = order.WalletId,
                            DeliveryId = order.DeliveryId,
                            BranchId = order.BranchId,
                            PreviousBranchId = order.PreviousBranchId,
                            OrderOperationHistoryId = order.OrderOperationHistoryId,
                            ReturnedFinished = order.ReturnedFinished,
                            ReturnedOrderCompleted = order.ReturnedOrderCompleted,
                            ReturnedWalletId = order.ReturnedWalletId,
                            ReturnedCompletedId = order.ReturnedCompletedId,

                        };
                        await _orders.Add(PartialReturned);
                    }
                    else
                    {
                        PartialReturned = (await _orders.GetObj(x => x.Code == 'R' + order.Code));
                        PartialReturned.ArrivedCost = order.ArrivedCost;
                        PartialReturned.ReturnedCost = order.TotalCost - order.ArrivedCost;
                        await _orders.Update(PartialReturned);
                    }
                    await _orderNotes.Add(new OrderNotes()
                    {
                        Content = model.Note,
                        OrderId = PartialReturned.Id,
                        UserId = order.DeliveryId
                    });
                    OrderOperationHistory history = new OrderOperationHistory()
                    {
                        OrderId = PartialReturned.Id,
                        Create_UserId = _userManger.GetUserId(User),
                        CreateDate = PartialReturned.CreateOn,
                    };
                    if (!await _Histories.Add(history))
                    {
                        return BadRequest("من فضلك حاول لاحقاً");
                    }
                    PartialReturned.OrderOperationHistoryId = history.Id;
                    if (!await _orders.Update(PartialReturned))
                    {
                        return BadRequest("من فضل حاول في وقتاً أخر");
                    }
                    //
                    break;
                case OrderStatus.Returned:
                    order.ArrivedCost = 0;
                    order.DeliveryCost = 0;
                    order.ReturnedCost = order.TotalCost;
                    order.Status = model.Status;
                    await _orderNotes.Add(new OrderNotes()
                    {
                        Content = model.Note,
                        OrderId = order.Id,
                        UserId = order.DeliveryId
                    });
                    break;
                default:
                    order.ArrivedCost = model.Paid;
                    order.DeliveryCost = 0;
                    order.ReturnedCost = order.TotalCost - order.ArrivedCost;
                    order.Status = model.Status;
                    await _orderNotes.Add(new OrderNotes()
                    {
                        Content = model.Note,
                        OrderId = order.Id,
                        UserId = order.DeliveryId
                    });
                    break;
            }

            order.Returned_Image = Returned_Image == "" ? null : Returned_Image;

            await _orders.Update(order);

            // تحسين الأداء: إرسال الإشعارات في الخلفية (Fire-and-forget) لتسريع الاستجابة
            var deliveryId = order.DeliveryId;
            var orderNote = model.Note;
            var returnedImage = order.Returned_Image;
            var orderStatus = model.Status;
            var currentUserId = _userManger.GetUserId(User);
            var orderId = order.Id;

            // WhatsApp notification - separate Task with its own scope
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var wapilotService = scope.ServiceProvider.GetRequiredService<IWapilotService>();

                    var statusNote = $"تم تحديث حالة الطلب إلى: {GetStatusInArabic(orderStatus)}";
                    if (!string.IsNullOrWhiteSpace(orderNote))
                    {
                        statusNote += $"\nملاحظة: {orderNote}";
                    }
                    await wapilotService.EnqueueOrderStatusUpdateByIdAsync(orderId, currentUserId, statusNote);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"WhatsApp Error: {ex.Message}");
                }
            });

            // Push notification - separate Task
            _ = Task.Run(async () =>
            {
                try
                {
                    await SendNotify(order, user, orderNote, returnedImage);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Push Error: {ex.Message}");
                }
            });

            return RedirectToAction(nameof(PrintClientOrders), new { Id = deliveryId, message = "تم تنفيذ العملية بنجاح" });
        }

        private string GetStatusInArabic(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Placed => "جديد",
                OrderStatus.Assigned => "جارى التوصيل",
                OrderStatus.Delivered => "تم التوصيل",
                OrderStatus.Waiting => "مؤجل",
                OrderStatus.Rejected => "مرفوض",
                OrderStatus.Finished => "منتهي",
                OrderStatus.Completed => "تم تسويته",
                OrderStatus.PartialDelivered => "تم التوصيل جزئي",
                OrderStatus.Returned => "مرتجع كامل",
                OrderStatus.PartialReturned => "مرتجع جزئي",
                OrderStatus.Delivered_With_Edit_Price => "تم التوصيل مع تعديل السعر",
                OrderStatus.Returned_And_Paid_DeliveryCost => "مرتجع ودفع شحن",
                OrderStatus.Returned_And_DeliveryCost_On_Sender => "مرتجع وشحن على الراسل",
                _ => status.ToString()
            };
        }
        public async Task<IActionResult> FinishAllOrders(DriverSubmitOrdersDto model)
        {

            if (model.OrdersIds != null)
            {
                foreach (var id in model.OrdersIds)
                {

                    if (await _orders.IsExist(x => x.Id == id && !x.IsDeleted))
                    {
                        var order = (await _orders.GetObj(x => x.Id == id));
                        //عشان نشيلها من اي تقفيله قبل كده
                        order.WalletId = null;
                        ///

                        var user = await _users.GetSingle(x => x.Id == order.DeliveryId);
                        switch (model.Status)
                        {
                            case OrderStatus.Delivered:
                                order.ArrivedCost = order.TotalCost;
                                order.DeliveryCost = user.DeliveryCost != null ? user.DeliveryCost.Value : 0;
                                order.Status = model.Status;
                                break;

                            case OrderStatus.Waiting:
                                order.ArrivedCost = 0;
                                order.DeliveryCost = 0;
                                order.Status = model.Status;
                                break;
                            default:
                                break;
                        }
                        if (order.Status == OrderStatus.Delivered || order.Status == OrderStatus.Waiting)
                        {
                            if (!string.IsNullOrWhiteSpace(model.Note))
                            {
                                await _orderNotes.Add(new OrderNotes()
                                {
                                    Content = model.Note,
                                    OrderId = order.Id,
                                    UserId = order.DeliveryId
                                });
                            }

                            await _orders.Update(order);
                            await SendNotify(order, user, model.Note, order.Returned_Image);
                        }
                    }
                }
                return RedirectToAction(nameof(PrintClientOrders), new { Id = model.DeliveryId, message = "تم تنفيذ العملية بنجاح" });
            }
            return RedirectToAction(nameof(PrintClientOrders), new { Id = model.DeliveryId, message = "" });
        }
        private async Task<bool> SendNotify(Order order, ApplicationUser Captian, string note, string image = null)
        {
            var Title = $"تحديث حاله الطلب :  {order.Code} ";
            var Body = $"";
            switch (order.Status)
            {
                case OrderStatus.Delivered:
                    Body = $"تم تسليم طلبكم بنجاح. كود الطلب {order.Code}.";
                    break;
                case OrderStatus.Delivered_With_Edit_Price:
                    Body = $"تم تسليم طلبكم مع تعديل السعر. كود الطلب {order.Code}.";
                    break;
                case OrderStatus.Returned_And_DeliveryCost_On_Sender:
                    Body = $"تم إرجاع طلبكم وتكلفة التوصيل على الراسل . كود الطلب {order.Code}.";
                    break;
                case OrderStatus.Returned_And_Paid_DeliveryCost:
                    Body = $"تم إرجاع طلبكم بالكامل وتم دفع تكلفة التوصيل. كود الطلب {order.Code}.";
                    break;
                case OrderStatus.PartialDelivered:
                    Body = $"تم تسليم جزء من طلبكم. كود الطلب {order.Code}.";
                    break;
                case OrderStatus.Returned:
                    Body = $"تم إرجاع طلبكم بالكامل . كود الطلب {order.Code}.";
                    break;
                case OrderStatus.Waiting:
                    Body = $"طلبكم في حالة تأجيل . كود الطلب {order.Code}.";
                    break;
                default:
                    break;
            }
            Body += $"\n  المندوب :{Captian.Name} ," +
                $"\n رقم الهاتف : {Captian.PhoneNumber}," +
                $"\n ملاحظات المندوب : {note} .";

            var send = new SendNotification(_pushNotification, _notification, _firebaseService.CustomerMessaging);
            await send.SendToAllSpecificAndroidUserDevices(order.ClientId, Title, Body, Image: image, notificationType: "order_status");

            return true;
        }
        [HttpPost]
        public async Task<IActionResult> AddNotes(long OrderId, string NewNote)
        {
            if (!await _orders.IsExist(x => x.Id == OrderId && !x.IsDeleted))
            {
                return Json(new { success = false, message = "لا يوجد طلب" });
            }

            var order = await _orders.GetObj(x => x.Id == OrderId);

            await _orderNotes.Add(new OrderNotes()
            {
                Content = NewNote,
                OrderId = order.Id,
                UserId = order.DeliveryId
            });

            await _orders.Update(order);

            return Json(new { success = true, message = "تم حفظ الملاحظات بنجاح", deliveryId = order.DeliveryId });
        }

        public byte[] getBarcode(string Code)
        {
            //            id += 1000;
            var barcodeWriter = new ZXing.BarcodeWriter
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Height = 50,
                    Width = 175
                }
            };
            using var barcodeBitmap = barcodeWriter.Write(Code);
            using var ms = new MemoryStream();
            barcodeBitmap.Save(ms, ImageFormat.Png);
            var barcodeImage = ms.ToArray();
            return barcodeImage;
        }
        #region PAGINATION METHODS

        [Authorize]
        public async Task<PagedList<Order>> GetPagedListItems(string searchStr, int pageNumber, int pageSize, string q,
            long BranchId = -1)
        {
            bool auth = User.IsInRole("Client");
            string userId = null;
            if (auth)
            {
                userId = _userManger.GetUserId(User);
            }

            Expression<Func<Order, bool>> filter = null;
            Func<IQueryable<Order>, IOrderedQueryable<Order>> orderBy = o => o.OrderByDescending(c => c.Id);

            if (!string.IsNullOrEmpty(searchStr) || q != null)
            {
                if (!string.IsNullOrEmpty(searchStr))
                    searchStr = searchStr.ToLower();
                if (q == "deleted")
                {
                    filter = f => f.IsDeleted &&
                                  ((string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.ClientPhone.Contains(searchStr))
                                    || (string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.ClientCode.Contains(searchStr))
                                    || (string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.Code.Contains(searchStr)))
                                  && ((BranchId == -1
                                      ? true
                                      : f.BranchId == BranchId
                                        && (auth ? f.ClientId == userId : true)) || (BranchId == -1
                                      ? true
                                      : f.Client.BranchId == BranchId
                                        && (auth ? f.ClientId == userId : true)) || (BranchId == -1
                                      ? true
                                      : f.PreviousBranchId == BranchId && !f.TransferredConfirmed
                                        && (auth ? f.ClientId == userId : true)));
                }
                else if (q == "placed")
                {
                    filter = f => f.Status == OrderStatus.Placed && !f.IsDeleted &&
                                  !f.Pending &&
                                  ((string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.ClientPhone.Contains(searchStr))
                                    || (string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.ClientCode.Contains(searchStr))
                                   || (string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.Code.Contains(searchStr)))
                                  && ((BranchId == -1 ? true : f.BranchId == BranchId) || (BranchId == -1 ? true : f.Client.BranchId == BranchId) || (BranchId == -1 ? true : f.PreviousBranchId == BranchId && !f.TransferredConfirmed))
                                  && (auth ? f.ClientId == userId /*&& f.Status != OrderStatus.PartialReturned*/: true);
                }
                else if (q == "fin")
                {
                    filter = f => (f.Status == OrderStatus.PartialDelivered || f.Status == OrderStatus.Delivered || f.Status == OrderStatus.Delivered_With_Edit_Price) && !f.IsDeleted && f.OrderCompleted == OrderCompleted.NOK &&
                                  ((string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.ClientPhone.Contains(searchStr))
                                    || (string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.ClientCode.Contains(searchStr))
                                   || (string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.Code.Contains(searchStr)))
                                  && ((BranchId == -1 ? true : f.BranchId == BranchId) || (BranchId == -1 ? true : f.Client.BranchId == BranchId) || (BranchId == -1 ? true : f.PreviousBranchId == BranchId && !f.TransferredConfirmed))
                                  && (auth ? f.ClientId == userId /*&& f.Status != OrderStatus.PartialReturned*/: true);
                }
                else if (q == "ass")
                {
                    filter = f => f.Status == OrderStatus.Assigned && !f.IsDeleted && f.DeliveryId != null &&
                                  f.OrderCompleted == OrderCompleted.NOK && !f.Finished &&
                                  ((string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.ClientPhone.Contains(searchStr))
                                    || (string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.ClientCode.Contains(searchStr))
                                    || (string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.Code.Contains(searchStr)))
                                  && ((BranchId == -1 ? true : f.BranchId == BranchId) || (BranchId == -1 ? true : f.Client.BranchId == BranchId) || (BranchId == -1 ? true : f.PreviousBranchId == BranchId && !f.TransferredConfirmed))
                                  && (auth ? f.ClientId == userId /*&& f.Status != OrderStatus.PartialReturned*/: true);
                }
                else if (q == "wai")
                {
                    filter = f => f.Status == OrderStatus.Waiting && !f.IsDeleted &&
                                  f.OrderCompleted == OrderCompleted.NOK && !f.Finished &&
                                  ((string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.ClientPhone.Contains(searchStr))
                                    || (string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.ClientCode.Contains(searchStr))
                                   || (string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.Code.Contains(searchStr)))
                                  && ((BranchId == -1 ? true : f.BranchId == BranchId) || (BranchId == -1 ? true : f.Client.BranchId == BranchId) || (BranchId == -1 ? true : f.PreviousBranchId == BranchId && !f.TransferredConfirmed))
                                  && (auth ? f.ClientId == userId /*&& f.Status != OrderStatus.PartialReturned*/: true);
                }
                else if (q == "com")
                {
                    filter = f => !f.IsDeleted && f.Finished &&
                                  f.OrderCompleted == OrderCompleted.OK && f.Status != OrderStatus.Rejected &&
                                  ((string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.ClientPhone.Contains(searchStr))
                                    || (string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.ClientCode.Contains(searchStr))
                                    || (string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.Code.Contains(searchStr)))
                                  && ((BranchId == -1 ? true : f.BranchId == BranchId) || (BranchId == -1 ? true : f.Client.BranchId == BranchId) || (BranchId == -1 ? true : f.PreviousBranchId == BranchId && !f.TransferredConfirmed))
                                  && (auth ? f.ClientId == userId /*&& f.Status != OrderStatus.PartialReturned*/: true);
                }
                else if (q == "fsh")
                {
                    filter = f => f.Status == OrderStatus.Completed && f.Finished && !f.IsDeleted &&
                                  f.OrderCompleted == OrderCompleted.OK &&
                                  ((string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.ClientPhone.Contains(searchStr))
                                    || (string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.ClientCode.Contains(searchStr))
                                   || (string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.Code.Contains(searchStr)))
                                  && ((BranchId == -1 ? true : f.BranchId == BranchId) || (BranchId == -1 ? true : f.Client.BranchId == BranchId) || (BranchId == -1 ? true : f.PreviousBranchId == BranchId && !f.TransferredConfirmed))
                                  && (auth ? f.ClientId == userId /*&& f.Status != OrderStatus.PartialReturned*/: true);
                }
                else if (q == "pen")
                {
                    filter = f => f.Status == OrderStatus.Placed && f.Pending &&
                                  !f.IsDeleted &&
                                  ((string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.ClientPhone.Contains(searchStr))
                                    || (string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.ClientCode.Contains(searchStr))
                                  || (string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.Code.Contains(searchStr)))
                                  && ((BranchId == -1 ? true : f.BranchId == BranchId) || (BranchId == -1 ? true : f.Client.BranchId == BranchId) || (BranchId == -1 ? true : f.PreviousBranchId == BranchId && !f.TransferredConfirmed))
                                  && (auth ? f.ClientId == userId /*&& f.Status != OrderStatus.PartialReturned*/: true);
                }
                else if (q == "returned")
                {
                    filter = f => (f.Status == OrderStatus.Returned || f.Status == OrderStatus.PartialReturned || f.Status == OrderStatus.Returned_And_Paid_DeliveryCost || f.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender) &&
                                      !f.IsDeleted &&
                                  ((string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.ClientPhone.Contains(searchStr))
                                    || (string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.ClientCode.Contains(searchStr))
                                  || (string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.Code.Contains(searchStr)))
                                  && ((BranchId == -1 ? true : f.BranchId == BranchId) || (BranchId == -1 ? true : f.Client.BranchId == BranchId) || (BranchId == -1 ? true : f.PreviousBranchId == BranchId && !f.TransferredConfirmed))
                                  && (auth ? f.ClientId == userId /*&& f.Status != OrderStatus.PartialReturned*/: true);
                }
                else if (q == "rej")
                {
                    filter = f => f.Status == OrderStatus.Rejected && !f.IsDeleted &&
                                  !f.Finished &&
                                  ((string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.ClientPhone.Contains(searchStr))
                                    || (string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.ClientCode.Contains(searchStr))
                                   || (string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.Code.Contains(searchStr)))
                                  && ((BranchId == -1 ? true : f.BranchId == BranchId) || (BranchId == -1 ? true : f.Client.BranchId == BranchId) || (BranchId == -1 ? true : f.PreviousBranchId == BranchId && !f.TransferredConfirmed))
                                  && (auth ? f.ClientId == userId /*&& f.Status != OrderStatus.PartialReturned*/: true);
                }
                else if (q == "trans")
                {
                    filter = f => f.Client.BranchId != f.BranchId && !f.IsDeleted &&
                                  !f.Finished &&
                                  ((string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.ClientPhone.Contains(searchStr))
                                    || (string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.ClientCode.Contains(searchStr))
                                   || (string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.Code.Contains(searchStr)))
                                  && ((BranchId == -1 ? true : f.BranchId == BranchId) || (BranchId == -1 ? true : f.Client.BranchId == BranchId) || (BranchId == -1 ? true : f.PreviousBranchId == BranchId && !f.TransferredConfirmed))
                                  && (auth ? f.ClientId == userId /*&& f.Status != OrderStatus.PartialReturned*/: true);
                }
                else if (q == "notprinted")
                {
                    filter = f => !f.IsDeleted && !f.IsPrinted &&
                              ((string.IsNullOrEmpty(searchStr)
                                   ? true
                                   : f.ClientPhone.Contains(searchStr))
                                || (string.IsNullOrEmpty(searchStr)
                                   ? true
                                   : f.ClientCode.Contains(searchStr))
                                || (string.IsNullOrEmpty(searchStr)
                                   ? true
                                   : f.Code.Contains(searchStr)))
                              && ((BranchId == -1 ? true : f.BranchId == BranchId) || (BranchId == -1 ? true : f.Client.BranchId == BranchId) || (BranchId == -1 ? true : f.PreviousBranchId == BranchId && !f.TransferredConfirmed))
                              && (auth ? f.ClientId == userId : true);
                }
                else
                {
                    filter = f => !f.IsDeleted &&
                                  ((string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.ClientPhone.Contains(searchStr))
                                    || (string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.ClientCode.Contains(searchStr))
                                    || (string.IsNullOrEmpty(searchStr)
                                       ? true
                                       : f.Code.Contains(searchStr)))
                                  && ((BranchId == -1 ? true : f.BranchId == BranchId) || (BranchId == -1 ? true : f.Client.BranchId == BranchId) || (BranchId == -1 ? true : f.PreviousBranchId == BranchId && !f.TransferredConfirmed))
                                  && (auth ? f.ClientId == userId /*&& f.Status != OrderStatus.PartialReturned*/: true);
                }
            }

            /*  var orders = _orders.Get(filter).ToList();
              ViewBag.TotalPrice = orders.Sum(x => x.Cost+x.DeliveryFees);*/
            ViewBag.PageStartRowNum = ((pageNumber - 1) * pageSize) + 1;
            var countQuery = _orders.GetAllAsIQueryable(filter, orderBy, null, asNoTracking: true);
            return await PagedList<Order>.CreateAsync(
                _orders.GetAllAsIQueryable(filter, orderBy, "Client,Client.Branch,Delivery,OrderNotes,Branch,PreviousBranch", asNoTracking: true),
                countQuery,
                pageNumber, pageSize);
        }

        public async Task<IActionResult> GetItems(string searchStr, string q, long BranchId = -1, int pageNumber = 1,
            int pageSize = 50)
        {
            return PartialView("_TableList",
                (await GetPagedListItems(searchStr, pageNumber, pageSize, q, BranchId)).ToList());
        }


        public async Task<IActionResult> GetPagination(string searchStr, string q, long BranchId = -1, int pageNumber = 1,
            int pageSize = 50)
        {
            var model = PagedList<Order>.GetPaginationObject(
                await GetPagedListItems(searchStr, pageNumber, pageSize, q, BranchId));
            model.GetItemsUrl = "/Orders/GetItems";
            model.GetPaginationUrl = "/Orders/GetPagination";
            return PartialView("_Pagination", model);
        }

        #region Switch Sender Orders (ClientId) [Admin only]

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SwitchSenderOrders(
            string fromClientId,
            string toClientId = null,
            int pageNumber = 1,
            int pageSize = 15,
            string searchStr = "")
        {
            if (!await IsSenderTransferManagerAsync())
                return Forbid();

            var eligibleSenders = _orderService.GetUsers(o =>
                    o.OrderCompleted == OrderCompleted.NOK &&
                    !o.IsDeleted)
                .Where(u => !u.IsDeleted && u.UserType == Models.Enums.UserType.Client)
                .OrderBy(u => u.Name)
                .ToList();

            if (eligibleSenders.Count == 0)
                return BadRequest("لا توجد طلبات غير مسواة مؤهلة للتحويل.");

            if (string.IsNullOrWhiteSpace(fromClientId))
            {
                fromClientId = eligibleSenders.First().Id;
            }

            var fromClient = await _users.GetObj(x => x.Id == fromClientId);
            if (fromClient == null || fromClient.IsDeleted || fromClient.UserType != Models.Enums.UserType.Client)
                return BadRequest("الراسل من غير موجود.");

            var toClients = _users.Get(x =>
                    !x.IsDeleted &&
                    x.UserType == Models.Enums.UserType.Client &&
                    x.BranchId == fromClient.BranchId &&
                    x.Id != fromClientId)
                .ToList();

            if (toClients.Count == 0)
                return BadRequest("لا يوجد رسل بديلة في نفس الفرع.");

            if (string.IsNullOrWhiteSpace(toClientId))
                toClientId = toClients.First().Id;

            var query = GetSenderTransferEligibleOrders(fromClientId, searchStr);

            query = query
                .Where(o => o.Client.BranchId == fromClient.BranchId)
                .OrderByDescending(o => o.CreateOn);

            var paged = await PagedList<Order>.CreateAsync(query, pageNumber, pageSize);

            ViewBag.FromClientId = fromClientId;
            ViewBag.FromClient = fromClient;
            ViewBag.FromClients = eligibleSenders;
            ViewBag.ToClients = toClients;
            ViewBag.ToClientId = toClientId;

            return View(paged);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TransferSenderOrders(List<long> OrderId, string fromClientId, string toClientId)
        {
            if (!await IsSenderTransferManagerAsync())
                return Forbid();

            if (OrderId == null || OrderId.Count == 0)
                return BadRequest("من فضلك اختر طلب واحد على الأقل.");

            if (string.IsNullOrWhiteSpace(fromClientId) || string.IsNullOrWhiteSpace(toClientId))
                return BadRequest("من فضلك أدخل الراسلين بشكل صحيح.");

            if (fromClientId == toClientId)
                return BadRequest("لا يمكنك تحويل الطلب لنفس الراسل.");

            var fromClient = await _users.GetObj(x => x.Id == fromClientId);
            var toClient = await _users.GetObj(x => x.Id == toClientId);

            if (fromClient == null || fromClient.IsDeleted || fromClient.UserType != Models.Enums.UserType.Client)
                return BadRequest("الراسل من غير موجود.");
            if (toClient == null || toClient.IsDeleted || toClient.UserType != Models.Enums.UserType.Client)
                return BadRequest("الراسل إلى غير موجود.");
            if (toClient.BranchId != fromClient.BranchId)
                return BadRequest("لازم يكون الراسلين في نفس الفرع.");

            using (var scope = new TransactionScope(TransactionScopeOption.Required,
                       new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted, Timeout = TimeSpan.FromMinutes(10) },
                       TransactionScopeAsyncFlowOption.Enabled))
            {
                foreach (var id in OrderId)
                {
                    var order = await _orders.GetObj(x => x.Id == id);
                    if (order == null || order.IsDeleted)
                        throw new Exception($"الطلب {id} غير موجود أو تم حذفه.");

                    // نفس شروط صفحة الـ Complete
                    if (order.ClientId != fromClientId)
                        throw new Exception($"الطلب {order.Code} ليس تابعًا للراسل المحدد.");
                    if (order.OrderCompleted != OrderCompleted.NOK)
                        throw new Exception($"الطلب {order.Code} تم تسويته بالفعل.");

                    order.ClientId = toClientId;
                    order.LastUpdated = DateTime.Now.ToUniversalTime();

                    if (order.OrderOperationHistoryId != null)
                    {
                        var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
                        if (history != null)
                        {
                            history.Transfer_UserId = _userManger.GetUserId(User);
                            history.TransferDate = DateTime.Now.ToUniversalTime();
                            await _Histories.Update(history);
                        }
                    }

                    await _orders.Update(order);
                    await _CRUD.Update(order.Id);
                }

                scope.Complete();
            }

            return RedirectToAction(nameof(SwitchSenderOrders), new { fromClientId = fromClientId, toClientId = toClientId });
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetItemsSenderTransfer(string searchStr, string q, long BranchId = -1, int pageNumber = 1, int pageSize = 15)
        {
            if (!await IsSenderTransferManagerAsync())
                return Forbid();

            // q = fromClientId
            if (string.IsNullOrWhiteSpace(q))
                return PartialView("_SenderTransferCardsList", new List<Order>());

            var fromClient = await _users.GetObj(x => x.Id == q);
            if (fromClient == null || fromClient.IsDeleted || fromClient.UserType != Models.Enums.UserType.Client)
                return PartialView("_SenderTransferCardsList", new List<Order>());

            var query = GetSenderTransferEligibleOrders(q, searchStr);

            if (BranchId != -1 && fromClient.BranchId != BranchId)
                return PartialView("_SenderTransferCardsList", new List<Order>());

            query = query
                .Where(o => o.Client.BranchId == fromClient.BranchId)
                .OrderByDescending(o => o.CreateOn);

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return PartialView("_SenderTransferCardsList", items);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPaginationSenderTransfer(string searchStr, string q, long BranchId = -1, int pageNumber = 1, int pageSize = 15)
        {
            if (!await IsSenderTransferManagerAsync())
                return Forbid();

            if (string.IsNullOrWhiteSpace(q))
                return PartialView("_Pagination", new PaginationVM());

            var fromClient = await _users.GetObj(x => x.Id == q);
            if (fromClient == null || fromClient.IsDeleted || fromClient.UserType != Models.Enums.UserType.Client)
                return PartialView("_Pagination", new PaginationVM());

            var query = GetSenderTransferEligibleOrders(q, searchStr);

            if (BranchId != -1 && fromClient.BranchId != BranchId)
                return PartialView("_Pagination", new PaginationVM());

            query = query
                .Where(o => o.Client.BranchId == fromClient.BranchId)
                .OrderByDescending(o => o.CreateOn);

            var paged = await PagedList<Order>.CreateAsync(query, pageNumber, pageSize);
            var model = PagedList<Order>.GetPaginationObject(paged);
            model.GetItemsUrl = "/Orders/GetItemsSenderTransfer";
            model.GetPaginationUrl = "/Orders/GetPaginationSenderTransfer";
            return PartialView("_Pagination", model);
        }

        private IQueryable<Order> GetSenderTransferEligibleOrders(string fromClientId, string searchStr)
        {
            var query = _orderService.GetQueryableList(o =>
                o.ClientId == fromClientId &&
                !o.IsDeleted &&
                o.OrderCompleted == OrderCompleted.NOK);

            if (!string.IsNullOrWhiteSpace(searchStr))
            {
                var s = searchStr.ToLower();
                query = query.Where(o =>
                    (o.ClientPhone != null && o.ClientPhone.Contains(s)) ||
                    (o.ClientCode != null && o.ClientCode.Contains(s)) ||
                    (o.Code != null && o.Code.Contains(searchStr)));
            }

            return query;
        }

        private async Task<bool> IsSenderTransferManagerAsync()
        {
            var user = await _userManger.GetUserAsync(User);
            if (user == null)
                return false;

            if (string.Equals(user.Id, SenderTransferAuthorizedUserId, StringComparison.OrdinalIgnoreCase))
                return true;

            if (!string.IsNullOrWhiteSpace(user.Email) && SenderTransferAuthorizedEmails.Contains(user.Email.Trim()))
                return true;

            return false;
        }

        #endregion

        #endregion

        [HttpPost]
        public async Task<IActionResult> MarkAsPrinted([FromBody] List<long> orderIds)
        {
            Console.WriteLine($"Received order IDs: {string.Join(", ", orderIds)}"); // للتتبع

            if (orderIds == null || !orderIds.Any())
            {
                return BadRequest(new { success = false, message = "No order IDs provided" });
            }

            foreach (var id in orderIds)
            {
                var order = await _orders.GetObj(x => x.Id == id);
                if (order != null)
                {
                    order.IsPrinted = true;
                    await _orders.Update(order);
                    await _CRUD.Update(order.Id);
                }
            }

            return Ok(new { success = true, message = "تم تحديث حالة الطباعة بنجاح" });
        }

        #region قوائم تحميل المندوبين
        [Authorize(Roles = "Admin,HighAdmin,Accountant,LowAdmin,TrustAdmin")]
        public async Task<IActionResult> CourierSheets(string courierId = "0", DateTime? FilterTime = null, DateTime? FilterTimeTo = null, int pageNumber = 1, int pageSize = 50)
        {
            var drivers = _users.Get(x => !x.IsDeleted && x.UserType == Models.Enums.UserType.Driver).ToList();
            ViewBag.Drivers = drivers;
            ViewBag.CourierId = courierId;
            ViewBag.FilterTime = FilterTime;
            ViewBag.FilterTimeTo = FilterTimeTo;

            bool filterByCourier = courierId != "0" && !string.IsNullOrEmpty(courierId);
            var filterTimeDate = FilterTime.HasValue ? FilterTime.Value.Date : DateTime.MinValue;
            var filterTimeToDate = FilterTimeTo.HasValue ? FilterTimeTo.Value.Date.AddDays(1) : DateTime.MaxValue;

            Expression<Func<CourierOrderSheet, bool>> filter = x => !x.IsDeleted
                && (!filterByCourier || x.CourierId == courierId)
                && (!FilterTime.HasValue || x.CreateOn >= filterTimeDate)
                && (!FilterTimeTo.HasValue || x.CreateOn <= filterTimeToDate);

            Func<IQueryable<CourierOrderSheet>, IOrderedQueryable<CourierOrderSheet>> orderBy = o => o.OrderByDescending(c => c.Id);
            var sheets = await PagedList<CourierOrderSheet>.CreateAsync(
                _courierOrderSheet.GetAllAsIQueryable(filter, orderBy, "Courier,CreatedBy"),
                pageNumber, pageSize);

            return View(sheets);
        }

        [Authorize(Roles = "Admin,HighAdmin,Accountant,LowAdmin,TrustAdmin")]
        public IActionResult GetItemsCourierSheets(string courierId = "0", DateTime? FilterTime = null, DateTime? FilterTimeTo = null, int pageNumber = 1, int pageSize = 50)
        {
            bool filterByCourier = courierId != "0" && !string.IsNullOrEmpty(courierId);
            var filterTimeDate = FilterTime.HasValue ? FilterTime.Value.Date : DateTime.MinValue;
            var filterTimeToDate = FilterTimeTo.HasValue ? FilterTimeTo.Value.Date.AddDays(1) : DateTime.MaxValue;

            Expression<Func<CourierOrderSheet, bool>> filter = x => !x.IsDeleted
                && (!filterByCourier || x.CourierId == courierId)
                && (!FilterTime.HasValue || x.CreateOn >= filterTimeDate)
                && (!FilterTimeTo.HasValue || x.CreateOn <= filterTimeToDate);

            Func<IQueryable<CourierOrderSheet>, IOrderedQueryable<CourierOrderSheet>> orderBy = o => o.OrderByDescending(c => c.Id);
            var sheets = _courierOrderSheet.GetAllAsIQueryable(filter, orderBy, "Courier,CreatedBy")
                .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            return PartialView("_TableListCourierSheets", sheets);
        }

        [Authorize(Roles = "Admin,HighAdmin,Accountant,LowAdmin,TrustAdmin")]
        public async Task<IActionResult> GetPaginationCourierSheets(string courierId = "0", DateTime? FilterTime = null, DateTime? FilterTimeTo = null, int pageNumber = 1, int pageSize = 50)
        {
            bool filterByCourier = courierId != "0" && !string.IsNullOrEmpty(courierId);
            var filterTimeDate = FilterTime.HasValue ? FilterTime.Value.Date : DateTime.MinValue;
            var filterTimeToDate = FilterTimeTo.HasValue ? FilterTimeTo.Value.Date.AddDays(1) : DateTime.MaxValue;

            Expression<Func<CourierOrderSheet, bool>> filter = x => !x.IsDeleted
                && (!filterByCourier || x.CourierId == courierId)
                && (!FilterTime.HasValue || x.CreateOn >= filterTimeDate)
                && (!FilterTimeTo.HasValue || x.CreateOn <= filterTimeToDate);

            Func<IQueryable<CourierOrderSheet>, IOrderedQueryable<CourierOrderSheet>> orderBy = o => o.OrderByDescending(c => c.Id);
            var sheets = await PagedList<CourierOrderSheet>.CreateAsync(
                _courierOrderSheet.GetAllAsIQueryable(filter, orderBy, "Courier,CreatedBy"),
                pageNumber, pageSize);

            var paginationVM = new PaginationVM()
            {
                PageIndex = sheets.PageIndex,
                TotalPages = sheets.TotalPages,
                PreviousPage = sheets.PreviousPage,
                NextPage = sheets.NextPage,
                GetItemsUrl = "/Orders/GetItemsCourierSheets",
                GetPaginationUrl = "/Orders/GetPaginationCourierSheets",
                ItemsCount = sheets.ItemsCount,
                StartIndex = sheets.StartIndex,
                EndIndex = sheets.EndIndex
            };
            return PartialView("_Pagination3", paginationVM);
        }

        [Authorize(Roles = "Admin,HighAdmin,Accountant,LowAdmin,TrustAdmin")]
        public IActionResult CourierSheetDetails(long id)
        {
            var sheet = _courierOrderSheet.GetAllAsIQueryable(x => x.Id == id && !x.IsDeleted, null, "Courier,CreatedBy").FirstOrDefault();
            if (sheet == null)
                return NotFound();

            var items = _courierOrderSheetItem.GetAllAsIQueryable(x => x.SheetId == id && !x.IsDeleted, null, "Order,Order.Client,Order.Delivery,Order.Branch").ToList();
            ViewBag.Sheet = sheet;
            return View(items);
        }

        [Authorize(Roles = "Admin,HighAdmin,Accountant,LowAdmin,TrustAdmin")]
        public IActionResult ExportSheetExcel(long id,
            bool showProductName = false, bool showSenderPhone = false, bool showSenderName = false,
            bool showOrderCost = false, bool showDeliveryFees = false, bool showClientCode = false,
            bool useCustomColumns = false)
        {
            var items = _courierOrderSheetItem.GetAllAsIQueryable(x => x.SheetId == id && !x.IsDeleted, null, null).ToList();
            var orderIds = items.Select(x => x.OrderId).ToList();

            return ExportToExecl(orderIds, showProductName, showSenderPhone, showSenderName,
                showOrderCost, showDeliveryFees, showClientCode, useCustomColumns: useCustomColumns);
        }
        #endregion

        #region طلبات في انتظار التقفيل (لكل مندوب)

        /// <summary>
        /// صفحة طلبات التوصيل في انتظار التقفيل - فلتر بالمندوب
        /// </summary>
        [Authorize(Roles = "Admin,HighAdmin,Accountant,TrustAdmin")]
        public IActionResult PendingDeliveriesSettlement(string driverId = "0")
        {
            var drivers = _users.Get(x => !x.IsDeleted && x.UserType == UserType.Driver).OrderBy(x => x.Name).ToList();
            ViewBag.Drivers = drivers;
            ViewBag.DriverId = driverId;

            var orders = new List<Order>();
            if (driverId != "0" && !string.IsNullOrEmpty(driverId))
            {
                orders = _orders.GetAllAsIQueryable(
                    filter: x => x.DeliveryId == driverId && !x.IsDeleted && !x.Finished
                        && (x.Status == OrderStatus.Delivered
                            || x.Status == OrderStatus.Delivered_With_Edit_Price
                            || x.Status == OrderStatus.PartialDelivered
                            || x.Status == OrderStatus.Waiting),
                    orderby: o => o.OrderByDescending(c => c.LastUpdated ?? c.CreateOn),
                    IncludeProperties: "Client,Delivery,OrderNotes").ToList();

                var driver = drivers.FirstOrDefault(x => x.Id == driverId);
                ViewBag.DriverName = driver?.Name ?? "-";
                ViewBag.DriverPhone = driver?.PhoneNumber ?? "-";
            }

            // الإحصائيات
            ViewBag.TotalOrders = orders.Count;
            ViewBag.DeliveredCount = orders.Count(o => o.Status == OrderStatus.Delivered);
            ViewBag.DeliveredWithEditPriceCount = orders.Count(o => o.Status == OrderStatus.Delivered_With_Edit_Price);
            ViewBag.PartialDeliveredCount = orders.Count(o => o.Status == OrderStatus.PartialDelivered);
            ViewBag.WaitingCount = orders.Count(o => o.Status == OrderStatus.Waiting);
            ViewBag.TotalCollected = orders.Sum(o => o.ArrivedCost);
            ViewBag.TotalDriverCommission = orders.Sum(o => o.DeliveryCost);
            ViewBag.TotalToCompany = orders.Sum(o => o.ArrivedCost) - orders.Sum(o => o.DeliveryCost);

            return View(orders);
        }

        /// <summary>
        /// صفحة مرتجعات في انتظار التقفيل - فلتر بالمندوب
        /// </summary>
        [Authorize(Roles = "Admin,HighAdmin,Accountant,TrustAdmin")]
        public IActionResult PendingReturnsSettlement(string driverId = "0")
        {
            var drivers = _users.Get(x => !x.IsDeleted && x.UserType == UserType.Driver).OrderBy(x => x.Name).ToList();
            ViewBag.Drivers = drivers;
            ViewBag.DriverId = driverId;

            var orders = new List<Order>();
            if (driverId != "0" && !string.IsNullOrEmpty(driverId))
            {
                orders = _orders.GetAllAsIQueryable(
                    filter: x => x.DeliveryId == driverId && !x.IsDeleted && !x.ReturnedFinished
                        && (x.Status == OrderStatus.Returned
                            || x.Status == OrderStatus.PartialReturned
                            || x.Status == OrderStatus.Returned_And_Paid_DeliveryCost
                            || x.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender),
                    orderby: o => o.OrderByDescending(c => c.LastUpdated ?? c.CreateOn),
                    IncludeProperties: "Client,Delivery,OrderNotes").ToList();

                var driver = drivers.FirstOrDefault(x => x.Id == driverId);
                ViewBag.DriverName = driver?.Name ?? "-";
                ViewBag.DriverPhone = driver?.PhoneNumber ?? "-";
            }

            // الإحصائيات
            ViewBag.TotalOrders = orders.Count;
            ViewBag.ReturnedCount = orders.Count(o => o.Status == OrderStatus.Returned);
            ViewBag.PartialReturnedCount = orders.Count(o => o.Status == OrderStatus.PartialReturned);
            ViewBag.ReturnedPaidDeliveryCount = orders.Count(o => o.Status == OrderStatus.Returned_And_Paid_DeliveryCost);
            ViewBag.ReturnedOnSenderCount = orders.Count(o => o.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender);
            ViewBag.TotalCollected = orders.Sum(o => o.ArrivedCost);
            ViewBag.TotalDriverCommission = orders.Sum(o => o.DeliveryCost);
            ViewBag.TotalToCompany = orders.Sum(o => o.ArrivedCost) - orders.Sum(o => o.DeliveryCost);

            return View(orders);
        }

        #endregion

        #region Bulk Settlement - تقفيل بالاكسيل

        [Authorize(Roles = "Admin,HighAdmin,Accountant,TrustAdmin")]
        public IActionResult BulkSettlement()
        {
            ViewBag.Clients = _users.Get(x => !x.IsDeleted && x.UserType == UserType.Client).OrderBy(x => x.Name).ToList();
            ViewBag.Drivers = _users.Get(x => !x.IsDeleted && x.UserType == UserType.Driver).OrderBy(x => x.Name).ToList();
            // التنفيذ متاح لأي دور بيقدر يفتح الصفحة (بقى تحديث حالات مش تقفيل مالي)
            ViewBag.CanExecute = true;
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Admin,HighAdmin,Accountant,TrustAdmin")]
        public IActionResult GetBulkSettlementOrders(string clientId = null, string driverId = null)
        {
            var query = _orders.GetAllAsIQueryable(
                filter: x => !x.IsDeleted
                    && (
                        (!x.Finished && (x.Status == OrderStatus.Delivered
                            || x.Status == OrderStatus.Delivered_With_Edit_Price
                            || x.Status == OrderStatus.PartialDelivered
                            || x.Status == OrderStatus.Waiting
                            || x.Status == OrderStatus.Returned
                            || x.Status == OrderStatus.Assigned))
                        ||
                        (!x.ReturnedFinished && (x.Status == OrderStatus.Returned_And_Paid_DeliveryCost
                            || x.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender))
                    ),
                orderby: o => o.OrderByDescending(c => c.LastUpdated ?? c.CreateOn),
                IncludeProperties: "Client,Delivery"
            );

            if (!string.IsNullOrEmpty(clientId) && clientId != "0")
                query = query.Where(x => x.ClientId == clientId);
            if (!string.IsNullOrEmpty(driverId) && driverId != "0")
                query = query.Where(x => x.DeliveryId == driverId);

            var egyptTZ = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            var orders = query.Select(x => new
            {
                x.Id,
                x.Code,
                Status = x.Status.ToString(),
                StatusText = x.Status == OrderStatus.Delivered ? "تم التوصيل"
                    : x.Status == OrderStatus.PartialDelivered ? "تسليم جزئي"
                    : x.Status == OrderStatus.Delivered_With_Edit_Price ? "توصيل مع تعديل سعر"
                    : x.Status == OrderStatus.Returned ? "مرتجع كامل"
                    : x.Status == OrderStatus.Returned_And_Paid_DeliveryCost ? "مرتجع ودفع شحن"
                    : x.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender ? "مرتجع وشحن على الراسل"
                    : x.Status == OrderStatus.Waiting ? "مؤجل"
                    : x.Status == OrderStatus.Assigned ? "جارى التوصيل"
                    : "-",
                ClientName = x.Client != null ? x.Client.Name : "-",
                RecipientName = x.ClientName,
                x.ClientPhone,
                Address = x.AddressCity + " - " + x.Address,
                x.TotalCost,
                x.ArrivedCost,
                x.DeliveryCost,
                DriverName = x.Delivery != null ? x.Delivery.Name : "-",
                x.LastUpdated,
                x.CreateOn
            }).ToList();

            var result = orders.Select(x => new
            {
                x.Id,
                x.Code,
                x.Status,
                x.StatusText,
                x.ClientName,
                x.RecipientName,
                x.ClientPhone,
                x.Address,
                x.TotalCost,
                x.ArrivedCost,
                x.DeliveryCost,
                x.DriverName,
                LastUpdate = x.LastUpdated.HasValue
                    ? TimeZoneInfo.ConvertTimeFromUtc(x.LastUpdated.Value, egyptTZ).ToString("dd/MM/yyyy hh:mm tt")
                    : TimeZoneInfo.ConvertTimeFromUtc(x.CreateOn, egyptTZ).ToString("dd/MM/yyyy hh:mm tt")
            });

            return Json(result);
        }

        [Authorize(Roles = "Admin,HighAdmin,Accountant,TrustAdmin")]
        public IActionResult DownloadBulkSettlementTemplate(string clientId = null, string driverId = null)
        {
            try
            {
            var query = _orders.GetAllAsIQueryable(
                filter: x => !x.IsDeleted
                    && (
                        (!x.Finished && (x.Status == OrderStatus.Delivered
                            || x.Status == OrderStatus.Delivered_With_Edit_Price
                            || x.Status == OrderStatus.PartialDelivered
                            || x.Status == OrderStatus.Waiting
                            || x.Status == OrderStatus.Returned
                            || x.Status == OrderStatus.Assigned))
                        ||
                        (!x.ReturnedFinished && (x.Status == OrderStatus.Returned_And_Paid_DeliveryCost
                            || x.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender))
                    ),
                orderby: null,
                IncludeProperties: "Client,Delivery"
            );

            if (!string.IsNullOrEmpty(clientId) && clientId != "0")
                query = query.Where(x => x.ClientId == clientId);
            if (!string.IsNullOrEmpty(driverId) && driverId != "0")
                query = query.Where(x => x.DeliveryId == driverId);

            var orders = query.OrderBy(x => x.Code).ToList();

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("تقفيل");
                ws.RightToLeft = true;

                ws.Cell(1, 1).Value = "كود الشحنه";
                ws.Cell(1, 2).Value = "سعر الطلب";
                ws.Cell(1, 3).Value = "حاله الشحنه";
                ws.Cell(1, 4).Value = "الراسل";
                ws.Cell(1, 5).Value = "المندوب";
                ws.Cell(1, 6).Value = "السعر الأصلي";

                var headerRange = ws.Range(1, 1, 1, 6);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1a3a6b");
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                int row = 2;
                foreach (var order in orders)
                {
                    ws.Cell(row, 1).Value = order.Code;
                    ws.Cell(row, 2).Value = "";
                    ws.Cell(row, 3).Value = "";
                    ws.Cell(row, 4).Value = order.Client?.Name ?? "-";
                    ws.Cell(row, 5).Value = order.Delivery?.Name ?? "-";
                    ws.Cell(row, 6).Value = order.TotalCost;
                    row++;
                }

                // عرض ثابت للأعمدة بدل AdjustToContents — الأخيرة بتقيس النص بالخطوط وبترمي error
                // على استضافات مفيهاش خطوط (زي السيرفر)، والتصدير الشغّال في النظام مش بيستخدمها أصلاً.
                ws.Column(1).Width = 18;
                ws.Column(2).Width = 12;
                ws.Column(3).Width = 24;
                ws.Column(4).Width = 22;
                ws.Column(5).Width = 18;
                ws.Column(6).Width = 14;

                var statusSheet = workbook.Worksheets.Add("الحالات");
                statusSheet.Cell(1, 1).Value = "الحالات المتاحة";
                statusSheet.Cell(1, 1).Style.Font.Bold = true;
                string[] statuses = { "تم التوصيل", "تسليم جزئي", "تم التوصيل مع تعديل السعر", "مرتجع كامل", "مرتجع ودفع شحن", "مرتجع وشحن على الراسل" };
                for (int i = 0; i < statuses.Length; i++)
                    statusSheet.Cell(i + 2, 1).Value = statuses[i];

                // قائمة منسدلة (dropdown) لعمود "حاله الشحنه" — الموظف يختار الحالة بدل ما يكتبها
                int lastDvRow = Math.Max(row + 200, 2000);
                var dvList = "\"" + string.Join(",", statuses) + "\"";
                var statusDv = ws.Range(2, 3, lastDvRow, 3).CreateDataValidation();
                statusDv.List(dvList, true);

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"BulkSettlement_{DateTime.Now:yyyyMMdd}.xlsx");
                }
            }
            }
            catch (Exception ex)
            {
                Console.WriteLine("DownloadBulkSettlementTemplate error: " + ex);
                // مؤقتاً للتشخيص: نرجّع تفاصيل الخطأ كنص (الصفحة للأدمن بس) عشان نعرف السبب بالظبط
                return Content("خطأ أثناء إنشاء ملف الاكسيل — ابعت النص ده للمطوّر:\n\n" + ex.ToString(), "text/plain; charset=utf-8");
            }
        }

        // خريطة حالات الشيت (النص العربي -> OrderStatus) — مشتركة بين المراجعة والتنفيذ
        private static Dictionary<string, OrderStatus> BulkStatusMap() => new Dictionary<string, OrderStatus>(StringComparer.OrdinalIgnoreCase)
        {
            { "تم التوصيل", OrderStatus.Delivered },
            { "تسليم جزئي", OrderStatus.PartialDelivered },
            { "تم التوصيل مع تعديل السعر", OrderStatus.Delivered_With_Edit_Price },
            { "مرتجع كامل", OrderStatus.Returned },
            { "مرتجع ودفع شحن", OrderStatus.Returned_And_Paid_DeliveryCost },
            { "مرتجع وشحن على الراسل", OrderStatus.Returned_And_DeliveryCost_On_Sender },
        };

        // قراءة صفوف الشيت مع تحديد الأعمدة بالاسم (مش بالترتيب الثابت) عشان لو المستخدم زوّد/حرّك عمود مايبوظش
        private static List<(int RowNum, string Code, string PriceStr, string StatusStr)> ReadBulkRows(Stream stream)
        {
            var list = new List<(int, string, string, string)>();
            using (var workbook = new XLWorkbook(stream))
            {
                var ws = workbook.Worksheet(1);
                var used = ws.RowsUsed();
                var header = used.FirstOrDefault();
                if (header == null) return list;

                int codeCol = 0, priceCol = 0, statusCol = 0;
                foreach (var cell in header.CellsUsed())
                {
                    var h = cell.GetString()?.Trim();
                    if (h == "كود الشحنه" || h == "كود الشحنة") codeCol = cell.Address.ColumnNumber;
                    else if (h == "سعر الطلب") priceCol = cell.Address.ColumnNumber;
                    else if (h == "حاله الشحنه" || h == "حالة الشحنة") statusCol = cell.Address.ColumnNumber;
                }
                // fallback للترتيب الافتراضي لو الهيدر مش متطابق
                if (codeCol == 0) codeCol = 1;
                if (priceCol == 0) priceCol = 2;
                if (statusCol == 0) statusCol = 3;

                foreach (var row in used.Skip(1))
                {
                    string code = row.Cell(codeCol).GetString()?.Trim();
                    string priceStr = row.Cell(priceCol).GetString()?.Trim();
                    string statusStr = row.Cell(statusCol).GetString()?.Trim();
                    list.Add((row.RowNumber(), code, priceStr, statusStr));
                }
            }
            return list;
        }

        // تحميل الطلبات بالأكواد على دفعات (chunks) عشان نتجنب حد الـ 2100 parameter بتاع SQL Server في IN(...)
        private List<Order> LoadOrdersByCodes(List<string> codes, bool tracked)
        {
            var result = new List<Order>();
            for (int i = 0; i < codes.Count; i += 1000)
            {
                var chunk = codes.Skip(i).Take(1000).ToList();
                result.AddRange(_orders.GetAllAsIQueryable(
                    x => chunk.Contains(x.Code) && !x.IsDeleted,
                    IncludeProperties: "Delivery",
                    asNoTracking: !tracked).ToList());
            }
            return result;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,HighAdmin,Accountant,TrustAdmin")]
        public IActionResult ValidateBulkSettlement(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "لم يتم رفع ملف" });

            string ext = Path.GetExtension(file.FileName ?? "").ToLowerInvariant();
            if (ext != ".xlsx")
                return Json(new { success = false, message = "الملف لازم يكون بصيغة .xlsx (لو عندك .xls افتحه واعمله Save As بصيغة .xlsx)" });

            var errors = new List<object>();
            var warnings = new List<object>();
            var validRows = new List<object>();
            var driverCounts = new Dictionary<string, int>();
            int totalRows = 0;

            var statusMap = BulkStatusMap();

            List<(int RowNum, string Code, string PriceStr, string StatusStr)> rawRows;
            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                using (var stream = file.OpenReadStream())
                    rawRows = ReadBulkRows(stream);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ValidateBulkSettlement read error: " + ex);
                return Json(new { success = false, message = "تعذّر قراءة الملف. تأكد إنه ملف Excel صحيح بصيغة .xlsx" });
            }

            // إزالة التكرار: لو نفس الكود اتكرر ناخد آخر صف (نية المستخدم النهائية) ونحذّر على الباقي
            var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var dedupRows = new List<(int RowNum, string Code, string PriceStr, string StatusStr)>();
            for (int i = rawRows.Count - 1; i >= 0; i--)
            {
                var r = rawRows[i];
                if (string.IsNullOrEmpty(r.Code)) { dedupRows.Add(r); continue; }
                if (seenCodes.Add(r.Code)) dedupRows.Add(r);
                else warnings.Add(new { row = r.RowNum, code = r.Code, message = "كود مكرر في الملف - هيتحسب آخر صف بس", type = "duplicate" });
            }
            dedupRows.Reverse();

            // تحميل كل الطلبات مرة واحدة بدل استعلام لكل صف
            var codes = dedupRows.Where(r => !string.IsNullOrEmpty(r.Code)).Select(r => r.Code).Distinct().ToList();
            var ordersByCode = LoadOrdersByCodes(codes, tracked: false)
                .GroupBy(x => x.Code)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var r in dedupRows)
            {
                totalRows++;
                int rowNum = r.RowNum;
                string code = r.Code;
                string priceStr = r.PriceStr;
                string statusStr = r.StatusStr;

                if (string.IsNullOrEmpty(code))
                {
                    errors.Add(new { row = rowNum, code = "", message = "كود الشحنة فارغ" });
                    continue;
                }
                if (string.IsNullOrEmpty(statusStr))
                {
                    errors.Add(new { row = rowNum, code, message = "حالة الشحنة فارغة" });
                    continue;
                }
                if (!statusMap.ContainsKey(statusStr))
                {
                    errors.Add(new { row = rowNum, code, message = $"حالة غير معروفة: {statusStr}" });
                    continue;
                }

                double arrivedCost = 0;
                var targetStatus = statusMap[statusStr];
                bool isReturnStatus = targetStatus == OrderStatus.Returned
                    || targetStatus == OrderStatus.Returned_And_Paid_DeliveryCost
                    || targetStatus == OrderStatus.Returned_And_DeliveryCost_On_Sender;

                if (!isReturnStatus)
                {
                    if (string.IsNullOrEmpty(priceStr))
                    {
                        errors.Add(new { row = rowNum, code, message = "سعر الطلب فارغ (مطلوب لحالات التوصيل)" });
                        continue;
                    }
                    if (!double.TryParse(priceStr, out arrivedCost) || arrivedCost < 0)
                    {
                        errors.Add(new { row = rowNum, code, message = $"سعر الطلب غير صالح: {priceStr}" });
                        continue;
                    }
                }

                ordersByCode.TryGetValue(code, out var order);
                if (order == null)
                {
                    errors.Add(new { row = rowNum, code, message = "كود الشحنة غير موجود في النظام" });
                    continue;
                }

                if (order.Finished && order.Status != OrderStatus.Returned_And_Paid_DeliveryCost
                    && order.Status != OrderStatus.Returned_And_DeliveryCost_On_Sender)
                {
                    warnings.Add(new { row = rowNum, code, message = "الطلب مقفل مسبقاً - سيتم تخطيه", type = "skipped" });
                    continue;
                }

                if (order.ReturnedFinished && (targetStatus == OrderStatus.Returned_And_Paid_DeliveryCost
                    || targetStatus == OrderStatus.Returned_And_DeliveryCost_On_Sender))
                {
                    warnings.Add(new { row = rowNum, code, message = "مرتجع مقفل مسبقاً - سيتم تخطيه", type = "skipped" });
                    continue;
                }

                if (string.IsNullOrEmpty(order.DeliveryId))
                {
                    errors.Add(new { row = rowNum, code, message = "الطلب غير مسند لمندوب" });
                    continue;
                }

                // فرق السعر: تحذير أحمر للحالات العادية، وتنبيه معلوماتي (متوقع) لتعديل السعر/الجزئي
                if (!isReturnStatus && arrivedCost != order.TotalCost)
                {
                    string msg = $"السعر مختلف: في الشيت {arrivedCost:N2} والسيستم {order.TotalCost:N2}";
                    if (targetStatus == OrderStatus.Delivered_With_Edit_Price || targetStatus == OrderStatus.PartialDelivered)
                        warnings.Add(new { row = rowNum, code, message = msg + " (متوقع لتعديل السعر/الجزئي)", type = "info" });
                    else
                        warnings.Add(new { row = rowNum, code, message = msg, type = "price" });
                }

                string driverName = order.Delivery != null ? order.Delivery.Name : "-";
                driverCounts.TryGetValue(driverName, out var dcnt);
                driverCounts[driverName] = dcnt + 1;

                validRows.Add(new
                {
                    row = rowNum,
                    code,
                    arrivedCost,
                    status = statusStr,
                    currentStatus = order.Status.ToString(),
                    totalCost = order.TotalCost,
                    driverName
                });
            }

            var driverSummary = driverCounts
                .OrderByDescending(kv => kv.Value)
                .Select(kv => new { driverName = kv.Key, count = kv.Value });

            return Json(new
            {
                success = true,
                totalRows,
                validCount = validRows.Count,
                errorCount = errors.Count,
                warningCount = warnings.Count,
                errors,
                warnings,
                validRows,
                driverSummary
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin,HighAdmin,Accountant,TrustAdmin")]
        public async Task<IActionResult> ExecuteBulkSettlement(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "لم يتم رفع ملف" });

            string ext = Path.GetExtension(file.FileName ?? "").ToLowerInvariant();
            if (ext != ".xlsx")
                return Json(new { success = false, message = "الملف لازم يكون بصيغة .xlsx" });

            var statusMap = BulkStatusMap();

            // 1) قراءة الصفوف مرة واحدة
            List<(int RowNum, string Code, string PriceStr, string StatusStr)> rawRows;
            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                using (var stream = file.OpenReadStream())
                    rawRows = ReadBulkRows(stream);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ExecuteBulkSettlement read error: " + ex);
                return Json(new { success = false, message = "تعذّر قراءة الملف. تأكد إنه ملف Excel صحيح بصيغة .xlsx" });
            }

            // إزالة التكرار (آخر ظهور للكود) + استبعاد الصفوف غير الصالحة مبدئياً
            var rows = new List<(int RowNum, string Code, string PriceStr, string StatusStr)>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = rawRows.Count - 1; i >= 0; i--)
            {
                var r = rawRows[i];
                if (string.IsNullOrEmpty(r.Code) || string.IsNullOrEmpty(r.StatusStr)) continue;
                if (!statusMap.ContainsKey(r.StatusStr)) continue;
                if (seen.Add(r.Code)) rows.Add(r);
            }
            rows.Reverse();

            if (rows.Count == 0)
                return Json(new { success = false, message = "لا توجد صفوف صالحة في الملف" });

            using (var scope = new TransactionScope(TransactionScopeOption.Required,
                new TransactionOptions { IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted, Timeout = TimeSpan.FromMinutes(10) },
                TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    var userid = _userManger.GetUserId(User);

                    int successCount = 0;
                    int skipCount = 0;
                    var processedOrderIds = new List<long>();
                    var skipped = new List<object>();

                    // تحميل الطلبات (tracked، مع بيانات المندوب) دفعة واحدة بدل استعلام لكل صف
                    var codes = rows.Select(r => r.Code).Distinct().ToList();
                    var orders = LoadOrdersByCodes(codes, tracked: true);
                    var ordersByCode = orders
                        .GroupBy(x => x.Code)
                        .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                    var nowUtc = DateTime.Now.ToUniversalTime();

                    foreach (var r in rows)
                    {
                        var targetStatus = statusMap[r.StatusStr];
                        double paid = 0;
                        if (!string.IsNullOrEmpty(r.PriceStr))
                            double.TryParse(r.PriceStr, out paid);

                        ordersByCode.TryGetValue(r.Code, out var order);
                        if (order == null) { skipped.Add(new { row = r.RowNum, code = r.Code, message = "غير موجود في النظام" }); skipCount++; continue; }
                        if (string.IsNullOrEmpty(order.DeliveryId)) { skipped.Add(new { row = r.RowNum, code = r.Code, message = "غير مسند لمندوب" }); skipCount++; continue; }

                        bool isReturnPaidStatus = targetStatus == OrderStatus.Returned_And_Paid_DeliveryCost
                            || targetStatus == OrderStatus.Returned_And_DeliveryCost_On_Sender;

                        // منمسّش الطلبات المقفّلة ماليًا عشان منبوّظش تقفيلة اتعملت خلاص
                        if (order.Finished && !isReturnPaidStatus) { skipped.Add(new { row = r.RowNum, code = r.Code, message = "الطلب مقفول ماليًا — اتخطى" }); skipCount++; continue; }
                        if (order.ReturnedFinished && isReturnPaidStatus) { skipped.Add(new { row = r.RowNum, code = r.Code, message = "المرتجع مقفول ماليًا — اتخطى" }); skipCount++; continue; }

                        double driverFee = (order.Delivery != null && order.Delivery.DeliveryCost != null) ? order.Delivery.DeliveryCost.Value : 0;

                        // بنشيل الطلب من أي تقفيلة سابقة (زي FinishOrder بالظبط)
                        order.WalletId = null;

                        switch (targetStatus)
                        {
                            case OrderStatus.Delivered:
                                order.ArrivedCost = order.TotalCost;
                                order.DeliveryCost = driverFee;
                                break;
                            case OrderStatus.Delivered_With_Edit_Price:
                                order.ArrivedCost = paid;
                                order.DeliveryCost = driverFee;
                                break;
                            case OrderStatus.Returned_And_Paid_DeliveryCost:
                                order.ArrivedCost = paid;
                                order.DeliveryCost = driverFee;
                                order.ReturnedCost = order.Cost;
                                break;
                            case OrderStatus.Returned_And_DeliveryCost_On_Sender:
                                order.ArrivedCost = 0;
                                order.DeliveryCost = driverFee;
                                order.ReturnedCost = order.Cost;
                                break;
                            case OrderStatus.Returned:
                                order.ArrivedCost = 0;
                                order.DeliveryCost = 0;
                                order.ReturnedCost = order.TotalCost;
                                break;
                            case OrderStatus.PartialDelivered:
                                order.ArrivedCost = paid;
                                order.DeliveryCost = driverFee;
                                // طلب عكسي مرتجع جزئي بنفس الكود + R (نفس FinishOrder: يُنشأ لو مش موجود، أو يُحدَّث لو موجود)
                                var revCode = "R" + order.Code;
                                var existingRev = await _orders.GetObj(x => x.Code == revCode && !x.IsDeleted);
                                if (existingRev == null)
                                {
                                    var partialReturned = new Order
                                    {
                                        Code = revCode,
                                        Notes = order.Notes,
                                        AddressCity = order.AddressCity,
                                        Address = order.Address,
                                        ClientCode = order.ClientCode,
                                        ClientName = order.ClientName,
                                        ClientPhone = order.ClientPhone,
                                        ClientSecondaryPhone = order.ClientSecondaryPhone,
                                        Cost = order.Cost,
                                        DeliveryFees = order.DeliveryFees,
                                        TotalCost = order.TotalCost,
                                        Pending = order.Pending,
                                        TransferredConfirmed = order.TransferredConfirmed,
                                        PendingReturnTransferrConfirmed = order.PendingReturnTransferrConfirmed,
                                        ArrivedCost = order.ArrivedCost,
                                        DeliveryCost = 0,
                                        ReturnedCost = order.TotalCost - order.ArrivedCost,
                                        Finished = order.Finished,
                                        Status = OrderStatus.PartialReturned,
                                        OrderCompleted = order.OrderCompleted,
                                        ClientId = order.ClientId,
                                        LastUpdated = nowUtc,
                                        WalletId = order.WalletId,
                                        DeliveryId = order.DeliveryId,
                                        BranchId = order.BranchId,
                                        PreviousBranchId = order.PreviousBranchId,
                                        OrderOperationHistoryId = order.OrderOperationHistoryId,
                                        ReturnedFinished = order.ReturnedFinished,
                                        ReturnedOrderCompleted = order.ReturnedOrderCompleted,
                                        ReturnedWalletId = order.ReturnedWalletId,
                                        ReturnedCompletedId = order.ReturnedCompletedId,
                                    };
                                    await _orders.Add(partialReturned);
                                    var prHistory = new OrderOperationHistory
                                    {
                                        OrderId = partialReturned.Id,
                                        Create_UserId = userid,
                                        CreateDate = partialReturned.CreateOn,
                                    };
                                    await _Histories.Add(prHistory);
                                    partialReturned.OrderOperationHistoryId = prHistory.Id;
                                    await _orders.Update(partialReturned);
                                }
                                else
                                {
                                    existingRev.ArrivedCost = order.ArrivedCost;
                                    existingRev.ReturnedCost = order.TotalCost - order.ArrivedCost;
                                    await _orders.Update(existingRev);
                                }
                                break;
                            default:
                                order.ArrivedCost = paid;
                                order.DeliveryCost = 0;
                                order.ReturnedCost = order.TotalCost - paid;
                                break;
                        }

                        order.Status = targetStatus;
                        order.LastUpdated = nowUtc;

                        // ملاحظة على الطلب (وبتحفظ تعديلات الطلب كمان لأنها SaveChanges على نفس الـ context)
                        await _orderNotes.Add(new OrderNotes
                        {
                            Content = "تم تحديث الحالة إلى: " + r.StatusStr + " (عن طريق ملف الاكسيل)",
                            OrderId = order.Id,
                            UserId = userid
                        });

                        processedOrderIds.Add(order.Id);
                        successCount++;
                    }

                    scope.Complete();

                    return Json(new
                    {
                        success = true,
                        message = $"تم تحديث حالة {successCount} طلب (جاهزة لتقفيل المندوب)"
                            + (skipCount > 0 ? $" — واتخطى {skipCount} طلب" : ""),
                        successCount,
                        skipCount,
                        skipped
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ExecuteBulkSettlement error: " + ex);
                    return Json(new { success = false, message = "حدث خطأ أثناء التنفيذ، حاول مرة أخرى" });
                }
            }
        }

        #endregion

        #region Warehouse Receipt - استلام مخزن

        // صفحة استلام المخزن: بتعرض الطلبات المعلّقة (Placed + Pending) عشان الأدمن يقبلها أو يرفضها
        [Authorize(Roles = "Admin,HighAdmin,TrustAdmin")]
        public IActionResult WarehouseReceipt(long BranchId = -1)
        {
            ViewBag.Branchs = _branch.Get(x => !x.IsDeleted).ToList();
            ViewBag.BranchId = BranchId;

            var orders = _orders.GetAllAsIQueryable(
                filter: x => x.WarehousePending && x.Status == OrderStatus.Placed && !x.IsDeleted
                    && (BranchId == -1 || x.BranchId == BranchId),
                orderby: o => o.OrderByDescending(c => c.CreateOn),
                IncludeProperties: "Client",
                asNoTracking: true).ToList();

            return View(orders);
        }

        // قبول الطلبات: تشيل حالة الانتظار فتشتغل عادي
        [Authorize(Roles = "Admin,HighAdmin,TrustAdmin")]
        [HttpPost]
        public async Task<IActionResult> AcceptWarehouseOrders(List<long> Orders)
        {
            if (Orders == null || Orders.Count == 0)
                return Json(new { success = false, message = "لم يتم اختيار أي طلب" });

            var userid = _userManger.GetUserId(User);
            var nowUtc = DateTime.Now.ToUniversalTime();
            int count = 0;
            foreach (var id in Orders)
            {
                var order = await _orders.GetObj(x => x.Id == id && x.WarehousePending && !x.IsDeleted);
                if (order == null) continue;
                order.Pending = false;
                order.WarehousePending = false;
                order.LastUpdated = nowUtc;
                await _orders.Update(order);
                if (order.OrderOperationHistoryId != null)
                {
                    var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
                    if (history != null)
                    {
                        history.Accept_UserId = userid;
                        history.AcceptDate = nowUtc;
                        await _Histories.Update(history);
                    }
                }
                count++;
            }
            return Json(new { success = true, message = $"تم استلام {count} طلب" });
        }

        // رفض الطلبات: حذف ناعم (IsDeleted) — المخزن مستلمش الطلبات دي
        [Authorize(Roles = "Admin,HighAdmin,TrustAdmin")]
        [HttpPost]
        public async Task<IActionResult> RejectWarehouseOrders(List<long> Orders)
        {
            if (Orders == null || Orders.Count == 0)
                return Json(new { success = false, message = "لم يتم اختيار أي طلب" });

            var nowUtc = DateTime.Now.ToUniversalTime();
            int count = 0;
            foreach (var id in Orders)
            {
                var order = await _orders.GetObj(x => x.Id == id && x.WarehousePending && !x.IsDeleted);
                if (order == null) continue;
                order.IsDeleted = true;
                order.WarehousePending = false;
                order.LastUpdated = nowUtc;
                await _orders.Update(order);
                count++;
            }
            return Json(new { success = true, message = $"تم رفض {count} طلب" });
        }

        // استلام شحنة واحدة بمسح الباركود (scan) — أسرع طريقة للمخزن
        [Authorize(Roles = "Admin,HighAdmin,TrustAdmin")]
        [HttpPost]
        public async Task<IActionResult> AcceptWarehouseOrderByCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Json(new { success = false, status = "empty", message = "الكود فارغ" });

            code = code.Trim();
            var order = await _orders.GetObj(x => x.Code == code && !x.IsDeleted);
            if (order == null)
                return Json(new { success = false, status = "notfound", code, message = $"الكود {code} مش موجود في النظام" });
            if (!order.WarehousePending)
                return Json(new { success = false, status = "notpending", code, message = $"الشحنة {code} مش في انتظار الاستلام (اتقبلت قبل كده أو مش من المخزن)" });

            var userid = _userManger.GetUserId(User);
            var nowUtc = DateTime.Now.ToUniversalTime();
            order.Pending = false;
            order.WarehousePending = false;
            order.LastUpdated = nowUtc;
            await _orders.Update(order);
            if (order.OrderOperationHistoryId != null)
            {
                var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
                if (history != null)
                {
                    history.Accept_UserId = userid;
                    history.AcceptDate = nowUtc;
                    await _Histories.Update(history);
                }
            }
            return Json(new { success = true, status = "accepted", code = order.Code, recipient = order.ClientName ?? "", message = $"تم استلام {order.Code}" });
        }

        #endregion
    }
}