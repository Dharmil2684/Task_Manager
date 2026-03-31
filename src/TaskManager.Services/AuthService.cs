using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TaskManager.Core.DTOs.Auth;
using TaskManager.Core.Entities;
using TaskManager.Core.Exceptions;
using TaskManager.Core.Interfaces;

namespace TaskManager.Services
{
    public class AuthService : IAuthService
    {
        private readonly ICosmosDbRepository<Tenant> _tenantRepository;
        private readonly ICosmosDbRepository<User> _userRepository;
        private readonly IConfiguration _configuration;

        public AuthService(ICosmosDbRepository<Tenant> tenantRepository, ICosmosDbRepository<User> userRepository, IConfiguration configuration)
        {
            _tenantRepository = tenantRepository;
            _userRepository = userRepository;
            _configuration = configuration;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrWhiteSpace(request.Email))
                throw new ArgumentException("Email is required.", nameof(request));
            if (string.IsNullOrWhiteSpace(request.Password))
                throw new ArgumentException("Password is required.", nameof(request));

            var query = "SELECT * FROM c WHERE c.email = @email";
            var parameters = new Dictionary<string, object> { { "@email", request.Email } };
            
            var users = await _userRepository.GetItemsAsync(query, parameters);
            var user = users.FirstOrDefault();

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                throw new AuthorizationException("Invalid credentials");
            }

            return await GenerateTokensAsync(user);
        }

        public async Task<LoginResponse> RefreshTokenAsync(string refreshToken)
        {
            var query = "SELECT * FROM c WHERE c.refreshToken = @token";
            var parameters = new Dictionary<string, object> { { "@token", refreshToken } };
            
            var users = await _userRepository.GetItemsAsync(query, parameters);
            var user = users.FirstOrDefault();

            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                throw new AuthorizationException("Invalid or expired refresh token");
            }

            return await GenerateTokensAsync(user);
        }

        public async Task<LoginResponse> RegisterTenantAsync(RegisterRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (string.IsNullOrWhiteSpace(request.Email))
                throw new ArgumentException("Email is required.", nameof(request));
            if (string.IsNullOrWhiteSpace(request.Password))
                throw new ArgumentException("Password is required.", nameof(request));
            if (string.IsNullOrWhiteSpace(request.CompanyName))
                throw new ArgumentException("CompanyName is required.", nameof(request));

            // Check for duplicate email before creating anything
            var existingQuery = "SELECT * FROM c WHERE c.email = @email";
            var existingParams = new Dictionary<string, object> { { "@email", request.Email } };
            var existingUsers = await _userRepository.GetItemsAsync(existingQuery, existingParams);
            if (existingUsers.Any())
            {
                throw new InvalidOperationException("A user with this email already exists.");
            }

            // 1. Create Tenant
            var tenantId = Guid.NewGuid();
            var tenant = new Tenant
            {
                Id = tenantId,
                CompanyName = request.CompanyName,
                SubscriptionTier = "Free",
                CreatedAt = DateTime.UtcNow
            };
            await _tenantRepository.AddItemAsync(tenant, tenantId.ToString());

            // 2. Create Tenant Admin (with compensating rollback if user creation fails)
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                TenantId = tenantId,
                Email = request.Email,
                Role = UserRole.TenantAdmin,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _userRepository.AddItemAsync(user, tenantId.ToString());
            }
            catch
            {
                // Compensating action: roll back the tenant if user creation fails
                await _tenantRepository.DeleteItemAsync(tenantId.ToString(), tenantId.ToString());
                throw;
            }

            return await GenerateTokensAsync(user);
        }

        public async Task RevokeRefreshTokenAsync(Guid userId, Guid tenantId)
        {
            var user = await _userRepository.GetItemAsync(userId.ToString(), tenantId.ToString());
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
                await _userRepository.UpdateItemAsync(userId.ToString(), user, tenantId.ToString());
            }
        }

        private async Task<LoginResponse> GenerateTokensAsync(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtKey = _configuration["Jwt:SecretKey"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("Jwt:SecretKey configuration is missing. The application cannot generate tokens without a secret key.");
            }
            var key = Encoding.ASCII.GetBytes(jwtKey);
            
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("tenantId", user.TenantId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            if (!int.TryParse(_configuration["Jwt:AccessTokenExpiryMinutes"], out var expiryMinutes) || expiryMinutes <= 0)
            {
                expiryMinutes = 15;
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var accessToken = tokenHandler.WriteToken(token);

            var refreshToken = GenerateSecureToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            
            await _userRepository.UpdateItemAsync(user.Id.ToString(), user, user.TenantId.ToString());

            return new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }

        private static string GenerateSecureToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
