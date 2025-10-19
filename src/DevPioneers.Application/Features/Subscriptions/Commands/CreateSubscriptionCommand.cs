// ============================================
// File: DevPioneers.Application/Features/Subscriptions/Commands/CreateSubscriptionCommand.cs
// ============================================
using DevPioneers.Application.Common.Attributes;
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Subscriptions.DTOs;
using MediatR;

namespace DevPioneers.Application.Features.Subscriptions.Commands;

[RequireSubscriptionManagement]
// [RequireTimeWindow("09:00", "17:00")] // Only during business hours
public record CreateSubscriptionCommand(
    int UserId,
    int SubscriptionPlanId,
    int? PaymentId = null
) : IRequest<Result<SubscriptionDto>>;