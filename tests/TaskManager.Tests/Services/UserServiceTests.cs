using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using TaskManager.Core.Entities;
using TaskManager.Core.Interfaces;
using TaskManager.Services;
using Xunit;

namespace TaskManager.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<ICosmosDbRepository<User>> _mockUserRepo;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _mockUserRepo = new Mock<ICosmosDbRepository<User>>();
            _userService = new UserService(_mockUserRepo.Object);
        }

        [Fact]
        public async Task GetUserByEmailAsync_EmptyEmail_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _userService.GetUserByEmailAsync(""));
        }

        [Fact]
        public async Task GetUserByEmailAsync_ValidEmail_ReturnsUser()
        {
            // Arrange
            var email = "test@example.com";
            var expectedUser = new User { Email = email };
            
            _mockUserRepo.Setup(x => x.GetItemsAsync(
                It.Is<string>(q => q.Contains("email = @email")),
                It.Is<Dictionary<string, object>>(d => d["@email"].ToString() == email)))
            .ReturnsAsync(new List<User> { expectedUser });

            // Act
            var result = await _userService.GetUserByEmailAsync(email);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(email, result.Email);
        }

        [Fact]
        public async Task GetUsersByTenantAsync_ReturnsTenantUsers()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var expectedUsers = new List<User>
            {
                new User { Id = Guid.NewGuid(), TenantId = tenantId },
                new User { Id = Guid.NewGuid(), TenantId = tenantId }
            };

            _mockUserRepo.Setup(x => x.GetItemsAsync(
                It.Is<string>(q => q.Contains("tenantId = @tenantId")),
                It.Is<Dictionary<string, object>>(d => (Guid)d["@tenantId"] == tenantId)))
            .ReturnsAsync(expectedUsers);

            // Act
            var results = await _userService.GetUsersByTenantAsync(tenantId);

            // Assert
            Assert.Equal(2, results.Count());
            Assert.All(results, u => Assert.Equal(tenantId, u.TenantId));
        }
    }
}
