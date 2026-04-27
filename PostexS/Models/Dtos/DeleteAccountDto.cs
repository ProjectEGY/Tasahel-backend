using System.ComponentModel.DataAnnotations;

namespace PostexS.Models.Dtos
{
    /// <summary>
    /// DTO لحذف الحساب من تطبيق الموبايل (Apple يطلب وجود endpoint لحذف الحساب).
    /// لتأكيد الهوية، نطلب كلمة السر قبل الحذف.
    /// </summary>
    public class DeleteAccountDto
    {
        /// <summary>
        /// كلمة السر الحالية للتأكد من هوية المستخدم.
        /// </summary>
        [Required(ErrorMessage = "كلمة السر مطلوبة لتأكيد عملية الحذف")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        /// <summary>
        /// سبب الحذف (اختياري) — للسجلات الإحصائية.
        /// </summary>
        public string Reason { get; set; }
    }
}
