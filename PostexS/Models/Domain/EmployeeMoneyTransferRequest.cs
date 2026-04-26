using PostexS.Models.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PostexS.Models.Domain
{
    public class EmployeeMoneyTransferRequest : BaseModel
    {
        // الموظف اللي قدّم الطلب
        [Required]
        public string EmployeeUserId { get; set; }

        [ForeignKey("EmployeeUserId")]
        public virtual ApplicationUser Employee { get; set; }

        // المبلغ اللي قاله الموظف
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "المبلغ لازم يكون أكبر من صفر")]
        public double RequestedAmount { get; set; }

        // ملاحظات الموظف
        public string EmployeeNotes { get; set; }

        // حالة الطلب
        public TransferRequestStatus Status { get; set; } = TransferRequestStatus.Pending;

        // الأدمن اللي وافق/رفض (null لحد ما يتاخد قرار)
        public string AdminUserId { get; set; }

        [ForeignKey("AdminUserId")]
        public virtual ApplicationUser Admin { get; set; }

        // المبلغ الفعلي اللي الأدمن أكّده (null في الـ Pending)
        public double? ApprovedAmount { get; set; }

        // ملاحظات الأدمن
        public string AdminNotes { get; set; }

        // علم: لو فيه فرق بين الطلب والمعتمد
        public bool HasDiscrepancy { get; set; }

        // ملاحظة الفرق التلقائية
        public string DiscrepancyNote { get; set; }

        // تاريخ المعالجة (موافقة/رفض)
        public DateTime? ProcessedAt { get; set; }

        // ربط بسجلات المحفظة الناتجة (للـ tracking)
        public long? EmployeeWalletEntryId { get; set; }
        public long? AdminWalletEntryId { get; set; }
    }
}
