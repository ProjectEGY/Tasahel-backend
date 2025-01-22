using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostexS.Interfaces;
using PostexS.Models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Controllers
{
    [Authorize(Roles = "Admin,TrustAdmin")]
    public class ContactUsController : Controller
    {
        private readonly IGeneric<ContactUs> _contactUs;
        public ContactUsController(IGeneric<ContactUs> contactUs)
        {
            _contactUs = contactUs;
        }
        public IActionResult Index()
        {

            var x = _contactUs.Get(x => !x.IsDeleted).First();
            return View(x);
        }
        [HttpPost]
        public async Task<IActionResult> Index(ContactUs model)
        {
            await _contactUs.Update(model);
            return RedirectToAction(nameof(Index));
        }
    }
}
