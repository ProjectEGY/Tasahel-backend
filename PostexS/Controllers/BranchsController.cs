using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PostexS.Helper;
using PostexS.Interfaces;
using PostexS.Models.Domain;
using PostexS.Models.Enums;
using PostexS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Order = PostexS.Models.Domain.Order;

namespace PostexS.Controllers
{
    [Authorize(Roles = "Admin,HighAdmin,Accountant,TrustAdmin")]
    public class BranchsController : Controller
    {
        private readonly IGeneric<Branch> _branch;
        private readonly IGeneric<Order> _orders;
        private readonly ICRUD<Branch> _CRUD;
        private readonly IGeneric<OrderOperationHistory> _Histories;
        private readonly IGeneric<OrderTransferrHistory> _TransferHistories;
        private readonly ICRUD<OrderOperationHistory> _CRUDHistory;
        private readonly IGeneric<ApplicationUser> _user;
        private readonly UserManager<ApplicationUser> _userManger;
        private readonly IOrderService _orderService;
        private IGeneric<DeviceTokens> _pushNotification;
        private IGeneric<Notification> _notification;

        public BranchsController(UserManager<ApplicationUser> userManger, ICRUD<OrderOperationHistory> CRUDhistory, IGeneric<OrderOperationHistory> histories, IGeneric<OrderTransferrHistory> TransferHistories
            , IOrderService orderService, IGeneric<DeviceTokens> pushNotification, IGeneric<Notification> notification, IGeneric<Branch> branch, IGeneric<Order> orders, ICRUD<Branch> CRUD, IGeneric<ApplicationUser> users)
        {
            _orderService = orderService;
            _branch = branch;
            _CRUD = CRUD;
            _Histories = histories;
            _TransferHistories = TransferHistories;
            _CRUDHistory = CRUDhistory;
            _orders = orders;
            _user = users;
            _userManger = userManger;
            _notification = notification;
            _pushNotification = pushNotification;

        }
        [Authorize(Roles = "Admin,HighAdmin,Accountant,TrustAdmin")]
        public async Task<IActionResult> Index(string q)
        {
            if (User.IsInRole("HighAdmin") || User.IsInRole("Accountant"))
            {
                var user = await _user.GetObj(x => x.Id == _userManger.GetUserId(User));
                var BranchId = user.BranchId;
                if (q == "deleted")
                {
                    ViewBag.State = "D";
                    return View(_branch.Get(x => x.IsDeleted && x.Id != BranchId).ToList());
                }
                return View(_branch.Get(x => !x.IsDeleted && x.Id != BranchId).ToList());
            }
            if (q == "deleted")
            {
                ViewBag.State = "D";
                return View(_branch.Get(x => x.IsDeleted).ToList());
            }
            return View(_branch.Get(x => !x.IsDeleted).ToList());
        }
        [Authorize(Roles = "Admin,TrustAdmin")]
        [HttpPost]
        public async Task<IActionResult> Create(Branch model, string AdminEmail, string AdminName)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("حدث خطأ اثناء ادخال البيانات");
            }
            if (await _user.IsExist(x => x.PhoneNumber == model.PhoneNumber))
            {
                return BadRequest("هذا الرقم موجود من قبل");
            }
            string Email = "";
            if (string.IsNullOrEmpty(AdminEmail))
            {
                Email = RandomGenerator.GenerateString(4) + "@Tasahel.com";
            }
            else
            {
                if (await _user.IsExist(x => x.Email.Trim().ToLower() == AdminEmail.Trim().ToLower()))
                {
                    return BadRequest("هذا الايميل موجود من قبل");
                }
                Email = AdminEmail;
            }
            if (!await _branch.Add(model))
            {
                return BadRequest("من فضل حاول في وقت أخر");
            }
            var user = new ApplicationUser()
            {
                UserName = Email,
                Email = Email,
                Name = AdminName,
                PhoneNumber = model.PhoneNumber,
                SecurityStamp = Guid.NewGuid().ToString(),
                Address = model.Address,
                WhatsappPhone = model.Whatsapp,
                UserType = UserType.HighAdmin,
                BranchId = model.Id,
                IsPending = true
            };

            var result = await _userManger.CreateAsync(user, "123456");
            if (!result.Succeeded)
            {
                return BadRequest("من فضلك حاول مره اخري لاحقاً");
            }
            await _userManger.AddToRoleAsync(user, "HighAdmin");

            return RedirectToAction(nameof(Index));
        }
        [Authorize(Roles = "Admin,TrustAdmin")]
        public async Task<IActionResult> Edit(long id)
        {
            if (!await _branch.IsExist(x => x.Id == id))
            {
                return NotFound();
            }
            ViewBag.Title = "تعديل الفرع";
            return View(_branch.Get(x => x.Id == id).First());
        }
        [Authorize(Roles = "Admin,TrustAdmin")]
        [HttpPost]
        public async Task<IActionResult> Edit(Branch model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("حدث خطأ اثناء ادخال البيانات");
            }
            if (!await _branch.Update(model))
            {
                return BadRequest("من فضل حاول في وقتاً أخر");
            }
            await _CRUD.Update(model.Id);
            return RedirectToAction(nameof(Index));
        }
        [Authorize(Roles = "Admin,TrustAdmin")]
        [HttpDelete]
        public async Task<IActionResult> Delete(long id)
        {
            if (!await _branch.IsExist(x => x.Id == id))
            {
                return Json(new { success = false, message = "هذا الفرع غير موجود" });
            }
            else
            {
                if (!await _CRUD.ToggleDelete(id))
                {
                    return Json(new { success = false, message = "حدث خطاء ما من فضلك حاول لاحقاً " });
                }
                if (_branch.Get(x => x.Id == id).First().IsDeleted)
                    return Json(new { success = true, message = "تم حذف الفرع بنجاح لاستراجعه قم بالتوجهه الفروع المحذوفة " });
                else
                    return Json(new { success = true, message = "تم استراجاع الفرح بنجاح " });
            }

        }

        #region Switch Orders
        [Route("orders-transferr")]
        [Authorize(Roles = "Admin,HighAdmin,Accountant,TrustAdmin")]
        public async Task<IActionResult> SwitchOrders(long id)
        {

            if (User.IsInRole("HighAdmin") || User.IsInRole("Accountant"))
            {
                var user = await _user.GetObj(x => x.Id == _userManger.GetUserId(User));

                ViewBag.BranchId = id;
                ViewBag.Branch = _branch.Get(x => x.Id == id).First();
                ViewBag.Orders = _orders.Get(x => (x.Status == OrderStatus.Placed && x.DeliveryId == null) && !x.IsDeleted
                && !x.Pending && x.BranchId == user.BranchId && ((x.Client.BranchId == user.BranchId && (x.PreviousBranchId == null || x.PreviousBranchId == user.BranchId)) || x.TransferredConfirmed)).ToList();

                ViewBag.TransferedOrdersUnConfirmed = _orderService.GetList(x => (x.Status == OrderStatus.Placed && x.DeliveryId == null) && !x.IsDeleted
                              && !x.Pending && x.BranchId == id && x.PreviousBranchId == user.BranchId && !x.TransferredConfirmed).ToList();

                return View();
            }
            ViewBag.BranchId = id;
            ViewBag.Branch = _branch.Get(x => x.Id == id).First();
            ViewBag.Orders = _orders.Get(x => x.Status == OrderStatus.Placed && x.DeliveryId == null && !x.IsDeleted
            && !x.Pending && x.BranchId != id && ((x.BranchId == x.Client.BranchId && (x.PreviousBranchId == null || x.PreviousBranchId == x.BranchId)) || (x.BranchId == x.Client.BranchId && x.TransferredConfirmed) || (x.BranchId != x.Client.BranchId && x.TransferredConfirmed))).ToList();

            ViewBag.TransferedOrdersUnConfirmed = _orderService.GetList(x => x.Status == OrderStatus.Placed && x.DeliveryId == null && !x.IsDeleted
                      && !x.Pending && x.BranchId == id && x.PreviousBranchId != id && !x.TransferredConfirmed).ToList();

            return View();
        }
        [Route("orders-Transferr")]
        [Authorize(Roles = "Admin,HighAdmin,Accountant,TrustAdmin")]
        [HttpPost]
        public async Task<IActionResult> SwitchOrders(dto model)
        {
            List<long> Orders = model.Orders;
            long BranchId = model.BranchId;
            //long? previousBranchId;
            if (User.IsInRole("HighAdmin") || User.IsInRole("Accountant"))
            {
                var user = await _user.GetObj(x => x.Id == _userManger.GetUserId(User));
                foreach (var item in Orders)
                {
                    var order = _orders.GetAllAsIQueryable(x => x.Id == item, null, "Client,Branch").First();
                    if ((order.Status == OrderStatus.Placed && order.DeliveryId == null) && !order.IsDeleted
                    && !order.Pending && order.BranchId == user.BranchId && ((order.Client.BranchId == user.BranchId && (order.PreviousBranchId == null || order.PreviousBranchId == order.BranchId)) || order.TransferredConfirmed))
                    {
                        long? previousBranchId = order.PreviousBranchId;
                        order.PreviousBranchId = order.BranchId;
                        order.BranchId = BranchId;
                        order.TransferredConfirmed = false;
                        order.LastUpdated = DateTime.Now.ToUniversalTime();
                        if (await _orders.Update(order))
                        {
                            // نضيف التحويل الجديد
                            var NewTransfer = new OrderTransferrHistory
                            {
                                OrderId = order.Id,
                                FromBranchId = order.PreviousBranchId.Value,
                                ToBranchId = order.BranchId,
                                Transfer_UserId = user.Id,
                                PreviousBranchId = previousBranchId,
                            };
                            //

                            await _TransferHistories.Add(NewTransfer);

                            if (order.OrderOperationHistoryId != null)
                            {
                                var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
                                history.Transfer_UserId = _userManger.GetUserId(User);
                                history.TransferDate = DateTime.Now.ToUniversalTime();
                                await _Histories.Update(history);
                                //await _CRUDHistory.Update(history.Id);
                            }
                        }
                    }
                }
                if (Orders.Count() > 0)
                {
                    await SendNotify(BranchId);
                }
                return RedirectToAction(nameof(SwitchOrders), new { id = BranchId });
            }

            foreach (var item in Orders)
            {
                var order = _orders.GetAllAsIQueryable(x => x.Id == item, null, "Client,Branch").First();
                if ((order.Status == OrderStatus.Placed && order.DeliveryId == null) && !order.IsDeleted && !order.Pending
                    && order.BranchId != BranchId && ((order.BranchId == order.Client.BranchId && (order.PreviousBranchId == null || order.PreviousBranchId == order.BranchId))
                    || (order.BranchId == order.Client.BranchId && order.TransferredConfirmed)
                    || (order.BranchId != order.Client.BranchId && order.TransferredConfirmed)
                    ))
                {
                    long? previousBranchId = order.PreviousBranchId;
                    order.PreviousBranchId = order.BranchId;
                    order.BranchId = BranchId;
                    order.TransferredConfirmed = false;
                    order.LastUpdated = DateTime.Now.ToUniversalTime();
                    if (await _orders.Update(order))
                    {
                        // نضيف التحويل الجديد
                        var NewTransfer = new OrderTransferrHistory
                        {
                            OrderId = order.Id,
                            FromBranchId = order.PreviousBranchId.Value,
                            ToBranchId = order.BranchId,
                            Transfer_UserId = _userManger.GetUserId(User),
                            PreviousBranchId = previousBranchId,
                        };
                        //

                        await _TransferHistories.Add(NewTransfer);

                        if (order.OrderOperationHistoryId != null)
                        {
                            var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
                            history.Transfer_UserId = _userManger.GetUserId(User);
                            history.TransferDate = DateTime.Now.ToUniversalTime();
                            await _Histories.Update(history);
                            //await _CRUDHistory.Update(history.Id);
                        }
                    }
                }
            }
            if (Orders.Count() > 0)
            {
                await SendNotify(BranchId);
            }
            return RedirectToAction(nameof(SwitchOrders), new { id = BranchId });


        }
        [Route("orders-CancelTransferr")]
        [HttpGet]
        public async Task<IActionResult> CancelTransfer(long id, long BranchId)
        {
            var order = _orders.GetAllAsIQueryable(x => x.Id == id, null, "Client,Branch").First();
            if (order != null)
            {
                var trans = _TransferHistories.Get(x => x.OrderId == order.Id &&
                               x.FromBranchId == order.PreviousBranchId && x.ToBranchId == order.BranchId).OrderBy(a => a.Id).Last();

                if (User.IsInRole("HighAdmin") || User.IsInRole("Accountant"))
                {
                    var user = await _user.GetObj(x => x.Id == _userManger.GetUserId(User));

                    if ((order.Status == OrderStatus.Placed && order.DeliveryId == null) && !order.IsDeleted
                    && !order.Pending && order.BranchId != user.BranchId && order.PreviousBranchId == user.BranchId && !order.TransferredConfirmed)
                    {
                        order.BranchId = order.PreviousBranchId.Value;
                        order.PreviousBranchId = trans.PreviousBranchId;
                        order.LastUpdated = DateTime.Now.ToUniversalTime();
                        if (order.BranchId != order.Client.BranchId || (order.BranchId == order.Client.BranchId && order.PreviousBranchId != null))
                            order.TransferredConfirmed = true;

                        await _orders.Update(order);
                        trans.TransferCancel = true;
                        await _TransferHistories.Update(trans);
                        await SendCancelNotify(BranchId, order.Code);
                    }
                    return RedirectToAction(nameof(SwitchOrders), new { id = BranchId });
                }
                else
                {
                    if ((order.Status == OrderStatus.Placed && order.DeliveryId == null) && !order.IsDeleted && !order.Pending
                     && order.BranchId == BranchId && !order.TransferredConfirmed)
                    {
                        order.BranchId = order.PreviousBranchId.Value;
                        order.PreviousBranchId = trans.PreviousBranchId;
                        order.LastUpdated = DateTime.Now.ToUniversalTime();
                        if (order.BranchId != order.Client.BranchId || (order.BranchId == order.Client.BranchId && order.PreviousBranchId != null))
                            order.TransferredConfirmed = true;
                        await _orders.Update(order);
                        await SendCancelNotify(BranchId, order.Code);
                    }
                }
            }
            return RedirectToAction(nameof(SwitchOrders), new { id = BranchId });
        }
        [Route("orders-AcceptTransferr")]
        [Authorize(Roles = "Admin,HighAdmin,Accountant,TrustAdmin")]
        public async Task<IActionResult> AcceptSwitchOrders(long id)
        {

            ViewBag.BranchId = id;
            ViewBag.Branch = _branch.Get(x => x.Id == id).First();
            if (User.IsInRole("HighAdmin") || User.IsInRole("Accountant"))
            {
                var user = await _user.GetObj(x => x.Id == _userManger.GetUserId(User));

                ViewBag.Orders = _orderService.GetList(x => x.Status == OrderStatus.Placed && x.DeliveryId == null && !x.IsDeleted
                && !x.Pending && x.BranchId == user.BranchId && x.PreviousBranchId == id && x.TransferredConfirmed == false).ToList();
                return View();
            }
            ViewBag.Orders = _orderService.GetList(x => x.Status == OrderStatus.Placed
               && x.DeliveryId == null && !x.IsDeleted
               && !x.Pending && x.BranchId == id && x.PreviousBranchId != id && x.TransferredConfirmed == false).ToList();

            return View();
        }
        [Route("orders-AcceptTransferr")]
        [Authorize(Roles = "Admin,HighAdmin,Accountant,TrustAdmin")]
        [HttpPost]
        public async Task<IActionResult> AcceptSwitchOrders(dto model)
        {
            List<long> Orders = model.Orders;
            long BranchId = model.BranchId;
            if (User.IsInRole("HighAdmin") || User.IsInRole("Accountant"))
            {
                var user = await _user.GetObj(x => x.Id == _userManger.GetUserId(User));
                foreach (var item in Orders)
                {
                    var order = _orders.GetAllAsIQueryable(x => x.Id == item, null, "Client,Branch").First();
                    if (order.Status == OrderStatus.Placed
                    && order.DeliveryId == null && !order.IsDeleted
                    && !order.Pending && order.BranchId == user.BranchId && order.PreviousBranchId == BranchId && order.TransferredConfirmed == false)
                    {
                        order.TransferredConfirmed = true;
                        order.LastUpdated = DateTime.Now.ToUniversalTime();
                        if (await _orders.Update(order))
                        {
                            //نأكد التحويل في قائمة التحويلات
                            var trans = _TransferHistories.Get(x => x.OrderId == order.Id &&
                            x.FromBranchId == order.PreviousBranchId && x.ToBranchId == order.BranchId).OrderBy(a => a.Id).Last();

                            if (trans != null)
                            {
                                trans.AcceptTransfer_UserId = _userManger.GetUserId(User);
                                trans.AcceptTransferDate = DateTime.Now.ToUniversalTime();
                                await _TransferHistories.Update(trans);
                            }

                            if (order.OrderOperationHistoryId != null)
                            {
                                var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
                                history.AcceptTransfer_UserId = _userManger.GetUserId(User);
                                history.AcceptTransferDate = DateTime.Now.ToUniversalTime();
                                await _Histories.Update(history);
                                // await _CRUDHistory.Update(history.Id);
                            }
                        }
                    }
                }
                if (Orders.Count() > 0)
                {
                    await SendAcceptNotify(BranchId);
                }
                return RedirectToAction(nameof(AcceptSwitchOrders), new { id = BranchId });
            }

            foreach (var item in Orders)
            {
                var order = _orders.GetAllAsIQueryable(x => x.Id == item, null, "Client,Branch").First();
                if (order.Status == OrderStatus.Placed && order.DeliveryId == null && !order.IsDeleted
                     && !order.Pending && order.BranchId == BranchId && order.PreviousBranchId != BranchId && order.TransferredConfirmed == false)
                {
                    order.TransferredConfirmed = true;
                    order.LastUpdated = DateTime.Now.ToUniversalTime();
                    if (await _orders.Update(order))
                    {
                        //نأكد التحويل في قائمة التحويلات
                        var trans = _TransferHistories.Get(x => x.OrderId == order.Id &&
                        x.FromBranchId == order.PreviousBranchId && x.ToBranchId == order.BranchId).OrderBy(a => a.Id).Last();

                        if (trans != null)
                        {
                            trans.AcceptTransfer_UserId = _userManger.GetUserId(User);
                            trans.AcceptTransferDate = DateTime.Now.ToUniversalTime();
                            await _TransferHistories.Update(trans);
                        }

                        if (order.OrderOperationHistoryId != null)
                        {
                            var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
                            history.AcceptTransfer_UserId = _userManger.GetUserId(User);
                            history.AcceptTransferDate = DateTime.Now.ToUniversalTime();
                            await _Histories.Update(history);
                            // await _CRUDHistory.Update(history.Id);
                        }
                    }
                }
            }
            if (Orders.Count() > 0)
            {
                await SendAcceptNotify(BranchId);
            }
            return RedirectToAction(nameof(AcceptSwitchOrders), new { id = BranchId });


        }

        #endregion
        public class dto
        {
            public List<long> Orders { get; set; }
            public long BranchId { get; set; }
        }

        #region Switch Returned Orders
        [Route("Returned-Transferr")]
        [Authorize(Roles = "Admin,HighAdmin,Accountant,TrustAdmin")]
        public async Task<IActionResult> SwitchReturnedOrders(long id)
        {

            if (User.IsInRole("HighAdmin") || User.IsInRole("Accountant"))
            {
                var user = await _user.GetObj(x => x.Id == _userManger.GetUserId(User));

                ViewBag.BranchId = id;
                ViewBag.Branch = _branch.Get(x => x.Id == id).First();
                ViewBag.Orders = _orders.Get(x => !x.IsDeleted
                 && x.BranchId != x.Client.BranchId && x.TransferredConfirmed && id == x.Client.BranchId &&
                     (
                     (x.OrderCompleted == OrderCompleted.NOK && x.Finished && (x.Status == OrderStatus.Returned || x.Status == OrderStatus.PartialReturned))
                     ||
                     (x.ReturnedOrderCompleted == OrderCompleted.NOK && x.ReturnedFinished && (x.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender || x.Status == OrderStatus.Returned_And_Paid_DeliveryCost))
                     )
                     && x.BranchId == user.BranchId
                     && !x.PendingReturnTransferrConfirmed
                 ).ToList();


                ViewBag.TransferedOrdersUnConfirmed = _orderService.GetList(x => !x.IsDeleted
                  && x.BranchId != x.Client.BranchId && x.TransferredConfirmed && id == x.Client.BranchId &&
                      (
                      (x.OrderCompleted == OrderCompleted.NOK && x.Finished && (x.Status == OrderStatus.Returned || x.Status == OrderStatus.PartialReturned))
                      ||
                      (x.ReturnedOrderCompleted == OrderCompleted.NOK && x.ReturnedFinished && (x.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender || x.Status == OrderStatus.Returned_And_Paid_DeliveryCost))
                      )
                        && x.BranchId == user.BranchId
                  //&& x.PendingReturnTransferrConfirmed
                  ).ToList();

                return View();
            }
            ViewBag.BranchId = id;
            ViewBag.Branch = _branch.Get(x => x.Id == id).First();
            ViewBag.Orders = _orders.Get(x => !x.IsDeleted
            && x.BranchId != x.Client.BranchId && x.TransferredConfirmed && id == x.Client.BranchId &&
                (
                (x.OrderCompleted == OrderCompleted.NOK && x.Finished && (x.Status == OrderStatus.Returned || x.Status == OrderStatus.PartialReturned))
                ||
                (x.ReturnedOrderCompleted == OrderCompleted.NOK && x.ReturnedFinished && (x.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender || x.Status == OrderStatus.Returned_And_Paid_DeliveryCost))
                )
                && !x.PendingReturnTransferrConfirmed
            ).ToList();

            ViewBag.TransferedOrdersUnConfirmed = _orderService.GetList(x => !x.IsDeleted
            && x.BranchId != x.Client.BranchId && x.TransferredConfirmed && id == x.Client.BranchId &&
                (
                (x.OrderCompleted == OrderCompleted.NOK && x.Finished && (x.Status == OrderStatus.Returned || x.Status == OrderStatus.PartialReturned))
                ||
                (x.ReturnedOrderCompleted == OrderCompleted.NOK && x.ReturnedFinished && (x.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender || x.Status == OrderStatus.Returned_And_Paid_DeliveryCost))
                )
            //&& x.PendingReturnTransferrConfirmed
            ).ToList();

            return View();
        }
        [Route("Returned-Transferr")]
        [Authorize(Roles = "Admin,HighAdmin,Accountant,TrustAdmin")]
        [HttpPost]
        public async Task<IActionResult> SwitchReturnedOrders(dto model)
        {
            List<long> Orders = model.Orders;
            long BranchId = model.BranchId;
            if (User.IsInRole("HighAdmin") || User.IsInRole("Accountant"))
            {
                var user = await _user.GetObj(x => x.Id == _userManger.GetUserId(User));
                foreach (var item in Orders)
                {
                    var order = _orders.GetAllAsIQueryable(x => x.Id == item, null, "Client,Branch").First();
                    if (
                        order.BranchId != order.Client.BranchId && order.BranchId == user.BranchId && order.TransferredConfirmed && BranchId == order.Client.BranchId
                && (
                (order.OrderCompleted == OrderCompleted.NOK && order.Finished && (order.Status == OrderStatus.Returned || order.Status == OrderStatus.PartialReturned))
                ||
                (order.ReturnedOrderCompleted == OrderCompleted.NOK && order.ReturnedFinished && (order.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender || order.Status == OrderStatus.Returned_And_Paid_DeliveryCost))
                ) &&
                !order.IsDeleted && !order.PendingReturnTransferrConfirmed)
                    {
                        order.PendingReturnTransferrConfirmed = true;
                        order.LastUpdated = DateTime.Now.ToUniversalTime();
                        if (await _orders.Update(order))
                        {
                            if (order.OrderOperationHistoryId != null)
                            {
                                var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
                                history.TransferReturned_UserId = _userManger.GetUserId(User);
                                history.TransferReturnedDate = DateTime.Now.ToUniversalTime();
                                await _Histories.Update(history);
                                //await _CRUDHistory.Update(history.Id);
                            }
                        }
                    }
                }
                if (Orders.Count() > 0)
                {
                    await SendNotify(BranchId, true);
                }
                return RedirectToAction(nameof(SwitchReturnedOrders), new { id = BranchId });
            }

            foreach (var item in Orders)
            {
                var order = _orders.GetAllAsIQueryable(x => x.Id == item, null, "Client,Branch").First();
                if (
                order.BranchId != order.Client.BranchId && order.TransferredConfirmed && BranchId == order.Client.BranchId
                && (
                (order.OrderCompleted == OrderCompleted.NOK && order.Finished && (order.Status == OrderStatus.Returned || order.Status == OrderStatus.PartialReturned))
                ||
                (order.ReturnedOrderCompleted == OrderCompleted.NOK && order.ReturnedFinished && (order.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender || order.Status == OrderStatus.Returned_And_Paid_DeliveryCost))
                ) &&
                !order.IsDeleted && !order.PendingReturnTransferrConfirmed)
                {
                    order.PendingReturnTransferrConfirmed = true;
                    order.LastUpdated = DateTime.Now.ToUniversalTime();
                    if (await _orders.Update(order))
                    {
                        if (order.OrderOperationHistoryId != null)
                        {
                            var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
                            history.TransferReturned_UserId = _userManger.GetUserId(User);
                            history.TransferReturnedDate = DateTime.Now.ToUniversalTime();
                            await _Histories.Update(history);
                            //await _CRUDHistory.Update(history.Id);
                        }
                    }
                }
            }
            if (Orders.Count() > 0)
            {
                await SendNotify(BranchId, true);
            }
            return RedirectToAction(nameof(SwitchReturnedOrders), new { id = BranchId });


        }
        [HttpGet]
        public async Task<IActionResult> CancelReturnedTransfer(long id, long BranchId)
        {
            var order = _orders.GetAllAsIQueryable(x => x.Id == id, null, "Client,Branch").First();
            if (order != null)
            {
                if (User.IsInRole("HighAdmin") || User.IsInRole("Accountant"))
                {
                    var user = await _user.GetObj(x => x.Id == _userManger.GetUserId(User));

                    if (
                 order.BranchId != order.Client.BranchId && order.BranchId == user.BranchId && order.TransferredConfirmed && BranchId == order.Client.BranchId
                && (
                (order.OrderCompleted == OrderCompleted.NOK && order.Finished && (order.Status == OrderStatus.Returned || order.Status == OrderStatus.PartialReturned))
                ||
                (order.ReturnedOrderCompleted == OrderCompleted.NOK && order.ReturnedFinished && (order.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender || order.Status == OrderStatus.Returned_And_Paid_DeliveryCost))
                ) &&
                !order.IsDeleted && order.PendingReturnTransferrConfirmed)
                    {
                        order.PendingReturnTransferrConfirmed = false;
                        order.LastUpdated = DateTime.Now.ToUniversalTime();
                        await _orders.Update(order);
                        await SendCancelNotify(BranchId, order.Code, true);
                    }
                    return RedirectToAction(nameof(SwitchReturnedOrders), new { id = BranchId });
                }
                else
                {
                    //var order = _orders.GetAllAsIQueryable(x => x.Id == id, null, "Client,Branch").First();
                    if (order.BranchId != order.Client.BranchId && order.TransferredConfirmed && BranchId == order.Client.BranchId
                 && (
                (order.OrderCompleted == OrderCompleted.NOK && order.Finished && (order.Status == OrderStatus.Returned || order.Status == OrderStatus.PartialReturned))
                ||
                (order.ReturnedOrderCompleted == OrderCompleted.NOK && order.ReturnedFinished && (order.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender || order.Status == OrderStatus.Returned_And_Paid_DeliveryCost))
                ) &&
                 !order.IsDeleted && order.PendingReturnTransferrConfirmed)
                    {
                        order.PendingReturnTransferrConfirmed = false;
                        order.LastUpdated = DateTime.Now.ToUniversalTime();
                        await _orders.Update(order);
                        await SendCancelNotify(BranchId, order.Code, true);
                    }
                }
            }
            return RedirectToAction(nameof(SwitchReturnedOrders), new { id = BranchId });

        }
        [Route("AcceptReturned-Transferr")]
        [Authorize(Roles = "Admin,HighAdmin,Accountant,TrustAdmin")]
        public async Task<IActionResult> AcceptSwitchReturnedOrders(long id)
        {

            ViewBag.BranchId = id;
            ViewBag.Branch = _branch.Get(x => x.Id == id).First();
            if (User.IsInRole("HighAdmin") || User.IsInRole("Accountant"))
            {
                var user = await _user.GetObj(x => x.Id == _userManger.GetUserId(User));

                ViewBag.Orders = _orderService.GetList(x => !x.IsDeleted
                && x.BranchId != x.Client.BranchId && x.TransferredConfirmed && id == x.BranchId &&
                 (
                 (x.OrderCompleted == OrderCompleted.NOK && x.Finished && (x.Status == OrderStatus.Returned || x.Status == OrderStatus.PartialReturned))
                 ||
                 (x.ReturnedOrderCompleted == OrderCompleted.NOK && x.ReturnedFinished && (x.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender || x.Status == OrderStatus.Returned_And_Paid_DeliveryCost))
                 )
                && x.Client.BranchId == user.BranchId
                && x.PendingReturnTransferrConfirmed
                ).ToList();
                return View();
            }
            ViewBag.Orders = _orderService.GetList(x => !x.IsDeleted
            && x.BranchId != x.Client.BranchId && x.TransferredConfirmed && id == x.Client.BranchId &&
                (
                 (x.OrderCompleted == OrderCompleted.NOK && x.Finished && (x.Status == OrderStatus.Returned || x.Status == OrderStatus.PartialReturned))
                 ||
                 (x.ReturnedOrderCompleted == OrderCompleted.NOK && x.ReturnedFinished && (x.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender || x.Status == OrderStatus.Returned_And_Paid_DeliveryCost))
                 )
                && x.PendingReturnTransferrConfirmed
            ).ToList();

            return View();
        }
        [Route("AcceptReturned-Transferr")]
        [Authorize(Roles = "Admin,HighAdmin,Accountant,TrustAdmin")]
        [HttpPost]
        public async Task<IActionResult> AcceptSwitchReturnedOrders(dto model)
        {
            List<long> Orders = model.Orders;
            long BranchId = model.BranchId;
            if (User.IsInRole("HighAdmin") || User.IsInRole("Accountant"))
            {
                var user = await _user.GetObj(x => x.Id == _userManger.GetUserId(User));
                foreach (var item in Orders)
                {
                    var order = _orders.GetAllAsIQueryable(x => x.Id == item, null, "Client,Branch").First();
                    if (order.BranchId != order.Client.BranchId && order.BranchId == BranchId && order.TransferredConfirmed
                    && (
                        (order.OrderCompleted == OrderCompleted.NOK && order.Finished && (order.Status == OrderStatus.Returned || order.Status == OrderStatus.PartialReturned))
                        ||
                        (order.ReturnedOrderCompleted == OrderCompleted.NOK && order.ReturnedFinished && (order.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender || order.Status == OrderStatus.Returned_And_Paid_DeliveryCost))
                        ) &&
                    order.Client.BranchId == user.BranchId
                    && !order.IsDeleted && order.PendingReturnTransferrConfirmed)
                    {
                        order.TransferredConfirmed = false;
                        order.BranchId = user.BranchId;
                        order.LastUpdated = DateTime.Now.ToUniversalTime();
                        if (await _orders.Update(order))
                        {
                            if (order.OrderOperationHistoryId != null)
                            {
                                var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
                                history.AcceptTransferReturned_UserId = _userManger.GetUserId(User);
                                history.AcceptTransferReturnedDate = DateTime.Now.ToUniversalTime();
                                await _Histories.Update(history);
                                // await _CRUDHistory.Update(history.Id);
                            }
                        }
                    }
                }
                if (Orders.Count() > 0)
                {
                    await SendAcceptNotify(BranchId, true);
                }
                return RedirectToAction(nameof(AcceptSwitchReturnedOrders), new { id = BranchId });
            }

            foreach (var item in Orders)
            {
                var order = _orders.GetAllAsIQueryable(x => x.Id == item, null, "Client,Branch").First();
                if (order.BranchId != order.Client.BranchId && order.TransferredConfirmed && BranchId == order.Client.BranchId
                 && (
                (order.OrderCompleted == OrderCompleted.NOK && order.Finished && (order.Status == OrderStatus.Returned || order.Status == OrderStatus.PartialReturned))
                ||
                (order.ReturnedOrderCompleted == OrderCompleted.NOK && order.ReturnedFinished && (order.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender || order.Status == OrderStatus.Returned_And_Paid_DeliveryCost))
                ) &&
                 !order.IsDeleted && order.PendingReturnTransferrConfirmed)
                {
                    order.TransferredConfirmed = false;
                    order.BranchId = BranchId;
                    order.LastUpdated = DateTime.Now.ToUniversalTime();
                    if (await _orders.Update(order))
                    {
                        if (order.OrderOperationHistoryId != null)
                        {
                            var history = await _Histories.GetObj(x => x.Id == order.OrderOperationHistoryId);
                            history.AcceptTransferReturned_UserId = _userManger.GetUserId(User);
                            history.AcceptTransferReturnedDate = DateTime.Now.ToUniversalTime();
                            await _Histories.Update(history);
                            // await _CRUDHistory.Update(history.Id);
                        }
                    }
                }
            }
            if (Orders.Count() > 0)
            {
                await SendAcceptNotify(BranchId, true);
            }
            return RedirectToAction(nameof(AcceptSwitchReturnedOrders), new { id = BranchId });

        }
        #endregion 

        #region notifivcations
        private async Task<bool> SendNotify(long BranchId, bool returned = false)
        {

            var BranchAdmins = _user.Get(x => x.UserType == UserType.HighAdmin
                 && x.BranchId == BranchId && !x.IsDeleted && !x.Branch.IsDeleted).ToList();

            var Title = $"طلبات جديده محولة للفرع";
            var Body = $"طلبات محوله في الطريق الي الفرع , تم تحويل طلبات جديده من فرع الي الفرع لديك , يرجي مراجعتها عند الوصول وتأكيد استلامها .";
            if (returned)
            {
                Title = $"مرتجعات جديده محولة للفرع";
                Body = $"مرتجعات محوله في الطريق الي الفرع , تم تحويل مرتجعات جديده من فرع الي الفرع لديك , يرجي مراجعتها عند الوصول وتأكيد استلامها .";
            }
            var send = new SendNotification(_pushNotification, _notification);
            foreach (var admin in BranchAdmins)
            {
                await send.SendToAllSpecificAndroidUserDevices(admin.Id, Title, Body);
            }
            return true;
        }
        private async Task<bool> SendCancelNotify(long BranchId, string Code, bool returned = false)
        {
            var BranchAdmins = _user.Get(x => x.UserType == UserType.HighAdmin
                 && x.BranchId == BranchId && !x.IsDeleted && !x.Branch.IsDeleted).ToList();

            var Title = $"إلغاء تحويل طلب للفرع";
            var Body = $"تم إلغاء تحويل الطلب {Code} الي الفرع لديك";
            if (returned)
            {
                Title = $"إلغاء تحويل مرتجع للفرع";
                Body = $"تم إلغاء تحويل المرتجع {Code} الي الفرع لديك";
            }
            var send = new SendNotification(_pushNotification, _notification);
            foreach (var admin in BranchAdmins)
            {
                await send.SendToAllSpecificAndroidUserDevices(admin.Id, Title, Body);
            }
            return true;
        }
        private async Task<bool> SendAcceptNotify(long BranchId, bool returned = false)
        {

            var BranchAdmins = _user.Get(x => x.UserType == UserType.HighAdmin
                 && x.BranchId == BranchId && !x.IsDeleted && !x.Branch.IsDeleted).ToList();

            var Title = $"تأكيد إستلام طلبات محولة";
            var Body = $"تم تأكيد إستلام الطلبات المحوله من الفرع ";
            if (returned)
            {
                Title = $"تأكيد إستلام مرتجعات محولة";
                Body = $"تم تأكيد إستلام المرتجعات المحوله من فرعنا ";
            }
            var send = new SendNotification(_pushNotification, _notification);
            foreach (var admin in BranchAdmins)
            {
                await send.SendToAllSpecificAndroidUserDevices(admin.Id, Title, Body);
            }
            return true;
        }

        #endregion
    }
}
