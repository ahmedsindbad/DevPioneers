// ============================================
// File: DevPioneers.Application/Features/Subscriptions/Commands/CancelSubscriptionCommandHandler.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using DevPioneers.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Application.Features.Subscriptions.Commands;

public class CancelSubscriptionCommandHandler : IRequestHandler<CancelSubscriptionCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CancelSubscriptionCommandHandler> _logger;
    private readonly IDateTime _dateTime;

    public CancelSubscriptionCommandHandler(
        IApplicationDbContext context,
        ILogger<CancelSubscriptionCommandHandler> logger,
        IDateTime dateTime)
    {
        _context = context;
        _logger = logger;
        _dateTime = dateTime;
    }

    public async Task<Result<bool>> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var subscription = await _context.UserSubscriptions
                .FirstOrDefaultAsync(us => us.Id == request.SubscriptionId && 
                    us.UserId == request.UserId, cancellationToken);

            if (subscription == null)
            {
                return Result<bool>.Failure("Subscription not found");
            }

            if (subscription.Status == SubscriptionStatus.Cancelled)
            {
                return Result<bool>.Failure("Subscription is already cancelled");
            }

            if (subscription.Status == SubscriptionStatus.Expired)
            {
                return Result<bool>.Failure("Cannot cancel an expired subscription");
            }

            // Cancel subscription
            subscription.Cancel(request.Reason);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Subscription {SubscriptionId} cancelled for user {UserId}", 
                request.SubscriptionId, request.UserId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel subscription {SubscriptionId} for user {UserId}", 
                request.SubscriptionId, request.UserId);
            return Result<bool>.Failure("An error occurred while cancelling subscription");
        }
    }
}