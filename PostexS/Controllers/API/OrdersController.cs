using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json.Linq;
using PostexS.Helper;
using PostexS.Interfaces;
using PostexS.Models.Domain;
using PostexS.Models.Dtos;
using PostexS.Models.Enums;
using ZXing;
using ZXing.Common;
using Location = PostexS.Models.Domain.Location;

namespace PostexS.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IGeneric<Order> _order;
        private readonly IGeneric<OrderOperationHistory> _Histories;
        private readonly IGeneric<ApplicationUser> _user;
        private readonly IGeneric<OrderNotes> _orderNotes;
        private readonly IGeneric<Notification> _notification;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private IGeneric<DeviceTokens> _pushNotification;
        private readonly BaseResponse baseResponse;
        private readonly ICRUD<Order> _CRUD;
        private readonly IGeneric<Location> _locations;

        public OrdersController(IGeneric<Notification> notification, IGeneric<DeviceTokens> pushNotification, IGeneric<Order> order, IGeneric<ApplicationUser> user,
            IGeneric<OrderNotes> orderNotes, IGeneric<Location> locations, IWebHostEnvironment webHostEnvironment, ICRUD<Order> CRUD, IGeneric<OrderOperationHistory> histories)
        {
            _user = user;
            _order = order;
            _orderNotes = orderNotes;
            baseResponse = new BaseResponse();
            _locations = locations;
            _notification = notification;
            _pushNotification = pushNotification;
            _CRUD = CRUD;
            _Histories = histories;
            _webHostEnvironment = webHostEnvironment;
        }
        [HttpPut("Finshed")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Finshed([FromHeader(Name = "Latitude")] double? latitude, [FromHeader(Name = "Longitude")] double? longitude, DriverSubmitOrderDto model)
        {

            var userid = User.Identity.Name;
            var UserData = _user.Get(x => x.Id == userid).First();
            if (!await _order.IsExist(x => x.Id == model.OrderId && !x.IsDeleted))
            {
                baseResponse.ErrorCode = Errors.TheOrderNotExistOrDeleted;
                return StatusCode((int)HttpStatusCode.NotFound, baseResponse);
            }
            if (!await _order.IsExist(x => x.Id == model.OrderId && x.DeliveryId == userid))
            {
                baseResponse.ErrorCode = Errors.ThisOrderAssignedToAnotherAgent;
                return StatusCode((int)HttpStatusCode.NotFound, baseResponse);
            }
            var order = (await _order.GetObj(x => x.Id == model.OrderId));
            //if (model.Image == null
            //    && (order.Status == OrderStatus.Returned
            //    || order.Status == OrderStatus.PartialDelivered
            //    || order.Status == OrderStatus.Returned_And_DeliveryCost_On_Sender
            //    || order.Status == OrderStatus.Returned_And_Paid_DeliveryCost
            //    || order.Status == OrderStatus.Delivered_With_Edit_Price
            //    ))
            //{
            //    baseResponse.ErrorCode = Errors.ReturnedImageIsRequired;
            //    return StatusCode((int)HttpStatusCode.NotFound, baseResponse);
            //}
            string Returned_Image = "";
            if (model.Image != null)
            {
                var Image = Convert.FromBase64String(model.Image);
                Returned_Image = MediaControl.Upload(FilePath.OrderReturns, Image, MediaType.Image, _webHostEnvironment);
            }

            if (order.DeliveryId == userid &&
                             (order.Status == OrderStatus.Assigned || order.Status == OrderStatus.Waiting)
                             && !order.IsDeleted)
            {

                //عشان نشيلها من اي تقفيله قبل كده
                order.WalletId = null;
                ///
                var user = await _user.GetSingle(x => x.Id == order.DeliveryId);
                switch (model.Status)
                {
                    case OrderStatus.Delivered:
                        order.ArrivedCost = order.TotalCost;
                        order.DeliveryCost = user.DeliveryCost != null ? user.DeliveryCost.Value : 0;
                        order.Status = model.Status;
                        await _orderNotes.Add(new OrderNotes()
                        {
                            Content = model.Note,
                            OrderId = order.Id,
                            UserId = userid
                        });
                        break;
                    case OrderStatus.Delivered_With_Edit_Price:
                        order.ArrivedCost = model.Paid;
                        order.DeliveryCost = user.DeliveryCost != null ? user.DeliveryCost.Value : 0;
                        order.Status = model.Status;
                        await _orderNotes.Add(new OrderNotes()
                        {
                            Content = model.Note,
                            OrderId = order.Id,
                            UserId = userid
                        });
                        break;
                    case OrderStatus.Returned_And_DeliveryCost_On_Sender:
                    case OrderStatus.Returned_And_Paid_DeliveryCost when model.Paid == 0:
                        order.ArrivedCost = 0;
                        order.DeliveryCost = user.DeliveryCost != null ? user.DeliveryCost.Value : 0;
                        order.Status = model.Status;
                        order.ReturnedCost = order.Cost;
                        await _orderNotes.Add(new OrderNotes()
                        {
                            Content = model.Note,
                            OrderId = order.Id,
                            UserId = userid
                        });
                        break;
                    case OrderStatus.Returned_And_Paid_DeliveryCost:
                        order.ArrivedCost = model.Paid;
                        order.DeliveryCost = user.DeliveryCost != null ? user.DeliveryCost.Value : 0;
                        order.Status = model.Status;
                        order.ReturnedCost = order.Cost;
                        await _orderNotes.Add(new OrderNotes()
                        {
                            Content = model.Note,
                            OrderId = order.Id,
                            UserId = userid
                        });
                        break;
                    case OrderStatus.PartialDelivered:
                        order.Status = model.Status;
                        order.DeliveryCost = user.DeliveryCost != null ? user.DeliveryCost.Value : 0;
                        order.ArrivedCost = model.Paid;
                        await _orderNotes.Add(new OrderNotes()
                        {
                            Content = model.Note,
                            OrderId = order.Id,
                            UserId = userid
                        });
                        Order PartialReturned;
                        //هنعمل طلب عكسي مرتجع جزئي بنفس الكود + R
                        if (!await _order.IsExist(x => x.Code == 'R' + order.Code && !x.IsDeleted))
                        {
                            PartialReturned = new Order()
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
                                PendingReturnTransferrConfirmed = order.PendingReturnTransferrConfirmed,
                                ArrivedCost = order.ArrivedCost,
                                DeliveryCost = 0,
                                ReturnedCost = order.TotalCost - order.ArrivedCost,
                                Finished = order.Finished,
                                Status = OrderStatus.PartialReturned,
                                Returned_Image = Returned_Image,
                                OrderCompleted = order.OrderCompleted,
                                ClientId = order.ClientId,
                                LastUpdated = DateTime.Now.ToUniversalTime(),
                                WalletId = order.WalletId,
                                DeliveryId = order.DeliveryId,
                                BranchId = order.BranchId,
                                PreviousBranchId = order.PreviousBranchId,
                                OrderOperationHistoryId = order.OrderOperationHistoryId,
                                ReturnedFinished = order.ReturnedFinished,
                                ReturnedOrderCompleted = order.ReturnedOrderCompleted,
                                ReturnedWalletId = order.ReturnedWalletId,
                                ReturnedCompletedId = order.ReturnedCompletedId,
                            };
                            await _order.Add(PartialReturned);
                        }
                        else
                        {
                            PartialReturned = (await _order.GetObj(x => x.Code == 'R' + order.Code));
                            PartialReturned.IsDeleted = false;
                            PartialReturned.ArrivedCost = order.ArrivedCost;
                            PartialReturned.ReturnedCost = order.TotalCost - order.ArrivedCost;
                            await _order.Update(PartialReturned);
                        }
                        await _orderNotes.Add(new OrderNotes()
                        {
                            Content = model.Note,
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
                            return BadRequest("من فضلك حاول لاحقاً");
                        }
                        PartialReturned.OrderOperationHistoryId = history.Id;
                        if (!await _order.Update(PartialReturned))
                        {
                            return BadRequest("من فضل حاول في وقتاً أخر");
                        }

                        break;
                    case OrderStatus.Returned:
                        order.ArrivedCost = 0;
                        order.DeliveryCost = 0;
                        order.ReturnedCost = order.TotalCost /*- order.ArrivedCost*/;
                        order.Status = model.Status;
                        await _orderNotes.Add(new OrderNotes()
                        {
                            Content = model.Note,
                            OrderId = order.Id,
                            UserId = userid
                        });

                        break;
                    default:
                        order.ArrivedCost = 0;
                        order.DeliveryCost = 0;
                        order.ReturnedCost = order.TotalCost;
                        order.Status = model.Status;
                        await _orderNotes.Add(new OrderNotes()
                        {
                            Content = model.Note,
                            OrderId = order.Id,
                            UserId = userid
                        });

                        break;
                }
                order.Returned_Image = Returned_Image == "" ? null : Returned_Image;
                await SendNotify(order, user, model.Note, model.Image);
                await _order.Update(order);
            }
            if (latitude.HasValue && longitude.HasValue)
            {
                UpdateUserLocation locationdto = new UpdateUserLocation()
                {
                    Longitude = longitude.Value,
                    Latitude = latitude.Value,
                };
                await UpdateLocationMethod(locationdto, UserData);
            }
            return Ok(baseResponse);
        }
        private async Task<bool> SendNotify(Order order, ApplicationUser Captian, string note, string image = null)
        {
            var Title = $"تحديث حاله الطلب :  {order.Code} ";
            var Body = $"";
            switch (order.Status)
            {
                case OrderStatus.Delivered:
                    Body = $"تم تسليم طلبكم بنجاح. كود الطلب {order.Code}.";
                    break;
                case OrderStatus.Delivered_With_Edit_Price:
                    Body = $"تم تسليم طلبكم مع تعديل السعر. كود الطلب {order.Code}.";
                    break;
                case OrderStatus.Returned_And_DeliveryCost_On_Sender:
                    Body = $"تم إرجاع طلبكم وتكلفة التوصيل على الراسل . كود الطلب {order.Code}.";
                    break;
                case OrderStatus.Returned_And_Paid_DeliveryCost:
                    Body = $"تم إرجاع طلبكم بالكامل وتم دفع تكلفة التوصيل. كود الطلب {order.Code}.";
                    break;
                case OrderStatus.PartialDelivered:
                    Body = $"تم تسليم جزء من طلبكم. كود الطلب {order.Code}.";
                    break;
                case OrderStatus.Returned:
                    Body = $"تم إرجاع طلبكم بالكامل . كود الطلب {order.Code}.";
                    break;
                case OrderStatus.Waiting:
                    Body = $"طلبكم في حالة تأجيل . كود الطلب {order.Code}.";
                    break;
                default:
                    break;
            }
            Body += $"\n  المندوب :{Captian.Name} ," +
                $"\n رقم الهاتف : {Captian.PhoneNumber}," +
                $"\n ملاحظات المندوب : {note} .";

            var send = new SendNotification(_pushNotification, _notification);
            await send.SendToAllSpecificAndroidUserDevices(order.ClientId, Title, Body, Image: order.Returned_Image);

            return true;
        }

        [HttpPost("NewOrder")]
        [AllowAnonymous]
        public async Task<IActionResult> NewOrder(string PublicKey, string PrivateKey, OrderVM model)
        {
            if (PublicKey == null)
            {
                baseResponse.ErrorMessage = "Public Key Is Required";
                baseResponse.ErrorCode = Errors.PublicKeyIsRequired;
                return StatusCode((int)HttpStatusCode.BadRequest, baseResponse);
            }
            if (PrivateKey == null)
            {
                baseResponse.ErrorMessage = "Private Key Is Required";
                baseResponse.ErrorCode = Errors.PrivateKeyIsRequired;
                return StatusCode((int)HttpStatusCode.BadRequest, baseResponse);
            }
            var user = await _user.GetObj(x => x.PublicKey == PublicKey && x.PrivateKey == PrivateKey);
            if (user == null)
            {
                baseResponse.ErrorMessage = "Private Key is Wrong Or Public Key Is Wrong";
                baseResponse.ErrorCode = Errors.PrivateKeyIsWrongOrPublicKeyIsWrong;
                return StatusCode((int)HttpStatusCode.BadRequest, baseResponse);
            }
            string GeneralNote = user.OrdersGeneralNote != null ? user.OrdersGeneralNote + " - " : "";
            Order order = new Order()
            {
                Address = model.Address,
                AddressCity = model.ClientCity,
                ClientName = model.ClientName,
                ClientCode = model.ClientCode,
                ClientPhone = model.ClientPhone,
                Cost = model.Cost,
                DeliveryFees = model.DeliveryFees,
                Notes = GeneralNote + model.Notes,
                ClientId = user.Id,
                Pending = true,
                TotalCost = model.Cost + model.DeliveryFees,
                Status = OrderStatus.Placed,
                BranchId = user.BranchId,
            };
            if (!await _order.Add(order))
            {
                baseResponse.ErrorMessage = "Something went wrong, please try again later";
                baseResponse.ErrorCode = Errors.SomeThingWentwrong;
            }
            OrderOperationHistory history = new OrderOperationHistory()
            {
                OrderId = order.Id,
                Create_UserId = user.Id,
                CreateDate = order.CreateOn,
            };
            if (!await _Histories.Add(history))
            {
                baseResponse.ErrorMessage = "Something went wrong, please try again later";
                baseResponse.ErrorCode = Errors.SomeThingWentwrong;
            }
            order.OrderOperationHistoryId = history.Id;

            //string datetoday = DateTime.Now.ToString("ddMMyyyy");
            order.Code = "Tas" + /*datetoday +*/ order.Id.ToString();
            order.BarcodeImage = getBarcode(order.Code);

            if (!await _order.Update(order))
            {
                baseResponse.ErrorMessage = "Something went wrong, please try again later";
                baseResponse.ErrorCode = Errors.SomeThingWentwrong;
            }

            await _CRUD.Update(order.Id);
            //var Client = await _user.GetObj(x => x.Id == order.ClientId);
            //SendEmail email = new SendEmail();
            //await email.Send(Client.Name, (order.Id + 1000).ToString());
            baseResponse.Data = new OrderDto(order);

            return Ok(baseResponse);
        }
        public byte[] getBarcode(string Code)
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
            var barcodeBitmap = barcodeWriter.Write(Code);
            var ms = new MemoryStream();
            barcodeBitmap.Save(ms, ImageFormat.Png);
            var barcodeImage = ms.ToArray();
            return barcodeImage;
        }

        private async Task<bool> UpdateLocationMethod(UpdateUserLocation dto, ApplicationUser user)
        {
            if (user.Tracking)
            {
                if (dto.Longitude.HasValue)
                    user.Longitude = dto.Longitude;

                if (dto.Latitude.HasValue)
                    user.Latitude = dto.Latitude;
                var address = await GetAddressFromCoordinatesAsync(dto.Latitude.Value, dto.Longitude.Value);

                await _user.Update(user);

                //add to locations list 
                // Define the Egypt time zone
                TimeZoneInfo egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");

                // Get the current UTC time
                DateTime utcNow = DateTime.UtcNow;

                // Convert the UTC time to Egypt time
                DateTime egyptTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, egyptTimeZone);

                Location location = new Location()
                {
                    DeliveryId = user.Id,
                    Longitude = dto.Longitude,
                    Latitude = dto.Latitude,
                    CreateOn = egyptTime,
                    Address = address,
                };
                await _locations.Add(location);
            }
            return true;
        }
        private async Task<string> GetAddressFromCoordinatesAsync(double latitude, double longitude)
        {
            string apiKey = "AIzaSyDR45xVCCyl8qLGg7tnQZcsAm4DGrhypFY";  // Replace with your actual API key
            string language = "ar";  // Arabic language code

            string url = $"https://maps.googleapis.com/maps/api/geocode/json?latlng={latitude},{longitude}&key={apiKey}&language={language}";

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(responseBody);

                // Check if the status returned is OK
                string status = json["status"]?.ToString();
                if (status != "OK")
                {
                    return "Address not found";
                }

                // Retrieve the formatted address in Arabic if available
                var results = json["results"];
                if (results == null || results.Count() == 0)
                {
                    return "Address not found";
                }


                // If no detailed address found, return the first formatted_address
                string fallbackAddress = results[1]["formatted_address"]?.ToString();
                return fallbackAddress ?? "Address not found";
            }
        }

    }
}
