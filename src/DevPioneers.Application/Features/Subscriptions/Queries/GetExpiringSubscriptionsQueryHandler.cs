// ============================================
// File: DevPioneers.Application/Features/Subscriptions/Queries/GetExpiringSubscriptionsQueryHandler.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Subscriptions.DTOs;
using DevPioneers.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Application.Features.Subscriptions.Queries;

public class GetExpiringSubscriptionsQueryHandler : IRequestHandler<GetExpiringSubscriptionsQuery, Result<List<ExpiringSubscriptionDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetExpiringSubscriptionsQueryHandler> _logger;
    private readonly IDateTime _dateTime;

    public GetExpiringSubscriptionsQueryHandler(
        IApplicationDbContext context,
        ILogger<GetExpiringSubscriptionsQueryHandler> logger,
        IDateTime dateTime)
    {
        _context = context;
        _logger = logger;
        _dateTime = dateTime;
    }

    public async Task<Result<List<ExpiringSubscriptionDto>>> Handle(GetExpiringSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var cutoffDate = _dateTime.UtcNow.AddDays(request.DaysAhead);

            var expiringSubscriptions = await _context.UserSubscriptions
                .Include(us => us.User)
                .Include(us => us.SubscriptionPlan)
                .Where(us => (us.Status == SubscriptionStatus.Active || us.Status == SubscriptionStatus.Trial) &&
                            us.EndDate <= cutoffDate &&
                            us.EndDate > _dateTime.UtcNow)
                .OrderBy(us => us.EndDate)
                .Select(us => new ExpiringSubscriptionDto
                {
                    SubscriptionId = us.Id,
                    UserId = us.UserId,
                    UserName = us.User.FullName,
                    UserEmail = us.User.Email,
                    PlanName = us.SubscriptionPlan.Name,
                    EndDate = us.EndDate,
                    DaysUntilExpiry = (int)(us.EndDate.Date - _dateTime.UtcNow.Date).TotalDays,
                    AutoRenewal = us.AutoRenewal,
                    Status = us.Status.ToString()
                })
                .ToListAsync(cancellationToken);

            return Result<List<ExpiringSubscriptionDto>>.Success(expiringSubscriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get expiring subscriptions");
            return Result<List<ExpiringSubscriptionDto>>.Failure("An error occurred while retrieving expiring subscriptions");
        }
    }
}
