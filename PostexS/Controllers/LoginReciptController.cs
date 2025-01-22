using DocumentFormat.OpenXml.Office2021.DocumentTasks;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PostexS.Helper;
using PostexS.Interfaces;
using PostexS.Migrations;
using PostexS.Models.Domain;
using PostexS.Models.Enums;
using PostexS.Models.ViewModels;
using PostexS.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Controllers
{
    public class LoginReciptController : Controller
    {
        private readonly ILogger<LoginReciptController> _logger;
        private readonly IGeneric<ApplicationUser> _user;
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManger;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IGeneric<Branch> _branch;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public LoginReciptController(ILogger<LoginReciptController> logger, 
          IGeneric<ApplicationUser> user, IGeneric<Branch> branch, RoleManager<IdentityRole> roleManager, IConfiguration configurationUserManager, UserManager<ApplicationUser> userManger, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _user = user;
            _configuration = configurationUserManager;
            _userManger = userManger;
            _branch = branch;
            _roleManager = roleManager;  
            _webHostEnvironment = webHostEnvironment;
        }
        [HttpGet]
        public  IActionResult Login()
        {
            ViewBag.Branchs = _branch.Get(x => !x.IsDeleted).ToList();
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(AddClientVm model)
        {
            //if (await _user.IsExist(x => x.PhoneNumber == model.Phone))
            //{
            //    return BadRequest("هذا الرقم موجود من قبل");
            //}

            string Email = "";
            if (string.IsNullOrEmpty(model.Email))
            {

                Email = RandomGenerator.GenerateString(4) + "@Tasahel.com";
            }
            else
            {
                if (await _user.IsExist(x => x.Email.Trim().ToLower() == model.Email.Trim().ToLower()))
                {
                    return BadRequest("هذا الايميل موجود من قبل");
                }
                Email = model.Email;
            }
            var user = new ApplicationUser()
            {
                UserName = Email,
                Email = Email,
                Name = model.Name,
                PhoneNumber = model.Phone,
                SecurityStamp = Guid.NewGuid().ToString(),
                Address = model.Address,
                WhatsappPhone = model.WhatsappPhone,
                UserType = UserType.Client,
                BranchId = model.BranchId,
                IsPending=false
                
            };
            //var file = HttpContext.Request.Form.Files.GetFile("IdentityFrontPhoto");
            //if (file != null)
            //{
            //    user.IdentityFrontPhoto = await MediaControl.Upload(FilePath.Users, file, _webHostEnvironment);
            //}
            //var file1 = HttpContext.Request.Form.Files.GetFile("IdentityBackPhoto");
            //if (file1 != null)
            //{
            //    user.IdentityBackPhoto = await MediaControl.Upload(FilePath.Users, file1, _webHostEnvironment);
            //}
            //var file2 = HttpContext.Request.Form.Files.GetFile("RidingLecencePhoto");
            //if (file2 != null)
            //{
            //    user.RidingLecencePhoto = await MediaControl.Upload(FilePath.Users, file2, _webHostEnvironment);
            //}
            //var file3 = HttpContext.Request.Form.Files.GetFile("ViecleLecencePhoto");
            //if (file3 != null)
            //{
            //    user.ViecleLecencePhoto = await MediaControl.Upload(FilePath.Users, file3, _webHostEnvironment);
            //}
            //var file4 = HttpContext.Request.Form.Files.GetFile("FishPhotoPhoto");
            //if (file4 != null)
            //{
            //    user.FishPhotoPhoto = await MediaControl.Upload(FilePath.Users, file4, _webHostEnvironment);
            //}
            var result = await _userManger.CreateAsync(user, "123456");
            if (!result.Succeeded)
            {
                return BadRequest("من فضلك حاول مره اخري لاحقاً");
            }
            
                await _userManger.AddToRoleAsync(user, "Client");
            
         
            return RedirectToAction("Start", "Home");
        }
    }
}
