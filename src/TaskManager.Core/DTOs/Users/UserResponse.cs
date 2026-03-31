using System;
using TaskManager.Core.Entities;

namespace TaskManager.Core.DTOs.Users
{
    /// <summary>
    /// Projection DTO for User entities — never exposes PasswordHash or RefreshToken.
    /// </summary>
    public class UserResponse
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
