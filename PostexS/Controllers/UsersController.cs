using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using ExcelDataReader;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Nancy;
using PostexS.Helper;
using PostexS.Interfaces;
using PostexS.Migrations;
using PostexS.Models.Domain;
using PostexS.Models.Dtos;
using PostexS.Models.Enums;
using PostexS.Models.ViewModel;
using PostexS.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ZXing.Common;
using ZXing;
using static PostexS.Helper.ExportToExcel;
using OrderStatus = PostexS.Models.Enums.OrderStatus;
using PostexS.Services;
using System.Transactions;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office.CustomXsn;
using Location = PostexS.Models.Domain.Location;
using FirebaseAdmin.Messaging;
using Notification = PostexS.Models.Domain.Notification;
using Microsoft.EntityFrameworkCore;

namespace PostexS.Controllers
{
    [Authorize(Roles = "Admin,HighAdmin,Accountant,LowAdmin,Client,TrustAdmin,TrackingAdmin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManger;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IGeneric<ApplicationUser> _user;
        private readonly IGeneric<Order> _orders;
        private readonly ICRUD<Order> _CRUD;
        private readonly IGeneric<OrderOperationHistory> _Histories;
        private readonly ICRUD<OrderOperationHistory> _CRUDHistory;
        private readonly IOrderService _orderService;
        private readonly IGeneric<OrderNotes> _orderNotes;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IGeneric<Wallet> _wallet;
        private readonly IWalletService _walletService;
        private readonly IGeneric<Models.Domain.Location> _locations;
        private readonly IGeneric<Branch> _branch;
        private readonly IGeneric<Notification> _notification;
        private IGeneric<DeviceTokens> _pushNotification;
        private readonly IWapilotService _wapilotService;
        private readonly IWhatsAppBotCloudService _whatsAppBotCloudService;
        private readonly IWhaStackService _whaStackService;
        private readonly IWhatsAppProviderService _providerService;

        public UsersController(UserManager<ApplicationUser> userManger, IGeneric<ApplicationUser> users, IGeneric<OrderOperationHistory> histories,
            ICRUD<OrderOperationHistory> CRUDhistory, IGeneric<Order> orders, IGeneric<Branch> branch, RoleManager<IdentityRole> roleManager,
            IGeneric<Wallet> wallet, ICRUD<Order> CRUD, IWalletService walletService,
            IOrderService orderService, IGeneric<DeviceTokens> pushNotification, IGeneric<Notification> notification, IGeneric<Location> locations, IGeneric<OrderNotes> orderNotes, IWebHostEnvironment webHostEnvironment, IWapilotService wapilotService, IWhatsAppBotCloudService whatsAppBotCloudService,
            IWhaStackService whaStackService, IWhatsAppProviderService providerService)
        {
            _userManger = userManger;
            _user = users;
            _orders = orders;
            _branch = branch;
            _CRUD = CRUD;
            _Histories = histories;
            _CRUDHistory = CRUDhistory;
            _roleManager = roleManager;
            _wallet = wallet;
            _walletService = walletService;
            _orderService = orderService;
            _orderNotes = orderNotes;
            _webHostEnvironment = webHostEnvironment;
            _locations = locations;
            _notification = notification;
            _pushNotification = pushNotification;
            _wapilotService = wapilotService;
            _whatsAppBotCloudService = whatsAppBotCloudService;
            _whaStackService = whaStackService;
            _providerService = providerService;
        }
        [Authorize(Roles = "Admin,HighAdmin,Accountant,LowAdmin,TrustAdmin,TrackingAdmin")]
        public async Task<IActionResult> Index(string q, string? message, bool deleted = false, long BranchId = -1)
        {
            if (message != null)
            {
                ViewBag.message = message;
            }
            ViewBag.Branchs = _branch.Get(x => !x.IsDeleted).ToList();
            if (User.IsInRole("HighAdmin") || User.IsInRole("Accountant") || User.IsInRole("LowAdmin") || User.IsInRole("TrackingAdmin"))
            {
                var user = await _user.GetObj(x => x.Id == _userManger.GetUserId(User));
                BranchId = user.BranchId;
                ViewBag.Branchs = _branch.Get(x => !x.IsDeleted && x.Id == BranchId).ToList();
            }
            ViewBag.branch = BranchId;
            ViewBag.deleted = 0;

            if (User.IsInRole("TrackingAdmin"))
                q = "c";

            if (q == "d")
            {
                ViewBag.q = "d";
                ViewBag.r = 1;
                if (deleted)
                {
                    ViewBag.deleted = 1;
                    return View(_user.Get(x => x.UserType == UserType.Driver
                && (BranchId == -1 ? true : x.BranchId == BranchId)
                && x.IsDeleted && !x.Branch.IsDeleted).ToList());
                }
                return View(_user.Get(x => x.UserType == UserType.Driver
                && (BranchId == -1 ? true : x.BranchId == BranchId)
                && !x.IsDeleted && !x.Branch.IsDeleted).ToList());
            }
            if (q == "a")
            {
                ViewBag.q = "a";
                ViewBag.r = 4;
                if (deleted)
                {
                    ViewBag.deleted = 1;
                    return View(_user.Get(x => (x.UserType == UserType.HighAdmin || x.UserType == UserType.Admin || x.UserType == UserType.Accountant ||
                x.UserType == UserType.LowAdmin)
                && (BranchId == -1 ? true : x.BranchId == BranchId)
                && x.IsDeleted && !x.Branch.IsDeleted).ToList());
                }
                return View(_user.Get(x => (x.UserType == UserType.HighAdmin || x.UserType == UserType.Admin || x.UserType == UserType.TrackingAdmin || x.UserType == UserType.Accountant ||
                x.UserType == UserType.LowAdmin)
                && (BranchId == -1 ? true : x.BranchId == BranchId)
                && !x.IsDeleted && !x.Branch.IsDeleted).ToList());
            }
            if (q == "c")
            {
                ViewBag.q = "c";
                ViewBag.r = 0;
                if (deleted)
                {
                    ViewBag.deleted = 1;
                    return View(_user.Get(x => x.UserType == UserType.Client && x.IsPending == true
                && (BranchId == -1 ? true : x.BranchId == BranchId)
                && x.IsDeleted && !x.Branch.IsDeleted).ToList());
                }

                return View(_user.Get(x => x.UserType == UserType.Client && x.IsPending == true
                && (BranchId == -1 ? true : x.BranchId == BranchId)
                && !x.IsDeleted && !x.Branch.IsDeleted).ToList());
            }
            if (q == "o")
            {
                ViewBag.q = "o";
                ViewBag.r = 2;
                if (deleted)
                {
                    ViewBag.deleted = 1;
                    return View(_user.Get(x => x.UserType == UserType.Owner
                && (BranchId == -1 ? true : x.BranchId == BranchId)
                && x.IsDeleted && !x.Branch.IsDeleted).ToList());
                }
                return View(_user.Get(x => x.UserType == UserType.Owner
                && (BranchId == -1 ? true : x.BranchId == BranchId)
                && !x.IsDeleted && !x.Branch.IsDeleted).ToList());
            }
            else
            {
                ViewBag.q = "s";
                ViewBag.r = 3;
                if (deleted)
                {
                    ViewBag.deleted = 1;
                    return View(_user.Get(x => BranchId == -1 ? true : x.BranchId == BranchId
                      && x.IsDeleted && x.Id != "0d3f3ae4-26c7-47d3-9b29-099645457815").ToList());
                }
                else
                    return View(_user.Get(x => BranchId == -1 ? true : x.BranchId == BranchId
                    && !x.IsDeleted && x.Id != "0d3f3ae4-26c7-47d3-9b29-099645457815").ToList());
            }

        }
        [Authorize(Roles = "Admin,HighAdmin,Accountant,LowAdmin,TrustAdmin")]
        public IActionResult PendingIndex()
        {
            ViewBag.q = "c";

            return View(_user.Get(x => x.UserType == UserType.Client && x.IsPending == false
            && !x.IsDeleted).ToList());

        }
        [Authorize(Roles = "Admin,HighAdmin,Accountant,LowAdmin,TrustAdmin")]
        public async Task<IActionResult> Accpet(string UserId)
        {
            var user = await _user.GetObj(x => x.Id == UserId);
            user.IsPending = true;
            if (user.UserType == UserType.Driver)
                user.Tracking = true;
            if (!await _user.Update(user))
            {
                return BadRequest("من فضل حاول في وقتاً أخر");
            }
            string type = "";
            if (user.UserType == UserType.Driver)
                type = "d";
            else if (user.UserType == UserType.Client)
                type = "c";
            else if (user.UserType == UserType.Admin || user.UserType == UserType.HighAdmin || user.UserType == UserType.Accountant)
                type = "a";
            if (type != "")
            {
                return RedirectToAction(nameof(Index), new { q = type, BranchId = user.BranchId });
            }
            return RedirectToAction(nameof(Index));
        }
        [Authorize(Roles = "Admin,HighAdmin,Accountant,LowAdmin,TrustAdmin")]
        public async Task<IActionResult> CreateClient(AddClientVm model, string page)
        {
            if (await _user.IsExist(x => x.PhoneNumber == model.Phone))
            {
                return BadRequest("هذا الرقم موجود من قبل");
            }
            if (await _user.IsExist(x => x.Name == model.Name))
            {
                return BadRequest("هذا الاسم موجود من قبل");
            }

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
                //site = model.site,
                WhatsappPhone = model.WhatsappPhone,
                UserType = model.UserType == UserType.subAdmin ? UserType.Admin : model.UserType,
                BranchId = model.BranchId,
                IsPending = true

            };
            if (user.UserType == UserType.Driver)
                user.Tracking = true;
            var file = HttpContext.Request.Form.Files.GetFile("IdentityFrontPhoto");
            if (file != null)
            {
                user.IdentityFrontPhoto = await MediaControl.Upload(FilePath.Users, file, _webHostEnvironment);
            }
            var file1 = HttpContext.Request.Form.Files.GetFile("IdentityBackPhoto");
            if (file1 != null)
            {
                user.IdentityBackPhoto = await MediaControl.Upload(FilePath.Users, file1, _webHostEnvironment);
            }
            var file2 = HttpContext.Request.Form.Files.GetFile("RidingLecencePhoto");
            if (file2 != null)
            {
                user.RidingLecencePhoto = await MediaControl.Upload(FilePath.Users, file2, _webHostEnvironment);
            }
            var file3 = HttpContext.Request.Form.Files.GetFile("ViecleLecencePhoto");
            if (file3 != null)
            {
                user.ViecleLecencePhoto = await MediaControl.Upload(FilePath.Users, file3, _webHostEnvironment);
            }
            var file4 = HttpContext.Request.Form.Files.GetFile("FishPhotoPhoto");
            if (file4 != null)
            {
                user.FishPhotoPhoto = await MediaControl.Upload(FilePath.Users, file4, _webHostEnvironment);
            }
            var result = await _userManger.CreateAsync(user, "123456");
            if (!result.Succeeded)
            {
                return BadRequest("من فضلك حاول مره اخري لاحقاً");
            }
            if (!await _roleManager.RoleExistsAsync("Accountant"))
                await _roleManager.CreateAsync(new IdentityRole("Accountant"));
            if (!await _roleManager.RoleExistsAsync("LowAdmin"))
                await _roleManager.CreateAsync(new IdentityRole("LowAdmin"));
            if (!await _roleManager.RoleExistsAsync("Admin"))
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
            if (!await _roleManager.RoleExistsAsync("TrackingAdmin"))
                await _roleManager.CreateAsync(new IdentityRole("TrackingAdmin"));

            if (model.UserType == UserType.HighAdmin)
            {
                await _userManger.AddToRoleAsync(user, "HighAdmin");
            }
            else if (model.UserType == UserType.LowAdmin)
            {
                await _userManger.AddToRoleAsync(user, "LowAdmin");
            }
            else if (model.UserType == UserType.Admin)
            {
                await _userManger.AddToRoleAsync(user, "Admin");
            }
            else if (model.UserType == UserType.subAdmin)
            {
                await _userManger.AddToRoleAsync(user, "TrustAdmin");
            }
            else if (model.UserType == UserType.TrackingAdmin)
            {
                await _userManger.AddToRoleAsync(user, "TrackingAdmin");
            }
            else if (model.UserType == UserType.Client)
            {
                await _userManger.AddToRoleAsync(user, "Client");
            }
            else if (model.UserType == UserType.Driver)
            {
                await _userManger.AddToRoleAsync(user, "Driver");
            }
            else if (model.UserType == UserType.Accountant)
            {
                await _userManger.AddToRoleAsync(user, "Accountant");
            }
            if (string.IsNullOrEmpty(page))
            {
                return RedirectToAction("Index", "Orders");
            }
            string type = "";
            if (user.UserType == UserType.Driver)
                type = "d";
            else if (user.UserType == UserType.Client)
                type = "c";
            else if (user.UserType == UserType.Admin || user.UserType == UserType.subAdmin || user.UserType == UserType.HighAdmin || user.UserType == UserType.TrackingAdmin || user.UserType == UserType.Accountant)
                type = "a";
            if (type != "")
            {
                return RedirectToAction(nameof(Index), new { q = type, BranchId = model.BranchId });
            }
            return RedirectToAction(nameof(Index));
        }
        [Authorize(Roles = "Admin,HighAdmin,Accountant,LowAdmin,TrustAdmin")]
        public async Task<IActionResult> ChangePassword(string UserId, string q)
        {
            var user = new ApplicationUser();
            if (UserId == "1")
            {
                user = await _userManger.GetUserAsync(User);
            }
            else
            {
                user = _user.Get(x => x.Id == UserId).First();

            }
            var vm = new ChangePasswordVm()
            {
                UserId = user.Id
            };
            ViewBag.q = q;
            ViewBag.user = user;
            return View(vm);
        }
        [Authorize(Roles = "Admin,HighAdmin,Accountant,LowAdmin,TrustAdmin")]
        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordVm model)
        {
            var user = _user.Get(x => x.Id == model.UserId).First();
            if (User.IsInRole("Admin"))
            {
                var password = _userManger.PasswordHasher.HashPassword(user, model.Password);
                user.PasswordHash = password;
                await _user.Update(user);
            }
            else if ((user.UserType == UserType.Admin || user.UserType == UserType.HighAdmin || user.UserType == UserType.Accountant) && !User.IsInRole("Admin"))
            {

            }
            else if ((user.UserType == UserType.Driver || user.UserType == UserType.Client) && (User.IsInRole("TrustAdmin") || User.IsInRole("HighAdmin")))
            {

                var password = _userManger.PasswordHasher.HashPassword(user, model.Password);
                user.PasswordHash = password;
                await _user.Update(user);
            }

            string type = "";
            if (user.UserType == UserType.Driver)
                type = "d";
            else if (user.UserType == UserType.Client)
                type = "c";
            else if (user.UserType == UserType.Admin || user.UserType == UserType.HighAdmin || user.UserType == UserType.Accountant)
                type = "a";
            ViewBag.q = model.q;
            if (type != "")
            {
                model.q = type;
                ViewBag.q = model.q;
                return RedirectToAction(nameof(Index), new { q = type, BranchId = user.BranchId });
            }
            return RedirectToAction(nameof(Index), new { q = model.q });
        }
        [Authorize(Roles = "Admin,HighAdmin,TrustAdmin")]
        public IActionResult TrackingList(string id)
        {
            var user = _user.Get(x => x.Id == id).First();
            ViewBag.User = user;

            var TrackingList = _locations.Get(x => !x.IsDeleted && x.DeliveryId == id).OrderByDescending(x => x.Id).ToList();
            return View(TrackingList);
        }
        #region Free-Returned-Orders_AND_DeliveredOrders

        [Authorize(Roles = "HighAdmin")]
        public IActionResult FinshedOrders(string id)
        {
            ViewBag.UserId = id;
            var orders = _orderService.GetList(x =>
            (x.Status == OrderStatus.Delivered
            || (x.Status == OrderStatus.Waiting && x.DeliveryId != null)
            || (x.Status == OrderStatus.Rejected)
            || (x.Status == OrderStatus.PartialDelivered)
            || (x.Status == OrderStatus.Delivered_With_Edit_Price)
            || (x.Status == OrderStatus.Returned_And_Paid_DeliveryCost)
            || (x.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender)
            ) && !x.Finished && !x.IsDeleted
            && x.DeliveryId == id).ToList();
            return View(orders.Take(200));
        }

        [Authorize(Roles = "HighAdmin")]
        [Route("Returns-Orders")]
        public IActionResult FinshedReturnedOrders(string id)
        {
            ViewBag.UserId = id;
            var orders = _orderService.GetList(x =>
            (
            (((x.Status == OrderStatus.PartialReturned
            || x.Status == OrderStatus.Returned)
            ) && !x.Finished) ||
            ((x.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender || x.Status == OrderStatus.Returned_And_Paid_DeliveryCost)) && !x.ReturnedFinished)
             && !x.IsDeleted && x.DeliveryId == id).ToList();
            return View(orders.Take(200));
        }

        [HttpPost]
        [Authorize(Roles = "HighAdmin")]
        public async Task<IActionResult> FinshedOrders(List<long> OrderId, List<double> DeliveryCost, List<double> ArrivedCost, List<string> OrderNotes, List<OrderStatus> orderStatus, bool Returned)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required,
                            new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted, Timeout = TimeSpan.FromMinutes(10) },
                            TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    var userid = _userManger.GetUserId(User);
                    var user = await _user.GetObj(x => x.Id == userid);
                    var wallet = await InitializeWallet(OrderId, user);
                    double total = 0;

                    for (int i = 0; i < OrderId.Count; i++)
                    {
                        var orderId = OrderId[i];
                        #region Check Order Status
                        var order = await _orders.GetObj(x => x.Id == orderId);
                        if (Returned)
                        {
                            if (order.Status != OrderStatus.PartialReturned && order.Status != OrderStatus.Returned
                                && order.Status != OrderStatus.Returned_And_Paid_DeliveryCost && order.Status != OrderStatus.Returned_And_DeliveryCost_On_Sender)
                            { goto Skip; }
                        }
                        else
                        {
                            if (order.Status == OrderStatus.PartialReturned || order.Status == OrderStatus.Returned)
                            { goto Skip; }
                        }
                        #endregion
                        var delivery = DeliveryCost[i];
                        var arrived = ArrivedCost[i];
                        var notes = OrderNotes[i];
                        var status = orderStatus[i];

                        if (delivery >= 0)
                        {
                            if (order != null)
                            {
                                // check
                                if (order.Status != status)
                                {
                                    if (!Returned)
                                    {
                                        switch (order.Status)
                                        {
                                            case OrderStatus.Delivered:
                                                switch (status)
                                                {
                                                    case OrderStatus.PartialDelivered:
                                                        //change status from Delivered To Partial Delivered  
                                                        order.DeliveryCost = delivery;
                                                        order.ArrivedCost = arrived;
                                                        order.WalletId = wallet.Id;
                                                        order.Status = status;

                                                        //هنعمل طلب عكسي مرتجع جزئي بنفس الكود + R
                                                        var PartialReturned = new Order()
                                                        {
                                                            Code = 'R' + order.Code,
                                                            Notes = order.Notes,
                                                            AddressCity = order.AddressCity,
                                                            Address = order.Address,
                                                            ClientName = order.ClientName,
                                                            ClientCode = order.ClientCode,
                                                            ClientPhone = order.ClientPhone,
                                                            Cost = order.Cost,
                                                            DeliveryFees = order.DeliveryFees,
                                                            TotalCost = order.TotalCost,
                                                            Pending = order.Pending,
                                                            TransferredConfirmed = order.TransferredConfirmed,
                                                            ArrivedCost = order.ArrivedCost,
                                                            DeliveryCost = 0,
                                                            ReturnedCost = order.TotalCost - order.ArrivedCost,
                                                            Finished = order.Finished,
                                                            Status = OrderStatus.PartialReturned,
                                                            OrderCompleted = order.OrderCompleted,
                                                            ClientId = order.ClientId,
                                                            LastUpdated = DateTime.Now.ToUniversalTime(),
                                                            WalletId = null,
                                                            DeliveryId = order.DeliveryId,
                                                            BranchId = order.BranchId,
                                                            OrderOperationHistoryId = order.OrderOperationHistoryId,
                                                        };
                                                        await _orders.Add(PartialReturned);
                                                        await _orderNotes.Add(new OrderNotes()
                                                        {
                                                            Content = OrderNotes[i],
                                                            OrderId = PartialReturned.Id,
                                                            UserId = userid
                                                        });
                                                        OrderOperationHistory history = new OrderOperationHistory()
                                                        {
                                                            OrderId = PartialReturned.Id,
                                                            Create_UserId = userid,
                                                            CreateDate = PartialReturned.CreateOn,
                                                        };
                                                        if (!await _Histories.Add(history))
                                                        {
                                                            //return BadRequest("من فضلك حاول لاحقاً");
                                                        }
                                                        PartialReturned.OrderOperationHistoryId = history.Id;
                                                        if (!await _orders.Update(PartialReturned))
                                                        {
                                                            //return BadRequest("من فضل حاول في وقتاً أخر");
                                                        }
                                                        break;
                                                    case OrderStatus.Waiting:
                                                        order.DeliveryCost = 0; delivery = 0;
                                                        order.ArrivedCost = 0;
                                                        order.WalletId = wallet.Id;
                                                        order.Status = status;
                                                        break;
                                                    case OrderStatus.Delivered_With_Edit_Price:
                                                        order.DeliveryCost = delivery;
                                                        order.ArrivedCost = arrived;
                                                        order.WalletId = wallet.Id;
                                                        order.Status = status;
                                                        break;
                                                    case OrderStatus.Returned:
                                                        order.DeliveryCost = 0; delivery = 0;
                                                        order.ArrivedCost = 0; arrived = 0;
                                                        order.ReturnedCost = order.TotalCost;
                                                        order.Status = status;
                                                        var orderNotes = _orderNotes.Get(a => a.OrderId == orderId).FirstOrDefault();
                                                        if (orderNotes != null)
                                                        {
                                                            orderNotes.Content = notes;
                                                            await _orderNotes.Update(orderNotes);
                                                        }
                                                        goto Skip;

                                                    default:
                                                        order.DeliveryCost = delivery;
                                                        order.ArrivedCost = arrived;
                                                        order.WalletId = wallet.Id;
                                                        order.Status = status;
                                                        break;
                                                }
                                                break;
                                            case OrderStatus.PartialDelivered:
                                                //remove the Partial Returned Order
                                                var PartialReturnedOrder = await _orders.GetObj(x => x.Code == ("R" + order.Code));
                                                if (PartialReturnedOrder != null)
                                                {
                                                    //PartialReturnedOrder.IsDeleted = true;
                                                    await _CRUD.ToggleDelete(PartialReturnedOrder.Id);
                                                }
                                                //
                                                switch (status)
                                                {
                                                    case OrderStatus.Delivered:
                                                        order.DeliveryCost = delivery;
                                                        order.ArrivedCost = order.TotalCost; arrived = order.TotalCost;
                                                        order.WalletId = wallet.Id;
                                                        order.Status = status;
                                                        break;
                                                    case OrderStatus.Waiting:
                                                        order.DeliveryCost = 0; delivery = 0;
                                                        order.ArrivedCost = arrived;
                                                        order.WalletId = wallet.Id;
                                                        order.Status = status;
                                                        break;
                                                    case OrderStatus.Delivered_With_Edit_Price:
                                                        order.DeliveryCost = delivery;
                                                        order.ArrivedCost = arrived;
                                                        order.WalletId = wallet.Id;
                                                        order.Status = status;
                                                        break;
                                                    case OrderStatus.Returned:
                                                        order.ReturnedCost = order.TotalCost;
                                                        order.DeliveryCost = 0; delivery = 0;
                                                        order.ArrivedCost = 0; arrived = 0;
                                                        order.Status = status;
                                                        var orderNotes = _orderNotes.Get(a => a.OrderId == orderId).FirstOrDefault();
                                                        if (orderNotes != null)
                                                        {
                                                            orderNotes.Content = notes;
                                                            await _orderNotes.Update(orderNotes);
                                                        }
                                                        goto Skip;
                                                    default:
                                                        order.DeliveryCost = delivery;
                                                        order.ArrivedCost = arrived;
                                                        order.WalletId = wallet.Id;
                                                        order.Status = status;
                                                        break;
                                                }
                                                break;
                                            case OrderStatus.Waiting:
                                                switch (status)
                                                {
                                                    case OrderStatus.Delivered:
                                                        order.DeliveryCost = delivery;
                                                        order.ArrivedCost = order.TotalCost; arrived = order.TotalCost;
                                                        order.WalletId = wallet.Id;
                                                        order.Status = status;
                                                        break;
                                                    case OrderStatus.PartialDelivered:
                                                        //change status from Delivered To Partial Delivered  
                                                        order.DeliveryCost = delivery;
                                                        order.ArrivedCost = arrived;
                                                        order.WalletId = wallet.Id;
                                                        order.Status = status;

                                                        //هنعمل طلب عكسي مرتجع جزئي بنفس الكود + R
                                                        var PartialReturned = new Order()
                                                        {
                                                            Code = 'R' + order.Code,
                                                            Notes = order.Notes,
                                                            AddressCity = order.AddressCity,
                                                            Address = order.Address,
                                                            ClientName = order.ClientName,
                                                            ClientCode = order.ClientCode,
                                                            ClientPhone = order.ClientPhone,
                                                            Cost = order.Cost,
                                                            DeliveryFees = order.DeliveryFees,
                                                            TotalCost = order.TotalCost,
                                                            Pending = order.Pending,
                                                            TransferredConfirmed = order.TransferredConfirmed,
                                                            ArrivedCost = order.ArrivedCost,
                                                            DeliveryCost = 0,
                                                            ReturnedCost = order.TotalCost - order.ArrivedCost,
                                                            Finished = order.Finished,
                                                            Status = OrderStatus.PartialReturned,
                                                            OrderCompleted = order.OrderCompleted,
                                                            ClientId = order.ClientId,
                                                            LastUpdated = DateTime.Now.ToUniversalTime(),
                                                            WalletId = null,
                                                            DeliveryId = order.DeliveryId,
                                                            BranchId = order.BranchId,
                                                            OrderOperationHistoryId = order.OrderOperationHistoryId,
                                                        };
                                                        await _orders.Add(PartialReturned);
                                                        await _orderNotes.Add(new OrderNotes()
                                                        {
                                                            Content = OrderNotes[i],
                                                            OrderId = PartialReturned.Id,
                                                            UserId = userid
                                                        });
                                                        OrderOperationHistory history = new OrderOperationHistory()
                                                        {
                                                            OrderId = PartialReturned.Id,
                                                            Create_UserId = userid,
                                                            CreateDate = PartialReturned.CreateOn,
                                                        };
                                                        if (!await _Histories.Add(history))
                                                        {
                                                            //return BadRequest("من فضلك حاول لاحقاً");
                                                        }
                                                        PartialReturned.OrderOperationHistoryId = history.Id;
                                                        if (!await _orders.Update(PartialReturned))
                                                        {
                                                            //return BadRequest("من فضل حاول في وقتاً أخر");
                                                        }
                                                        break;
                                                    case OrderStatus.Returned:
                                                        order.ReturnedCost = order.TotalCost;
                                                        order.DeliveryCost = 0; delivery = 0;
                                                        order.ArrivedCost = 0; arrived = 0;
                                                        order.Status = status;
                                                        var orderNotes = _orderNotes.Get(a => a.OrderId == orderId).FirstOrDefault();
                                                        if (orderNotes != null)
                                                        {
                                                            orderNotes.Content = notes;
                                                            await _orderNotes.Update(orderNotes);
                                                        }
                                                        goto Skip;
                                                    case OrderStatus.Delivered_With_Edit_Price:
                                                        order.DeliveryCost = delivery;
                                                        order.ArrivedCost = arrived;
                                                        order.WalletId = wallet.Id;
                                                        order.Status = status;
                                                        break;
                                                    default:
                                                        order.DeliveryCost = delivery;
                                                        order.ArrivedCost = arrived;
                                                        order.WalletId = wallet.Id;
                                                        order.Status = status;
                                                        break;
                                                }
                                                break;
                                            case OrderStatus.Delivered_With_Edit_Price:

                                                switch (status)
                                                {
                                                    case OrderStatus.Delivered:
                                                        order.DeliveryCost = delivery;
                                                        order.ArrivedCost = order.TotalCost; arrived = order.TotalCost;
                                                        order.WalletId = wallet.Id;
                                                        order.Status = status;
                                                        break;
                                                    case OrderStatus.PartialDelivered:
                                                        //change status from Delivered To Partial Delivered  
                                                        order.DeliveryCost = delivery;
                                                        order.ArrivedCost = arrived;
                                                        order.WalletId = wallet.Id;
                                                        order.Status = status;

                                                        //هنعمل طلب عكسي مرتجع جزئي بنفس الكود + R
                                                        var PartialReturned = new Order()
                                                        {
                                                            Code = 'R' + order.Code,
                                                            Notes = order.Notes,
                                                            AddressCity = order.AddressCity,
                                                            Address = order.Address,
                                                            ClientName = order.ClientName,
                                                            ClientCode = order.ClientCode,
                                                            ClientPhone = order.ClientPhone,
                                                            Cost = order.Cost,
                                                            DeliveryFees = order.DeliveryFees,
                                                            TotalCost = order.TotalCost,
                                                            Pending = order.Pending,
                                                            TransferredConfirmed = order.TransferredConfirmed,
                                                            ArrivedCost = order.ArrivedCost,
                                                            DeliveryCost = 0,
                                                            ReturnedCost = order.TotalCost - order.ArrivedCost,
                                                            Finished = order.Finished,
                                                            Status = OrderStatus.PartialReturned,
                                                            OrderCompleted = order.OrderCompleted,
                                                            ClientId = order.ClientId,
                                                            LastUpdated = DateTime.Now.ToUniversalTime(),
                                                            WalletId = null,
                                                            DeliveryId = order.DeliveryId,
                                                            BranchId = order.BranchId,
                                                            OrderOperationHistoryId = order.OrderOperationHistoryId,
                                                        };
                                                        await _orders.Add(PartialReturned);
                                                        await _orderNotes.Add(new OrderNotes()
                                                        {
                                                            Content = OrderNotes[i],
                                                            OrderId = PartialReturned.Id,
                                                            UserId = userid
                                                        });
                                                        OrderOperationHistory history = new OrderOperationHistory()
                                                        {
                                                            OrderId = PartialReturned.Id,
                                                            Create_UserId = userid,
                                                            CreateDate = PartialReturned.CreateOn,
                                                        };
                                                        if (!await _Histories.Add(history))
                                                        {
                                                            //return BadRequest("من فضلك حاول لاحقاً");
                                                        }
                                                        PartialReturned.OrderOperationHistoryId = history.Id;
                                                        if (!await _orders.Update(PartialReturned))
                                                        {
                                                            //return BadRequest("من فضل حاول في وقتاً أخر");
                                                        }
                                                        break;
                                                    case OrderStatus.Waiting:
                                                        order.DeliveryCost = 0; delivery = 0;
                                                        order.ArrivedCost = arrived;
                                                        order.WalletId = wallet.Id;
                                                        order.Status = status;
                                                        break;
                                                    case OrderStatus.Returned:
                                                        order.DeliveryCost = 0; delivery = 0;
                                                        order.ArrivedCost = 0; arrived = 0;
                                                        order.ReturnedCost = order.TotalCost;
                                                        order.Status = status;
                                                        var orderNotes = _orderNotes.Get(a => a.OrderId == orderId).FirstOrDefault();
                                                        if (orderNotes != null)
                                                        {
                                                            orderNotes.Content = notes;
                                                            await _orderNotes.Update(orderNotes);
                                                        }
                                                        goto Skip;

                                                    default:
                                                        order.DeliveryCost = delivery;
                                                        order.ArrivedCost = arrived;
                                                        order.WalletId = wallet.Id;
                                                        order.Status = status;
                                                        break;
                                                }

                                                break;
                                            default:
                                                order.DeliveryCost = delivery;
                                                order.ArrivedCost = arrived;
                                                order.WalletId = wallet.Id;
                                                order.Status = status;
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        //order.DeliveryCost = delivery;
                                        //order.ArrivedCost = arrived;
                                        order.WalletId = wallet.Id;
                                        //order.Status = status;
                                    }
                                }
                                //
                                else
                                {
                                    if (!Returned)
                                    {
                                        order.DeliveryCost = delivery;
                                        order.ArrivedCost = arrived;
                                        order.Status = status;
                                        order.WalletId = wallet.Id;
                                    }
                                    else
                                    {
                                        if (status == OrderStatus.PartialReturned || status == OrderStatus.Returned)
                                            order.WalletId = wallet.Id;
                                        else if (status == OrderStatus.Returned_And_Paid_DeliveryCost || status == OrderStatus.Returned_And_DeliveryCost_On_Sender)
                                            order.ReturnedWalletId = wallet.Id;
                                    }

                                }

                                if (order.Status == OrderStatus.Waiting)
                                {
                                    order.DeliveryId = null;
                                    order.DeliveryCost = 0; delivery = 0;
                                }
                                else
                                {
                                    if (!Returned)
                                        order.Finished = true;
                                    else
                                    {
                                        if (status == OrderStatus.PartialReturned || status == OrderStatus.Returned)
                                            order.Finished = true;
                                        else if (status == OrderStatus.Returned_And_Paid_DeliveryCost || status == OrderStatus.Returned_And_DeliveryCost_On_Sender)
                                            order.ReturnedFinished = true;
                                    }
                                }
                                //if (status != OrderStatus.PartialReturned && status != OrderStatus.Returned)
                                if (!Returned)
                                    total += (arrived - delivery);

                                order.LastUpdated = DateTime.Now.ToUniversalTime();
                                await _orders.Update(order);

                                if (!Returned)
                                {
                                    if (order.OrderOperationHistoryId != null)
                                    {
                                        var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
                                        history.Finish_UserId = userid;
                                        history.FinishDate = DateTime.Now.ToUniversalTime();
                                        await _Histories.Update(history);
                                    }
                                }
                                else
                                {
                                    if (status == OrderStatus.PartialReturned || status == OrderStatus.Returned)
                                    {
                                        if (order.OrderOperationHistoryId != null)
                                        {
                                            var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
                                            history.Finish_UserId = userid;
                                            history.FinishDate = DateTime.Now.ToUniversalTime();
                                            await _Histories.Update(history);
                                        }
                                    }
                                    else if (status == OrderStatus.Returned_And_Paid_DeliveryCost || status == OrderStatus.Returned_And_DeliveryCost_On_Sender)
                                    {
                                        if (order.OrderOperationHistoryId != null)
                                        {
                                            var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
                                            history.ReturnedFinish_UserId = userid;
                                            history.ReturnedFinishDate = DateTime.Now.ToUniversalTime();
                                            await _Histories.Update(history);
                                        }
                                    }
                                }

                                var orderNote = _orderNotes.Get(a => a.OrderId == orderId).FirstOrDefault();
                                if (orderNote != null)
                                {
                                    orderNote.Content = notes;
                                    await _orderNotes.Update(orderNote);
                                }
                            }
                        }
                    Skip: continue;
                    }

                    wallet.Amount = total;
                    await _wallet.Update(wallet);

                    user.Wallet -= total;
                    // await _user.Update(user);

                    bool userUpdated = await _user.Update(user);

                    // تحقق من أن جميع التحديثات قد نجحت
                    if (userUpdated)
                    {
                        wallet.AddedToAdminWallet = true;
                        await _wallet.Update(wallet);
                        scope.Complete();

                        // Enqueue WhatsApp notifications for completed orders
                        try
                        {
                            var completedOrders = _orders.Get(x => OrderId.Contains(x.Id)).ToList();
                            await _wapilotService.EnqueueBulkOrderCompletionAsync(completedOrders, userid);
                        }
                        catch (Exception)
                        {
                            // Don't fail the operation if WhatsApp queueing fails
                        }

                        return RedirectToAction(nameof(Index), new { q = "d" });
                    }
                    else
                    {
                        throw new Exception("فشل العمليه .");
                    }
                }
                catch (Exception ex)
                {
                    var ID = (await _orders.GetObj(x => x.Id == OrderId[0]))?.DeliveryId;
                    if (Returned)
                        return RedirectToAction(nameof(FinshedReturnedOrders), new { id = ID });
                    else
                        return RedirectToAction(nameof(FinshedOrders), new { id = ID });
                }
            }
        }

        #endregion

        #region Returns-Paid-Shipping

        [Authorize(Roles = "HighAdmin")]
        [Route("Returns-Paid-Shipping")]
        public IActionResult FinshedPaidReturnedOrders(string id)
        {
            ViewBag.UserId = id;
            var orders = _orderService.GetList(x =>
            (
             (x.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender)
            || (x.Status == OrderStatus.Returned_And_Paid_DeliveryCost)
            ) && !x.Finished && !x.IsDeleted
            && x.DeliveryId == id).ToList();
            return View(orders.Take(200));
        }

        [HttpPost]
        [Authorize(Roles = "HighAdmin")]
        [Route("Returns-Paid-Shipping")]
        public async Task<IActionResult> FinshedPaidReturnedOrders(List<long> OrderId, List<double> DeliveryCost, List<double> ArrivedCost, List<string> OrderNotes, List<OrderStatus> orderStatus, bool Returned)
        {
            using (var scope = new TransactionScope(TransactionScopeOption.Required,
                            new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted, Timeout = TimeSpan.FromMinutes(10) },
                            TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    var userid = _userManger.GetUserId(User);
                    var user = await _user.GetObj(x => x.Id == userid);
                    var wallet = await InitializeWallet(OrderId, user);
                    double total = 0;

                    for (int i = 0; i < OrderId.Count; i++)
                    {
                        var orderId = OrderId[i];
                        #region Check Order Status
                        var order = await _orders.GetObj(x => x.Id == orderId);
                        if (Returned)
                        {
                            if (order.Status != OrderStatus.Returned_And_DeliveryCost_On_Sender && order.Status != OrderStatus.Returned_And_Paid_DeliveryCost)
                            { goto Skip; }
                        }

                        #endregion
                        var delivery = DeliveryCost[i];
                        var arrived = ArrivedCost[i];
                        var notes = OrderNotes[i];
                        var status = orderStatus[i];

                        if (delivery >= 0)
                        {
                            if (order != null)
                            {

                                order.DeliveryCost = delivery;
                                order.ArrivedCost = arrived;
                                order.WalletId = wallet.Id;
                                order.Status = status;

                                order.Finished = true;

                                total += (arrived - delivery);

                                order.LastUpdated = DateTime.Now.ToUniversalTime();
                                await _orders.Update(order);

                                if (order.OrderOperationHistoryId != null)
                                {
                                    var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
                                    history.Finish_UserId = userid;
                                    history.FinishDate = DateTime.Now.ToUniversalTime();
                                    await _Histories.Update(history);
                                }

                                var orderNote = _orderNotes.Get(a => a.OrderId == orderId).FirstOrDefault();
                                if (orderNote != null && notes != ".")
                                {
                                    orderNote.Content = notes;
                                    await _orderNotes.Update(orderNote);
                                }
                            }
                        }
                    Skip: continue;
                    }

                    wallet.Amount = total;
                    wallet.AddedToAdminWallet = true;
                    await _wallet.Update(wallet);

                    user.Wallet -= total;
                    await _user.Update(user);

                    scope.Complete();

                    // Enqueue WhatsApp notifications for completed orders
                    try
                    {
                        var completedOrders = _orders.Get(x => OrderId.Contains(x.Id)).ToList();
                        await _wapilotService.EnqueueBulkOrderCompletionAsync(completedOrders, _userManger.GetUserId(User));
                    }
                    catch (Exception)
                    {
                        // Don't fail the operation if WhatsApp queueing fails
                    }

                    return RedirectToAction(nameof(Index), new { q = "d" });
                }
                catch (Exception ex)
                {
                    var ID = (await _orders.GetObj(x => x.Id == OrderId[0]))?.DeliveryId;
                    if (Returned)
                        return RedirectToAction(nameof(FinshedPaidReturnedOrders), new { id = ID });
                    else
                        return RedirectToAction(nameof(FinshedOrders), new { id = ID });
                }
            }
        }
        #endregion

        private async void DeleteReturnedOrder(long id)
        {
            if (!await _orders.IsExist(x => x.Id == id))
            {
            }
            else
            {
                if (!await _CRUD.ToggleDelete(id))
                {
                }

                var order = _orders.Get(x => x.Id == id).First();
                if (_orders.Get(x => x.Id == id).First().IsDeleted)
                {
                    if (order.OrderOperationHistoryId != null)
                    {
                        var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
                        history.Delete_UserId = _userManger.GetUserId(User);
                        history.DeleteDate = DateTime.Now.ToUniversalTime();
                        await _Histories.Update(history);
                    }
                }
                else
                {
                    if (order.OrderOperationHistoryId != null)
                    {
                        var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
                        history.Restore_UserId = _userManger.GetUserId(User);
                        history.RestoreDate = DateTime.Now.ToUniversalTime();
                        await _Histories.Update(history);
                    }
                }
            }
        }
        private async Task<Wallet> InitializeWallet(List<long> orderIds, ApplicationUser user)
        {
            var wallet = new Wallet();
            if (orderIds.Count > 0)
            {
                var firstOrder = await _orders.GetObj(x => x.Id == orderIds[0]);
                wallet = new Wallet()
                {
                    UserId = user.Id,
                    Amount = 0,
                    TransactionType = TransactionType.OrderFinished,
                    ActualUserId = firstOrder.DeliveryId,
                    UserWalletLast = user.Wallet,
                    AddedToAdminWallet = false
                };
                await _wallet.Add(wallet);
            }
            return wallet;
        }
        [Authorize(Roles = "Admin,HighAdmin,Accountant,LowAdmin,TrustAdmin")]
        public async Task<IActionResult> Edit(string id, string type = "")
        {
            ViewBag.Branchs = _branch.Get(x => !x.IsDeleted).ToList();
            ViewBag.q = type;
            if (!await _user.IsExist(x => x.Id == id))
            {
                return NotFound();
            }
            ViewBag.Title = "تعديل المستخدم";
            return View(_user.Get(x => x.Id == id).First());
        }
        [Authorize(Roles = "Admin,HighAdmin,Accountant,LowAdmin,TrustAdmin")]
        public async Task<IActionResult> CurrentStatistics(string id)
        {
            if (!await _user.IsExist(x => x.Id == id))
            {
                return NotFound();
            }
            var user = _user.Get(x => x.Id == id).First();
            CurrentStatisticsVM model = new CurrentStatisticsVM();
            model.Name = user.Name;
            if (user.UserType == UserType.Driver)
                ViewBag.Title = "الإحصائيات الحاليه للمندوب : " + model.Name;
            else if (user.UserType == UserType.Client)
                return RedirectToAction(nameof(Statistics), new { id = id });
            else
                return RedirectToAction("Index", "Home");

            if (user != null)
            {
                if (user.UserType == UserType.Driver)
                {
                    //عدد الطلبات الحاليه
                    model.CurrentOrdersCount = _orders.Get(x => x.DeliveryId == id &&
                            (x.Status == OrderStatus.Assigned || x.Status == OrderStatus.Waiting)
                            && !x.IsDeleted).Count();
                    var orders = _orders.Get(x =>
           (x.Status == OrderStatus.Delivered
           || (x.Status == OrderStatus.Waiting)
           || (x.Status == OrderStatus.Rejected)
           || (x.Status == OrderStatus.PartialDelivered)
           || (x.Status == OrderStatus.Returned)
           ) && !x.Finished && !x.IsDeleted
           && x.DeliveryId == id).ToList();

                    model.ReturnedCount = orders.Count(x => x.Status == OrderStatus.Returned);
                    model.PartialDeliveredCount = orders.Count(x => x.Status == OrderStatus.PartialDelivered);
                    model.DeliveredCount = orders.Count(x => x.Status == OrderStatus.Delivered);
                    model.WaitingCount = orders.Count(x => x.Status == OrderStatus.Waiting);
                    model.RejectedCount = orders.Count(x => x.Status == OrderStatus.Rejected);
                    model.AllOrdersCount = model.CurrentOrdersCount + orders.Count();

                    var OrdersMoney = orders.Where(x => x.Status != OrderStatus.PartialReturned).Sum(x => x.ArrivedCost);
                    model.OrdersMoney = OrdersMoney;
                    var DriverMoney = orders.Where(x => x.Status != OrderStatus.PartialReturned).Sum(x => x.DeliveryCost);
                    model.DriverMoney = DriverMoney;
                    model.SystemMoney = OrdersMoney - DriverMoney;
                    // حساب النسب المئوية
                    if (model.AllOrdersCount > 0)
                    {
                        model.DeliveredPercentage = (double)model.DeliveredCount / model.AllOrdersCount * 100;
                        model.ReturnedPercentage = (double)model.ReturnedCount / model.AllOrdersCount * 100;
                    }
                }

            }
            return View(model);
        }
        [Authorize(Roles = "Admin,HighAdmin,Accountant,LowAdmin,TrustAdmin")]
        public async Task<IActionResult> Statistics(string id)
        {
            if (!await _user.IsExist(x => x.Id == id && x.UserType == UserType.Client))
            {
                return NotFound();
            }
            var user = _user.Get(x => x.Id == id).First();
            DriverStatisticsVM model = new DriverStatisticsVM();
            model.Name = user.Name;
            ViewBag.Title = "إحصائيات الراسل : " + model.Name;
            if (user != null)
            {
                //عدد الطلبات الحاليه
                model.CurrentOrdersCount = _orders.Get(x => x.ClientId == id &&
                        (x.Status == OrderStatus.Assigned || x.Status == OrderStatus.Waiting)
                        && !x.IsDeleted).Count();
                var orders = _orders.Get(x =>
       (x.Status != OrderStatus.PartialReturned) && !x.IsDeleted && x.ClientId == id).ToList();

                model.ReturnedCount = orders.Count(x => x.Status == OrderStatus.Returned);
                model.PartialDeliveredCount = orders.Count(x => x.Status == OrderStatus.PartialDelivered);
                model.PartialReturned_ReceivedCount = orders.Count(x => x.Status == OrderStatus.PartialReturned && x.Finished);
                model.Returned_ReceivedCount = orders.Count(x => x.Status == OrderStatus.Returned && x.Finished);
                model.DeliveredCount = orders.Count(x => x.Status == OrderStatus.Delivered);
                model.DeliveredFinishedCount = orders.Count(x => x.Status == OrderStatus.Delivered && x.Finished);
                model.WaitingCount = orders.Count(x => x.Status == OrderStatus.Waiting);
                model.RejectedCount = orders.Count(x => x.Status == OrderStatus.Rejected);
                model.AllOrdersCount = model.CurrentOrdersCount + orders.Count();

                var OrdersMoney = orders.Where(x => x.Status != OrderStatus.PartialReturned).Sum(x => x.ArrivedCost);
                model.OrdersMoney = OrdersMoney;
                var DriverMoney = orders.Where(x => x.Status != OrderStatus.PartialReturned).Sum(x => x.DeliveryCost);
                model.DriverMoney = DriverMoney;
                model.SystemMoney = OrdersMoney - DriverMoney;
                // حساب النسب المئوية
                if (model.AllOrdersCount > 0)
                {
                    model.PartialDeliveredPercentage = (double)model.PartialDeliveredCount / model.AllOrdersCount * 100;
                    model.DeliveredPercentage = (double)model.DeliveredCount / model.AllOrdersCount * 100;
                    model.ReturnedPercentage = (double)model.ReturnedCount / model.AllOrdersCount * 100;
                }
            }
            return View(model);
        }
        [Authorize(Roles = "Admin,HighAdmin,Accountant,LowAdmin,TrustAdmin")]
        public async Task<IActionResult> DriverStatistics(string id)
        {
            if (!await _user.IsExist(x => x.Id == id && x.UserType == UserType.Driver))
            {
                return NotFound();
            }
            var user = _user.Get(x => x.Id == id).First();
            DriverStatisticsVM model = new DriverStatisticsVM();
            model.Name = user.Name;
            ViewBag.Title = "إحصائيات المندوب : " + model.Name;
            if (user != null)
            {
                //عدد الطلبات الحاليه
                model.CurrentOrdersCount = _orders.Get(x => x.DeliveryId == id &&
                        (x.Status == OrderStatus.Assigned || x.Status == OrderStatus.Waiting)
                        && !x.IsDeleted).Count();
                var orders = _orders.Get(x =>
       (x.Status != OrderStatus.PartialReturned) && !x.IsDeleted && x.DeliveryId == id).ToList();

                model.ReturnedCount = orders.Count(x => x.Status == OrderStatus.Returned);
                model.PartialDeliveredCount = orders.Count(x => x.Status == OrderStatus.PartialDelivered);
                model.PartialReturned_ReceivedCount = orders.Count(x => x.Status == OrderStatus.PartialReturned && x.Finished);
                model.Returned_ReceivedCount = orders.Count(x => x.Status == OrderStatus.Returned && x.Finished);
                model.DeliveredCount = orders.Count(x => x.Status == OrderStatus.Delivered);
                model.DeliveredFinishedCount = orders.Count(x => x.Status == OrderStatus.Delivered && x.Finished);
                model.WaitingCount = orders.Count(x => x.Status == OrderStatus.Waiting);
                model.RejectedCount = orders.Count(x => x.Status == OrderStatus.Rejected);
                model.AllOrdersCount = model.CurrentOrdersCount + orders.Count();

                var OrdersMoney = orders.Where(x => x.Status != OrderStatus.PartialReturned).Sum(x => x.ArrivedCost);
                model.OrdersMoney = OrdersMoney;
                var DriverMoney = orders.Where(x => x.Status != OrderStatus.PartialReturned).Sum(x => x.DeliveryCost);
                model.DriverMoney = DriverMoney;
                model.SystemMoney = OrdersMoney - DriverMoney;
                // حساب النسب المئوية
                if (model.AllOrdersCount > 0)
                {
                    model.PartialDeliveredPercentage = (double)model.PartialDeliveredCount / model.AllOrdersCount * 100;
                    model.DeliveredPercentage = (double)model.DeliveredCount / model.AllOrdersCount * 100;
                    model.ReturnedPercentage = (double)model.ReturnedCount / model.AllOrdersCount * 100;
                }
            }
            return View(model);
        }
        [Authorize(Roles = "Admin,HighAdmin,Accountant,LowAdmin,TrustAdmin")]
        [HttpPost]
        public async Task<IActionResult> Edit(ApplicationUser model, string type = "")
        {
            ViewBag.Branchs = _branch.Get(x => !x.IsDeleted).ToList();

            if (!ModelState.IsValid)
            {
                return BadRequest("حدث خطأ اثناء ادخال البيانات");
            }
            if (await _user.IsExist(x => x.Id != model.Id && x.Email == model.Email))
            {
                return BadRequest("هذا الايميل موجود من قبل");
            }
            var user = await _user.GetObj(x => x.Id == model.Id);
            user.Email = model.Email;
            user.UserName = model.Email;
            user.NormalizedEmail = model.Email.ToUpper();
            user.NormalizedUserName = model.Email.ToUpper();
            user.PhoneNumber = model.PhoneNumber;
            user.WhatsappPhone = model.WhatsappPhone;
            user.Address = model.Address;
            //user.site = model.site;
            user.DeliveryCost = model.DeliveryCost;
            if (model.UserType == UserType.Driver)
                user.Tracking = model.Tracking;
            else user.Tracking = false;
            user.Name = model.Name;
            user.BranchId = model.BranchId;

            if (model.UserType == UserType.Client)
            {
                user.OrdersGeneralNote = model.OrdersGeneralNote;
                user.WhatsappGroupId = model.WhatsappGroupId;
                user.HideSenderName = model.HideSenderName;
                user.HideSenderPhone = model.HideSenderPhone;
                user.HideSenderCode = model.HideSenderCode;
            }

            var file = HttpContext.Request.Form.Files.GetFile("IdentityFrontPhoto");
            if (file != null)
            {
                user.IdentityFrontPhoto = await MediaControl.Upload(FilePath.Users, file, _webHostEnvironment);
            }
            var file1 = HttpContext.Request.Form.Files.GetFile("IdentityBackPhoto");
            if (file1 != null)
            {
                user.IdentityBackPhoto = await MediaControl.Upload(FilePath.Users, file1, _webHostEnvironment);
            }
            var file2 = HttpContext.Request.Form.Files.GetFile("RidingLecencePhoto");
            if (file2 != null)
            {
                user.RidingLecencePhoto = await MediaControl.Upload(FilePath.Users, file2, _webHostEnvironment);
            }
            var file3 = HttpContext.Request.Form.Files.GetFile("ViecleLecencePhoto");
            if (file3 != null)
            {
                user.ViecleLecencePhoto = await MediaControl.Upload(FilePath.Users, file3, _webHostEnvironment);
            }
            var file4 = HttpContext.Request.Form.Files.GetFile("FishPhotoPhoto");
            if (file4 != null)
            {
                user.FishPhotoPhoto = await MediaControl.Upload(FilePath.Users, file4, _webHostEnvironment);
            }
            if (!await _user.Update(user))
            {
                return BadRequest("من فضل حاول في وقتاً أخر");
            }
            if (user.UserType == UserType.Driver)
                type = "d";
            else if (user.UserType == UserType.Client)
                type = "c";
            else if (user.UserType == UserType.Admin || user.UserType == UserType.HighAdmin || user.UserType == UserType.Accountant)
                type = "a";
            if (type != "")
            {
                return RedirectToAction(nameof(Index), new { q = type, BranchId = model.BranchId });
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: تحديث إعدادات إخفاء بيانات الراسل لجميع الرواسل
        [HttpPost]
        [Authorize(Roles = "Admin,HighAdmin,TrustAdmin")]
        public async Task<IActionResult> UpdateAllSendersHideSettings(bool hideSenderName, bool hideSenderPhone, bool hideSenderCode)
        {
            var clients = _user.Get(x => x.UserType == UserType.Client && !x.IsDeleted).ToList();
            foreach (var client in clients)
            {
                client.HideSenderName = hideSenderName;
                client.HideSenderPhone = hideSenderPhone;
                client.HideSenderCode = hideSenderCode;
            }
            foreach (var client in clients)
            {
                await _user.Update(client);
            }
            return Json(new { success = true, message = $"تم تحديث إعدادات الإخفاء لـ {clients.Count} راسل بنجاح" });
        }

        // POST: Get Groups (no phone number needed - gets all groups)
        [HttpPost]
        [Authorize(Roles = "Admin,HighAdmin,Accountant,LowAdmin,TrustAdmin")]
        public async Task<IActionResult> GetGroupsForPhone(string phoneNumber = null)
        {
            // جلب المزود المفعل حالياً
            var providerSettings = await _providerService.GetProviderSettingsAsync();
            var activeProvider = providerSettings.ActiveProvider;

            List<WhatsAppGroupInfo> groups = new List<WhatsAppGroupInfo>();
            bool success = false;
            string errorMessage = null;

            if (activeProvider == WhatsAppProvider.WhaStack)
            {
                var whaResult = await _whaStackService.GetGroupsAsync();
                success = whaResult.Success;
                groups = whaResult.Groups;
                errorMessage = whaResult.ErrorMessage;
            }
            else if (activeProvider == WhatsAppProvider.WhatsAppBotCloud)
            {
                var botResult = await _whatsAppBotCloudService.GetGroupsAsync();
                success = botResult.Success;
                groups = botResult.Groups;
                errorMessage = botResult.ErrorMessage;
            }
            else
            {
                errorMessage = "المزود المفعل حالياً لا يدعم جلب الجروبات";
            }

            return Json(new
            {
                success = success,
                groups = groups.Select(g => new { id = g.GroupId, name = g.GroupName, description = g.Description }),
                message = success
                    ? $"تم جلب {groups.Count} جروب بنجاح"
                    : $"فشل جلب الجروبات: {errorMessage}"
            });
        }

        [Authorize(Roles = "Admin,HighAdmin,Accountant,LowAdmin,TrustAdmin")]
        public IActionResult AssignOrders(string id)
        {
            ViewBag.UserId = id;
            var user = _user.Get(x => x.Id == id).First();
            ViewBag.UserData = user;
            ViewBag.Orders = _orders.Get(x => (x.Status == OrderStatus.Placed ||
            (x.Status == OrderStatus.Waiting && x.DeliveryId == null)) && !x.IsDeleted && !x.Pending
            && ((x.BranchId == user.BranchId && x.Client.BranchId == user.BranchId) || (x.BranchId == user.BranchId && x.TransferredConfirmed))).ToList();
            return View();
        }
        [Authorize(Roles = "Admin,HighAdmin,Accountant,LowAdmin,TrustAdmin")]
        [HttpPost]
        public async Task<IActionResult> AssignOrders(AssignOrdersViewModel model)
        {

            List<long> OrdersPrint = new List<long>();
            using (var scope = new TransactionScope(TransactionScopeOption.Required,
                                new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted, Timeout = TimeSpan.FromMinutes(10) },
                                TransactionScopeAsyncFlowOption.Enabled))
            {
                try
                {
                    var user = await _user.GetSingle(x => x.Id == model.UserId);
                    foreach (var itemId in model.Orders)
                    {
                        var order = await _orders.GetSingle(x => x.Id == itemId, "Client,Branch");
                        //var order = _orders.GetAllAsIQueryable(x => x.Id == item, null, "Client,Branch").First();

                        if (IsValidOrderForAssignment(order, user))
                        {
                            order.DeliveryId = model.UserId;
                            //order.DeliveryCost = user.DeliveryCost != null ? user.DeliveryCost.Value : 0;
                            order.Status = OrderStatus.Assigned;
                            //عشان نشيلها من اي تقفيله قبل كده
                            order.WalletId = null;
                            ///
                            order.LastUpdated = DateTime.Now.ToUniversalTime();
                            if (await _orders.Update(order))
                            {
                                OrdersPrint.Add(itemId);
                                await UpdateOrderHistory(order.OrderOperationHistoryId);
                            }
                        }
                    }
                    scope.Complete();
                    //print ecxll sheet
                    string message = "تم تحميل عدد " + OrdersPrint.Count() + " طلب على المندوب : " + user.Name;
                    if (OrdersPrint.Count() > 0)
                    {
                        if (model.print)
                        {
                            return RedirectToAction("PrintExcelClientNewOrders", "Orders", new
                            {
                                Id = model.UserId,
                                OrdersPrint = OrdersPrint,
                                showProductName = model.showProductName,
                                showSenderPhone = model.showSenderPhone,
                                showSenderName = model.showSenderName,
                                showOrderCost = model.showOrderCost,
                                showDeliveryFees = model.showDeliveryFees,
                                showClientCode = model.showClientCode
                            });
                        }
                        return RedirectToAction(nameof(Index), new { q = "d", message = message });
                    }
                    else
                    {
                        return RedirectToAction(nameof(Index), new { q = "d" });
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception
                    return RedirectToAction(nameof(AssignOrders), new { Id = model.UserId });
                }
            }
        }
        private bool IsValidOrderForAssignment(Order order, ApplicationUser user)
        {
            return order != null &&
                   (order.Status == OrderStatus.Placed || (order.Status == OrderStatus.Waiting && order.DeliveryId == null)) &&
                   !order.IsDeleted && !order.Pending &&
                   ((order.BranchId == user.BranchId && order.Client.BranchId == user.BranchId) ||
                    (order.BranchId == user.BranchId && order.TransferredConfirmed));
        }
        private async Task UpdateOrderHistory(long? orderHistoryId)
        {
            if (orderHistoryId.HasValue)
            {
                var history = await _Histories.GetObj(x => x.Id == orderHistoryId);
                history.Assign_To_Driver_UserId = _userManger.GetUserId(User);
                history.Assign_To_DriverDate = DateTime.Now.ToUniversalTime();
                await _Histories.Update(history);
            }
        }
        [Authorize(Roles = "Admin,HighAdmin,Accountant,LowAdmin,TrustAdmin")]
        public async Task<IActionResult> Wallet(int pageNumber = 1, int pageSize = 10,
             string id = "all", long BranchId = -1)
        {
            var history = _orderService.GetOrdersHistory(x => !x.IsDeleted && x.Finish_UserId == id);
            ViewBag.history = history;
            ViewBag.BranchId = BranchId;
            ViewBag.q = id;
            if (!User.IsInRole("Admin") && !User.IsInRole("TrustAdmin"))
            {
                var SubAdminUser = await _userManger.GetUserAsync(User);
                ViewBag.User = SubAdminUser;
                history = _orderService.GetOrdersHistory(x => !x.IsDeleted && x.Finish_UserId == SubAdminUser.Id);
                ViewBag.history = history;
                return View(await GetPagedListItems("", pageNumber, pageSize, SubAdminUser.Id, BranchId));
            }
            if (!await _user.IsExist(x => x.Id == id))
            {
                return NotFound("هذا المستخد غير موجود او محذوف");
            }
            var user = _user.Get(x => x.Id == id).First();
            ViewBag.User = user;
            return View(await GetPagedListItems("", pageNumber, pageSize, user.Id, BranchId));

        }

        #region TestWallet
        //[AllowAnonymous]

        //public async Task<IActionResult> TestWallet(int pageNumber = 1, int pageSize = 10,
        //   string id = "all", long BranchId = -1)
        //{


        //    if (!await _user.IsExist(x => x.Id == id))
        //    {
        //        return NotFound("هذا المستخد غير موجود او محذوف");
        //    }
        //    var user = _user.Get(x => x.Id == id).First();
        //    ViewBag.User = user;
        //    // Step 1: Fetch wallets from the database (without complex filtering)
        //    var wallets = await _wallet.GetAllAsIQueryable()
        //    .Where(f => f.UserId == id)
        //        .OrderByDescending(f => f.Id)  // Order by Id for easier comparison
        //        .Include("ActualUser")
        //        .Include("Orders").Take(500)
        //        .ToListAsync(); // Fetch as a list first

        //    // Step 2: Filter the wallets in memory based on the condition
        //    var filteredWallets = new List<Wallet>();
        //    for (int i = 1; i < wallets.Count; i++)
        //    {
        //        if (wallets[i].UserWalletLast == wallets[i - 1].UserWalletLast && wallets[i].Amount > 0 && wallets[i].TransactionType != TransactionType.OrderReturnedComplete)
        //        {
        //            filteredWallets.Add(wallets[i]);
        //        }
        //    }


        //    foreach (var WalletProblem in filteredWallets)
        //    {
        //        //هنغير الحاله بتاعته انها متضافتش في المحفظة

        //        WalletProblem.AddedToAdminWallet = false;
        //        await _wallet.Update(WalletProblem);
        //    }
        //    return View(filteredWallets);
        //}
        #endregion

        #region Add To AdminWallet 
        [Authorize(Roles = "Admin,TrustAdmin")]
        public async Task<IActionResult> ReAddToWallet(long walletId)
        {
            if (!await _wallet.IsExist(x => x.Id == walletId))
            {
                return Json(new { success = false, message = "هذه التقفيله غير موجوده" });
            }
            var wallet = await _wallet.GetObj(x => x.Id == walletId);

            if (wallet.Amount <= 0 || wallet.AddedToAdminWallet || wallet.TransactionType == TransactionType.OrderReturnedComplete)
            {
                return Json(new { success = false, message = "هذه التقفيله ليس بها أي مشكله , غير مسموح بتعديلها" });
            }
            var ActualUser = _user.Get(x => x.Id == wallet.ActualUserId).First();
            var Note = $"حل مشكلة التقفيله رقم: {wallet.Id} \n من: {ActualUser.Name} \n مبلغ: {wallet.Id}";


            //اضافة في المحفظة
            var user = _user.Get(x => x.Id == wallet.UserId).First();
            var userwallat = user.Wallet;
            if (wallet.TransactionType == TransactionType.OrderFinished || wallet.TransactionType == TransactionType.RemovedByAdmin)
            {
                user.Wallet -= wallet.Amount;
            }
            else if (wallet.TransactionType == TransactionType.AddedByAdmin || wallet.TransactionType == TransactionType.OrderComplete)
            {
                if (wallet.UserId == "9897454b-add0-45ef-ad3b-53027814ede7")
                    user.Wallet += wallet.Amount;
            }
            ///

            bool userUpdated = await _user.Update(user);

            if (userUpdated)
            {
                //// تحديث التقفيله
                wallet.AddedToAdminWallet = true;
                await _wallet.Update(wallet);
                ///

                ////اضافة مبلغ التقفيله
                await _wallet.Add(new Wallet()
                {
                    UserId = wallet.UserId,
                    Amount = wallet.Amount,
                    TransactionType = TransactionType.ReAddToWallet,
                    ActualUserId = wallet.ActualUserId,
                    Note = Note,
                    UserWalletLast = userwallat,
                    AddedToAdminWallet = true
                });
                /////

                return Json(new { success = true, message = "تم حل مشكلة التقفيله بنجاح " });
            }
            else
                return Json(new { success = false, message = "حدث خطأ , يرجي المحاوله مرةً أخرى" });
        }

        #endregion

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminWallet(int pageNumber = 1, int pageSize = 10,
            string id = "all", long BranchId = -1)
        {
            ViewBag.BranchId = BranchId;
            ViewBag.q = id;

            var SubAdminUser = await _userManger.GetUserAsync(User);
            ViewBag.User = SubAdminUser;
            var history = _orderService.GetOrdersHistory(x => !x.IsDeleted && x.Finish_UserId == SubAdminUser.Id);
            ViewBag.history = history;
            return View(await GetPagedListItems("", pageNumber, pageSize, SubAdminUser.Id, BranchId));

        }
        [Authorize(Roles = "Admin,TrustAdmin")]
        public async Task<IActionResult> AddOrSubtractToUserWallet(WalletTransactionVM model)
        {
            var Actual = await _userManger.GetUserAsync(User);
            if (!await _user.IsExist(x => x.Id == model.UserId))
            {
                return NotFound("هذا المستخدم غير موجود");
            }
            if (model.Amount <= 0)
            {
                return BadRequest("المبلغ المطلوب اضافته غير صحيح");
            }
            var user = _user.Get(x => x.Id == model.UserId).First();
            var userwallat = user.Wallet;
            if (model.IsAdd)
            {
                user.Wallet += model.Amount;
                await _wallet.Add(new Wallet()
                {
                    UserId = model.UserId,
                    Amount = model.Amount,
                    TransactionType = TransactionType.AddedByAdmin,
                    ActualUserId = Actual.Id,
                    Note = model.Note,
                    UserWalletLast = userwallat,
                    AddedToAdminWallet = true
                });
            }
            else
            {
                user.Wallet -= model.Amount;
                await _wallet.Add(new Wallet()
                {
                    UserId = model.UserId,
                    Amount = model.Amount,
                    TransactionType = TransactionType.RemovedByAdmin,
                    ActualUserId = Actual.Id,
                    Note = model.Note,
                    UserWalletLast = userwallat,
                    AddedToAdminWallet = true
                });
            }
            return RedirectToAction(nameof(Index), new { q = "a" });
        }
        [Authorize(Roles = "Admin,HighAdmin,TrustAdmin")]
        public async Task<IActionResult> Delete(string id)
        {
            if (!await _user.IsExist(x => x.Id == id))
            {
                return Json(new { success = false, message = "هذا المستخدم غير موجود" });
            }
            else
            {
                if (User.IsInRole("Admin") || User.IsInRole("TrustAdmin"))
                {
                    var user = await _user.GetObj(x => x.Id == id);
                    user.IsDeleted = !user.IsDeleted;
                    if (!await _user.Update(user))
                    {
                        return Json(new { success = false, message = "حدث خطاء ما من فضلك حاول لاحقاً " });
                    }
                    if (user.IsDeleted)
                        return Json(new { success = true, message = "تم حذف المستخدم بنجاح لاستراجعه قم بالتوجهه المستخدمين المحذوفين " });
                    else
                        return Json(new { success = true, message = "تم استراجاع المستخدم بنجاح " });

                }
                else
                {
                    var HighAdmin = await _user.GetObj(x => x.Id == _userManger.GetUserId(User));
                    var user = await _user.GetObj(x => x.Id == id);
                    if (HighAdmin.BranchId == user.BranchId && (user.UserType == UserType.Driver || user.UserType == UserType.Client))
                    {
                        user.IsDeleted = !user.IsDeleted;
                        if (!await _user.Update(user))
                        {
                            return Json(new { success = false, message = "حدث خطاء ما من فضلك حاول لاحقاً " });
                        }
                        if (user.IsDeleted)
                            return Json(new { success = true, message = "تم حذف المستخدم بنجاح لاستراجعه قم بالتوجهه المستخدمين المحذوفين " });
                        else
                            return Json(new { success = true, message = "تم استراجاع المستخدم بنجاح " });
                    }
                    else
                    {
                        return Json(new { success = true, message = "غير مسموح لك بإتمام هذه العمليه !" });
                    }
                }

            }
        }
        [Authorize(Roles = "Admin,HighAdmin,Accountant,LowAdmin,TrustAdmin")]
        public IActionResult WalletOrders(long walletId)
        {
            ViewBag.walletId = walletId;
            return View(_orderService.GetList(c => c.WalletId == walletId || c.ReturnedWalletId == walletId).ToList());
        }
        #region createOrders
        public ActionResult createOrders(string id)
        {
            OrdersVM productsVM = new OrdersVM();
            productsVM.ClientId = id;
            return View(productsVM);
        }
        public FileContentResult Downloadfile()
        {
            //var sDocument = System.IO.Path.GetFullPath("OrdersDetails.xlsx");
            string webRootPath = _webHostEnvironment.WebRootPath;
            string contentRootPath = _webHostEnvironment.ContentRootPath;
            string path = "";
            path = Path.Combine(webRootPath, "ExcelOrdersDetails.xlsx");


            byte[] fileBytes = System.IO.File.ReadAllBytes(path);
            string fileName = "OrdersDetails.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);

        }

        public ActionResult createUsers()
        {
            ViewBag.Branchs = _branch.Get(x => !x.IsDeleted).ToList();
            UsersVM productsVM = new UsersVM();
            return View(productsVM);
        }
        public FileContentResult DownloadUsersFile()
        {
            //var sDocument = System.IO.Path.GetFullPath("OrdersDetails.xlsx");
            string webRootPath = _webHostEnvironment.WebRootPath;
            string contentRootPath = _webHostEnvironment.ContentRootPath;
            string path = "";
            path = Path.Combine(webRootPath, "UsersDetails.xlsx");


            byte[] fileBytes = System.IO.File.ReadAllBytes(path);
            string fileName = "UsersDetails.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);

        }
        [HttpPost]
        public async Task<ActionResult> createOrders(OrdersVM model)
        {
            var user = _user.Get(x => x.Id == model.ClientId).First();
            string GeneralNote = user.OrdersGeneralNote != null ? user.OrdersGeneralNote + " - " : "";
            if (model.file != null)
            {
                string extension = System.IO.Path.GetExtension(model.file.FileName);
                if (extension == ".xls" || extension == ".xlsx")
                {   /////Upload The Excel File (Products`s Data)
                    var filename = System.IO.Path.GetFullPath("~/Files/OrdersExcel/") + model.file.FileName;

                    string webRootPath = _webHostEnvironment.WebRootPath;
                    string contentRootPath = _webHostEnvironment.ContentRootPath;
                    string path = "";
                    path = Path.Combine(webRootPath, "Orders.xlsx");

                    using (var stream = System.IO.File.Create(path))
                    {
                        await model.file.CopyToAsync(stream);
                    }

                    //
                    ////read the products data and split it in a list
                    ////
                    var file = $"{Directory.GetCurrentDirectory()}{@"\wwwroot\"}" + "\\" + "Orders.xlsx";
                    List<Order> orders = new List<Order>();

                    // First Pass: Validate all codes if user wants to use uploaded codes
                    #region ValidationCodes
                    if (model.UseUploadedCodes)
                    {
                        List<string> problematicCodes = new List<string>();
                        List<string> excelCodes = new List<string>(); // To store codes from the Excel file
                        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                        using (var steram = System.IO.File.Open(file, FileMode.Open, FileAccess.Read))
                        {
                            using (var reader = ExcelReaderFactory.CreateReader(steram))
                            {
                                int x = 0;
                                while (reader.Read())
                                {
                                    if (x >= 1)
                                    {
                                        if (reader.GetValue(8) != null)
                                        {
                                            string code = reader.GetValue(8).ToString();
                                            // Check for duplicate codes within the Excel file
                                            if (excelCodes.Contains(code))
                                            {
                                                problematicCodes.Add($"الكود {code} مكرر في السطر رقم {x + 1}");
                                            }


                                            else
                                            {
                                                excelCodes.Add(code);
                                            }

                                            var existingOrder = _orders.Get(o => o.Code == code).FirstOrDefault();
                                            if (existingOrder != null)
                                            {
                                                problematicCodes.Add($"الكود {code} موجود مسبقاً في النظام");
                                            }
                                        }
                                        else
                                        {
                                            string error = " لم يتم إدخال كود الطلب في السطر رقم " + (x + 1).ToString();
                                            problematicCodes.Add(error);
                                        }
                                    }
                                    x++;
                                }
                            }
                        }

                        if (problematicCodes.Any())
                        {
                            foreach (var error in problematicCodes)
                            {
                                ModelState.AddModelError("", error);
                            }
                            return View(model);
                        }
                    }
                    #endregion




                    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                    using (var steram = System.IO.File.Open(file, FileMode.Open, FileAccess.Read))
                    {
                        using (var reader = ExcelReaderFactory.CreateReader(steram))
                        {
                            //declare a variable to skip reading the first row in the excel sheet
                            int x = 0;
                            var b = false;

                            if (User.IsInRole("Client"))
                                b = true;
                            // while (reader.Read())
                            while (reader.Read())
                            {
                                string tips = GeneralNote;
                                var temp = reader.GetValue(6);
                                if (temp != null)
                                    tips = reader.GetValue(6).ToString();
                                //new product 

                                if (x >= 1)
                                {
                                    try
                                    {
                                        //var barcodeWriter = new BarcodeWriter
                                        //{
                                        //    Format = BarcodeFormat.CODE_128,
                                        //    Options = new EncodingOptions
                                        //    {
                                        //        Height = 30,
                                        //        Width = 75
                                        //    }
                                        //};

                                        orders.Add(new Order()
                                        {
                                            ClientId = model.ClientId,
                                            ClientName = reader.GetValue(0).ToString(),
                                            ClientPhone = "0" + reader.GetValue(1).ToString(),
                                            Address = reader.GetValue(2).ToString(),
                                            AddressCity = reader.GetValue(3).ToString(),
                                            Cost = Convert.ToDouble(reader.GetValue(4)),
                                            DeliveryFees = Convert.ToDouble(reader.GetValue(5)),
                                            Pending = b,
                                            TotalCost = Convert.ToDouble(reader.GetValue(4)) + Convert.ToDouble(reader.GetValue(5)),
                                            Status = OrderStatus.Placed,
                                            BranchId = user.BranchId,
                                            Notes = tips,
                                            ClientCode = reader.GetValue(7).ToString()

                                        });

                                    }
                                    catch
                                    {
                                        return BadRequest("يوجد خطا في بيانات الطلبات , من فضلك حاول لاحقاً");
                                    }
                                    if (model.UseUploadedCodes)
                                    {

                                        var existingOrder = _orders.Get(o => o.Code == reader.GetValue(8).ToString()).FirstOrDefault();
                                        if (existingOrder != null)
                                        {
                                            goto skip;
                                        }
                                    }
                                    if (!await _orders.Add(orders[x - 1]))
                                    {
                                        return BadRequest("من فضلك حاول لاحقاً");
                                    }
                                    OrderOperationHistory history = new OrderOperationHistory()
                                    {
                                        OrderId = orders[x - 1].Id,
                                        Create_UserId = _userManger.GetUserId(User),
                                        CreateDate = orders[x - 1].CreateOn,
                                    };
                                    if (!await _Histories.Add(history))
                                    {
                                        return BadRequest("من فضلك حاول لاحقاً");
                                    }
                                    orders[x - 1].OrderOperationHistoryId = history.Id;
                                    if (model.UseUploadedCodes)
                                    {
                                        orders[x - 1].Code = reader.GetValue(8).ToString();
                                        orders[x - 1].UseCustomCode = true;
                                    }
                                    else
                                    {

                                        //string datetoday = DateTime.Now.ToString("ddMMyyyy");
                                        orders[x - 1].Code = "Tas" + /*datetoday +*/ orders[x - 1].Id.ToString();
                                    }
                                    orders[x - 1].BarcodeImage = getBarcode(orders[x - 1].Code);



                                    orders[x - 1].LastUpdated = orders[x - 1].CreateOn;
                                    if (!await _orders.Update(orders[x - 1]))
                                    {
                                        return BadRequest("من فضل حاول في وقتاً أخر");
                                    }

                                    await _CRUD.Update(orders[x - 1].Id);

                                }

                            skip:
                                x++;
                            }
                        }
                    }
                    if (User.IsInRole("Client") && orders.Count() > 0)
                    {
                        var BranchAdmins = _user.Get(x => x.UserType == UserType.HighAdmin
                          && x.BranchId == user.BranchId && !x.IsDeleted && !x.Branch.IsDeleted).ToList();

                        var Title = $"طلبات جديده للراسل :{user.Name}";
                        var Body = $"قام الراسل : {user.Name} . برفع طلبات جديده عن طريق الاكسيل شيت , يرجي مراجعتها .  ";

                        var send = new SendNotification(_pushNotification, _notification);
                        foreach (var admin in BranchAdmins)
                        {
                            await send.SendToAllSpecificAndroidUserDevices(admin.Id, Title, Body);
                        }
                    }

                    if (!User.IsInRole("Client"))
                    {
                        string type = "";
                        if (user.UserType == UserType.Driver)
                            type = "d";
                        else if (user.UserType == UserType.Client)
                            type = "c";
                        else if (user.UserType == UserType.Admin || user.UserType == UserType.HighAdmin || user.UserType == UserType.Accountant)
                            type = "a"; if (type != "")
                        {
                            return RedirectToAction(nameof(Index), new { q = type, BranchId = user.BranchId });
                        }
                        return RedirectToAction(nameof(Index));
                    }
                    return RedirectToAction(actionName: "Index", controllerName: "Orders");

                }

                return BadRequest(" صيغة الملف غير صحيحه , من فضلك حاول لاحقاً");

            }
            return BadRequest(" يجب ادخال ملف اكسيل , من فضلك حاول لاحقاً");
        }
        #endregion

        [HttpPost]
        public async Task<ActionResult> createUsers(UsersVM model)
        {
            var branch = _branch.Get(x => x.Id == model.BranchId).First();
            if (model.UserType != UserType.Client && model.UserType != UserType.Driver)
            {
                return BadRequest(" من فضلك حاول لاحقاً");
            }

            if (branch != null)
            {

                if (model.file != null)
                {
                    string extension = System.IO.Path.GetExtension(model.file.FileName);
                    if (extension == ".xls" || extension == ".xlsx")
                    {   /////Upload The Excel File (Products`s Data)
                        var filename = System.IO.Path.GetFullPath("~/Files/UsersExcel/") + model.file.FileName;

                        string webRootPath = _webHostEnvironment.WebRootPath;
                        string contentRootPath = _webHostEnvironment.ContentRootPath;
                        string path = "";
                        path = Path.Combine(webRootPath, "Users.xlsx");

                        using (var stream = System.IO.File.Create(path))
                        {
                            await model.file.CopyToAsync(stream);
                        }

                        //
                        ////read the products data and split it in a list
                        ////
                        var file = $"{Directory.GetCurrentDirectory()}{@"\wwwroot\"}" + "\\" + "Users.xlsx";
                        List<ApplicationUser> users = new List<ApplicationUser>();
                        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                        using (var steram = System.IO.File.Open(file, FileMode.Open, FileAccess.Read))
                        {
                            using (var reader = ExcelReaderFactory.CreateReader(steram))
                            {
                                //declare a variable to skip reading the first row in the excel sheet
                                int x = 0;
                                while (reader.Read())
                                {
                                    //new user 

                                    if (x >= 1)
                                    {
                                        try
                                        {

                                            users.Add(new ApplicationUser()
                                            {
                                                BranchId = model.BranchId,
                                                Name = reader.GetValue(0).ToString(),
                                                PhoneNumber = reader.GetValue(1).ToString(),
                                                WhatsappPhone = reader.GetValue(2).ToString(),
                                                Email = reader.GetValue(3).ToString(),
                                                Address = reader.GetValue(4).ToString(),
                                                UserType = model.UserType,
                                                SecurityStamp = Guid.NewGuid().ToString(),
                                                IsPending = true
                                            });

                                        }
                                        catch
                                        {
                                            return BadRequest("يوجد خطا في بيانات الطلبات , من فضلك حاول لاحقاً");
                                        }
                                        if (!await _user.Add(users[x - 1]))
                                        {
                                            return BadRequest("من فضلك حاول لاحقاً");
                                        }
                                        if (model.UserType == UserType.Client)
                                        {
                                            await _userManger.AddToRoleAsync(users[x - 1], "Client");
                                        }
                                        else if (model.UserType == UserType.Driver)
                                        {
                                            await _userManger.AddToRoleAsync(users[x - 1], "Driver");
                                        }
                                    }
                                    x++;
                                }
                            }
                        }
                        if (model.UserType == UserType.Client)
                            return RedirectToAction(nameof(Index), new { q = "c", BranchId = model.BranchId });
                        else return RedirectToAction(nameof(Index), new { q = "d", BranchId = model.BranchId });
                    }

                    return BadRequest(" صيغة الملف غير صحيحه , من فضلك حاول لاحقاً");

                }
                return BadRequest(" يجب ادخال ملف اكسيل , من فضلك حاول لاحقاً");
            }
            return BadRequest(" يجب اختيار الفرع , من فضلك حاول لاحقاً");
        }

        public IActionResult ExportToExecl(int spec, long BranchId = -1)
        {
            var drivers = new List<ApplicationUser>();
            if (spec == 0)
                drivers = _user.Get(x => !x.IsDeleted && x.UserType == UserType.Client && (BranchId == -1 || BranchId == x.BranchId)).ToList();

            else if (spec == 1)
                drivers = _user.Get(x => !x.IsDeleted && x.UserType == UserType.Driver && (BranchId == -1 || BranchId == x.BranchId)).ToList();

            var dt = ExcelExport.DriversExport(drivers);
            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dt);
                using (MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Drivers.xlsx");
                }
            }

        }

        #region Accept Pending Orders
        [Authorize(Roles = "Admin,HighAdmin,TrustAdmin")]
        public IActionResult AcceptOrders(string id)
        {
            ViewBag.UserId = id;
            var user = _user.Get(x => x.Id == id).First();
            ViewBag.Orders = _orders.Get(x => x.ClientId == id && x.Status == OrderStatus.Placed && !x.IsDeleted && x.Pending
            && ((x.BranchId == user.BranchId && x.Client.BranchId == user.BranchId) || (x.BranchId == user.BranchId && x.TransferredConfirmed))).ToList();
            return View();
        }
        [Authorize(Roles = "Admin,HighAdmin,TrustAdmin")]
        [HttpPost]
        public async Task<IActionResult> AcceptOrders(List<long> Orders, string UserId)
        {
            var user = _user.Get(x => x.Id == UserId).First();

            //اقبل
            foreach (var id in Orders)
            {
                if (!await _orders.IsExist(x => x.Id == id && user.Id == x.ClientId))
                {
                    return BadRequest("هذه الطلب غير موجود");
                }
                else
                {
                    var order = await _orders.GetObj(x => x.Id == id);
                    order.Pending = false;
                    order.LastUpdated = DateTime.Now.ToUniversalTime();
                    await _orders.Update(order);
                    if (order.OrderOperationHistoryId != null)
                    {
                        var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
                        history.Accept_UserId = _userManger.GetUserId(User);
                        history.AcceptDate = DateTime.Now.ToUniversalTime();
                        await _Histories.Update(history);
                        //await _CRUDHistory.Update(history.Id);
                    }
                }
            }
            string type = "";
            if (user.UserType == UserType.Driver)
                type = "d";
            else if (user.UserType == UserType.Client)
                type = "c";
            else if (user.UserType == UserType.Admin || user.UserType == UserType.HighAdmin || user.UserType == UserType.Accountant)
                type = "a";
            if (type != "")
            {
                return RedirectToAction(nameof(Index), new { q = type, BranchId = user.BranchId });
            }
            return RedirectToAction(nameof(Index));
        }

        #endregion
        private byte[] getBarcode(string Code)
        {
            //            id += 1000;
            var barcodeWriter = new ZXing.BarcodeWriter
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Height = 50,
                    Width = 175
                }
            };
            using var barcodeBitmap = barcodeWriter.Write(Code);
            using var ms = new MemoryStream();
            barcodeBitmap.Save(ms, ImageFormat.Png);
            var barcodeImage = ms.ToArray();
            return barcodeImage;
        }
        #region PAGINATION METHODS

        [Authorize]
        public async Task<PagedList<Wallet>> GetPagedListItems(string searchStr, int pageNumber, int pageSize, string q,
            long BranchId = -1)
        {
            bool auth = User.IsInRole("Client");
            var user = new ApplicationUser();
            if (auth)
            {
                user = await _userManger.GetUserAsync(User);
            }

            Expression<Func<Wallet, bool>> filter = null;
            Func<IQueryable<Wallet>, IOrderedQueryable<Wallet>> orderBy = o => o.OrderByDescending(c => c.Id);

            if (!string.IsNullOrEmpty(searchStr) || q != null)
            {
                if (!string.IsNullOrEmpty(searchStr))
                    searchStr = searchStr.ToLower();

                filter = f =>
                f.UserId == q &&
                              ((string.IsNullOrEmpty(searchStr) ? true : f.Note.ToLower().Contains(searchStr))
                               || (string.IsNullOrEmpty(searchStr)
                                   ? true
                                   : f.Amount.ToString().ToLower().Contains(searchStr))

                               || (string.IsNullOrEmpty(searchStr) ? true : f.ActualUser.Name.ToLower().Contains(searchStr))
                               || (string.IsNullOrEmpty(searchStr)
                                   ? true
                                   : (f.Id).ToString().Contains(searchStr)));
            }

            /*  var orders = _orders.Get(filter).ToList();
              ViewBag.TotalPrice = orders.Sum(x => x.Cost+x.DeliveryFees);*/
            ViewBag.PageStartRowNum = ((pageNumber - 1) * pageSize) + 1;

            return await PagedList<Wallet>.CreateAsync(
                _wallet.GetAllAsIQueryable(filter, orderBy, "ActualUser,Orders"),
                pageNumber, pageSize);
        }

        public async Task<IActionResult> GetItems(string searchStr, string q, long BranchId = -1, int pageNumber = 1,
            int pageSize = 10)
        {
            return PartialView("_TableList",
                (await GetPagedListItems(searchStr, pageNumber, pageSize, q, BranchId)).ToList());
        }


        public async Task<IActionResult> GetPagination(string searchStr, string q, long BranchId = -1, int pageNumber = 1,
            int pageSize = 10)
        {
            var model = PagedList<Wallet>.GetPaginationObject(
                await GetPagedListItems(searchStr, pageNumber, pageSize, q, BranchId));
            model.GetItemsUrl = "/Users/GetItems";
            model.GetPaginationUrl = "/Users/GetPagination";
            return PartialView("_Pagination2", model);
        }

        #endregion
    }
}
