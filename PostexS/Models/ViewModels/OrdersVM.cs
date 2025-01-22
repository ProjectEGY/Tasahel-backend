using Microsoft.AspNetCore.Http;
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
    }
}