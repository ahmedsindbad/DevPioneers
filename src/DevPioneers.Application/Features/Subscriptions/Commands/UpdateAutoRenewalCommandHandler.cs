// ============================================
// File: DevPioneers.Application/Features/Subscriptions/Commands/UpdateAutoRenewalCommandHandler.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Subscriptions.DTOs;
using DevPioneers.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Application.Features.Subscriptions.Commands;

public class UpdateAutoRenewalCommandHandler : IRequestHandler<UpdateAutoRenewalCommand, Result<SubscriptionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<UpdateAutoRenewalCommandHandler> _logger;

    public UpdateAutoRenewalCommandHandler(
        IApplicationDbContext context,
        ILogger<UpdateAutoRenewalCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<SubscriptionDto>> Handle(UpdateAutoRenewalCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var subscription = await _context.UserSubscriptions
                .Include(us => us.SubscriptionPlan)
                .Include(us => us.Payment)
                .Where(us => us.UserId == request.UserId && 
                    (us.Status == SubscriptionStatus.Active || us.Status == SubscriptionStatus.Trial))
                .OrderByDescending(us => us.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (subscription == null)
            {
                return Result<SubscriptionDto>.Failure("No active subscription found");
            }

            subscription.AutoRenewal = request.AutoRenewal;
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

            _logger.LogInformation("Auto-renewal updated to {AutoRenewal} for user {UserId}", 
                request.AutoRenewal, request.UserId);

            return Result<SubscriptionDto>.Success(subscriptionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update auto-renewal for user {UserId}", request.UserId);
            return Result<SubscriptionDto>.Failure("An error occurred while updating auto-renewal setting");
        }
    }
}
