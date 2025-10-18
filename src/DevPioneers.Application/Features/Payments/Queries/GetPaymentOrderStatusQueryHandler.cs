// ============================================
// File: DevPioneers.Application/Features/Payments/Queries/GetPaymentOrderStatusQueryHandler.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Payments.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Application.Features.Payments.Queries;

public class GetPaymentOrderStatusQueryHandler : IRequestHandler<GetPaymentOrderStatusQuery, Result<PaymentOrderStatusDto?>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<GetPaymentOrderStatusQueryHandler> _logger;

    public GetPaymentOrderStatusQueryHandler(
        IApplicationDbContext context,
        IPaymentService paymentService,
        ILogger<GetPaymentOrderStatusQueryHandler> logger)
    {
        _context = context;
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task<Result<PaymentOrderStatusDto?>> Handle(GetPaymentOrderStatusQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.SubscriptionPlan)
                .FirstOrDefaultAsync(p => p.PaymobOrderId == request.OrderId && p.UserId == request.UserId, cancellationToken);

            if (payment == null)
            {
                return Result<PaymentOrderStatusDto?>.Success(null);
            }

            // Get latest status from Paymob
            var paymentStatus = await _paymentService.GetPaymentStatusAsync(request.OrderId, cancellationToken);

            var statusDto = new PaymentOrderStatusDto
            {
                PaymentId = payment.Id,
                PaymobOrderId = payment.PaymobOrderId ?? string.Empty,
                Status = paymentStatus.Status,
                Amount = payment.Amount,
                Currency = payment.Currency,
                Description = payment.Description,
                CreatedAt = payment.CreatedAtUtc,
                CompletedAt = paymentStatus.CompletedAt,
                SubscriptionPlanName = payment.SubscriptionPlan?.Name,
                ErrorMessage = paymentStatus.ErrorMessage
            };

            return Result<PaymentOrderStatusDto?>.Success(statusDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get payment order status for {OrderId}", request.OrderId);
            return Result<PaymentOrderStatusDto?>.Failure("An error occurred while retrieving payment status");
        }
    }
}