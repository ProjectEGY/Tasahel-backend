using Microsoft.AspNetCore.Http;
using PostexS.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace PostexS.Models.ViewModels
{
    public class OrdersVM
    {
        public string ClientId { get; set; }
        [Required(ErrorMessage = "ملف المنتجات مطلوب")]
        public IFormFile file { get; set; }
        [Display(Name = "استخدام الأكواد المرفوعة؟")]
        public bool UseUploadedCodes { get; set; } = false; // القيمة الافتراضية false

    }
    public class UsersVM
    {
        public long BranchId { get; set; }
        public UserType UserType { get; set; }
        [Required(ErrorMessage = "ملف المستخدمين الجداد مطلوب")]
        public IFormFile file { get; set; }
    }
}