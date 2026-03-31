using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Moq;
using TaskManager.Core.DTOs.Auth;
using TaskManager.Core.Entities;
using TaskManager.Core.Interfaces;
using TaskManager.Core.Exceptions;
using TaskManager.Services;
using Xunit;

namespace TaskManager.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<ICosmosDbRepository<Tenant>> _mockTenantRepo;
        private readonly Mock<ICosmosDbRepository<User>> _mockUserRepo;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _mockTenantRepo = new Mock<ICosmosDbRepository<Tenant>>();
            _mockUserRepo = new Mock<ICosmosDbRepository<User>>();      
            _mockConfig = new Mock<IConfiguration>();

            _mockConfig.Setup(c => c["Jwt:SecretKey"]).Returns("SuperSecretKeyThatIsAtLeast32BytesLong12345!@#");
            _mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
            _mockConfig.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
            _mockConfig.Setup(c => c["Jwt:AccessTokenExpiryMinutes"]).Returns("15");

            _authService = new AuthService(_mockTenantRepo.Object, _mockUserRepo.Object, _mockConfig.Object);
        }

        [Fact]
        public async Task LoginAsync_MissingEmail_ThrowsArgumentException()
        {
            var req = new LoginRequest { Email = "", Password = "pw" };
            await Assert.ThrowsAsync<ArgumentException>(() => _authService.LoginAsync(req));
        }

        [Fact]
        public async Task LoginAsync_UserNotFound_ThrowsUnauthorizedAccessException()
        {
            var req = new LoginRequest { Email = "test@test.com", Password = "pw" };
            _mockUserRepo.Setup(r => r.GetItemsAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                         .ReturnsAsync(new List<User>());

            await Assert.ThrowsAsync<AuthorizationException>(() => _authService.LoginAsync(req));
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ThrowsUnauthorizedAccessException()
        {
            var req = new LoginRequest { Email = "test@test.com", Password = "wrongpw" };
            var user = new User { Email = req.Email, PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctpw") };
            
            _mockUserRepo.Setup(r => r.GetItemsAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                         .ReturnsAsync(new List<User> { user });

            await Assert.ThrowsAsync<AuthorizationException>(() => _authService.LoginAsync(req));
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsTokens()
        {
            var req = new LoginRequest { Email = "test@test.com", Password = "password123" };
            var user = new User { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), Email = req.Email, Role = UserRole.Employee, PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123") };
            
            _mockUserRepo.Setup(r => r.GetItemsAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                         .ReturnsAsync(new List<User> { user });
            _mockUserRepo.Setup(r => r.UpdateItemAsync(user.Id.ToString(), user, user.TenantId.ToString()))
                         .Returns(Task.CompletedTask);

            var result = await _authService.LoginAsync(req);

            Assert.NotNull(result);
            Assert.NotEmpty(result.AccessToken);
            Assert.NotEmpty(result.RefreshToken);
        }
    }
}
