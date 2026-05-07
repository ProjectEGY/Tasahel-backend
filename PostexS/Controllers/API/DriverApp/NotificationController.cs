using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json.Linq;
using PostexS.Interfaces;
using PostexS.Models.Domain;
using PostexS.Models.Dtos;
using PostexS.Models.Enums;
using PostexS.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Location = PostexS.Models.Domain.Location;

namespace PostexS.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {

        private readonly IGeneric<Notification> _notification;
        private readonly IGeneric<DeviceTokens> _pushNotification;
        private readonly IGeneric<ApplicationUser> _user;
        private readonly IGeneric<Location> _locations;
        private readonly IHttpClientFactory _httpClientFactory;
        private BaseResponse baseResponse;
        public NotificationController(IGeneric<Notification> notification, IGeneric<ApplicationUser> user, IGeneric<Location> locations, IGeneric<DeviceTokens> pushNotification, IHttpClientFactory httpClientFactory)
        {
            _notification = notification;
            _pushNotification = pushNotification;
            _locations = locations;
            _user = user;
            _httpClientFactory = httpClientFactory;
            baseResponse = new BaseResponse();
        }
        [HttpPost("PushToken")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> PushToken([FromHeader(Name = "Latitude")] double? latitude, [FromHeader(Name = "Longitude")] double? longitude, string PushToken)
        {
            var UserId = User.Identity.Name;
            var user = _user.Get(x => x.Id == UserId).First();
            if (string.IsNullOrEmpty(PushToken))
            {
                baseResponse.ErrorCode = Errors.TheModelIsInvalid;
                return StatusCode((int)HttpStatusCode.BadRequest, baseResponse);
            }
            if (await _pushNotification.IsExist(x => x.Token == PushToken && !x.IsDeleted))
            {
                var token = _pushNotification.Get(x => x.Token == PushToken && !x.IsDeleted).First();
                token.IsDeleted = true;
                await _pushNotification.Update(token);
            }
            await _pushNotification.Add(new DeviceTokens { Token = PushToken, UserId = UserId });
            if (latitude.HasValue && longitude.HasValue)
            {
                UpdateUserLocation locationdto = new UpdateUserLocation()
                {
                    Longitude = longitude.Value,
                    Latitude = latitude.Value,
                };
                await UpdateLocationMethod(locationdto, user);
            }
            return Ok(baseResponse);
        }
        [HttpGet("UnSeenNotificationCount")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> UnSeenNotificationCount([FromHeader(Name = "Latitude")] double? latitude, [FromHeader(Name = "Longitude")] double? longitude)
        {
            var UserId = User.Identity.Name;
            var user = _user.Get(x => x.Id == UserId).First();
            var Notifiaction = _notification.Get(x => !x.IsDeleted && x.UserId == UserId && !x.IsSeen).ToList().Count;
            baseResponse.Data = new
            {
                Count = Notifiaction
            };
            if (latitude.HasValue && longitude.HasValue)
            {
                UpdateUserLocation locationdto = new UpdateUserLocation()
                {
                    Longitude = longitude.Value,
                    Latitude = latitude.Value,
                };
                await UpdateLocationMethod(locationdto, user);
            }
            return Ok(baseResponse);
        }
        [HttpGet("Notification")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Notification([FromHeader(Name = "Latitude")] double? latitude, [FromHeader(Name = "Longitude")] double? longitude)
        {
            var UserId = User.Identity.Name;
            var user = _user.Get(x => x.Id == UserId).First();
            var dto = new List<NotificationVM>();
            var Notifiaction = _notification.Get(x => !x.IsDeleted && x.UserId == UserId).OrderByDescending(x => x.Id).ToList();
            foreach (var item in Notifiaction)
            {
                dto.Add(new NotificationVM()
                {
                    Body = item.Body,
                    IsSeen = item.IsSeen,
                    Id = item.Id,
                    Title = item.Title
                });
                var seen = _notification.Get(x => x.Id == item.Id).First();
                seen.IsSeen = true;
                await _notification.Update(seen);
            }
            if (latitude.HasValue && longitude.HasValue)
            {
                UpdateUserLocation locationdto = new UpdateUserLocation()
                {
                    Longitude = longitude.Value,
                    Latitude = latitude.Value,
                };
                await UpdateLocationMethod(locationdto, user);
            }
            baseResponse.Data = dto;
            return Ok(baseResponse);
        }


        private static readonly TimeSpan LocationSnapshotInterval = TimeSpan.FromMinutes(5);

        private async Task<bool> UpdateLocationMethod(UpdateUserLocation dto, ApplicationUser user)
        {
            if (!user.Tracking) return true;
            if (!dto.Longitude.HasValue || !dto.Latitude.HasValue) return true;

            TimeZoneInfo egyptTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            DateTime egyptNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, egyptTimeZone);

            var lastSnapshotAt = _locations.GetAllAsIQueryable(
                    x => !x.IsDeleted && x.DeliveryId == user.Id,
                    orderby: q => q.OrderByDescending(x => x.CreateOn),
                    asNoTracking: true)
                .Select(x => (DateTime?)x.CreateOn)
                .FirstOrDefault();

            if (lastSnapshotAt.HasValue && (egyptNow - lastSnapshotAt.Value) < LocationSnapshotInterval)
            {
                return true;
            }

            user.Longitude = dto.Longitude;
            user.Latitude = dto.Latitude;

            await _user.Update(user);

            Location location = new Location()
            {
                DeliveryId = user.Id,
                Longitude = dto.Longitude,
                Latitude = dto.Latitude,
                CreateOn = egyptNow,
            };
            await _locations.Add(location);

            return true;
        }

    }
}
