// ============================================
// File: DevPioneers.Application/Features/Auth/Commands/RegisterCommand.cs
// ============================================
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Auth.DTOs;
using MediatR;

namespace DevPioneers.Application.Features.Auth.Commands;

public record RegisterCommand(
    string FullName,
    string Email,
    string? Mobile,
    string Password,
    string? IpAddress = null,
    string? UserAgent = null
) : IRequest<Result<AuthResponseDto>>;