using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.ViewModels
{
    public class CurrentStatisticsVM
    {
        public string Name { get; set; }
        public long AllOrdersCount { get; set; }
        public long CurrentOrdersCount { get; set; }
        public long DeliveredCount { get; set; }
        public long WaitingCount { get; set; }
        public long RejectedCount { get; set; }
        public long PartialDeliveredCount { get; set; }
        public long ReturnedCount { get; set; }
        public long PartialReturnedCount { get; set; }
        public long DeletedCount { get; set; } // جديد: عدد الطلبات المحذوفة
        public long AssignedCount { get; set; } // جديد: عدد الطلبات المحذوفة
        public double OrdersMoney { get; set; }
        public double SystemMoney { get; set; }
        public double DriverMoney { get; set; }
        public double DeliveredPercentage { get; set; }
        public double ReturnedPercentage { get; set; }
        public double PartialDeliveredPercentage { get; set; }
        public double RejectedPercentage { get; set; }
        public double WaitingPercentage { get; set; } // جديد: نسبة الطلبات المعلقة
        public double PartialReturnedPercentage { get; set; } // جديد: نسبة الطلبات المرتجعة جزئياً
        public double DeletedPercentage { get; set; } // جديد: نسبة الطلبات المحذوفة
        public double AssignedPercentage { get; set; }
    }
    public class DriverStatisticsVM
    {
        public string Name { get; set; }
        public long AllOrdersCount { get; set; }
        public long CurrentOrdersCount { get; set; }
        public long DeliveredCount { get; set; }
        public long DeliveredFinishedCount { get; set; }
        public long WaitingCount { get; set; }
        public long RejectedCount { get; set; }
        public long PartialDeliveredCount { get; set; }
        public long PartialReturned_ReceivedCount { get; set; }
        public long ReturnedCount { get; set; }
        public long Returned_ReceivedCount { get; set; }
        public double OrdersMoney { get; set; }
        public double SystemMoney { get; set; }
        public double DriverMoney { get; set; }
        public double DeliveredPercentage { get; set; }
        public double PartialDeliveredPercentage { get; set; }
        public double ReturnedPercentage { get; set; }
    }
}
