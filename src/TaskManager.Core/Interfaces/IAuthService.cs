using System;
using System.Threading.Tasks;
using TaskManager.Core.DTOs.Auth;

namespace TaskManager.Core.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> RegisterTenantAsync(RegisterRequest request);
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<LoginResponse> RefreshTokenAsync(string refreshToken);
        Task RevokeRefreshTokenAsync(Guid userId, Guid tenantId);
    }
}
