using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManager.Core.Entities;

namespace TaskManager.Core.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<User>> GetUsersByTenantAsync(Guid tenantId);
        Task<User?> GetUserByEmailAsync(string email);
    }
}
