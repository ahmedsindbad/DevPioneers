// ============================================
// File: DevPioneers.Application/Features/Subscriptions/Queries/GetUserSubscriptionsQuery.cs
// ============================================
using DevPioneers.Application.Common.Attributes;
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Subscriptions.DTOs;
using MediatR;

namespace DevPioneers.Application.Features.Subscriptions.Queries;
[RequireWalletAccess]
[RequireOwnership("UserId")]
public record GetUserSubscriptionsQuery(int UserId) : IRequest<Result<List<SubscriptionDto>>>;
