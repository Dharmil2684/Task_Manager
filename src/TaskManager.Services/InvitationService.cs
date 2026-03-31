using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
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
        private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
        private readonly Microsoft.Extensions.Logging.ILogger<InvitationService> _logger;

        public InvitationService(
            ICosmosDbRepository<InvitationToken> invitationRepository, 
            ICosmosDbRepository<User> userRepository,
            IEmailService emailService,
            Microsoft.Extensions.Configuration.IConfiguration configuration,
            Microsoft.Extensions.Logging.ILogger<InvitationService> logger)
        {
            _invitationRepository = invitationRepository;
            _userRepository = userRepository;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
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
                await _userRepository.AddItemAsync(user, user.TenantId.ToString());
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
                throw new ArgumentException($"Invalid role: '{request.Role}'. Valid roles are: {string.Join(", ", Enum.GetNames<UserRole>())}");
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


            var baseUrl = _configuration["FrontendUrl"] ?? "http://localhost:3000";
            var link = $"{baseUrl.TrimEnd('/')}/accept-invite?token={rawToken}";

            // Persist the token first
            await _invitationRepository.AddItemAsync(invitation, tenantId.ToString());

            try
            {
                await _emailService.SendInvitationEmailAsync(request.Email, link);  
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send invitation email to {Email}", request.Email);
                // Compensating action: remove the invitation since email failed
                await _invitationRepository.DeleteItemAsync(invitation.Id.ToString(), tenantId.ToString());
                throw new InvalidOperationException("Failed to send invitation email. Validation rolled back.", ex);
            }
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
