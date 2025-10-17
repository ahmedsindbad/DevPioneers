// ============================================
// File: DevPioneers.Application/Features/Subscriptions/Queries/GetSubscriptionPlansQuery.cs
// ============================================
using DevPioneers.Application.Common.Behaviors;
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Subscriptions.DTOs;
using MediatR;

namespace DevPioneers.Application.Features.Subscriptions.Queries;

public record GetSubscriptionPlansQuery(
    bool ActiveOnly = true
) : IRequest<Result<List<SubscriptionPlanDto>>>, ICacheableQuery
{
    public string CacheKey => $"subscription-plans-{(ActiveOnly ? "active" : "all")}";
    public TimeSpan? CacheExpiry => TimeSpan.FromHours(1);
}