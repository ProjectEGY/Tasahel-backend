using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.Dtos
{
    public class StatisticsDto
    {
        public string Name { get; set; }
        public long AllOrdersCount { get; set; }
        public long CurrentOrdersCount { get; set; }
        public long DeliveredCount { get; set; }
        public long WaitingCount { get; set; }
        public long RejectedCount { get; set; }
        public long PartialDeliveredCount { get; set; }
        public long ReturnedCount { get; set; }
        public double OrdersMoney { get; set; }
        public double SystemMoney { get; set; }
        public double DriverMoney { get; set; }
    }
}
