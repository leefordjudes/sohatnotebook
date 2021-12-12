using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SohatNotebook.Entities.Dtos.Generic
{
    public class PagedResult<T> : Result<List<T>>
    {
        public int Page { get; set; }
        public int ResultCount { get; set; }
        public int ResultPerPage { get; set; }
    }
}