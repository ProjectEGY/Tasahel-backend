using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.Dtos
{
    public class ContactUsDto
    {
        public List<BranchsDto> Branchs { get; set; } = new List<BranchsDto>();
        public string FaceBook { get; set; }
        public string Instgram { get; set; }
        public string Twitter { get; set; }
    }
}
