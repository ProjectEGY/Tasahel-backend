using PostexS.Migrations;
using PostexS.Models.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZXing;

namespace PostexS.Helper
{
    public class ExportToExcel
    {
        public class ExcelExport
        {
            public static DataTable DriversExport(List<ApplicationUser> order)
            {
                DataTable dt = new DataTable("Report");
                dt.Columns.AddRange(new DataColumn[8] { /*new DataColumn("                       ID                    "),*/
                                            new DataColumn("         الاسم          "),
                                            new DataColumn("         رقم الهاتف          "),
                                            new DataColumn("         رقم واتساب           "),
                                            new DataColumn("               البريد الالكتروني             "),
                                            new DataColumn("               العنوان              "),
                                             new DataColumn("         نوع المستخدم         "),
                                              new DataColumn("         الرصيد        "),
                                               new DataColumn("         الفرع       ")});
                foreach (var item in order)
                {


                    dt.Rows.Add(/*(item.Id),*/ item.Name,
                         item.PhoneNumber, item.WhatsappPhone, item.Email, item.Address, item.UserType, item.Wallet, item.Branch);

                    //    dt.Rows.Add((item.Code), item.Name + " " + item.UserName,
                    //         item.PhoneNumber, item.Email, item.UserType, item.Wallet,item.Branch );
                }
                return dt;
            }
            public static DataTable OrderExport(List<Order> order)
            {
                DataTable dt = new DataTable("Report");
                dt.Columns.AddRange(new DataColumn[15] {
                        new DataColumn("الحاله"),new DataColumn("التسويه"), new DataColumn("رقم الطلب"), new DataColumn("التاريخ"),
                        new DataColumn("الراسل"), new DataColumn("رقم تليفون الراسل"),
                    new DataColumn("كود العميل"), new DataColumn("المرسل إليه"), new DataColumn("رقم تليفون المرسل إليه"), new DataColumn("العنوان"),
                        new DataColumn("المطلوب دفعه"), new DataColumn("تم دفعه"), new DataColumn("المندوب"),
                        new DataColumn("الملاحظات"), new DataColumn("ملاحظات المندوب")
      });
                string status = "";
                string Complete = "";
                string DriverNotes = "";
                string DeliveryName = "";
                DateTime CreatedOn;
                foreach (var item in order)
                {
                    if (item.OrderCompleted == Models.Enums.OrderCompleted.NOK)
                    {
                        Complete = "لم يتم تسويتها";
                    }
                    else
                    {
                        Complete = "تم تسويتها";
                    }
                    if (item.IsDeleted == true)
                    {
                        status = "محذوف";
                    }
                    else if (item.Status == PostexS.Models.Enums.OrderStatus.Placed)
                    {
                        status = "جديد";
                    }
                    else if (item.Status == PostexS.Models.Enums.OrderStatus.PartialReturned)
                    {
                        status = "مرتجع جزئي";
                    }
                    else if (item.Status == PostexS.Models.Enums.OrderStatus.Returned)
                    {
                        status = "مرتجع كامل";
                    }
                    else if (item.Status == PostexS.Models.Enums.OrderStatus.Returned_And_DeliveryCost_On_Sender)
                    {
                        status = "مرتجع وشحن على الراسل";
                    }
                    else if (item.Status == PostexS.Models.Enums.OrderStatus.Returned_And_Paid_DeliveryCost)
                    {
                        status = " مرتجع ودفع شحن";
                    }
                    else if (item.Status == PostexS.Models.Enums.OrderStatus.Delivered_With_Edit_Price)
                    {
                        status = "تم التوصيل مع تعديل السعر";
                    }
                    else if (item.Status == PostexS.Models.Enums.OrderStatus.PartialDelivered)
                    {
                        status = "تم التوصيل جزئي";
                    }
                    else if (item.Status == PostexS.Models.Enums.OrderStatus.Assigned)
                    {
                        status = "جارى التوصيل";
                    }
                    else if (item.Status == PostexS.Models.Enums.OrderStatus.Delivered)
                    {
                        status = "تم التوصيل";
                    }
                    else if (item.Status == PostexS.Models.Enums.OrderStatus.Rejected)
                    {
                        status = "مرفوض";
                    }
                    else if (item.Status == PostexS.Models.Enums.OrderStatus.Waiting)
                    {
                        status = "مؤجل";
                    }
                    else if (item.Status == PostexS.Models.Enums.OrderStatus.Completed)
                    {
                        status = "تم تسويته";
                    }
                    // Convert CreateOn to local time zone and format the date and time
                    CreatedOn = TimeZoneInfo.ConvertTimeFromUtc(item.CreateOn, TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time"));
                    if (item.OrderNotes.Count > 0)
                    {
                        DriverNotes = item.OrderNotes.OrderBy(x => x.Id).Last().Content;
                    }
                    if (item.DeliveryId != null)
                    {
                        DeliveryName = item.Delivery.Name;
                    }
                    // Format the date and time
                    string formattedDateTime = CreatedOn.ToString("dd MMM, yyyy - hh:mm tt");
                    dt.Rows.Add(status, Complete, item.Code, formattedDateTime, item.Client.Name, item.Client.PhoneNumber, item.ClientCode, item.ClientName, item.ClientPhone,
                       item.AddressCity + " - " + item.Address, item.TotalCost, item.ArrivedCost, DeliveryName, item.Notes, DriverNotes);
                }
                return dt;
            }
        }
    }
}
