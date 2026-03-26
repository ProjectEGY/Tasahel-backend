using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostexS.Interfaces;
using PostexS.Models.Domain;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Controllers
{
    [Authorize(Roles = "Admin,TrustAdmin")]
    public class PrivacyPolicyController : Controller
    {
        private readonly IGeneric<PrivacyPolicy> _privacy;
        public PrivacyPolicyController(IGeneric<PrivacyPolicy> privacy)
        {
            _privacy = privacy;
        }

        public async Task<IActionResult> Index()
        {
            var policy = _privacy.Get(x => !x.IsDeleted).FirstOrDefault();
            if (policy == null)
            {
                policy = new PrivacyPolicy { Arabic = "", English = "" };
                await _privacy.Add(policy);
            }
            return View(policy);
        }

        [HttpPost]
        public async Task<IActionResult> Index(PrivacyPolicy model)
        {
            await _privacy.Update(model);
            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        public IActionResult WebView(string lang = "ar")
        {
            ViewBag.lang = lang;
            var policy = _privacy.Get(x => !x.IsDeleted).FirstOrDefault();
            if (policy == null)
            {
                policy = new PrivacyPolicy { Arabic = "", English = "" };
            }
            return View(policy);
        }
    }
}
