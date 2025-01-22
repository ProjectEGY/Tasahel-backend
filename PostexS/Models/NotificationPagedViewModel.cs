using PostexS.Models.Domain;
using System;
using System.Collections.Generic;

namespace PostexS.Models
{
    public class NotificationPagedViewModel
    {
        public List<Notification> Notifications { get; set; }
        public int PageNumber { get; set; }
        public int PageCount { get; set; }
    }
    public class CaptainWithOrderCountViewModel
    {
        public string CaptainId { get; set; }
        public long OrdersCount { get; set; }
    }


}
