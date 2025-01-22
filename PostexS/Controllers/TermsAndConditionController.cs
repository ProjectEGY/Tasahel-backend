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
    public class TermsAndConditionController : Controller
    {
        private readonly IGeneric<TermsAndCondition> _terms;
        public TermsAndConditionController(IGeneric<TermsAndCondition> terms)
        {
            _terms = terms;
        }
        public async Task<IActionResult> Index()
        {
            return View(_terms.Get(x=>!x.IsDeleted).First());
        }
        public IActionResult webView(string lang="ar")
        {
            ViewBag.lang = lang;
            return View(_terms.Get(x => !x.IsDeleted).First());
        }
        [HttpPost]
        public async Task<IActionResult> Index(TermsAndCondition model)
        {
            await _terms.Update(model);
            return RedirectToAction(nameof(Index));
        }
    }
}
