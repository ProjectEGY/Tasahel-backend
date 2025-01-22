using ExcelDataReader.Log;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PostexS.Interfaces;
using PostexS.Models.Domain;
using SixLabors.ImageSharp.Drawing;
using System;
using System.Drawing;
using System.Linq;
using ZXing;

namespace PostexS.Controllers
{
    public class BarcodeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IGeneric<ApplicationUser> _user;
        private readonly IGeneric<Order> _orders;
        private readonly IConfiguration _configuration;
        public BarcodeController(ILogger<HomeController> logger, SignInManager<ApplicationUser> signInManager,
           UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,
           IGeneric<ApplicationUser> user, IConfiguration configuration, IGeneric<Order> orders)
        {
            _logger = logger;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _userManager = userManager;
            _user = user;
            _configuration = configuration;
            _orders = orders;
        }
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public string ScanBarcode(string data)
        {
            // معالجة القيمة المرسلة
            long num = System.Int64.Parse(data);
            num -= 1000;
            string url = "/Orders/Details?id=" + num.ToString();
            // إرجاع النتيجة إلى الجانب الخاص بالعميل
            return url;
        }
    }
}
