using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskManager.Core.Entities;
using TaskManager.Core.Interfaces;

namespace TaskManager.Services
{
    public class UserService : IUserService
    {
        private readonly ICosmosDbRepository<User> _userRepository;

        public UserService(ICosmosDbRepository<User> userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email cannot be null or empty.", nameof(email));
            }

            var query = "SELECT * FROM c WHERE c.email = @email";
            var parameters = new Dictionary<string, object> { { "@email", email } };
            
            // Note: This is tenant-scoped via ITenantContext. For cross-partition lookups
            // (e.g. login), use a dedicated method or set the tenant context beforehand.
            var users = await _userRepository.GetItemsAsync(query, parameters);
            return users.FirstOrDefault();
        }

        public async Task<IEnumerable<User>> GetUsersByTenantAsync(Guid tenantId)
        {
            var query = "SELECT * FROM c WHERE c.tenantId = @tenantId";
            var parameters = new Dictionary<string, object> { { "@tenantId", tenantId } };
            return await _userRepository.GetItemsAsync(query, parameters);
        }
    }
}
