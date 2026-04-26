using System;
using System.ComponentModel.DataAnnotations;

namespace PostexS.Models.Domain
{
    public class WhaStackSessionInstance : BaseModel
    {
        [Required]
        [Display(Name = "اسم الجلسة / الوصف")]
        public string DisplayName { get; set; }

        [Display(Name = "رقم الهاتف")]
        public string PhoneNumber { get; set; }

        [Required]
        [Display(Name = "Session ID")]
        public string SessionId { get; set; }

        [Display(Name = "مفعل")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "آخر استخدام")]
        public DateTime? LastUsedAt { get; set; }

        [Display(Name = "آخر فشل")]
        public DateTime? LastFailureAt { get; set; }

        [Display(Name = "حالات فشل متتالية")]
        public int ConsecutiveFailures { get; set; }

        [Display(Name = "إجمالي الرسائل المرسلة بنجاح")]
        public long TotalSentSuccess { get; set; }

        [Display(Name = "إجمالي الرسائل الفاشلة")]
        public long TotalSentFailed { get; set; }
    }
}
