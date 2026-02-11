using System;
using System.Collections.Generic;

namespace PostexS.Models.Dtos
{
    /// <summary>
    /// إحصائيات المندوب الشاملة (جميع الطلبات)
    /// </summary>
    public class DriverStatisticsDto
    {
        public string Name { get; set; }
        public long AllOrdersCount { get; set; }
        public long CurrentOrdersCount { get; set; }
        public long DeliveredCount { get; set; }
        public long DeliveredFinishedCount { get; set; }
        public long WaitingCount { get; set; }
        public long RejectedCount { get; set; }
        public long PartialDeliveredCount { get; set; }
        public long ReturnedCount { get; set; }
        public long Returned_ReceivedCount { get; set; }
        public long PartialReturned_ReceivedCount { get; set; }
        public double OrdersMoney { get; set; }
        public double SystemMoney { get; set; }
        public double DriverMoney { get; set; }
        public double DeliveredPercentage { get; set; }
        public double PartialDeliveredPercentage { get; set; }
        public double ReturnedPercentage { get; set; }
    }

    /// <summary>
    /// إحصائيات المندوب الحالية (الطلبات الغير منتهية فقط)
    /// </summary>
    public class DriverCurrentStatisticsDto
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
        public long AssignedCount { get; set; }
        public double OrdersMoney { get; set; }
        public double SystemMoney { get; set; }
        public double DriverMoney { get; set; }
        public double DeliveredPercentage { get; set; }
        public double ReturnedPercentage { get; set; }
        public double PartialDeliveredPercentage { get; set; }
        public double RejectedPercentage { get; set; }
        public double WaitingPercentage { get; set; }
        public double PartialReturnedPercentage { get; set; }
        public double AssignedPercentage { get; set; }
    }

    /// <summary>
    /// تقرير المندوب بفلتر تاريخ
    /// </summary>
    public class DriverReportDto
    {
        public string Name { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public long TotalOrders { get; set; }
        public long DeliveredCount { get; set; }
        public long ReturnedCount { get; set; }
        public long PartialDeliveredCount { get; set; }
        public long RejectedCount { get; set; }
        public long WaitingCount { get; set; }
        public long PartialReturnedCount { get; set; }
        public double TotalCollected { get; set; }
        public double DriverCommission { get; set; }
        public double SystemMoney { get; set; }
        public double DeliveredPercentage { get; set; }
        public double ReturnedPercentage { get; set; }
        public double PartialDeliveredPercentage { get; set; }
        public List<OrderDto> Orders { get; set; } = new List<OrderDto>();
    }

    /// <summary>
    /// تفاصيل طلب واحد مع الملاحظات وتاريخ العمليات
    /// </summary>
    public class OrderDetailsDto
    {
        public long Id { get; set; }
        public string Code { get; set; }
        public string ClientName { get; set; }
        public string ClientPhone { get; set; }
        public string ClientCode { get; set; }
        public string Address { get; set; }
        public string AddressCity { get; set; }
        public string Notes { get; set; }
        public double Cost { get; set; }
        public double DeliveryFees { get; set; }
        public double TotalCost { get; set; }
        public double ArrivedCost { get; set; }
        public double DeliveryCost { get; set; }
        public double? ReturnedCost { get; set; }
        public string Status { get; set; }
        public int StatusCode { get; set; }
        public string SenderName { get; set; }
        public string SenderPhone { get; set; }
        public string BranchName { get; set; }
        public string CreatedDate { get; set; }
        public string LastUpdated { get; set; }
        public bool Finished { get; set; }
        public string ReturnedImage { get; set; }
        public List<OrderNoteDto> OrderNotes { get; set; } = new List<OrderNoteDto>();
        public OrderHistoryTimelineDto Timeline { get; set; }
    }

    public class OrderNoteDto
    {
        public string Content { get; set; }
        public string UserName { get; set; }
        public string Date { get; set; }
    }

    public class OrderHistoryTimelineDto
    {
        public string CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string AssignedToDriverDate { get; set; }
        public string AssignedBy { get; set; }
        public string FinishDate { get; set; }
        public string FinishedBy { get; set; }
        public string CompleteDate { get; set; }
        public string CompletedBy { get; set; }
    }

    /// <summary>
    /// الملخص المالي للمندوب
    /// </summary>
    public class DriverFinancialSummaryDto
    {
        public string Name { get; set; }
        // الطلبات الغير منتهية (المطلوب تسليمها للشركة)
        public double PendingCollected { get; set; }
        public double PendingDriverCommission { get; set; }
        public double PendingToDeliver { get; set; }
        public long PendingOrdersCount { get; set; }
        // الطلبات المنتهية (تم تسويتها)
        public double FinishedCollected { get; set; }
        public double FinishedDriverCommission { get; set; }
        public double FinishedDelivered { get; set; }
        public long FinishedOrdersCount { get; set; }
        // إجمالي
        public double TotalCollected { get; set; }
        public double TotalDriverCommission { get; set; }
        public double TotalDeliveredToCompany { get; set; }
        public long TotalOrdersCount { get; set; }
    }
}
