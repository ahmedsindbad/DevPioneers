// ============================================
// File: DevPioneers.Application/Features/Auth/Commands/CheckUserExistsCommand.cs
// ============================================
using DevPioneers.Application.Common.Models;
using MediatR;

namespace DevPioneers.Application.Features.Auth.Commands;

public record CheckUserExistsCommand(
    string EmailOrMobile
) : IRequest<Result<bool>>;
