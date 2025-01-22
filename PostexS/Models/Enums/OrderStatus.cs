using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.Enums
{
    public enum OrderStatus
    {
        Placed, Assigned, Delivered, Waiting, Rejected, Finished, Completed, PartialDelivered, Returned, PartialReturned
    , Delivered_With_Edit_Price, Returned_And_Paid_DeliveryCost, Returned_And_DeliveryCost_On_Sender,
    }
}
