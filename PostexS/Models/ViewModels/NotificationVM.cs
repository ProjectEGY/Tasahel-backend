using PostexS.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PostexS.Models.ViewModels
{
    public class NotificationVM
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public bool IsSeen { get; set; }
    }
}