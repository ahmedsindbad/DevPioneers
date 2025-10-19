// ============================================
// File: DevPioneers.Application/Features/Auth/Commands/ResendEmailVerificationCommand.cs
// ============================================
using DevPioneers.Application.Common.Models;
using MediatR;

namespace DevPioneers.Application.Features.Auth.Commands;

public record ResendEmailVerificationCommand(
    string Email,
    string? IpAddress = null
) : IRequest<Result<bool>>;