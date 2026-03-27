using System;
using System.Text.Json.Serialization;
using TaskManager.Core.Enums;
using TaskManager.Core.Models;

namespace TaskManager.Core.Models
{
    public class User : BaseEntity
    {
        [JsonPropertyName("tenantId")]
        public Guid TenantId { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("passwordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public UserRole Role { get; set; }

        // Required for our secure Refresh Token flow
        [JsonPropertyName("refreshToken")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("refreshTokenExpiryTime")]
        public DateTime? RefreshTokenExpiryTime { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}