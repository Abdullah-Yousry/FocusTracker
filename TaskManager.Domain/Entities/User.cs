using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskManager.Domain.Enums;
using static System.Collections.Specialized.BitVector32;

namespace TaskManager.Domain.Entities
{
    public class User
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public UserRole Role { get; set; } = UserRole.User;
        // Sessions realations
        public ICollection<Session> Sessions { get; set; } = new List<Session>();
        // Category realations
        public ICollection<Category> Categories { get; set; } = new List<Category>();
        // RefreshToken Relations
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
