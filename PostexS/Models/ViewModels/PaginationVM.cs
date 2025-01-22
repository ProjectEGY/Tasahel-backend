using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PostexS.Models.ViewModels
{
    public class PaginationVM
    {
        public int PageIndex { get; set; }
        public int TotalPages { get; set; }
        public bool PreviousPage { get; set; }
        public bool NextPage { get; set; }
        public string GetItemsUrl { get; set; }
        public string GetPaginationUrl { get; set; }
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public int ItemsCount { get; set; }
    }
}
