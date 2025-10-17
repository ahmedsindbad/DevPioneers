// ============================================
// File: DevPioneers.Application/Features/Auth/Commands/RefreshTokenCommand.cs
// ============================================
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Auth.DTOs;
using MediatR;

namespace DevPioneers.Application.Features.Auth.Commands;

public record RefreshTokenCommand(
    string RefreshToken,
    string? IpAddress = null,
    string? UserAgent = null
) : IRequest<Result<AuthResponseDto>>;