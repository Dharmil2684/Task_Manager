using TaskManager.Core.Entities;
using TaskManager.Core.Interfaces;
using Moq;
using Xunit;

namespace TaskManager.Tests
{
    /// <summary>
    /// Smoke tests to verify the solution structure compiles and basic types resolve correctly.
    /// </summary>
    public class SmokeTests
    {
        [Fact]
        public void BaseEntity_NewInstance_HasGeneratedId()
        {
            // Arrange & Act — use a concrete entity
            var tenant = new Tenant { CompanyName = "Test Corp" };

            // Assert
            Assert.NotEqual(Guid.Empty, tenant.Id);
        }

        [Fact]
        public void TaskItem_DefaultStatus_IsPending()
        {
            var task = new TaskItem { Title = "Test Task" };

            Assert.Equal(WorkTaskStatus.Pending, task.Status);
        }

        [Fact]
        public void User_DefaultRole_IsEmployee()
        {
            var user = new User { Email = "test@example.com" };

            // Default enum value is Employee (0) — least-privileged
            Assert.Equal(UserRole.Employee, user.Role);
        }

        [Fact]
        public void ICosmosDbRepository_CanBeMocked()
        {
            // Verify the interface is mockable (confirms Core project reference works)
            var mock = new Mock<ICosmosDbRepository<TaskItem>>();
            mock.Setup(r => r.GetItemAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TaskItem { Title = "Mocked" });

            Assert.NotNull(mock.Object);
        }

        [Fact]
        public void ITenantContext_CanBeMocked()
        {
            var mock = new Mock<ITenantContext>();
            mock.Setup(t => t.CurrentTenantId).Returns("test-tenant-id");

            Assert.Equal("test-tenant-id", mock.Object.CurrentTenantId);
        }

        [Fact]
        public void User_PasswordHash_IsNotSerialized()
        {
            var user = new User
            {
                Email = "test@example.com",
                PasswordHash = "hashed-secret"
            };

            var json = System.Text.Json.JsonSerializer.Serialize(user);

            Assert.DoesNotContain("passwordHash", json);
            Assert.DoesNotContain("hashed-secret", json);
        }

        [Fact]
        public void User_RefreshToken_IsNotSerialized()
        {
            var user = new User
            {
                Email = "test@example.com",
                RefreshToken = "secret-refresh-token"
            };

            var json = System.Text.Json.JsonSerializer.Serialize(user);

            Assert.DoesNotContain("refreshToken", json);
            Assert.DoesNotContain("secret-refresh-token", json);
        }
    }
}

