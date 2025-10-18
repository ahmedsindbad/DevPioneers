// ============================================
// File: DevPioneers.Application/Features/Subscriptions/Queries/GetSubscriptionPlanByIdQueryHandler.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Subscriptions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Application.Features.Subscriptions.Queries;

public class GetSubscriptionPlanByIdQueryHandler : IRequestHandler<GetSubscriptionPlanByIdQuery, Result<SubscriptionPlanDto?>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetSubscriptionPlanByIdQueryHandler> _logger;

    public GetSubscriptionPlanByIdQueryHandler(
        IApplicationDbContext context,
        ILogger<GetSubscriptionPlanByIdQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<SubscriptionPlanDto?>> Handle(GetSubscriptionPlanByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var plan = await _context.SubscriptionPlans
                .Where(sp => sp.Id == request.Id && sp.IsActive)
                .Select(sp => new SubscriptionPlanDto
                {
                    Id = sp.Id,
                    Name = sp.Name,
                    Description = sp.Description,
                    Price = sp.Price,
                    Currency = sp.Currency,
                    BillingCycle = sp.BillingCycle.ToString(),
                    TrialDurationDays = sp.TrialDurationDays,
                    Features = sp.Features,
                    MaxUsers = sp.MaxUsers,
                    MaxStorageGb = sp.MaxStorageGb,
                    PointsAwarded = sp.PointsAwarded,
                    IsActive = sp.IsActive,
                    DisplayOrder = sp.DisplayOrder,
                    DiscountPercentage = sp.DiscountPercentage,
                    CreatedAtUtc = sp.CreatedAtUtc,
                    UpdatedAtUtc = sp.UpdatedAtUtc
                })
                .FirstOrDefaultAsync(cancellationToken);

            return Result<SubscriptionPlanDto?>.Success(plan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get subscription plan {PlanId}", request.Id);
            return Result<SubscriptionPlanDto?>.Failure("An error occurred while retrieving subscription plan");
        }
    }
}
