using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskManager.Application.DTOs
{
    public class CategoryRequestDto
    {
        public string Name { get; set; } = string.Empty;
    }

    public class CategoryResponseDto
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class CategorySummaryDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public int TotalMinutes { get; set; }
        public int SessionsCount { get; set; }
    }
}
