// ============================================
// File: DevPioneers.Application/Features/Payments/Commands/ProcessRefundCommandHandler.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Payments.DTOs;
using DevPioneers.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Application.Features.Payments.Commands;

public class ProcessRefundCommandHandler : IRequestHandler<ProcessRefundCommand, Result<RefundDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<ProcessRefundCommandHandler> _logger;
    private readonly IDateTime _dateTime;

    public ProcessRefundCommandHandler(
        IApplicationDbContext context,
        IPaymentService paymentService,
        ILogger<ProcessRefundCommandHandler> logger,
        IDateTime dateTime)
    {
        _context = context;
        _paymentService = paymentService;
        _logger = logger;
        _dateTime = dateTime;
    }

    public async Task<Result<RefundDto>> Handle(ProcessRefundCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Find payment
            var payment = await _context.Payments
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == request.PaymentId, cancellationToken);

            if (payment == null)
            {
                return Result<RefundDto>.Failure("Payment not found");
            }

            if (payment.Status != PaymentStatus.Completed)
            {
                return Result<RefundDto>.Failure("Can only refund completed payments");
            }

            if (string.IsNullOrEmpty(payment.PaymobTransactionId))
            {
                return Result<RefundDto>.Failure("Payment transaction ID not found");
            }

            if (request.Amount > payment.Amount)
            {
                return Result<RefundDto>.Failure("Refund amount cannot exceed payment amount");
            }

            // Process refund with Paymob
            var refundRequest = new ProcessRefundRequest(
                PaymobTransactionId: payment.PaymobTransactionId,
                Amount: request.Amount,
                Reason: request.Reason
            );

            var refundResult = await _paymentService.ProcessRefundAsync(refundRequest, cancellationToken);

            if (!refundResult.Success)
            {
                _logger.LogError("Refund failed for payment {PaymentId}: {Error}", 
                    request.PaymentId, refundResult.ErrorMessage);
                return Result<RefundDto>.Failure(refundResult.ErrorMessage ?? "Refund processing failed");
            }

            // Update payment record
            payment.ProcessRefund(request.Amount, request.Reason);
            await _context.SaveChangesAsync(cancellationToken);

            var refundDto = new RefundDto
            {
                PaymentId = payment.Id,
                RefundId = refundResult.RefundId ?? string.Empty,
                Amount = request.Amount,
                Currency = payment.Currency,
                Reason = request.Reason,
                Status = "Processed",
                ProcessedAt = _dateTime.UtcNow,
                ProcessedByUserId = request.ProcessedByUserId
            };

            _logger.LogInformation("Refund processed successfully for payment {PaymentId}, amount {Amount}", 
                request.PaymentId, request.Amount);

            return Result<RefundDto>.Success(refundDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process refund for payment {PaymentId}", request.PaymentId);
            return Result<RefundDto>.Failure("An error occurred while processing refund");
        }
    }
}