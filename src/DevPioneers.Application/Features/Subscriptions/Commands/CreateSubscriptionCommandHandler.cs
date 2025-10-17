// ============================================
// File: DevPioneers.Application/Features/Subscriptions/Commands/CreateSubscriptionCommandHandler.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Subscriptions.DTOs;
using DevPioneers.Domain.Entities;
using DevPioneers.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Application.Features.Subscriptions.Commands;

public class CreateSubscriptionCommandHandler : IRequestHandler<CreateSubscriptionCommand, Result<SubscriptionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CreateSubscriptionCommandHandler> _logger;
    private readonly IDateTime _dateTime;

    public CreateSubscriptionCommandHandler(
        IApplicationDbContext context,
        ILogger<CreateSubscriptionCommandHandler> logger,
        IDateTime dateTime)
    {
        _context = context;
        _logger = logger;
        _dateTime = dateTime;
    }

    public async Task<Result<SubscriptionDto>> Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Verify user exists
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null)
            {
                return Result<SubscriptionDto>.Failure("User not found");
            }

            // Verify subscription plan exists and is active
            var subscriptionPlan = await _context.SubscriptionPlans
                .FirstOrDefaultAsync(sp => sp.Id == request.SubscriptionPlanId && sp.IsActive, 
                    cancellationToken);

            if (subscriptionPlan == null)
            {
                return Result<SubscriptionDto>.Failure("Subscription plan not found or not active");
            }

            // Check if user already has an active subscription
            var existingSubscription = await _context.UserSubscriptions
                .FirstOrDefaultAsync(us => us.UserId == request.UserId && 
                    (us.Status == SubscriptionStatus.Active || us.Status == SubscriptionStatus.Trial),
                    cancellationToken);

            if (existingSubscription != null)
            {
                return Result<SubscriptionDto>.Failure("User already has an active subscription");
            }

            // Verify payment if provided
            Payment? payment = null;
            if (request.PaymentId.HasValue)
            {
                payment = await _context.Payments
                    .FirstOrDefaultAsync(p => p.Id == request.PaymentId.Value && 
                        p.UserId == request.UserId &&
                        p.Status == PaymentStatus.Completed,
                        cancellationToken);

                if (payment == null)
                {
                    return Result<SubscriptionDto>.Failure("Payment not found or not completed");
                }
            }

            // Calculate subscription dates
            var startDate = _dateTime.UtcNow;
            var trialEndDate = subscriptionPlan.TrialDurationDays > 0 ? 
                startDate.AddDays(subscriptionPlan.TrialDurationDays) : null;
            
            var endDate = subscriptionPlan.BillingCycle switch
            {
                BillingCycle.Monthly => startDate.AddMonths(1),
                BillingCycle.Yearly => startDate.AddYears(1),
                BillingCycle.Weekly => startDate.AddDays(7),
                _ => startDate.AddMonths(1)
            };

            // Create subscription
            var subscription = new UserSubscription
            {
                UserId = request.UserId,
                SubscriptionPlanId = request.SubscriptionPlanId,
                PaymentId = request.PaymentId,
                Status = payment != null ? SubscriptionStatus.Active : 
                         (trialEndDate.HasValue ? SubscriptionStatus.Trial : SubscriptionStatus.PendingPayment),
                StartDate = startDate,
                EndDate = endDate,
                TrialEndDate = trialEndDate,
                NextBillingDate = payment != null ? endDate : trialEndDate,
                AutoRenewal = true,
                CreatedAtUtc = _dateTime.UtcNow
            };

            _context.UserSubscriptions.Add(subscription);

            // Award points to user's wallet if payment was made
            if (payment != null && subscriptionPlan.PointsAwarded > 0)
            {
                var wallet = await _context.Wallets
                    .FirstOrDefaultAsync(w => w.UserId == request.UserId, cancellationToken);

                if (wallet != null)
                {
                    wallet.AddPoints(subscriptionPlan.PointsAwarded, 
                        $"Points awarded for {subscriptionPlan.Name} subscription");

                    var transaction = new WalletTransaction
                    {
                        WalletId = wallet.Id,
                        Type = TransactionType.PointsCredit,
                        Amount = subscriptionPlan.PointsAwarded,
                        Currency = "PTS",
                        BalanceBefore = wallet.Points - subscriptionPlan.PointsAwarded,
                        BalanceAfter = wallet.Points,
                        Description = $"Points awarded for {subscriptionPlan.Name} subscription",
                        RelatedEntityType = "UserSubscription",
                        RelatedEntityId = subscription.Id,
                        CreatedAtUtc = _dateTime.UtcNow
                    };

                    _context.WalletTransactions.Add(transaction);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Reload subscription with related data
            var createdSubscription = await _context.UserSubscriptions
                .Include(us => us.SubscriptionPlan)
                .Include(us => us.Payment)
                .FirstAsync(us => us.Id == subscription.Id, cancellationToken);

            var subscriptionDto = MapToDto(createdSubscription);

            _logger.LogInformation("Subscription created successfully for user {UserId}, plan {PlanId}", 
                request.UserId, request.SubscriptionPlanId);

            return Result<SubscriptionDto>.Success(subscriptionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create subscription for user {UserId}", request.UserId);
            return Result<SubscriptionDto>.Failure("An error occurred while creating subscription");
        }
    }

    private static SubscriptionDto MapToDto(UserSubscription subscription)
    {
        return new SubscriptionDto
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
    }
}