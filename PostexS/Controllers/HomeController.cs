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
        private readonly IGeneric<LandingPageContent> _landingContent;
        private readonly IGeneric<LandingStatistic> _landingStats;
        private readonly IGeneric<LandingTestimonial> _landingTestimonials;
        private readonly IGeneric<LandingPartner> _landingPartners;
        private readonly IGeneric<ContactUs> _contactUs;
        public HomeController(ILogger<HomeController> logger, SignInManager<ApplicationUser> signInManager,
           UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,
          IGeneric<Branch> br, IGeneric<ApplicationUser> user, IConfiguration configuration, IGeneric<Order> orders,
          IGeneric<LandingPageContent> landingContent, IGeneric<LandingStatistic> landingStats,
          IGeneric<LandingTestimonial> landingTestimonials, IGeneric<LandingPartner> landingPartners,
          IGeneric<ContactUs> contactUs)
        {
            _logger = logger;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _userManager = userManager;
            _user = user;
            _configuration = configuration;
            _orders = orders;
            _branch = br;
            _landingContent = landingContent;
            _landingStats = landingStats;
            _landingTestimonials = landingTestimonials;
            _landingPartners = landingPartners;
            _contactUs = contactUs;
        }
        [Authorize]
        public async Task<IActionResult> Index(DateTime? FilterTime, DateTime? FilterTimeTo)
        {
            var id = _userManager.GetUserId(User);
            if (User.IsInRole("Client"))
            {
                if (!await _user.IsExist(x => x.Id == id))
                    return NotFound();

                var user = _user.Get(x => x.Id == id).First();
                CurrentStatisticsVM model = new CurrentStatisticsVM();
                model.Name = user.Name;
                ViewBag.Title = "الإحصائيات الحاليه : " + model.Name;

                if (user != null)
                {
                    var allOrders = _orders.Get(x => x.ClientId == id).ToList();

                    // الطلبات الحالية
                    model.CurrentOrdersCount = allOrders.Count(x =>
                        !x.IsDeleted && !x.Finished && !x.ReturnedFinished &&
                        (x.Status == OrderStatus.Placed || x.Status == OrderStatus.Assigned || x.Status == OrderStatus.Waiting));

                    // الطلبات المعلقة
                    model.WaitingCount = allOrders.Count(x => !x.IsDeleted && x.Status == OrderStatus.Waiting);

                    // الطلبات مع المناديب
                    model.AssignedCount = allOrders.Count(x => !x.IsDeleted && x.Status == OrderStatus.Assigned);

                    // الطلبات المنتهية
                    var finishedOrders = allOrders.Where(x =>
                        !x.IsDeleted &&
                        (x.Status == OrderStatus.Delivered || x.Status == OrderStatus.Rejected ||
                         x.Status == OrderStatus.Returned || x.Status == OrderStatus.PartialDelivered ||
                         x.Status == OrderStatus.PartialReturned || x.Status == OrderStatus.Finished ||
                         x.Status == OrderStatus.Completed || x.Status == OrderStatus.Delivered_With_Edit_Price ||
                         x.Status == OrderStatus.Returned_And_Paid_DeliveryCost ||
                         x.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender)).ToList();

                    // الطلبات المحذوفة
                    model.DeletedCount = allOrders.Count(x => x.IsDeleted);

                    // إجمالي الطلبات
                    model.AllOrdersCount = allOrders.Count;

                    model.DeliveredCount = finishedOrders.Count(x => x.Status == OrderStatus.Delivered || x.Status == OrderStatus.Delivered_With_Edit_Price);
                    model.RejectedCount = finishedOrders.Count(x => x.Status == OrderStatus.Rejected);
                    model.ReturnedCount = finishedOrders.Count(x => x.Status == OrderStatus.Returned || x.Status == OrderStatus.Returned_And_Paid_DeliveryCost || x.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender);
                    model.PartialDeliveredCount = finishedOrders.Count(x => x.Status == OrderStatus.PartialDelivered);
                    model.PartialReturnedCount = finishedOrders.Count(x => x.Status == OrderStatus.PartialReturned);

                    // حساب المبالغ المالية
                    model.OrdersMoney = finishedOrders.Sum(x => x.ArrivedCost);
                    model.DriverMoney = finishedOrders.Sum(x => x.ClientCost);
                    model.SystemMoney = model.OrdersMoney - model.DriverMoney;

                    // حساب النسب المئوية
                    if (model.AllOrdersCount > 0)
                    {
                        model.DeliveredPercentage = (double)model.DeliveredCount / model.AllOrdersCount * 100;
                        model.ReturnedPercentage = (double)model.ReturnedCount / model.AllOrdersCount * 100;
                        model.PartialDeliveredPercentage = (double)model.PartialDeliveredCount / model.AllOrdersCount * 100;
                        model.RejectedPercentage = (double)model.RejectedCount / model.AllOrdersCount * 100;
                        model.WaitingPercentage = (double)model.WaitingCount / model.AllOrdersCount * 100;
                        model.PartialReturnedPercentage = (double)model.PartialReturnedCount / model.AllOrdersCount * 100;
                        model.DeletedPercentage = (double)model.DeletedCount / model.AllOrdersCount * 100;
                        model.AssignedPercentage = (double)model.AssignedCount / model.AllOrdersCount * 100;
                    }
                }

                return View(model);
            }

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
            var existingUser = await _userManager.FindByEmailAsync("Developer@Tasahel.com");
            if (existingUser != null)
            {
                // الأكاونت موجود - حدث الباسورد
                var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
                await _userManager.ResetPasswordAsync(existingUser, token, "123456");
            }
            else
            {
                // الأكاونت مش موجود - اعمل واحد جديد
                var user = new ApplicationUser()
                {
                    Email = "Developer@Tasahel.com",
                    UserName = "Developer@Tasahel.com",
                    UserType = UserType.Admin,
                    Name = "Developer",
                    PhoneNumber = "000",
                    BranchId = 1,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    EmailConfirmed = true
                };
                var result = await _userManager.CreateAsync(user, "Admin@123");
                if (!await _roleManager.RoleExistsAsync("Admin"))
                    await _roleManager.CreateAsync(new IdentityRole("Admin"));
                await _userManager.AddToRoleAsync(user, "Admin");
            }
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
            if (!await _roleManager.RoleExistsAsync("TrackingAdmin"))
                await _roleManager.CreateAsync(new IdentityRole("TrackingAdmin"));
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
                     || currentUser.UserType == UserType.HighAdmin || currentUser.UserType == UserType.LowAdmin || currentUser.UserType == UserType.TrackingAdmin || currentUser.UserType == UserType.Accountant) && !currentUser.IsDeleted)
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

        [AllowAnonymous]
        public IActionResult Landing()
        {
            // جلب كل بيانات Landing Page الديناميكية
            var allContent = _landingContent.Get(x => !x.IsDeleted && x.IsActive).OrderBy(x => x.SortOrder).ToList();
            ViewBag.HeroContent = allContent.FirstOrDefault(x => x.SectionKey == "hero");
            ViewBag.AboutContent = allContent.FirstOrDefault(x => x.SectionKey == "about");
            ViewBag.Services = allContent.Where(x => x.SectionKey == "service").ToList();
            ViewBag.Packages = allContent.Where(x => x.SectionKey == "package").ToList();
            ViewBag.Statistics = _landingStats.Get(x => !x.IsDeleted && x.IsActive).OrderBy(x => x.SortOrder).ToList();
            ViewBag.Testimonials = _landingTestimonials.Get(x => !x.IsDeleted && x.IsActive).OrderBy(x => x.SortOrder).ToList();
            ViewBag.Partners = _landingPartners.Get(x => !x.IsDeleted && x.IsActive).OrderBy(x => x.SortOrder).ToList();

            // بيانات التواصل
            try
            {
                ViewBag.ContactInfo = _contactUs.Get(x => !x.IsDeleted).FirstOrDefault();
            }
            catch { ViewBag.ContactInfo = null; }

            return View();
        }

        // ===== Seed Landing Data from Old Start Page =====
        [Authorize(Roles = "Admin,TrustAdmin")]
        public async Task<IActionResult> SeedLandingData()
        {
            // التحقق من عدم وجود بيانات مسبقة
            var existingContent = _landingContent.Get(x => !x.IsDeleted).ToList();
            if (existingContent.Any())
            {
                TempData["SeedMessage"] = "البيانات موجودة بالفعل! تم العثور على " + existingContent.Count + " عنصر. احذف البيانات القديمة أولاً إذا أردت إعادة التهيئة.";
                return RedirectToAction("Landing");
            }

            // ===== 1. Hero Section =====
            await _landingContent.Add(new LandingPageContent
            {
                SectionKey = "hero",
                TitleAr = "تساهيل إكسبريس",
                SubTitleAr = "شريكك الأمثل في التوصيل السريع والآمن",
                DescriptionAr = "نقدم لك خدمة توصيل احترافية بأعلى جودة وأسرع وقت. نحن نهتم بطلبك من لحظة الاستلام حتى التسليم.",
                IconClass = "fas fa-truck-fast",
                LinkUrl = "/Home/Join",
                SortOrder = 1,
                IsActive = true
            });

            // ===== 2. About Section =====
            await _landingContent.Add(new LandingPageContent
            {
                SectionKey = "about",
                TitleAr = "من نحن",
                SubTitleAr = "سنوات من الخبرة في مجال الشحن والتوصيل",
                DescriptionAr = "نحن نقدم خدمات توصيل طرود سريعة ومريحة وسهلة الاستخدام تجعل المستهلكين يشعرون بالتقدير وتشعر الشركات بالتمكين. مع سنوات من الخبرة والخبرة في مجال الشحن، نحن ملتزمون بتزويد عملائنا بأفضل الخدمات وأكثرها احترافية في أقل فترة زمنية مقدرة.",
                IconClass = "fas fa-building",
                SortOrder = 1,
                IsActive = true
            });

            // ===== 3. Services Section =====
            await _landingContent.Add(new LandingPageContent
            {
                SectionKey = "service",
                TitleAr = "أفضل الأسعار",
                SubTitleAr = "أسعار مرنة ومعقولة",
                DescriptionAr = "نحن نقدم أسعارًا موثوقة ومرنة بشكل معقول لجميع الفئات مع خدمة متسقة ويمكن الاعتماد عليها ودقيقة بسعر فعال من حيث التكلفة",
                IconClass = "fas fa-dollar-sign",
                SortOrder = 1,
                IsActive = true
            });

            await _landingContent.Add(new LandingPageContent
            {
                SectionKey = "service",
                TitleAr = "أسرع شحن في مصر",
                SubTitleAr = "توصيل سريع لكل المحافظات",
                DescriptionAr = "توصيل سريع جدًا، في جميع أنحاء مصر في القاهرة والإسكندرية خلال 24 ساعة وفقط 48 ساعة إلى باقي الأماكن في جميع أنحاء مصر",
                IconClass = "fas fa-gauge-high",
                SortOrder = 2,
                IsActive = true
            });

            await _landingContent.Add(new LandingPageContent
            {
                SectionKey = "service",
                TitleAr = "24/7 الدعم",
                SubTitleAr = "خدمة عملاء متاحة دائماً",
                DescriptionAr = "نظام خدمة عملاء محترف يتفهم احتياجاتك ويسهل عليك حل مشاكلك",
                IconClass = "fas fa-headset",
                SortOrder = 3,
                IsActive = true
            });

            await _landingContent.Add(new LandingPageContent
            {
                SectionKey = "service",
                TitleAr = "تابع شحنتك في أي مكان",
                SubTitleAr = "تتبع لحظة بلحظة",
                DescriptionAr = "شغلك اونلاين وبتواجهك مشاكل كتير مع شركات الشحن! تساهيل إكسبريس وفرتلك أبلكيشن تتابع شحناتك من خلاله بكل سهولة. هتتابع شحنتك وانت في مكانك!",
                IconClass = "fas fa-map-location-dot",
                SortOrder = 4,
                IsActive = true
            });

            await _landingContent.Add(new LandingPageContent
            {
                SectionKey = "service",
                TitleAr = "شحنتك في أمان",
                SubTitleAr = "تغليف مجاني للطرود",
                DescriptionAr = "تعرف ان انطباع عميلك الأول عن البيزنس بتاعك بيكون من خلال شركة الشحن وتغليف الأوردر. وفرنالك في تساهيل خدمة التغليف المجاني عشان نساعدك تشحن اوردراتك وانت متطمن على رضا عميلك",
                IconClass = "fas fa-shield-halved",
                SortOrder = 5,
                IsActive = true
            });

            await _landingContent.Add(new LandingPageContent
            {
                SectionKey = "service",
                TitleAr = "سهولة التوصيل",
                SubTitleAr = "من الباب للباب",
                DescriptionAr = "طلبات البيت كتير ومعندكش وقت تنزل في الزحمة! متشلش هم الزحمة. طلبات البيت ومشاويرك دلوقتي أسهل مع تساهيل. هنجيبلك أي حاجة من أي مكان لحد باب بيتك",
                IconClass = "fas fa-box",
                SortOrder = 6,
                IsActive = true
            });

            // ===== 4. Packages Section =====
            await _landingContent.Add(new LandingPageContent
            {
                SectionKey = "package",
                TitleAr = "الباقة الأساسية",
                SubTitleAr = "45 جنيه",
                DescriptionAr = "بداية من 600 أوردر شهرياً\nالشحن 45 جنيه\nتشمل التغليف\nعودة التحصيل مجاناً",
                IconClass = "fas fa-cube",
                SortOrder = 1,
                IsActive = true
            });

            await _landingContent.Add(new LandingPageContent
            {
                SectionKey = "package",
                TitleAr = "الباقة المميزة",
                SubTitleAr = "40 جنيه",
                DescriptionAr = "من 1200 أوردر شهرياً\nالشحن 40 جنيه\nتشمل التغليف\nعودة التحصيل مجاناً\nأولوية في التوصيل",
                IconClass = "fas fa-cubes",
                SortOrder = 2,
                IsActive = true
            });

            await _landingContent.Add(new LandingPageContent
            {
                SectionKey = "package",
                TitleAr = "باقة كبار العملاء",
                SubTitleAr = "35 جنيه",
                DescriptionAr = "من 1800 أوردر شهرياً\nالشحن 35 جنيه\nتشمل التغليف\nعودة التحصيل مجاناً\nأولوية قصوى في التوصيل\nمندوب مخصص",
                IconClass = "fas fa-gem",
                SortOrder = 3,
                IsActive = true
            });

            // ===== 5. Statistics =====
            await _landingStats.Add(new LandingStatistic
            {
                Title = "العملاء",
                Value = "1232",
                IconClass = "fas fa-users",
                SortOrder = 1,
                IsActive = true
            });

            await _landingStats.Add(new LandingStatistic
            {
                Title = "الشركات",
                Value = "64",
                IconClass = "fas fa-building",
                SortOrder = 2,
                IsActive = true
            });

            await _landingStats.Add(new LandingStatistic
            {
                Title = "المناديب",
                Value = "42",
                IconClass = "fas fa-motorcycle",
                SortOrder = 3,
                IsActive = true
            });

            await _landingStats.Add(new LandingStatistic
            {
                Title = "طلب تم توصيله",
                Value = "15000",
                IconClass = "fas fa-box-open",
                SortOrder = 4,
                IsActive = true
            });

            // ===== 6. Testimonials =====
            await _landingTestimonials.Add(new LandingTestimonial
            {
                ClientName = "Donia Sabry",
                Content = "من افضل الشركات بجد فى التعامل والشحن. المندوبين قمه فى الاحترام والذوق. سرعه فى الشحن وتسليم الاوردرات وتحصيل المبالغ وتسليمها فى اسرع وقت وبدوت تاخير. بجد شكرا جدا ومبسوطه بالتعامل معاكوا. ويا رب دايما فى نجاح وتقدم",
                Rating = 5,
                SortOrder = 1,
                IsActive = true
            });

            await _landingTestimonials.Add(new LandingTestimonial
            {
                ClientName = "Ahmed Samir",
                Content = "اسرع خدمة وثقة وباقل تكلفة",
                Rating = 5,
                SortOrder = 2,
                IsActive = true
            });

            await _landingTestimonials.Add(new LandingTestimonial
            {
                ClientName = "Ahmed Amer",
                Content = "شركه شحن ممتازه ودقه في التعامل. اعتقد لو كملتوا علي الوضع دا هتبقوا رقم واحد في الجمهوريه. بالتوفيق ان شاء الله",
                Rating = 5,
                SortOrder = 3,
                IsActive = true
            });

            await _landingTestimonials.Add(new LandingTestimonial
            {
                ClientName = "Jomana Atef",
                Content = "شركة شحن فى منتهى المهنية. اسرع شركة جربتها ف التوصيل فعلا والتعامل ف منتهى الذوق واسرع تحصيل. مرسى ليكم وبالتوفيق",
                Rating = 5,
                SortOrder = 4,
                IsActive = true
            });

            // ===== 7. Partners (العملاء القدامى) =====
            await _landingPartners.Add(new LandingPartner
            {
                Name = "عميل 1",
                SortOrder = 1,
                IsActive = true
            });

            await _landingPartners.Add(new LandingPartner
            {
                Name = "عميل 2",
                SortOrder = 2,
                IsActive = true
            });

            await _landingPartners.Add(new LandingPartner
            {
                Name = "عميل 3",
                SortOrder = 3,
                IsActive = true
            });

            await _landingPartners.Add(new LandingPartner
            {
                Name = "عميل 4",
                SortOrder = 4,
                IsActive = true
            });

            await _landingPartners.Add(new LandingPartner
            {
                Name = "عميل 5",
                SortOrder = 5,
                IsActive = true
            });

            await _landingPartners.Add(new LandingPartner
            {
                Name = "عميل 6",
                SortOrder = 6,
                IsActive = true
            });

            await _landingPartners.Add(new LandingPartner
            {
                Name = "عميل 7",
                SortOrder = 7,
                IsActive = true
            });

            await _landingPartners.Add(new LandingPartner
            {
                Name = "عميل 8",
                SortOrder = 8,
                IsActive = true
            });

            TempData["SeedMessage"] = "تم إضافة جميع بيانات الموقع القديم بنجاح! يمكنك الآن تعديلها من إعدادات Landing Page.";
            return RedirectToAction("Landing");
        }

        [AllowAnonymous]
        public IActionResult Join()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Join(string Name, string Phone, string WhatsappPhone, string Address)
        {
            if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Phone) || string.IsNullOrEmpty(Address))
            {
                ViewBag.Error = "يرجى ملء جميع الحقول المطلوبة";
                ViewBag.Name = Name;
                ViewBag.Phone = Phone;
                ViewBag.WhatsappPhone = WhatsappPhone;
                ViewBag.Address = Address;
                return View();
            }

            // التحقق من عدم وجود رقم مسجل مسبقاً
            if (await _user.IsExist(x => x.PhoneNumber == Phone.Trim()))
            {
                ViewBag.Error = "هذا الرقم مسجل بالفعل، يرجى تسجيل الدخول";
                ViewBag.Name = Name;
                ViewBag.Phone = Phone;
                ViewBag.WhatsappPhone = WhatsappPhone;
                ViewBag.Address = Address;
                return View();
            }

            // إنشاء إيميل تلقائي
            var email = Helper.RandomGenerator.GenerateString(6) + "@Tasahel.com";

            // الحصول على أول فرع
            var branch = _branch.GetAll().FirstOrDefault();
            if (branch == null)
            {
                ViewBag.Error = "حدث خطأ، يرجى المحاولة لاحقاً";
                return View();
            }

            var user = new ApplicationUser()
            {
                UserName = email,
                Email = email,
                Name = Name.Trim(),
                PhoneNumber = Phone.Trim(),
                WhatsappPhone = WhatsappPhone?.Trim() ?? Phone.Trim(),
                Address = Address.Trim(),
                UserType = UserType.Client,
                BranchId = branch.Id,
                IsPending = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, "123456");
            if (!result.Succeeded)
            {
                ViewBag.Error = "حدث خطأ أثناء التسجيل، يرجى المحاولة لاحقاً";
                ViewBag.Name = Name;
                ViewBag.Phone = Phone;
                ViewBag.WhatsappPhone = WhatsappPhone;
                ViewBag.Address = Address;
                return View();
            }

            if (!await _roleManager.RoleExistsAsync("Client"))
                await _roleManager.CreateAsync(new IdentityRole("Client"));
            await _userManager.AddToRoleAsync(user, "Client");

            ViewBag.Success = true;
            return View();
        }

        [AllowAnonymous]
        public IActionResult TrackOrder()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult TrackOrder(string code, string phone)
        {
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(phone))
            {
                ViewBag.Error = "يرجى إدخال كود الطلب ورقم التليفون";
                return View();
            }

            var order = _orders.Get(x => x.Code == code.Trim() && !x.IsDeleted).FirstOrDefault();

            if (order == null)
            {
                ViewBag.Error = "لم يتم العثور على طلب بهذا الكود";
                ViewBag.Code = code;
                ViewBag.Phone = phone;
                return View();
            }

            // التحقق من رقم التليفون
            var cleanPhone = phone.Trim().Replace(" ", "");
            var orderPhone = (order.ClientPhone ?? "").Trim().Replace(" ", "");

            if (!orderPhone.EndsWith(cleanPhone) && !cleanPhone.EndsWith(orderPhone) && orderPhone != cleanPhone)
            {
                ViewBag.Error = "رقم التليفون غير مطابق لبيانات الطلب";
                ViewBag.Code = code;
                ViewBag.Phone = phone;
                return View();
            }

            // تمرير بيانات الطلب للعرض
            ViewBag.OrderFound = true;
            ViewBag.OrderCode = order.Code;
            ViewBag.OrderStatus = order.Status;
            ViewBag.OrderCity = order.AddressCity;
            ViewBag.OrderAddress = order.Address;
            ViewBag.OrderClientName = order.ClientName;
            ViewBag.OrderCost = order.TotalCost;
            ViewBag.OrderDate = order.CreateOn;
            ViewBag.OrderLastUpdated = order.LastUpdated;
            ViewBag.Code = code;
            ViewBag.Phone = phone;

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
