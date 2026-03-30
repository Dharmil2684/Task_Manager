using System.Threading.Tasks;

namespace TaskManager.Core.Interfaces
{
    public interface IEmailService
    {
        Task SendInvitationEmailAsync(string toEmail, string inviteLink);
    }
}
