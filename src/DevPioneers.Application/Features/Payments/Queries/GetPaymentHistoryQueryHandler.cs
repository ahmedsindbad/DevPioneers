// ============================================
// File: DevPioneers.Application/Features/Payments/Queries/GetPaymentHistoryQueryHandler.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Payments.DTOs;
using DevPioneers.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Application.Features.Payments.Queries;

public class GetPaymentHistoryQueryHandler : IRequestHandler<GetPaymentHistoryQuery, Result<PaginatedList<PaymentDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetPaymentHistoryQueryHandler> _logger;

    public GetPaymentHistoryQueryHandler(
        IApplicationDbContext context,
        ILogger<GetPaymentHistoryQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<PaginatedList<PaymentDto>>> Handle(GetPaymentHistoryQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _context.Payments
                .Include(p => p.SubscriptionPlan)
                .Where(p => p.UserId == request.UserId);

            // Apply filters
            if (request.Status.HasValue)
            {
                query = query.Where(p => p.Status == request.Status.Value);
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(p => p.CreatedAtUtc >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(p => p.CreatedAtUtc <= request.ToDate.Value);
            }

            // Order by creation date (newest first)
            query = query.OrderByDescending(p => p.CreatedAtUtc);

            var paymentsQuery = query.Select(p => new PaymentDto
            {
                Id = p.Id,
                UserId = p.UserId,
                Amount = p.Amount,
                Currency = p.Currency,
                Status = p.Status.ToString(),
                PaymentMethod = p.PaymentMethod.ToString(),
                Description = p.Description,
                PaymobOrderId = p.PaymobOrderId,
                PaymobTransactionId = p.PaymobTransactionId,
                PaidAt = p.PaidAt,
                FailedAt = p.FailedAt,
                FailureReason = p.FailureReason,
                RefundedAt = p.RefundedAt,
                RefundAmount = p.RefundAmount,
                RefundReason = p.RefundReason,
                SubscriptionPlanId = p.SubscriptionPlanId,
                SubscriptionPlanName = p.SubscriptionPlan != null ? p.SubscriptionPlan.Name : null,
                CreatedAtUtc = p.CreatedAtUtc
            });

            var paginatedPayments = await PaginatedList<PaymentDto>.CreateAsync(
                paymentsQuery, request.PageNumber, request.PageSize, cancellationToken);

            return Result<PaginatedList<PaymentDto>>.Success(paginatedPayments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get payment history for user {UserId}", request.UserId);
            return Result<PaginatedList<PaymentDto>>.Failure("An error occurred while retrieving payment history");
        }
    }
}
