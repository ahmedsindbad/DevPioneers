// DevPioneers.Infrastructure/Services/Auth/RefreshTokenService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using DevPioneers.Infrastructure.Configurations;
using DevPioneers.Domain.Entities;
using DevPioneers.Application.Common.Interfaces;

namespace DevPioneers.Infrastructure.Services.Auth;

public interface IRefreshTokenService
{
    Task<RefreshToken> GenerateRefreshTokenAsync(int userId, string? userAgent = null, string? ipAddress = null, string? deviceId = null);
    Task<RefreshToken?> GetValidRefreshTokenAsync(string token);
    Task<RefreshToken?> RefreshTokenAsync(string token, string? ipAddress = null);
    Task RevokeRefreshTokenAsync(string token, string? ipAddress = null, string? reason = null);
    Task RevokeAllUserRefreshTokensAsync(int userId, string? ipAddress = null, string? reason = null);
    Task CleanupExpiredTokensAsync();
}

public class RefreshTokenService : IRefreshTokenService
{
    private readonly IApplicationDbContext _context;
    private readonly JwtSettings _jwtSettings;

    public RefreshTokenService(IApplicationDbContext context, IOptions<JwtSettings> jwtSettings)
    {
        _context = context;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<RefreshToken> GenerateRefreshTokenAsync(int userId, string? userAgent = null, string? ipAddress = null, string? deviceId = null)
    {
        // Generate random token
        var tokenBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(tokenBytes);
        var token = Convert.ToBase64String(tokenBytes);

        // Hash the token for storage
        var hashedToken = ComputeHash(token);

        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = hashedToken, // Store hashed version
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ipAddress,
            UserAgent = userAgent,
            DeviceId = deviceId
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        // Return the original token (not hashed) for client
        refreshToken.Token = token;
        return refreshToken;
    }

    public async Task<RefreshToken?> GetValidRefreshTokenAsync(string token)
    {
        var hashedToken = ComputeHash(token);
        
        var refreshToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == hashedToken);

        return refreshToken?.IsActive == true ? refreshToken : null;
    }

   public async Task<RefreshToken?> RefreshTokenAsync(string token, string? ipAddress = null)
    {
        var refreshToken = await GetValidRefreshTokenAsync(token);
        if (refreshToken == null)
            return null;

        // Revoke current token
        await RevokeRefreshTokenAsync(token, ipAddress, "Replaced by new token");

        // Generate new token
        return await GenerateRefreshTokenAsync(
            refreshToken.UserId, 
            refreshToken.UserAgent, 
            ipAddress, 
            refreshToken.DeviceId
        );
    }

    public async Task RevokeRefreshTokenAsync(string token, string? ipAddress = null, string? reason = null)
    {
        var hashedToken = ComputeHash(token);
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == hashedToken);

        if (refreshToken != null && refreshToken.IsActive)
        {
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedByIp = ipAddress;
            refreshToken.RevokedReason = reason;
            await _context.SaveChangesAsync();
        }
    }

    public async Task RevokeAllUserRefreshTokensAsync(int userId, string? ipAddress = null, string? reason = null)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.IsActive)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            token.RevokedReason = reason ?? "Revoked all tokens";
        }

        await _context.SaveChangesAsync();
    }

    public async Task CleanupExpiredTokensAsync()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-90); // Keep for 90 days for audit
        var expiredTokens = await _context.RefreshTokens
            .Where(rt => rt.ExpiresAt < cutoffDate)
            .ToListAsync();

        _context.RefreshTokens.RemoveRange(expiredTokens);
        await _context.SaveChangesAsync();
    }

    private string ComputeHash(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}