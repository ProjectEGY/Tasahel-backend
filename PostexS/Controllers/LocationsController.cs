using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostexS.Models.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Controllers
{
    /// <summary>
    /// إدارة جدول اللوكيشنز - عرض الإحصائيات والمسح اليدوي للأدمن.
    /// المسح التلقائي بيشتغل من LocationCleanupService في الخلفية.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class LocationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LocationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: صفحة إدارة اللوكيشنز
        public async Task<IActionResult> Index()
        {
            var now = DateTime.UtcNow;
            var totalCount = await _context.Locations.CountAsync();
            var last24hCount = await _context.Locations.CountAsync(l => l.CreateOn >= now.AddDays(-1));
            var last7dCount = await _context.Locations.CountAsync(l => l.CreateOn >= now.AddDays(-7));
            var olderThan7dCount = await _context.Locations.CountAsync(l => l.CreateOn < now.AddDays(-7));
            var olderThan30dCount = await _context.Locations.CountAsync(l => l.CreateOn < now.AddDays(-30));

            var oldest = await _context.Locations
                .OrderBy(l => l.CreateOn)
                .Select(l => (DateTime?)l.CreateOn)
                .FirstOrDefaultAsync();

            ViewBag.TotalCount = totalCount;
            ViewBag.Last24hCount = last24hCount;
            ViewBag.Last7dCount = last7dCount;
            ViewBag.OlderThan7dCount = olderThan7dCount;
            ViewBag.OlderThan30dCount = olderThan30dCount;
            ViewBag.OldestDate = oldest;

            return View();
        }

        // POST: مسح اللوكيشنز الأقدم من عدد أيام معين
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOlderThan(int days)
        {
            if (days < 0)
            {
                TempData["Error"] = "عدد الأيام لازم يكون صفر أو أكثر";
                return RedirectToAction(nameof(Index));
            }

            var cutoff = DateTime.UtcNow.AddDays(-days);
            var deleted = await _context.Database.ExecuteSqlRawAsync(
                "DELETE FROM Locations WHERE CreateOn < {0}", cutoff);

            TempData["Success"] = $"تم حذف {deleted} لوكيشن أقدم من {days} يوم";
            return RedirectToAction(nameof(Index));
        }

        // POST: مسح كل اللوكيشنز (خطير)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAll(string confirm)
        {
            if (confirm != "DELETE")
            {
                TempData["Error"] = "لم يتم تأكيد المسح بشكل صحيح";
                return RedirectToAction(nameof(Index));
            }

            var deleted = await _context.Database.ExecuteSqlRawAsync("DELETE FROM Locations");
            TempData["Success"] = $"تم مسح جدول اللوكيشنز بالكامل ({deleted} سجل)";
            return RedirectToAction(nameof(Index));
        }
    }
}
