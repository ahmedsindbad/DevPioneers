// ============================================
// File: DevPioneers.Application/Features/Auth/Commands/SendOtpCommand.cs
// Updated with OtpPurpose enum
// ============================================
using DevPioneers.Application.Common.Models;
using DevPioneers.Domain.Enums;
using MediatR;

namespace DevPioneers.Application.Features.Auth.Commands;

public record SendOtpCommand(
    string EmailOrMobile,
    OtpPurpose Purpose = OtpPurpose.Login,
    string? IpAddress = null
) : IRequest<Result<string>>;