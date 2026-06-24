using PostexS.Models.Enums;
using System;

namespace PostexS.Models.ViewModels
{
    public class UserOrderItemVM
    {
        public long Id { get; set; }
        public string Code { get; set; }
        public DateTime CreateOn { get; set; }
        public string ClientName { get; set; }
        public string Address { get; set; }
        public string Notes { get; set; }
        public double TotalCost { get; set; }
        public double ArrivedCost { get; set; }
        public OrderStatus Status { get; set; }
        public OrderCompleted OrderCompleted { get; set; }
        public DateTime? CompletedOn { get; set; }
        public string SenderName { get; set; }
        public string DeliveryName { get; set; }
        public string DeliveryPhone { get; set; }
        public string LastNote { get; set; }
    }

    public class UserOrdersApiResponse
    {
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public UserOrdersStatsVM GlobalStats { get; set; }
        public UserOrdersStatsVM FilteredStats { get; set; }
        public System.Collections.Generic.List<UserOrderItemVM> Items { get; set; }
    }

    public class UserOrdersStatsVM
    {
        public int Total { get; set; }
        public int Placed { get; set; }
        public int Assigned { get; set; }
        public int Delivered { get; set; }
        public int Returned { get; set; }
        public int Rejected { get; set; }
        public int Waiting { get; set; }
        public int Completed { get; set; }
        public double TotalArrivedCost { get; set; }
    }
}
