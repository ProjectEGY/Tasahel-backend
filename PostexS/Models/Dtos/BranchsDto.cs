using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.Dtos
{
    public class BranchsDto
    {
        public long  ID { get; set; }
        public string Name { get; set; }
        public string Whatsapp { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
