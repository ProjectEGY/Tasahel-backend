using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PostexS.Models.ViewModels;

namespace PostexS.Helper
{
    public class PagedList<T> : List<T>
    {
        public int PageIndex { get; private set; }
        public int TotalPages { get; set; }
        public int ItemsCount { get; set; }
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public PagedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            ItemsCount = count;
            StartIndex = ((pageIndex - 1) * pageSize) + 1;
            EndIndex = StartIndex + pageSize - 1;
            if(EndIndex > count)
            {
                EndIndex = count;
            }
            this.AddRange(items);
        }

        public bool PreviousPage
        {
            get
            {
                return (PageIndex > 1);
            }
        }

        public bool NextPage
        {
            get
            {
                return (PageIndex < TotalPages);
            }
        }

        public static async Task<PagedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
        {
            var count = await source.CountAsync();
            var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PagedList<T>(items, count, pageIndex, pageSize);
        }

        public static PagedList<T> Create(List<T> source, int pageIndex, int pageSize)
        {
            var count = source.Count();
            var items = source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();
            return new PagedList<T>(items, count, pageIndex, pageSize);
        }

        public static PaginationVM GetPaginationObject(PagedList<T> pl)
        {
            return new PaginationVM()
            {
                PageIndex = pl.PageIndex,
                TotalPages = pl.TotalPages,
                PreviousPage = pl.PreviousPage,
                NextPage = pl.NextPage,
                ItemsCount = pl.ItemsCount,
                StartIndex=pl.StartIndex,
                EndIndex = pl.EndIndex
            };
        }
    }
}
