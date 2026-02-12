using PostexS.Migrations;
using PostexS.Models.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZXing;

namespace PostexS.Helper
{
    /// <summary>
    /// إعدادات الأعمدة الاختيارية في تصدير Excel
    /// </summary>
    public class ExcelOptionalColumns
    {
        public bool ShowProductName { get; set; } = false;    // اسم المنتج
        public bool ShowSenderPhone { get; set; } = false;    // رقم تليفون الراسل
        public bool ShowSenderName { get; set; } = false;     // اسم الراسل
        public bool ShowOrderCost { get; set; } = false;      // سعر الطلب
        public bool ShowDeliveryFees { get; set; } = false;   // سعر الشحن
        public bool ShowClientCode { get; set; } = false;     // كود العميل
        public bool ShowStatus { get; set; } = false;         // حالة الشحنة
    }

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

            /// <summary>
            /// تصدير الطلبات - النسخة القديمة (15 عمود ثابت) - للتوافقية
            /// </summary>
            public static DataTable OrderExport(List<Order> order)
            {
                return OrderExportWithOptions(order, null);
            }

            /// <summary>
            /// تصدير الطلبات مع أعمدة اختيارية
            /// الأعمدة الأساسية (ثابتة): رقم الطلب - التاريخ - المرسل اليه - تليفون المرسل اليه - المحافظة - العنوان - المطلوب دفعه - المندوب - ملاحظات
            /// الأعمدة الاختيارية: اسم المنتج - رقم تليفون الراسل - اسم الراسل - سعر الطلب - سعر الشحن - كود العميل
            /// </summary>
            public static DataTable OrderExportWithOptions(List<Order> order, ExcelOptionalColumns options)
            {
                // لو مفيش options يبقى نرجع كل الأعمدة (الطريقة القديمة)
                bool showAll = (options == null);

                DataTable dt = new DataTable("Report");

                // عمود حالة الشحنة - أول عمود لو مفعّل
                if (showAll || (options != null && options.ShowStatus))
                    dt.Columns.Add(new DataColumn("الحاله"));

                // الأعمدة الأساسية دايماً موجودة
                dt.Columns.Add(new DataColumn("رقم الطلب"));
                dt.Columns.Add(new DataColumn("التاريخ"));
                dt.Columns.Add(new DataColumn("المرسل إليه"));
                dt.Columns.Add(new DataColumn("تليفون المرسل إليه"));
                dt.Columns.Add(new DataColumn("المحافظة"));
                dt.Columns.Add(new DataColumn("العنوان"));
                dt.Columns.Add(new DataColumn("المطلوب دفعه"));
                dt.Columns.Add(new DataColumn("المندوب"));
                dt.Columns.Add(new DataColumn("الملاحظات"));

                // الأعمدة الاختيارية
                if (showAll || (options != null && options.ShowProductName))
                    dt.Columns.Add(new DataColumn("اسم المنتج"));
                if (showAll || (options != null && options.ShowSenderPhone))
                    dt.Columns.Add(new DataColumn("رقم تليفون الراسل"));
                if (showAll || (options != null && options.ShowSenderName))
                    dt.Columns.Add(new DataColumn("اسم الراسل"));
                if (showAll || (options != null && options.ShowOrderCost))
                    dt.Columns.Add(new DataColumn("سعر الطلب"));
                if (showAll || (options != null && options.ShowDeliveryFees))
                    dt.Columns.Add(new DataColumn("سعر الشحن"));
                if (showAll || (options != null && options.ShowClientCode))
                    dt.Columns.Add(new DataColumn("كود العميل"));

                // لو showAll نضيف الأعمدة الإضافية القديمة
                if (showAll)
                {
                    dt.Columns.Add(new DataColumn("التسويه"));
                    dt.Columns.Add(new DataColumn("تم دفعه"));
                    dt.Columns.Add(new DataColumn("ملاحظات المندوب"));
                }

                string DriverNotes = "";
                string DeliveryName = "";
                DateTime CreatedOn;
                foreach (var item in order)
                {
                    string status = GetStatusText(item);
                    string Complete = item.OrderCompleted == Models.Enums.OrderCompleted.NOK ? "لم يتم تسويتها" : "تم تسويتها";

                    CreatedOn = TimeZoneInfo.ConvertTimeFromUtc(item.CreateOn, TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time"));
                    DriverNotes = "";
                    if (item.OrderNotes != null && item.OrderNotes.Count > 0)
                    {
                        DriverNotes = item.OrderNotes.OrderBy(x => x.Id).Last().Content;
                    }
                    DeliveryName = "";
                    if (item.DeliveryId != null && item.Delivery != null)
                    {
                        DeliveryName = item.Delivery.Name;
                    }
                    string formattedDateTime = CreatedOn.ToString("dd MMM, yyyy - hh:mm tt");

                    var row = dt.NewRow();

                    // عمود حالة الشحنة - أول عمود
                    if (showAll || (options != null && options.ShowStatus))
                        row["الحاله"] = status;

                    // الأعمدة الأساسية
                    row["رقم الطلب"] = item.Code;
                    row["التاريخ"] = formattedDateTime;
                    row["المرسل إليه"] = item.ClientName;
                    row["تليفون المرسل إليه"] = item.ClientPhone;
                    row["المحافظة"] = item.AddressCity ?? "";
                    row["العنوان"] = item.Address ?? "";
                    row["المطلوب دفعه"] = item.TotalCost;
                    row["المندوب"] = DeliveryName;
                    row["الملاحظات"] = item.Notes ?? "";

                    // الأعمدة الاختيارية
                    if (showAll || (options != null && options.ShowProductName))
                        row["اسم المنتج"] = ""; // يمكن إضافة حقل المنتج لاحقاً
                    if (showAll || (options != null && options.ShowSenderPhone))
                        row["رقم تليفون الراسل"] = item.Client?.PhoneNumber ?? "";
                    if (showAll || (options != null && options.ShowSenderName))
                        row["اسم الراسل"] = item.Client?.Name ?? "";
                    if (showAll || (options != null && options.ShowOrderCost))
                        row["سعر الطلب"] = item.Cost;
                    if (showAll || (options != null && options.ShowDeliveryFees))
                        row["سعر الشحن"] = item.DeliveryFees;
                    if (showAll || (options != null && options.ShowClientCode))
                        row["كود العميل"] = item.ClientCode ?? "";

                    // الأعمدة الإضافية القديمة
                    if (showAll)
                    {
                        row["التسويه"] = Complete;
                        row["تم دفعه"] = item.ArrivedCost;
                        row["ملاحظات المندوب"] = DriverNotes;
                    }

                    dt.Rows.Add(row);
                }
                return dt;
            }

            private static string GetStatusText(Order item)
            {
                if (item.IsDeleted) return "محذوف";
                return item.Status switch
                {
                    Models.Enums.OrderStatus.Placed => "جديد",
                    Models.Enums.OrderStatus.PartialReturned => "مرتجع جزئي",
                    Models.Enums.OrderStatus.Returned => "مرتجع كامل",
                    Models.Enums.OrderStatus.Returned_And_DeliveryCost_On_Sender => "مرتجع وشحن على الراسل",
                    Models.Enums.OrderStatus.Returned_And_Paid_DeliveryCost => "مرتجع ودفع شحن",
                    Models.Enums.OrderStatus.Delivered_With_Edit_Price => "تم التوصيل مع تعديل السعر",
                    Models.Enums.OrderStatus.PartialDelivered => "تم التوصيل جزئي",
                    Models.Enums.OrderStatus.Assigned => "جارى التوصيل",
                    Models.Enums.OrderStatus.Delivered => "تم التوصيل",
                    Models.Enums.OrderStatus.Rejected => "مرفوض",
                    Models.Enums.OrderStatus.Waiting => "مؤجل",
                    Models.Enums.OrderStatus.Completed => "تم تسويته",
                    _ => ""
                };
            }
        }
    }
}
