using PostexS.Models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace PostexS.Interfaces
{
    public interface IOrderService
    {
        Order Get(Expression<Func<Order, bool>> expression);
        OrderOperationHistory GetOrderHistory(Expression<Func<OrderOperationHistory, bool>> expression);
        int GetOrdersHistory(Expression<Func<OrderOperationHistory, bool>> expression);
        IEnumerable<Order> GetList(Expression<Func<Order, bool>> expression);
        IQueryable<Order> GetQueryableList(Expression<Func<Order, bool>> expression);
        long Count(Expression<Func<Order, bool>> expression);
        IEnumerable<ApplicationUser> GetUsers(Expression<Func<Order, bool>> expression);
    }
}
