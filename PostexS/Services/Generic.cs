using Microsoft.EntityFrameworkCore;
using PostexS.Interfaces;
using PostexS.Models.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace PostexS.Services
{
    public class Generic<T> : IGeneric<T> where T : class
    {
        private readonly ApplicationDbContext _context;
        internal DbSet<T> _entity;

        public Generic(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<bool> Add(T obj)
        {
            await _context.Set<T>().AddAsync(obj);
            return await Save();
        }

        public async Task<bool> Delete(T obj)
        {
            _context.Set<T>().Remove(obj);
            return await Save();
        }
        public IQueryable<T> GetAllAsIQueryable(Expression<Func<T, bool>> filter = null, Func<IQueryable<T>, IOrderedQueryable<T>> orderby = null, string IncludeProperties = null, bool asNoTracking = false)
        {
            IQueryable<T> query = _context.Set<T>();

            // Use AsNoTracking for read-only queries to reduce memory usage
            if (asNoTracking)
            {
                query = query.AsNoTracking();
            }

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (IncludeProperties != null)
            {
                foreach (var item in IncludeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(item);
                }
            }

            if (orderby != null)
            {
                return orderby(query);
            }
            return query;
        }
        public async Task<T> GetSingle(
        Expression<Func<T, bool>> filter,
        string includeProperties = null)
        {
            IQueryable<T> query = GetAllAsIQueryable(filter, null, includeProperties);
            return await query.FirstOrDefaultAsync();
        }
        public IEnumerable<T> Get(System.Linq.Expressions.Expression<Func<T, bool>> expression)
        {
            return _context.Set<T>().Where(expression);
        }
        public int GetCount(System.Linq.Expressions.Expression<Func<T, bool>> expression)
        {
            return _context.Set<T>().Count(expression);
        }
        public async Task<T> GetSingle(Expression<Func<T, bool>> expression)
        {
            return await _context.Set<T>().FirstOrDefaultAsync(expression);
        }

        public IEnumerable<T> GetAll()
        {
            return _context.Set<T>().ToList();
        }
        public async Task<T> GetObj(Expression<Func<T, bool>> expression)
        {
            return await _context.Set<T>().FirstOrDefaultAsync(expression);
        }
        public async Task<bool> IsExist(System.Linq.Expressions.Expression<Func<T, bool>> expression)
        {
            return await _context.Set<T>().AnyAsync(expression);
        }
        public async Task<bool> Update(T obj)
        {
            _context.Entry<T>(obj).State = EntityState.Modified;
            return await Save();
        }
        private async Task<bool> Save()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<T> GetObja(Expression<Func<T, bool>> Filter = null, string Including = null)
        {
            IQueryable<T> query = _entity;
            if (Filter != null)
            {
                query = query.Where(Filter);
            }
            if (Including != null)
            {
                foreach (var item in Including.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(item);
                }
            }
            return await query.FirstOrDefaultAsync();
        }
    }

}
