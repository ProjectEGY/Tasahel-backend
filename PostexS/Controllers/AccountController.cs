using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PostexS.Interfaces;
using PostexS.Models.Domain;
using PostexS.Models.ViewModel;

namespace PostexS.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IGeneric<ApplicationUser> _user;
        private readonly IConfiguration _configuration;
        public AccountController(ILogger<HomeController> logger, SignInManager<ApplicationUser> signInManager,
           UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager,
           IGeneric<ApplicationUser> user, IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _userManager = userManager;
            _user = user;
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
        }
        [Authorize(Roles = "Client")]
        [Route("developer/api-keys")]
        public async Task<IActionResult> index()
        {
            var user = await _user.GetObj(x => x.Id == _userManager.GetUserId(User));
            return View(user);
        }
        [Authorize(Roles = "Client")]
        [Route("developer/api-keys/ar")]
        public async Task<IActionResult> indexAr()
        {
            var user = await _user.GetObj(x => x.Id == _userManager.GetUserId(User));
            return View(user);
        }

        [AllowAnonymous]
        [Route("developer/documentation")]
        public async Task<IActionResult> Documentation()
        {
            // If user is logged in, get their data, otherwise pass null
            ApplicationUser user = null;
            if (User.Identity.IsAuthenticated)
            {
                user = await _user.GetObj(x => x.Id == _userManager.GetUserId(User));
            }
            return View(user);
        }

        [Authorize(Roles = "Client")]
        public async Task<IActionResult> UpdateApiKeys(string lang = "ar")
        {
            var user = await _user.GetObj(x => x.Id == _userManager.GetUserId(User));

            // Generate a random GUID for the private key
            //var privateKey = Guid.NewGuid().ToString();
            var privateKey = GenerateRandomString(16); // You can adjust the length as needed

            // Convert the GUID to a Base64 string
            var privateKeyBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(privateKey));

            user.PrivateKey = $"private_{privateKeyBase64}";

            user.PublicKey = user.Id;
            user.APIkeys_UpdateOn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Arabian Standard Time"));

            if (!await _user.Update(user))
            {
                return BadRequest("من فضلك حاول في وقتٍ آخر");
            }
            if (lang == "en")
                return RedirectToAction(nameof(index));
            return RedirectToAction(nameof(indexAr));
        }
        public IActionResult Downloadfile()
        {
            try
            {
                string webRootPath = _webHostEnvironment.WebRootPath;
                string path = Path.Combine(webRootPath, "Postex.postman_collection.json");

                if (!System.IO.File.Exists(path))
                {
                    return NotFound("File not found");
                }

                byte[] fileBytes = System.IO.File.ReadAllBytes(path);
                string fileName = "Postex.postman_collection.json";

                return File(fileBytes, "application/json", fileName);
            }
            catch (Exception ex)
            {
                // Log the exception
                // _logger.LogError(ex, "Error downloading file");

                return StatusCode(500, "Internal server error");
            }
        }
        private string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
