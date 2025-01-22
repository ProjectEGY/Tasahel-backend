using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using PostexS.Interfaces;
using PostexS.Models;
using PostexS.Models.Domain;
using PostexS.Models.Enums;
using PostexS.Models.ViewModel;
using PostexS.Models.ViewModels;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace PostexS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IGeneric<ApplicationUser> _user;
        private readonly IGeneric<Order> _orders;
        private readonly IGeneric<Branch> _branch;
        private readonly IConfiguration _configuration;
        public HomeController(ILogger<HomeController> logger, SignInManager<ApplicationUser> signInManager,
           UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,
          IGeneric<Branch> br, IGeneric<ApplicationUser> user, IConfiguration configuration, IGeneric<Order> orders)
        {
            _logger = logger;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _userManager = userManager;
            _user = user;
            _configuration = configuration;
            _orders = orders;
            _branch = br;
        }
        [Authorize]
        public async Task<IActionResult> Index(DateTime? FilterTime, DateTime? FilterTimeTo)
        {
            var id = _userManager.GetUserId(User);
            if (User.IsInRole("Client"))
            {
                if (!await _user.IsExist(x => x.Id == id))
                {
                    return NotFound();
                }
                var user = _user.Get(x => x.Id == id).First();
                CurrentStatisticsVM model = new CurrentStatisticsVM();
                model.Name = user.Name;
                ViewBag.Title = "الإحصائيات الحاليه : " + model.Name;

                if (user != null)
                {

                    //عدد الطلبات الحاليه
                    model.CurrentOrdersCount = _orders.Get(x => x.ClientId == id
                            && !x.IsDeleted && x.Status != OrderStatus.Waiting && x.Status != OrderStatus.Assigned && x.Status != OrderStatus.Placed).Count();
                    var orders = _orders.Get(x =>
           (x.Status == OrderStatus.Delivered
           || (x.Status == OrderStatus.Waiting)
           || (x.Status == OrderStatus.Rejected)
           || (x.Status == OrderStatus.PartialDelivered)
           || (x.Status == OrderStatus.Returned)
           ) && !x.Finished && !x.IsDeleted
           && x.ClientId == id).ToList();

                    model.ReturnedCount = orders.Count(x => x.Status == OrderStatus.Returned);
                    model.PartialDeliveredCount = orders.Count(x => x.Status == OrderStatus.PartialDelivered);
                    model.DeliveredCount = orders.Count(x => x.Status == OrderStatus.Delivered);
                    model.WaitingCount = orders.Count(x => x.Status == OrderStatus.Waiting);
                    model.RejectedCount = orders.Count(x => x.Status == OrderStatus.Rejected);
                    model.AllOrdersCount = model.CurrentOrdersCount + orders.Count();

                    var OrdersMoney = orders.Where(x => x.Status != OrderStatus.PartialReturned).Sum(x => x.ArrivedCost);
                    model.OrdersMoney = OrdersMoney;
                    var DriverMoney = orders.Where(x => x.Status != OrderStatus.PartialReturned).Sum(x => x.ClientCost);
                    model.DriverMoney = DriverMoney;
                    model.SystemMoney = OrdersMoney - DriverMoney;
                    // حساب النسب المئوية
                    if (model.AllOrdersCount > 0)
                    {
                        model.DeliveredPercentage = (double)model.DeliveredCount / model.AllOrdersCount * 100;
                        model.ReturnedPercentage = (double)model.ReturnedCount / model.AllOrdersCount * 100;
                    }
                }

                return View(model);
            }

            /* if (!await _roleManager.RoleExistsAsync("Driver"))
                 await _roleManager.CreateAsync(new IdentityRole("Driver"));
             if (!await _roleManager.RoleExistsAsync("Client"))
                 await _roleManager.CreateAsync(new IdentityRole("Client"));*/



            /*  if (!User.Identity.IsAuthenticated)
              {
                  return RedirectToAction(nameof(Login));
              }*/

            var wallet = (await _user.GetObj(x => x.Id == "9897454b-add0-45ef-ad3b-53027814ede7")).Wallet;
            if (FilterTime == null && FilterTimeTo == null)
            {
                ViewBag.wallet = wallet;
                return View();
            }
            wallet = _orders.Get(x => (FilterTime.HasValue ? DateTime.Compare(FilterTime.Value.ToUniversalTime(), x.CreateOn) <= 0 : true) &&
            (FilterTimeTo.HasValue ? DateTime.Compare(FilterTimeTo.Value.ToUniversalTime(), x.CreateOn) >= 0 : true)).Sum(x => x.ArrivedCost - (x.ClientCost + x.DeliveryCost));
            ViewBag.wallet = wallet;
            return View();



        }
        public async Task<IActionResult> Login()
        {
            await GeneratRoles();
            if (_branch.GetAll().Count() == 0)
            {
                var model = new Branch()
                {
                    Name = "First Branch",
                    Whatsapp = "00000",
                    Address = "..",
                    PhoneNumber = "00000",
                };
                await _branch.Add(model);
            }
            var user = new ApplicationUser()
            {
                Email = "Admin@Tasahel.com",
                UserName = "Admin@Tasahel.com",
                UserType = UserType.Admin,
                Name = "Test",
                PhoneNumber = "000",
                BranchId = 1,
                SecurityStamp = Guid.NewGuid().ToString(),
                EmailConfirmed = true
            };
            var result = await _userManager.CreateAsync(user, "Admin@123");
            if (!await _roleManager.RoleExistsAsync("Admin"))
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
            await _userManager.AddToRoleAsync(user, "Admin");

            //var user2 = new ApplicationUser()
            //{
            //    Email = "SubAdmin2@Tasahel.com",
            //    UserName = "SubAdmin2@Tasahel.com",
            //    UserType = UserType.Admin,
            //    Name = "TrustAdmin2",
            //    PhoneNumber = "000",
            //    BranchId = 1,
            //    SecurityStamp = Guid.NewGuid().ToString(),
            //    EmailConfirmed = true
            //};
            //var result2 = await _userManager.CreateAsync(user2, "Admin@123");
            //if (!await _roleManager.RoleExistsAsync("TrustAdmin"))
            //    await _roleManager.CreateAsync(new IdentityRole("TrustAdmin"));
            //await _userManager.AddToRoleAsync(user2, "TrustAdmin");

            return View();
        }
        private async Task GeneratRoles()
        {
            if (!await _roleManager.RoleExistsAsync("Admin"))
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
            if (!await _roleManager.RoleExistsAsync("TrustAdmin"))
                await _roleManager.CreateAsync(new IdentityRole("TrustAdmin"));
            if (!await _roleManager.RoleExistsAsync("HighAdmin"))
                await _roleManager.CreateAsync(new IdentityRole("HighAdmin"));
            if (!await _roleManager.RoleExistsAsync("LowAdmin"))
                await _roleManager.CreateAsync(new IdentityRole("LowAdmin"));
            if (!await _roleManager.RoleExistsAsync("Accountant"))
                await _roleManager.CreateAsync(new IdentityRole("Accountant"));
            if (!await _roleManager.RoleExistsAsync("Driver"))
                await _roleManager.CreateAsync(new IdentityRole("Driver"));
            if (!await _roleManager.RoleExistsAsync("Client"))
                await _roleManager.CreateAsync(new IdentityRole("Client"));
        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginVM Model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }
            var user = await _signInManager.PasswordSignInAsync(Model.Email, Model.Password, false, false);
            if (user.Succeeded)
            {
                var currentUser = _user.Get(x => x.Email.Trim().ToLower()
                == Model.Email.Trim().ToLower()).First();
                if ((currentUser.UserType == UserType.Admin || currentUser.UserType == UserType.Client
                     || currentUser.UserType == UserType.HighAdmin || currentUser.UserType == UserType.LowAdmin || currentUser.UserType == UserType.Accountant) && !currentUser.IsDeleted)
                {
                    /* var token = GenerateToken(currentUser);*/

                    //switch (currentUser.site)
                    //{
                    //    case Site.Domain:
                    //        return Redirect("https://postexeg.com/Home/Index");
                    //    case Site.SubDomain1:
                    //        return Redirect("https://dashboard1.postexeg.com/Home/Index");
                    //    case Site.SubDomain2:
                    //        return Redirect("https://dashboard2.postexeg.com/Home/Index");
                    //    case Site.SubDomain3:
                    //        return Redirect("https://dashboard3.postexeg.com/Home/Index");
                    //    default:
                    //        return RedirectToAction("Index", "Home");
                    //}

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "غير مسموح لك بالدخول");
                    return View(Model);
                }
            }
            await _signInManager.SignOutAsync();
            ModelState.AddModelError("", "محاولة دخول خاطئه");
            return View(Model);
        }
        /* public async Task<string> GenerateToken(ApplicationUser user)
         {
             var userRoles = await _userManager.GetRolesAsync(user);
             var authClaims = new List<Claim>
                 {
                     new Claim(ClaimTypes.Name, user.Id),
                     new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                 };

             foreach (var userRole in userRoles)
             {
                 authClaims.Add(new Claim(ClaimTypes.Role, userRole));
             }

             var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

             var token = new JwtSecurityToken(
                 issuer: _configuration["JWT:ValidIssuer"],
                 audience: _configuration["JWT:ValidAudience"],
                 expires: DateTime.Now.AddYears(3),
                 claims: authClaims,
                 signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                 );
             return new JwtSecurityTokenHandler().WriteToken(token);
         }*/
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Home");
        }
        public IActionResult Start()
        {
            return View();
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        public IActionResult AccessDenied()
        {
            return RedirectToAction("index", "Home");
        }
    }
}
