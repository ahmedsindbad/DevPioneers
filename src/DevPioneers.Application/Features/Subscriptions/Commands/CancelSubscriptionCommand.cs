// ============================================
// File: DevPioneers.Application/Features/Subscriptions/Commands/CancelSubscriptionCommand.cs
// ============================================
using DevPioneers.Application.Common.Models;
using MediatR;

namespace DevPioneers.Application.Features.Subscriptions.Commands;

public record CancelSubscriptionCommand(
    int UserId,
    int SubscriptionId,
    string? Reason = null
) : IRequest<Result<bool>>;
