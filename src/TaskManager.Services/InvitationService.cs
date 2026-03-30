using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using TaskManager.Core.DTOs.Auth;
using TaskManager.Core.Entities;
using TaskManager.Core.Interfaces;

namespace TaskManager.Services
{
    public class InvitationService : IInvitationService
    {
        private readonly ICosmosDbRepository<InvitationToken> _invitationRepository;
        private readonly ICosmosDbRepository<User> _userRepository;
        private readonly IEmailService _emailService;

        public InvitationService(
            ICosmosDbRepository<InvitationToken> invitationRepository, 
            ICosmosDbRepository<User> userRepository,
            IEmailService emailService)
        {
            _invitationRepository = invitationRepository;
            _userRepository = userRepository;
            _emailService = emailService;
        }

        public async Task AcceptInvitationAsync(string token, string password)
        {
            var query = "SELECT * FROM c WHERE c.tokenHash = @hash AND c.isUsed = false";
            var hash = HashToken(token);
            var parameters = new Dictionary<string, object> { { "@hash", hash } };
            
            var tokens = await _invitationRepository.GetItemsAsync(query, parameters);
            var invitation = tokens.FirstOrDefault();

            if (invitation == null || invitation.ExpiresAt < DateTime.UtcNow)
            {
                throw new InvalidOperationException("Invalid or expired invitation token.");
            }

            // Mark invitation as used FIRST (optimistic concurrency) to prevent TOCTOU races
            invitation.IsUsed = true;
            try
            {
                await _invitationRepository.UpdateItemAsync(invitation.Id.ToString(), invitation, invitation.TenantId.ToString());
            }
            catch
            {
                // Another request already claimed this invitation
                throw new InvalidOperationException("This invitation has already been used.");
            }

            // Now create the user; if this fails, roll back the invitation
            var user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = invitation.TenantId,
                Email = invitation.Email,
                Role = invitation.Role,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _userRepository.AddItemAsync(user);
            }
            catch
            {
                // Compensating action: revert the invitation to unused
                invitation.IsUsed = false;
                await _invitationRepository.UpdateItemAsync(invitation.Id.ToString(), invitation, invitation.TenantId.ToString());
                throw;
            }
        }

        public async Task CreateInvitationAsync(InviteUserRequest request, Guid tenantId)
        {
            var rawToken = Guid.NewGuid().ToString("N");
            
            if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            {
                throw new ArgumentException($"Invalid role: '{request.Role}'. Valid roles are: {string.Join(", ", Enum.GetNames<UserRole>())}.");
            }

            var invitation = new InvitationToken
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Email = request.Email,
                Role = role,
                TokenHash = HashToken(rawToken),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsUsed = false
            };

            await _invitationRepository.AddItemAsync(invitation);

            var link = $"https://app.yourdomain.com/accept-invite?token={rawToken}";
            
            await _emailService.SendInvitationEmailAsync(request.Email, link);
        }

        private static string HashToken(string token)
        {
            using var sha256 = SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(token);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
