using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.ViewModel
{
    public class LoginVM
    {
        [Required(ErrorMessage = "برجاء كتابة البريد الالكترونى")]
        [EmailAddress(ErrorMessage = "البريد الالكترونى غير صحيح")]
        public string Email { get; set; }

        [Required(ErrorMessage = "برجاء كتابة كلمة المرور")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }
    }
}
