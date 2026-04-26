using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PostexS.Models.Data;
using PostexS.Models.Domain;
using PostexS.Models.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Controllers
{
    [Authorize]
    public class EmployeeTransfersController : Controller
    {
        // ===== ID الادمن الرئيسي اللي محفظته بتستلم كل التحويلات =====
        public const string MainAdminUserId = "9897454b-add0-45ef-ad3b-53027814ede7";

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EmployeeTransfersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private bool IsMainAdmin(string userId) =>
            string.Equals(userId, MainAdminUserId, StringComparison.OrdinalIgnoreCase);

        // ============== موظف: تقديم طلب جديد ==============

        [Authorize(Roles = "Accountant,HighAdmin")]
        public IActionResult Submit() => View(new EmployeeMoneyTransferRequest());

        [HttpPost]
        [Authorize(Roles = "Accountant,HighAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(EmployeeMoneyTransferRequest model)
        {
            if (model.RequestedAmount <= 0)
            {
                ModelState.AddModelError(nameof(model.RequestedAmount), "المبلغ لازم يكون أكبر من صفر");
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            var record = new EmployeeMoneyTransferRequest
            {
                EmployeeUserId = userId,
                RequestedAmount = model.RequestedAmount,
                EmployeeNotes = (model.EmployeeNotes ?? "").Trim(),
                Status = TransferRequestStatus.Pending,
                CreateOn = DateTime.UtcNow
            };

            _context.EmployeeMoneyTransferRequests.Add(record);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تقديم الطلب بنجاح. هينتظر موافقة الأدمن الرئيسي.";
            return RedirectToAction(nameof(MyRequests));
        }

        // ============== موظف: طلباتي ==============

        [Authorize(Roles = "Accountant,HighAdmin,Admin")]
        public async Task<IActionResult> MyRequests()
        {
            var userId = _userManager.GetUserId(User);
            var items = await _context.EmployeeMoneyTransferRequests
                .Include(x => x.Admin)
                .Where(x => x.EmployeeUserId == userId && !x.IsDeleted)
                .OrderByDescending(x => x.CreateOn)
                .ToListAsync();
            return View(items);
        }

        // ============== الأدمن الرئيسي: الطلبات المعلقة ==============

        public async Task<IActionResult> Pending()
        {
            var currentUserId = _userManager.GetUserId(User);
            if (!IsMainAdmin(currentUserId))
                return Forbid();

            var items = await _context.EmployeeMoneyTransferRequests
                .Include(x => x.Employee)
                .Where(x => x.Status == TransferRequestStatus.Pending && !x.IsDeleted)
                .OrderBy(x => x.CreateOn)
                .ToListAsync();
            return View(items);
        }

        // ============== الأدمن الرئيسي: كل الطلبات ==============

        public async Task<IActionResult> All(TransferRequestStatus? status = null)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (!IsMainAdmin(currentUserId))
                return Forbid();

            var query = _context.EmployeeMoneyTransferRequests
                .Include(x => x.Employee)
                .Include(x => x.Admin)
                .Where(x => !x.IsDeleted);

            if (status.HasValue) query = query.Where(x => x.Status == status.Value);

            var items = await query.OrderByDescending(x => x.CreateOn).ToListAsync();
            ViewBag.Status = status;
            return View(items);
        }

        // ============== الأدمن الرئيسي: تاريخ تحويلات موظف معين ==============

        [Authorize(Roles = "Admin,TrustAdmin,HighAdmin")]
        public async Task<IActionResult> UserHistory(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var items = await _context.EmployeeMoneyTransferRequests
                .Include(x => x.Admin)
                .Where(x => x.EmployeeUserId == userId && !x.IsDeleted)
                .OrderByDescending(x => x.CreateOn)
                .ToListAsync();

            ViewBag.User = user;
            return View(items);
        }

        // ============== موافقة الأدمن الرئيسي ==============

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(long id, double approvedAmount, string adminNotes)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (!IsMainAdmin(currentUserId))
                return Forbid();

            if (approvedAmount <= 0)
            {
                TempData["Error"] = "المبلغ المعتمد لازم يكون أكبر من صفر";
                return RedirectToAction(nameof(Pending));
            }

            var request = await _context.EmployeeMoneyTransferRequests
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (request == null)
            {
                TempData["Error"] = "الطلب غير موجود";
                return RedirectToAction(nameof(Pending));
            }
            if (request.Status != TransferRequestStatus.Pending)
            {
                TempData["Error"] = "تم التعامل مع هذا الطلب من قبل";
                return RedirectToAction(nameof(Pending));
            }

            var employee = await _userManager.FindByIdAsync(request.EmployeeUserId);
            var admin = await _userManager.FindByIdAsync(MainAdminUserId);
            if (employee == null || admin == null)
            {
                TempData["Error"] = "تعذّر العثور على المستخدم (الموظف أو الأدمن الرئيسي)";
                return RedirectToAction(nameof(Pending));
            }

            // ===== تحديث الطلب =====
            request.Status = TransferRequestStatus.Approved;
            request.AdminUserId = currentUserId;
            request.ApprovedAmount = approvedAmount;
            request.AdminNotes = (adminNotes ?? "").Trim();
            request.ProcessedAt = DateTime.UtcNow;
            request.ModifiedOn = DateTime.UtcNow;
            request.IsModified = true;

            // علم الفرق
            if (Math.Abs(approvedAmount - request.RequestedAmount) > 0.001)
            {
                request.HasDiscrepancy = true;
                var diff = approvedAmount - request.RequestedAmount;
                var sign = diff > 0 ? "زيادة" : "نقصان";
                request.DiscrepancyNote = $"فارق {sign} بمقدار {Math.Abs(diff):0.##} جنيه. " +
                    $"الموظف قدّم {request.RequestedAmount:0.##} والأدمن اعتمد {approvedAmount:0.##}.";
            }

            // ===== حركة المحفظتين =====
            // ملحوظة: نظام المحافظ هنا "معكوس" — الرصيد السالب = الموظف عليه فلوس للأدمن.
            // الموظف بعت/سلّم فلوس للأدمن → دينه قل → الرصيد يقرب من الصفر → += (يجمع)
            // مثال: محفظة -41151، استلم الأدمن 1000 → -41151 + 1000 = -40151 (الدين قل بـ 1000)
            var empWalletBefore = employee.Wallet;
            employee.Wallet += approvedAmount;
            var empEntry = new Wallet
            {
                UserId = employee.Id,
                Amount = approvedAmount,
                TransactionType = TransactionType.EmployeeTransferOut,
                ActualUserId = currentUserId,
                Note = $"تحويل #{request.Id} للأدمن الرئيسي. " + (request.AdminNotes ?? ""),
                UserWalletLast = empWalletBefore,
                AddedToAdminWallet = true,
                CreateOn = DateTime.UtcNow
            };
            _context.Wallets.Add(empEntry);

            // محفظة الأدمن الرئيسي = الخزنة → استلم → += (الخزنة بتزيد)
            var adminWalletBefore = admin.Wallet;
            admin.Wallet += approvedAmount;
            var adminEntry = new Wallet
            {
                UserId = admin.Id,
                Amount = approvedAmount,
                TransactionType = TransactionType.EmployeeTransferIn,
                ActualUserId = currentUserId,
                Note = $"استلام تحويل #{request.Id} من {employee.Name}. " + (request.AdminNotes ?? ""),
                UserWalletLast = adminWalletBefore,
                AddedToAdminWallet = true,
                CreateOn = DateTime.UtcNow
            };
            _context.Wallets.Add(adminEntry);

            await _context.SaveChangesAsync();

            // اربط الطلب بالسجلات
            request.EmployeeWalletEntryId = empEntry.Id;
            request.AdminWalletEntryId = adminEntry.Id;
            await _context.SaveChangesAsync();

            TempData["Success"] = request.HasDiscrepancy
                ? $"تمت الموافقة بمبلغ {approvedAmount:0.##} (فيه فرق عن المبلغ المُقدَّم - تم تسجيله)."
                : $"تمت الموافقة على الطلب وتسجيل {approvedAmount:0.##} بنجاح.";
            return RedirectToAction(nameof(Pending));
        }

        // ============== رفض الأدمن الرئيسي ==============

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(long id, string adminNotes)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (!IsMainAdmin(currentUserId))
                return Forbid();

            var request = await _context.EmployeeMoneyTransferRequests
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (request == null)
            {
                TempData["Error"] = "الطلب غير موجود";
                return RedirectToAction(nameof(Pending));
            }
            if (request.Status != TransferRequestStatus.Pending)
            {
                TempData["Error"] = "تم التعامل مع هذا الطلب من قبل";
                return RedirectToAction(nameof(Pending));
            }

            request.Status = TransferRequestStatus.Rejected;
            request.AdminUserId = currentUserId;
            request.AdminNotes = (adminNotes ?? "").Trim();
            request.ProcessedAt = DateTime.UtcNow;
            request.ModifiedOn = DateTime.UtcNow;
            request.IsModified = true;

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم رفض الطلب — لم يتم خصم أي مبلغ.";
            return RedirectToAction(nameof(Pending));
        }

        // ============== موظف: حذف طلب معلّق فقط ==============

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Accountant,HighAdmin")]
        public async Task<IActionResult> CancelMyRequest(long id)
        {
            var userId = _userManager.GetUserId(User);
            var request = await _context.EmployeeMoneyTransferRequests
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.EmployeeUserId == userId);

            if (request == null)
            {
                TempData["Error"] = "الطلب غير موجود";
                return RedirectToAction(nameof(MyRequests));
            }
            if (request.Status != TransferRequestStatus.Pending)
            {
                TempData["Error"] = "لا يمكن حذف طلب تم التعامل معه";
                return RedirectToAction(nameof(MyRequests));
            }

            request.IsDeleted = true;
            request.DeletedOn = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["Success"] = "تم إلغاء الطلب";
            return RedirectToAction(nameof(MyRequests));
        }
    }
}
