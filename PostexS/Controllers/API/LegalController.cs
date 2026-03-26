using Microsoft.AspNetCore.Mvc;
using PostexS.Interfaces;
using PostexS.Models.Domain;
using System.Linq;

namespace PostexS.Controllers.API
{
    /// <summary>
    /// الشروط والأحكام وسياسة الخصوصية - يرجع المحتوى كـ HTML للعرض في التطبيقات
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class LegalController : ControllerBase
    {
        private readonly IGeneric<TermsAndCondition> _terms;
        private readonly IGeneric<PrivacyPolicy> _privacy;

        public LegalController(IGeneric<TermsAndCondition> terms, IGeneric<PrivacyPolicy> privacy)
        {
            _terms = terms;
            _privacy = privacy;
        }

        /// <summary>
        /// الشروط والأحكام - يرجع المحتوى كـ HTML
        /// </summary>
        /// <param name="lang">اللغة: ar أو en (افتراضي ar)</param>
        [HttpGet("TermsAndConditions")]
        public IActionResult TermsAndConditions(string lang = "ar")
        {
            var terms = _terms.Get(x => !x.IsDeleted).FirstOrDefault();
            if (terms == null)
                return Ok(new { errorCode = 1, errorMessage = "NotFound", data = (object)null });

            var content = lang == "en" ? terms.English : terms.Arabic;
            return Ok(new
            {
                errorCode = 0,
                errorMessage = "Success",
                data = new
                {
                    content = content ?? "",
                    lastUpdated = terms.ModifiedOn ?? terms.CreateOn
                }
            });
        }

        /// <summary>
        /// سياسة الخصوصية - يرجع المحتوى كـ HTML
        /// </summary>
        /// <param name="lang">اللغة: ar أو en (افتراضي ar)</param>
        [HttpGet("PrivacyPolicy")]
        public IActionResult PrivacyPolicy(string lang = "ar")
        {
            var policy = _privacy.Get(x => !x.IsDeleted).FirstOrDefault();
            if (policy == null)
                return Ok(new { errorCode = 1, errorMessage = "NotFound", data = (object)null });

            var content = lang == "en" ? policy.English : policy.Arabic;
            return Ok(new
            {
                errorCode = 0,
                errorMessage = "Success",
                data = new
                {
                    content = content ?? "",
                    lastUpdated = policy.ModifiedOn ?? policy.CreateOn
                }
            });
        }
    }
}
