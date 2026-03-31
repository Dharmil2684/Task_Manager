using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaskManager.Core.DTOs.Auth
{
    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class InviteUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = "Employee";
    }

    public class AcceptInvitationRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "Token cannot be empty")]
        public string Token { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
