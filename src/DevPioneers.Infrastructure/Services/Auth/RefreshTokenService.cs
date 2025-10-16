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
        
        if (refreshToken == null || !refreshToken.CanBeRefreshed())
            return null;

        // Mark current token as used
        refreshToken.MarkAsUsed(ipAddress);

        // Generate new refresh token
        var newRefreshToken = await GenerateRefreshTokenAsync(
            refreshToken.UserId, 
            refreshToken.UserAgent, 
            ipAddress, 
            refreshToken.DeviceId);

        // Set replacement reference
        refreshToken.ReplacedByToken = newRefreshToken.Token;

        await _context.SaveChangesAsync();

        return newRefreshToken;
    }

    public async Task RevokeRefreshTokenAsync(string token, string? ipAddress = null, string? reason = null)
    {
        var hashedToken = ComputeHash(token);
        
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == hashedToken);

        if (refreshToken != null)
        {
            refreshToken.Revoke(ipAddress, reason ?? "Token manually revoked");
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
            token.Revoke(ipAddress, reason ?? "All tokens revoked for user");
        }

        await _context.SaveChangesAsync();
    }

    public async Task CleanupExpiredTokensAsync()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-7); // Keep revoked tokens for 7 days for audit
        
        var tokensToDelete = await _context.RefreshTokens
            .Where(rt => 
                (rt.IsExpired && rt.ExpiresAt < cutoffDate) ||
                (rt.IsRevoked && rt.RevokedAt < cutoffDate) ||
                (rt.IsUsed && rt.UsedAt < cutoffDate))
            .ToListAsync();

        _context.RefreshTokens.RemoveRange(tokensToDelete);
        await _context.SaveChangesAsync();
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hashBytes);
    }
}