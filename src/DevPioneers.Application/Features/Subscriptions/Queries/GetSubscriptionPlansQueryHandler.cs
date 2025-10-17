
// ============================================
// File: DevPioneers.Application/Features/Subscriptions/Queries/GetSubscriptionPlansQueryHandler.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Subscriptions.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Application.Features.Subscriptions.Queries;

public class GetSubscriptionPlansQueryHandler : IRequestHandler<GetSubscriptionPlansQuery, Result<List<SubscriptionPlanDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetSubscriptionPlansQueryHandler> _logger;

    public GetSubscriptionPlansQueryHandler(
        IApplicationDbContext context,
        ILogger<GetSubscriptionPlansQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<List<SubscriptionPlanDto>>> Handle(GetSubscriptionPlansQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _context.SubscriptionPlans.AsQueryable();

            if (request.ActiveOnly)
            {
                query = query.Where(sp => sp.IsActive);
            }

            var plans = await query
                .OrderBy(sp => sp.DisplayOrder)
                .ThenBy(sp => sp.Price)
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
                    CreatedAtUtc = sp.CreatedAtUtc
                })
                .ToListAsync(cancellationToken);

            return Result<List<SubscriptionPlanDto>>.Success(plans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get subscription plans");
            return Result<List<SubscriptionPlanDto>>.Failure("An error occurred while retrieving subscription plans");
        }
    }
}