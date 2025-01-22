using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.Domain
{
    public class ContactUs:BaseModel
    {
        public string FaceBook { get; set; }
        public string Instgram { get; set; }
        public string WhatsApp { get; set; }
        public string Twitter { get; set; }
    }
}
