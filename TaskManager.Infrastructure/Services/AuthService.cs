using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TaskManager.Application.DTOs;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Options;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using TaskManager.Infrastructure.Data;

namespace TaskManager.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly JwtOptions _jwtOptions;
        private readonly ILogger<AuthService> _logger;

        public AuthService(AppDbContext context, IOptions<JwtOptions> jwtOptions, ILogger<AuthService> logger)
        {
            _context = context;
            _jwtOptions = jwtOptions.Value;
            _logger = logger;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                throw new ArgumentException("Email is already registered.");

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = UserRole.User
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();


            var accessToken = GenerateJwtToken(user);

            var refreshToken = GenerateRefreshToken(user.UserId);
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New user registerd successfully with id {UserId}", user.UserId);

            return new AuthResponseDto 
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiresAt = refreshToken.ExpiresAt,
                Role = user.Role.ToString() 
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new ArgumentException("Invalid email or password.");

            var accessToken = GenerateJwtToken(user);

            var refreshToken = GenerateRefreshToken(user.UserId);
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User with {id} logged successfully", user.UserId);

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token,
                RefreshTokenExpiresAt = refreshToken.ExpiresAt,
                Role = user.Role.ToString()
            };
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
        {
            var storedRefreshToken = await _context.RefreshTokens
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Token == refreshToken);

            if (storedRefreshToken == null || !storedRefreshToken.IsActive)
            {
                _logger.LogWarning("Failed token refresh attempt for TokenId: {TokenId}, IsRevoked: {IsRevoked}",
                    storedRefreshToken?.RefreshTokenId, storedRefreshToken?.IsRevoked);

                throw new UnauthorizedAccessException("Invalid or expired refresh token.");
            }

            // Refresh Token Rotation
            storedRefreshToken.IsRevoked = true;
            storedRefreshToken.RevokedAt = DateTime.UtcNow;

            var newAccessToken = GenerateJwtToken(storedRefreshToken.User);

            var newRefreshToken = GenerateRefreshToken(storedRefreshToken.UserId);
            _context.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully refreshed token for User {UserId}. New TokenId: {NewTokenId}",
                storedRefreshToken.UserId, newRefreshToken.RefreshTokenId);

            return new AuthResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken.Token,
                RefreshTokenExpiresAt = newRefreshToken.ExpiresAt,
                Role = storedRefreshToken.User.Role.ToString()
            };
        }

        public async Task RevokeTokenAsync(string refreshToken, int userId)
        {
            var storedRefreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(r => r.Token == refreshToken && r.UserId == userId);

            if (storedRefreshToken == null || !storedRefreshToken.IsActive)
            {
                _logger.LogWarning("Revoke token failed. Token not found or already revoked for user {UserId}", userId);

                throw new UnauthorizedAccessException("Invalid or expired refresh token.");
            }

            storedRefreshToken.IsRevoked = true;
            storedRefreshToken.RevokedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Token {TokenId} successfully revoked for User {UserId}", storedRefreshToken.RefreshTokenId, userId);
        }

        private string GenerateJwtToken(User user)
        {
            var securitySigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = _jwtOptions.Issuer,
                Audience = _jwtOptions.Audience,
                SigningCredentials = new SigningCredentials(securitySigningKey, SecurityAlgorithms.HmacSha256),
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes),
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        private RefreshToken GenerateRefreshToken(int userId)
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);

            return new RefreshToken
            {
                Token = Convert.ToBase64String(randomNumber),
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
            };
        }
    }
}
