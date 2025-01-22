using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.ViewModel
{
    public class ChangePasswordVm
    {
        public string UserId { get; set; }
        [StringLength(100, MinimumLength = 6, ErrorMessage = "كلمه السر يجب ان لا تقل عن 6 أحرف")]
        [DataType(DataType.Password)]
        [Required(ErrorMessage = "كلمه السر الجديدة مطلوبة")]
        public string Password { get; set; }
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "كلمة السر غير متطابقة")]
        [Required(ErrorMessage = "تأكيد كلمه السر الجديدة مطلوبة")]
        public string ConfirmPassword { get; set; }
        public string? q { get; set; }
    }
}
