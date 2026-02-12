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
        // Date filter
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        // Driver financial details
        public double DriverDeliveryCostPerOrder { get; set; }
        public double TotalDriverProfit { get; set; }
        // Settlements
        public List<DriverSettlementVM> Settlements { get; set; } = new List<DriverSettlementVM>();
        // Live/Current stats (unfiltered - sidebar)
        public long LiveCurrentCount { get; set; }
        public long LiveDeliveredCount { get; set; }
        public long LiveWaitingCount { get; set; }
        public long LiveRejectedCount { get; set; }
        public long LiveReturnedCount { get; set; }
        public long LivePartialDeliveredCount { get; set; }
        public long LiveReadyForFinishCount { get; set; }
        public long LiveAllCount { get; set; }
    }
    public class DriverSettlementVM
    {
        public long WalletId { get; set; }
        public DateTime Date { get; set; }
        public int OrderCount { get; set; }
        public double TotalAmount { get; set; }
        public string TransactionTypeName { get; set; }
    }
}
