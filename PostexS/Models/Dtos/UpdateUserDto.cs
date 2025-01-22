using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.Dtos
{
    public class UpdateUserDto
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string WhatsappPhone { get; set; }
        public string Phone { get; set; }
    }
    public class UpdateUserLocation
    {
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
