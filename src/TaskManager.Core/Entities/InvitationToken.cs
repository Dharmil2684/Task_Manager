using System;

namespace TaskManager.Core.Entities
{
    public class InvitationToken : BaseEntity
    {
        public Guid TenantId { get; set; }
        
        public string Email { get; set; } = string.Empty;
        
        public UserRole Role { get; set; }
        
        public string TokenHash { get; set; } = string.Empty;
        
        public DateTime ExpiresAt { get; set; }
        
        public bool IsUsed { get; set; }
    }
}
