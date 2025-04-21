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
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        public OrderService(ApplicationDbContext context)
        {
            _context = context;
        }
        public Order Get(Expression<Func<Order, bool>> expression)
        {
            return _context.Orders.Include(x => x.Branch)
                .Include(x => x.Client).Include(x => x.OrderNotes).Include(x => x.OrderOperationHistory)
                .Include(x => x.Delivery).FirstOrDefault(expression);
        }
        public OrderOperationHistory GetOrderHistory(Expression<Func<OrderOperationHistory, bool>> expression)
        {
            return _context.OrdersOperationsHistories.Include(x => x.Order!).Include(x => x.Edit_User!).Include(x => x.Delete_User!).Include(x => x.Complete_User!).Include(x => x.ReturnedComplete_User!)
                .Include(x => x.Transfer_User!).Include(x => x.Create_User!).Include(x => x.Assign_To_Driver_User!)
                .Include(x => x.Reject_User!).Include(x => x.Restore_User!).Include(x => x.EditComplete_User!)
                .Include(x => x.AcceptTransferReturned_User!).Include(x => x.TransferReturned_User!)
                .Include(x => x.AcceptTransfer_User!).Include(x => x.Finish_User).Include(x => x.ReturnedFinish_User).Include(x => x.Accept_User!).FirstOrDefault(expression);
        }
        public int GetOrdersHistory(Expression<Func<OrderOperationHistory, bool>> expression)
        {
            return _context.OrdersOperationsHistories.Count(expression);
        }

        public IEnumerable<ApplicationUser> GetUsers(Expression<Func<Order, bool>> expression)
        {
            return _context.Orders.Where(expression).Select(x => x.Client).Distinct().ToList();
        }

        public IEnumerable<Order> GetList(Expression<Func<Order, bool>> expression)
        {
            return _context.Orders.Where(expression).Include(x => x.Branch).Include(x => x.Client).Include(x => x.OrderNotes)
                .Include(x => x.Delivery).Include(x => x.OrderOperationHistory).ToList();
        }
        public IQueryable<Order> GetQueryableList(Expression<Func<Order, bool>> expression)
        {
            return _context.Orders.Where(expression).Include(x => x.Branch).Include(x => x.Client).Include(x => x.OrderNotes)
                .Include(x => x.Delivery).Include(x => x.OrderOperationHistory);
        }
        public long Count(Expression<Func<Order, bool>> expression)
        {
            return _context.Orders.Count(expression);
        }
    }
}
