using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace PostexS.Interfaces
{
    public interface IGeneric<T> 
    {
        Task<bool> Add(T obj);
        IQueryable<T> GetAllAsIQueryable(
          Expression<Func<T, bool>> filter = null,
          Func<IQueryable<T>, IOrderedQueryable<T>> orderby = null,
          string IncludeProperties = null,
          bool asNoTracking = false);
        Task<T> GetSingle(Expression<Func<T, bool>> expression, string includeProperties = null);
        IEnumerable<T> Get(Expression<Func<T, bool>> expression);
        int GetCount(Expression<Func<T, bool>> expression);
        Task<T> GetSingle(Expression<Func<T, bool>> expression);
        Task<T> GetObj(Expression<Func<T, bool>> expression);
        IEnumerable<T> GetAll();
        Task<bool> IsExist(Expression<Func<T, bool>> expression);
        Task<bool> Update(T obj);
        Task<bool> Delete(T obj);
        Task<T> GetObja(Expression<Func<T, bool>> Filter = null, string Including = null);

    }
}
