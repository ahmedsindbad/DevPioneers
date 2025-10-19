// ============================================
// File: DevPioneers.Application/Features/Auth/Commands/VerifyMobileCommand.cs
// ============================================
using DevPioneers.Application.Common.Models;
using MediatR;

namespace DevPioneers.Application.Features.Auth.Commands;

public record VerifyMobileCommand(
    string Mobile,
    string OtpCode,
    string? IpAddress = null
) : IRequest<Result<bool>>;
