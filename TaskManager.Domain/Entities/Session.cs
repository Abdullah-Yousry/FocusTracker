using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManager.Domain.Enums;

namespace TaskManager.Domain.Entities
{
    public class Session
    {
        public int SessionId { get; set; }
        public int FocusTimeInMinutes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public int PauseCount { get; set; } = 0;
        public DateTime LastStatusChangedAt { get; set; } = DateTime.UtcNow;
        public int MaxAllowedPauseInMinutes { get; set; } = 60;
        public int TotalPausedInMinutes { get; set; }
        public Status Status { get; set; } = Status.Pending;

        // User realations
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        // Category realations
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;
    }
}
