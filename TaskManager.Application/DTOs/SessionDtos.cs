using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.DTOs
{
    public class CreateSessionDto
    {
        public string Title { get; set; } = string.Empty;
        public int MaxAllowedPauseMinutes { get; set; } = 60;
        public int CategoryId { get; set; }
    }

    public class SessionResponseDto
    {
        public int SessionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public Status Status { get; set; }
        public int PausedCount { get; set; }
        public int FocusTimeInMinutes { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public DateTime? LastStatusChangedAt { get; set; }
        public int MaxAllowedPauseInMinutes { get; set; } = 60;
        public int TotalPausedInMinutes { get; set; }
    }

    public class DaySummaryDto
    {
        public DateTime Date { get; set; }
        public int TotalMinutes { get; set; }
        public List<SessionResponseDto> Sessions { get; set; } = new();
    }

    public class UpdateSessionDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public int CategoryId { get; set; }
    }

    public class FinishSessionDto
    {
        public string? Summary { get; set; }
    }
}
