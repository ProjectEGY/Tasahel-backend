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

            var client = _httpClientFactory.CreateClient();
            using var response = await client.GetAsync(url);
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
