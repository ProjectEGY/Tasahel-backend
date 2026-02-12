using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PostexS.Helper;
using PostexS.Interfaces;
using PostexS.Models.Domain;
using PostexS.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Controllers
{
    [Authorize(Roles = "Admin,TrustAdmin")]
    public class LandingSettingsController : Controller
    {
        private readonly IGeneric<LandingPageContent> _content;
        private readonly IGeneric<LandingStatistic> _statistics;
        private readonly IGeneric<LandingTestimonial> _testimonials;
        private readonly IGeneric<LandingPartner> _partners;
        private readonly IWebHostEnvironment _hostEnvironment;

        public LandingSettingsController(
            IGeneric<LandingPageContent> content,
            IGeneric<LandingStatistic> statistics,
            IGeneric<LandingTestimonial> testimonials,
            IGeneric<LandingPartner> partners,
            IWebHostEnvironment hostEnvironment)
        {
            _content = content;
            _statistics = statistics;
            _testimonials = testimonials;
            _partners = partners;
            _hostEnvironment = hostEnvironment;
        }

        // الصفحة الرئيسية لإدارة Landing Page
        public IActionResult Index()
        {
            ViewBag.Contents = _content.Get(x => !x.IsDeleted).OrderBy(x => x.SortOrder).ToList();
            ViewBag.Statistics = _statistics.Get(x => !x.IsDeleted).OrderBy(x => x.SortOrder).ToList();
            ViewBag.Testimonials = _testimonials.Get(x => !x.IsDeleted).OrderBy(x => x.SortOrder).ToList();
            ViewBag.Partners = _partners.Get(x => !x.IsDeleted).OrderBy(x => x.SortOrder).ToList();
            return View();
        }

        #region المحتوى (Sections)

        [HttpPost]
        public async Task<IActionResult> SaveContent(LandingPageContent model, IFormFile ImageFile)
        {
            if (ImageFile != null)
            {
                model.ImageUrl = await MediaControl.Upload(FilePath.Landing, ImageFile, _hostEnvironment);
            }

            if (model.Id > 0)
            {
                var existing = await _content.GetObj(x => x.Id == model.Id);
                if (existing != null)
                {
                    existing.SectionKey = model.SectionKey;
                    existing.TitleAr = model.TitleAr;
                    existing.SubTitleAr = model.SubTitleAr;
                    existing.DescriptionAr = model.DescriptionAr;
                    existing.IconClass = model.IconClass;
                    existing.LinkUrl = model.LinkUrl;
                    existing.SortOrder = model.SortOrder;
                    existing.IsActive = model.IsActive;
                    existing.IsModified = true;
                    existing.ModifiedOn = DateTime.Now.ToUniversalTime();
                    if (!string.IsNullOrEmpty(model.ImageUrl))
                        existing.ImageUrl = model.ImageUrl;
                    await _content.Update(existing);
                }
            }
            else
            {
                await _content.Add(model);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteContent(long id)
        {
            var item = await _content.GetObj(x => x.Id == id);
            if (item != null)
            {
                item.IsDeleted = true;
                item.DeletedOn = DateTime.Now.ToUniversalTime();
                await _content.Update(item);
            }
            return Json(new { success = true, message = "تم الحذف بنجاح" });
        }

        #endregion

        #region الإحصائيات

        [HttpPost]
        public async Task<IActionResult> SaveStatistic(LandingStatistic model)
        {
            if (model.Id > 0)
            {
                var existing = await _statistics.GetObj(x => x.Id == model.Id);
                if (existing != null)
                {
                    existing.Title = model.Title;
                    existing.Value = model.Value;
                    existing.IconClass = model.IconClass;
                    existing.SortOrder = model.SortOrder;
                    existing.IsActive = model.IsActive;
                    existing.IsModified = true;
                    existing.ModifiedOn = DateTime.Now.ToUniversalTime();
                    await _statistics.Update(existing);
                }
            }
            else
            {
                await _statistics.Add(model);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteStatistic(long id)
        {
            var item = await _statistics.GetObj(x => x.Id == id);
            if (item != null)
            {
                item.IsDeleted = true;
                item.DeletedOn = DateTime.Now.ToUniversalTime();
                await _statistics.Update(item);
            }
            return Json(new { success = true, message = "تم الحذف بنجاح" });
        }

        #endregion

        #region آراء العملاء

        [HttpPost]
        public async Task<IActionResult> SaveTestimonial(LandingTestimonial model, IFormFile TestimonialImage)
        {
            if (TestimonialImage != null)
            {
                model.ImageUrl = await MediaControl.Upload(FilePath.Landing, TestimonialImage, _hostEnvironment);
            }

            if (model.Id > 0)
            {
                var existing = await _testimonials.GetObj(x => x.Id == model.Id);
                if (existing != null)
                {
                    existing.ClientName = model.ClientName;
                    existing.Content = model.Content;
                    existing.Rating = model.Rating;
                    existing.SortOrder = model.SortOrder;
                    existing.IsActive = model.IsActive;
                    existing.IsModified = true;
                    existing.ModifiedOn = DateTime.Now.ToUniversalTime();
                    if (!string.IsNullOrEmpty(model.ImageUrl))
                        existing.ImageUrl = model.ImageUrl;
                    await _testimonials.Update(existing);
                }
            }
            else
            {
                await _testimonials.Add(model);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTestimonial(long id)
        {
            var item = await _testimonials.GetObj(x => x.Id == id);
            if (item != null)
            {
                item.IsDeleted = true;
                item.DeletedOn = DateTime.Now.ToUniversalTime();
                await _testimonials.Update(item);
            }
            return Json(new { success = true, message = "تم الحذف بنجاح" });
        }

        #endregion

        #region الشركاء

        [HttpPost]
        public async Task<IActionResult> SavePartner(LandingPartner model, IFormFile PartnerLogo)
        {
            if (PartnerLogo != null)
            {
                model.LogoUrl = await MediaControl.Upload(FilePath.Landing, PartnerLogo, _hostEnvironment);
            }

            if (model.Id > 0)
            {
                var existing = await _partners.GetObj(x => x.Id == model.Id);
                if (existing != null)
                {
                    existing.Name = model.Name;
                    existing.WebsiteUrl = model.WebsiteUrl;
                    existing.SortOrder = model.SortOrder;
                    existing.IsActive = model.IsActive;
                    existing.IsModified = true;
                    existing.ModifiedOn = DateTime.Now.ToUniversalTime();
                    if (!string.IsNullOrEmpty(model.LogoUrl))
                        existing.LogoUrl = model.LogoUrl;
                    await _partners.Update(existing);
                }
            }
            else
            {
                await _partners.Add(model);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeletePartner(long id)
        {
            var item = await _partners.GetObj(x => x.Id == id);
            if (item != null)
            {
                item.IsDeleted = true;
                item.DeletedOn = DateTime.Now.ToUniversalTime();
                await _partners.Update(item);
            }
            return Json(new { success = true, message = "تم الحذف بنجاح" });
        }

        #endregion
    }
}
