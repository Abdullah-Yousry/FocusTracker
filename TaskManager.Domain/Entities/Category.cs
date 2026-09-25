using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace TaskManager.Domain.Entities
{
    public class Category
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // User Relation
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        // Session Realation
        public ICollection<Session> Sessions { get; set; } = new List<Session>();
    }
}
