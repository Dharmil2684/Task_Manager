using System;
using System.Threading.Tasks;
using TaskManager.Core.DTOs.Auth;

namespace TaskManager.Core.Interfaces
{
    public interface IInvitationService
    {
        Task CreateInvitationAsync(InviteUserRequest request, Guid tenantId);
        Task AcceptInvitationAsync(string token, string password);
    }
}
