// File: DevPioneers.Application/Features/Subscriptions/Queries/GetSubscriptionAnalyticsQueryHandler.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Subscriptions.DTOs;
using DevPioneers.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Application.Features.Subscriptions.Queries;

public class GetSubscriptionAnalyticsQueryHandler : IRequestHandler<GetSubscriptionAnalyticsQuery, Result<SubscriptionAnalyticsDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetSubscriptionAnalyticsQueryHandler> _logger;

    public GetSubscriptionAnalyticsQueryHandler(
        IApplicationDbContext context,
        ILogger<GetSubscriptionAnalyticsQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<SubscriptionAnalyticsDto>> Handle(GetSubscriptionAnalyticsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var subscriptions = await _context.UserSubscriptions
                .Include(us => us.SubscriptionPlan)
                .Where(us => us.CreatedAtUtc >= request.FromDate && us.CreatedAtUtc <= request.ToDate)
                .ToListAsync(cancellationToken);

            var analytics = new SubscriptionAnalyticsDto
            {
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                TotalSubscriptions = subscriptions.Count,
                ActiveSubscriptions = subscriptions.Count(s => s.Status == SubscriptionStatus.Active),
                TrialSubscriptions = subscriptions.Count(s => s.Status == SubscriptionStatus.Trial),
                CancelledSubscriptions = subscriptions.Count(s => s.Status == SubscriptionStatus.Cancelled),
                ExpiredSubscriptions = subscriptions.Count(s => s.Status == SubscriptionStatus.Expired),
                TotalRevenue = subscriptions
                    .Where(s => s.Payment != null && s.Payment.Status == PaymentStatus.Completed)
                    .Sum(s => s.SubscriptionPlan.Price),
                PlanBreakdown = subscriptions
                    .GroupBy(s => new { s.SubscriptionPlan.Id, s.SubscriptionPlan.Name })
                    .Select(g => new SubscriptionPlanAnalyticsDto
                    {
                        PlanId = g.Key.Id,
                        PlanName = g.Key.Name,
                        Count = g.Count(),
                        Revenue = g.Where(s => s.Payment != null && s.Payment.Status == PaymentStatus.Completed)
                                  .Sum(s => s.SubscriptionPlan.Price)
                    })
                    .OrderByDescending(p => p.Count)
                    .ToList()
            };

            return Result<SubscriptionAnalyticsDto>.Success(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get subscription analytics");
            return Result<SubscriptionAnalyticsDto>.Failure("An error occurred while retrieving analytics");
        }
    }
}
