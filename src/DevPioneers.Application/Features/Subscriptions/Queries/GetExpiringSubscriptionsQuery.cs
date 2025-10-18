// ============================================
// File: DevPioneers.Application/Features/Subscriptions/Queries/GetExpiringSubscriptionsQuery.cs
// ============================================
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Subscriptions.DTOs;
using MediatR;

namespace DevPioneers.Application.Features.Subscriptions.Queries;

public record GetExpiringSubscriptionsQuery(int DaysAhead) : IRequest<Result<List<ExpiringSubscriptionDto>>>;
