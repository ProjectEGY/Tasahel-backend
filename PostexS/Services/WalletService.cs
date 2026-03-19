using Microsoft.EntityFrameworkCore;
using PostexS.Interfaces;
using PostexS.Models.Data;
using PostexS.Models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace PostexS.Services
{
    public class WalletService : IWalletService
    {
        private readonly ApplicationDbContext _context;
        public WalletService(ApplicationDbContext context)
        {
            _context = context;
        }

        public IEnumerable<Wallet> GetList(Expression<Func<Wallet, bool>> expression)
        {
            return _context.Wallets.Where(expression).Include(x => x.ActualUser).OrderByDescending(x=>x.Id);
        }

        public IEnumerable<Order> GetWalletOrdersList(Expression<Func<Wallet, bool>> expression)
        {
            return _context.Wallets
                .Where(expression)
                .OrderByDescending(x => x.Id)
                .SelectMany(x => x.Orders).Include(a => a.Client)
                .ToList();
        }
    }
}
