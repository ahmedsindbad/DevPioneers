// ============================================
// File: DevPioneers.Application/Features/Subscriptions/Commands/ReactivateSubscriptionCommand.cs
// ============================================
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Subscriptions.DTOs;
using MediatR;

namespace DevPioneers.Application.Features.Subscriptions.Commands;

public record ReactivateSubscriptionCommand(int UserId) : IRequest<Result<SubscriptionDto>>;
