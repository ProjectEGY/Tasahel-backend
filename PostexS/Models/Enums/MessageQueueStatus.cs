using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.Enums
{
    public enum MessageQueueStatus
    {
        Pending = 0,
        Processing = 1,
        Sent = 2,
        Failed = 3,
        Cancelled = 4
    }
}
