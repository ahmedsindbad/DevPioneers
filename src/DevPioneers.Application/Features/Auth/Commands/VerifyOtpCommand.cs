// ============================================
// File: DevPioneers.Application/Features/Auth/Commands/VerifyOtpCommand.cs
// ============================================
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Auth.DTOs;
using MediatR;

namespace DevPioneers.Application.Features.Auth.Commands;

public record VerifyOtpCommand(
    string UserId,
    string OtpCode,
    string? IpAddress = null,
    string? UserAgent = null
) : IRequest<Result<AuthResponseDto>>;