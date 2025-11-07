// ============================================
// File: DevPioneers.Application/Features/Payments/Queries/GetPaymentByIdQueryHandler.cs
// Handler for GetPaymentByIdQuery - Retrieves a specific payment with authorization
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Payments.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Application.Features.Payments.Queries;

/// <summary>
/// Handler for retrieving a specific payment by ID
/// Implements authorization: users can only see their own payments, admins can see all
/// </summary>
public class GetPaymentByIdQueryHandler : IRequestHandler<GetPaymentByIdQuery, Result<PaymentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetPaymentByIdQueryHandler> _logger;

    public GetPaymentByIdQueryHandler(
        IApplicationDbContext context,
        ILogger<GetPaymentByIdQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<PaymentDto>> Handle(GetPaymentByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving payment {PaymentId} for user {UserId} (IsAdmin: {IsAdmin})",
                request.PaymentId, request.UserId, request.IsAdmin);

            // Build query with authorization
            var query = _context.Payments
                .Include(p => p.SubscriptionPlan)
                .Where(p => p.Id == request.PaymentId);

            // If not admin, restrict to user's own payments
            if (!request.IsAdmin)
            {
                query = query.Where(p => p.UserId == request.UserId);
            }

            var payment = await query
                .Select(p => new PaymentDto
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
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (payment == null)
            {
                _logger.LogWarning("Payment {PaymentId} not found or user {UserId} is not authorized to access it",
                    request.PaymentId, request.UserId);
                return Result<PaymentDto>.Failure("Payment not found or you are not authorized to access it");
            }

            _logger.LogInformation("Successfully retrieved payment {PaymentId}", request.PaymentId);
            return Result<PaymentDto>.Success(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get payment {PaymentId} for user {UserId}",
                request.PaymentId, request.UserId);
            return Result<PaymentDto>.Failure("An error occurred while retrieving payment details");
        }
    }
}
