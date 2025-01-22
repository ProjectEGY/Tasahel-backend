using PostexS.Models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace PostexS.Interfaces
{
    public interface IWalletService
    {
        IEnumerable<Wallet> GetList(Expression<Func<Wallet, bool>> expression);
    }
}
