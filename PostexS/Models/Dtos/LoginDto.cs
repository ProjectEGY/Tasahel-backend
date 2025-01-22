using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PostexS.Models.Domain;
using PostexS.Models.Enums;

namespace PostexS.Models.Dtos
{
    public class LoginDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool Tracking { get; set; }
        public UserType UserType { get; set; }
        public string WhatsappPhone { get; set; }
        public long BranchId { get; set; }
        public string Token { get; set; }
        public string Phone { get; set; }
        public LoginDto(ApplicationUser model)
        {
            this.Id = model.Id;
            this.UserName = model.Name;
            this.UserType = model.UserType;
            this.BranchId = model.BranchId;
            this.WhatsappPhone = model.WhatsappPhone;
            this.Email = model.Email;
            this.Phone = model.PhoneNumber;
            this.Longitude = model.Longitude;
            this.Latitude = model.Latitude;
            this.Tracking = model.Tracking;
        }
    }

}
