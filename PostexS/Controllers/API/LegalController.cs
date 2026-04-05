using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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
        private readonly IGeneric<ApplicationUser> _user;

        public LegalController(
            IGeneric<TermsAndCondition> terms,
            IGeneric<PrivacyPolicy> privacy,
            IGeneric<ApplicationUser> user)
        {
            _terms = terms;
            _privacy = privacy;
            _user = user;
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
        /// التحقق من حالة الحساب - يرجع هل المستخدم متاح ولا محذوف
        /// </summary>
        [HttpGet("CheckAccountStatus")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public IActionResult CheckAccountStatus()
        {
            var userId = User.Identity.Name;
            var user = _user.Get(x => x.Id == userId).FirstOrDefault();

            var isActive = user != null && !user.IsDeleted;

            return Ok(new
            {
                errorCode = 0,
                errorMessage = "Success",
                data = new { isActive }
            });
        }

        /// <summary>
        /// رابط صفحة حذف الحساب
        /// </summary>
        /// <param name="app">نوع التطبيق: driver أو sender (افتراضي driver)</param>
        [HttpGet("DeleteAccountUrl")]
        public IActionResult DeleteAccountUrl(string app = "driver")
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var path = app == "sender" ? "Sender" : "Driver";

            return Ok(new
            {
                errorCode = 0,
                errorMessage = "Success",
                data = new { url = $"{baseUrl}/DeleteAccount/{path}" }
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
