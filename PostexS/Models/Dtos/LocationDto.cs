using PostexS.Models.Domain;
using System;

namespace PostexS.Models.Dtos
{
    public class LocationDto
    {
        public long Id { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string Address { get; set; }
        public DateTime CreateOn { get; set; }

        public LocationDto() { }

        public LocationDto(Location location)
        {
            Id = location.Id;
            Latitude = location.Latitude;
            Longitude = location.Longitude;
            Address = location.Address;
            CreateOn = location.CreateOn;
        }
    }
}
