// ============================================
// File: DevPioneers.Application/Features/Subscriptions/Queries/GetUserSubscriptionsQuery.cs
// ============================================
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Subscriptions.DTOs;
using MediatR;

namespace DevPioneers.Application.Features.Subscriptions.Queries;

public record GetUserSubscriptionsQuery(int UserId) : IRequest<Result<List<SubscriptionDto>>>;
