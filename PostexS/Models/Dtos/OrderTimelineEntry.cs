using System;

namespace PostexS.Models.Dtos
{
    public class OrderTimelineEntry
    {
        public string Action { get; set; }
        public string ActionArabic { get; set; }
        public DateTime Date { get; set; }
        public string PerformedBy { get; set; }
    }
}
