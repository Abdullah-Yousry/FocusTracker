using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManager.Application.DTOs
{
    namespace TaskManager.Application.DTOs
    {
        public class PagedResultDto<T>
        {
            public IEnumerable<T> Items { get; set; } = new List<T>();

            public int PageNumber { get; set; }
            public int PageSize { get; set; }

            public int TotalCount { get; set; } // Count of records in DB

            public int TotalPages => (TotalCount+(PageSize-1)) / PageSize; 

            public bool HasPreviousPage => PageNumber > 1;
            public bool HasNextPage => PageNumber < TotalPages;
        }
    }
}
