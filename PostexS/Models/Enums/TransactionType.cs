using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.Enums
{
    public enum TransactionType
    {
        AddedByAdmin , RemovedByAdmin , OrderFinished , OrderComplete, OrderReturnedComplete, ReAddToWallet,
        EmployeeTransferOut, // الموظف بعت فلوس للأدمن — خصم من محفظة الموظف
        EmployeeTransferIn   // الأدمن استلم فلوس من الموظف — إضافة لمحفظة الأدمن
    }
}
