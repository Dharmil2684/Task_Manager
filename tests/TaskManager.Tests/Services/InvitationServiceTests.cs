using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManager.Core.DTOs.Auth;
using TaskManager.Core.Entities;
using TaskManager.Core.Interfaces;
using TaskManager.Services;
using Xunit;

namespace TaskManager.Tests.Services
{
    public class InvitationServiceTests
    {
        private readonly Mock<ICosmosDbRepository<InvitationToken>> _mockInviteRepo;
        private readonly Mock<ICosmosDbRepository<User>> _mockUserRepo;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<ILogger<InvitationService>> _mockLogger;
        private readonly InvitationService _invitationService;

        public InvitationServiceTests()
        {
            _mockInviteRepo = new Mock<ICosmosDbRepository<InvitationToken>>();
            _mockUserRepo = new Mock<ICosmosDbRepository<User>>();
            _mockEmailService = new Mock<IEmailService>();
            _mockConfig = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<InvitationService>>();

            _invitationService = new InvitationService(
                _mockInviteRepo.Object, 
                _mockUserRepo.Object, 
                _mockEmailService.Object, 
                _mockConfig.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task CreateInvitationAsync_InvalidRole_ThrowsArgumentException()
        {
            var req = new InviteUserRequest { Email = "test@company.com", Role = "SuperAdmin" };
            
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _invitationService.CreateInvitationAsync(req, Guid.NewGuid()));
        }

        [Fact]
        public async Task CreateInvitationAsync_ValidRequest_SendsEmailAndSavesToken()
        {
            var req = new InviteUserRequest { Email = "test@company.com", Role = "Employee" };
            var tenantId = Guid.NewGuid();
            
            _mockConfig.Setup(c => c["FrontendUrl"]).Returns("http://localhost:3000");

            _mockEmailService.Setup(e => e.SendInvitationEmailAsync(req.Email, It.IsAny<string>()))
                             .Returns(Task.CompletedTask);

            _mockInviteRepo.Setup(r => r.AddItemAsync(It.IsAny<InvitationToken>(), tenantId.ToString()))
                           .Returns(Task.CompletedTask);

            await _invitationService.CreateInvitationAsync(req, tenantId);

            _mockEmailService.Verify(e => e.SendInvitationEmailAsync(req.Email, It.Is<string>(s => s.Contains("accept-invite?token="))), Times.Once);
            _mockInviteRepo.Verify(r => r.AddItemAsync(It.Is<InvitationToken>(i => i.Email == req.Email && i.Role == UserRole.Employee), tenantId.ToString()), Times.Once);
        }

        [Fact]
        public async Task CreateInvitationAsync_EmailFails_DoesNotSaveToken()
        {
            var req = new InviteUserRequest { Email = "test@company.com", Role = "Employee" };
            var tenantId = Guid.NewGuid();
            
            _mockConfig.Setup(c => c["FrontendUrl"]).Returns("http://localhost:3000");

            _mockEmailService.Setup(e => e.SendInvitationEmailAsync(req.Email, It.IsAny<string>()))
                             .ThrowsAsync(new InvalidOperationException("Email fail"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _invitationService.CreateInvitationAsync(req, tenantId));

            // Wait! The new logic does insert and then delete, so let's verify Delete was called, not AddItem.
            // Actually, AddItem is called first, then DeleteItem.
            _mockInviteRepo.Verify(r => r.AddItemAsync(It.IsAny<InvitationToken>(), tenantId.ToString()), Times.Once);
            _mockInviteRepo.Verify(r => r.DeleteItemAsync(It.IsAny<string>(), tenantId.ToString()), Times.Once);
        }
    }
}
