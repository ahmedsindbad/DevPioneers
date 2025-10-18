
// ============================================
// File: DevPioneers.Application/Features/Subscriptions/Commands/ReactivateSubscriptionCommandHandler.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Subscriptions.DTOs;
using DevPioneers.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Application.Features.Subscriptions.Commands;

public class ReactivateSubscriptionCommandHandler : IRequestHandler<ReactivateSubscriptionCommand, Result<SubscriptionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ReactivateSubscriptionCommandHandler> _logger;
    private readonly IDateTime _dateTime;

    public ReactivateSubscriptionCommandHandler(
        IApplicationDbContext context,
        ILogger<ReactivateSubscriptionCommandHandler> logger,
        IDateTime dateTime)
    {
        _context = context;
        _logger = logger;
        _dateTime = dateTime;
    }

    public async Task<Result<SubscriptionDto>> Handle(ReactivateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var subscription = await _context.UserSubscriptions
                .Include(us => us.SubscriptionPlan)
                .Include(us => us.Payment)
                .Where(us => us.UserId == request.UserId && us.Status == SubscriptionStatus.Cancelled)
                .OrderByDescending(us => us.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (subscription == null)
            {
                return Result<SubscriptionDto>.Failure("No cancelled subscription found to reactivate");
            }

            // Check if subscription is still within valid period
            if (subscription.EndDate < _dateTime.UtcNow)
            {
                return Result<SubscriptionDto>.Failure("Cannot reactivate expired subscription");
            }

            // Reactivate subscription
            subscription.Status = SubscriptionStatus.Active;
            subscription.CancelledAt = null;
            subscription.CancellationReason = null;
            subscription.AutoRenewal = true;

            await _context.SaveChangesAsync(cancellationToken);

            var subscriptionDto = new SubscriptionDto
            {
                Id = subscription.Id,
                UserId = subscription.UserId,
                PlanId = subscription.SubscriptionPlanId,
                PlanName = subscription.SubscriptionPlan.Name,
                PlanPrice = subscription.SubscriptionPlan.Price,
                Currency = subscription.SubscriptionPlan.Currency,
                BillingCycle = subscription.SubscriptionPlan.BillingCycle.ToString(),
                Status = subscription.Status.ToString(),
                StartDate = subscription.StartDate,
                EndDate = subscription.EndDate,
                TrialEndDate = subscription.TrialEndDate,
                NextBillingDate = subscription.NextBillingDate,
                AutoRenewal = subscription.AutoRenewal,
                CreatedAtUtc = subscription.CreatedAtUtc,
                DaysRemaining = (subscription.EndDate.Date - DateTime.UtcNow.Date).Days,
                IsExpiringSoon = (subscription.EndDate.Date - DateTime.UtcNow.Date).Days <= 7,
                PaymentId = subscription.PaymentId
            };

            _logger.LogInformation("Subscription {SubscriptionId} reactivated for user {UserId}", 
                subscription.Id, request.UserId);

            return Result<SubscriptionDto>.Success(subscriptionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reactivate subscription for user {UserId}", request.UserId);
            return Result<SubscriptionDto>.Failure("An error occurred while reactivating subscription");
        }
    }
}
