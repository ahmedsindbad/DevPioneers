// ============================================
// File: DevPioneers.Application/Features/Subscriptions/Commands/UpdateAutoRenewalCommand.cs
// ============================================
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Subscriptions.DTOs;
using MediatR;

namespace DevPioneers.Application.Features.Subscriptions.Commands;

public record UpdateAutoRenewalCommand(int UserId, bool AutoRenewal) : IRequest<Result<SubscriptionDto>>;
