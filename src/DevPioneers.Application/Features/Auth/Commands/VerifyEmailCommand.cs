
// ============================================
// File: DevPioneers.Application/Features/Auth/Commands/VerifyEmailCommand.cs
// ============================================
using DevPioneers.Application.Common.Models;
using MediatR;

namespace DevPioneers.Application.Features.Auth.Commands;

public record VerifyEmailCommand(
    string Token,
    string? IpAddress = null
) : IRequest<Result<bool>>;
