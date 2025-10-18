// ============================================
// File: DevPioneers.Application/Features/Subscriptions/Queries/GetUserSubscriptionsQueryHandler.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Subscriptions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Application.Features.Subscriptions.Queries;

public class GetUserSubscriptionsQueryHandler : IRequestHandler<GetUserSubscriptionsQuery, Result<List<SubscriptionDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetUserSubscriptionsQueryHandler> _logger;

    public GetUserSubscriptionsQueryHandler(
        IApplicationDbContext context,
        ILogger<GetUserSubscriptionsQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<List<SubscriptionDto>>> Handle(GetUserSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var subscriptions = await _context.UserSubscriptions
                .Include(us => us.SubscriptionPlan)
                .Include(us => us.Payment)
                .Where(us => us.UserId == request.UserId)
                .OrderByDescending(us => us.CreatedAtUtc)
                .Select(us => new SubscriptionDto
                {
                    Id = us.Id,
                    UserId = us.UserId,
                    PlanId = us.SubscriptionPlanId,
                    PlanName = us.SubscriptionPlan.Name,
                    PlanPrice = us.SubscriptionPlan.Price,
                    Currency = us.SubscriptionPlan.Currency,
                    BillingCycle = us.SubscriptionPlan.BillingCycle.ToString(),
                    Status = us.Status.ToString(),
                    StartDate = us.StartDate,
                    EndDate = us.EndDate,
                    TrialEndDate = us.TrialEndDate,
                    NextBillingDate = us.NextBillingDate,
                    AutoRenewal = us.AutoRenewal,
                    CreatedAtUtc = us.CreatedAtUtc,
                    DaysRemaining = (us.EndDate.Date - DateTime.UtcNow.Date).Days,
                    IsExpiringSoon = (us.EndDate.Date - DateTime.UtcNow.Date).Days <= 7,
                    PaymentId = us.PaymentId
                })
                .ToListAsync(cancellationToken);

            return Result<List<SubscriptionDto>>.Success(subscriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get subscriptions for user {UserId}", request.UserId);
            return Result<List<SubscriptionDto>>.Failure("An error occurred while retrieving user subscriptions");
        }
    }
}
