// ============================================
// File: DevPioneers.Application/Features/Payments/Commands/VerifyPaymobCallbackCommandHandler.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Payments.DTOs;
using DevPioneers.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Application.Features.Payments.Commands;

public class VerifyPaymobCallbackCommandHandler : IRequestHandler<VerifyPaymobCallbackCommand, Result<PaymentVerificationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<VerifyPaymobCallbackCommandHandler> _logger;
    private readonly IDateTime _dateTime;
    private readonly IEmailService _emailService;

    public VerifyPaymobCallbackCommandHandler(
        IApplicationDbContext context,
        IPaymentService paymentService,
        ILogger<VerifyPaymobCallbackCommandHandler> logger,
        IDateTime dateTime,
        IEmailService emailService)
    {
        _context = context;
        _paymentService = paymentService;
        _logger = logger;
        _dateTime = dateTime;
        _emailService = emailService;
    }

    public async Task<Result<PaymentVerificationDto>> Handle(VerifyPaymobCallbackCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Find payment by Paymob order ID
            var payment = await _context.Payments
                .Include(p => p.User)
                .Include(p => p.SubscriptionPlan)
                .FirstOrDefaultAsync(p => p.PaymobOrderId == request.PaymobOrderId, cancellationToken);

            if (payment == null)
            {
                _logger.LogWarning("Payment not found for Paymob order {PaymobOrderId}", request.PaymobOrderId);
                return Result<PaymentVerificationDto>.Failure("Payment not found");
            }

            // Verify callback with Paymob service
            var callbackData = new PaymentCallbackData(
                PaymobOrderId: request.PaymobOrderId,
                PaymobTransactionId: request.PaymobTransactionId,
                Status: request.Status,
                Amount: request.Amount,
                Currency: request.Currency,
                AdditionalData: request.AdditionalData
            );

            var verificationResult = await _paymentService.VerifyCallbackAsync(callbackData, cancellationToken);

            if (!verificationResult.IsValid)
            {
                _logger.LogError("Invalid payment callback for order {PaymobOrderId}: {Error}", 
                    request.PaymobOrderId, verificationResult.ErrorMessage);
                return Result<PaymentVerificationDto>.Failure("Invalid payment callback");
            }

            // Update payment status
            if (verificationResult.IsSuccess)
            {
                payment.MarkAsCompleted(request.PaymobTransactionId);
                
                // Send payment receipt email
                try
                {
                    await _emailService.SendPaymentReceiptEmailAsync(
                        payment.User.Email,
                        payment.User.FullName,
                        $"Payment of {payment.Amount:C} {payment.Currency} completed successfully",
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send payment receipt email for payment {PaymentId}", payment.Id);
                }
            }
            else
            {
                payment.MarkAsFailed(verificationResult.ErrorMessage ?? "Payment verification failed");
            }

            await _context.SaveChangesAsync(cancellationToken);

            var verificationDto = new PaymentVerificationDto
            {
                PaymentId = payment.Id,
                PaymobOrderId = payment.PaymobOrderId ?? string.Empty,
                PaymobTransactionId = payment.PaymobTransactionId,
                IsSuccess = verificationResult.IsSuccess,
                Amount = payment.Amount,
                Currency = payment.Currency,
                Status = payment.Status.ToString(),
                VerifiedAt = _dateTime.UtcNow
            };

            _logger.LogInformation("Payment {PaymentId} verification completed. Success: {IsSuccess}", 
                payment.Id, verificationResult.IsSuccess);

            return Result<PaymentVerificationDto>.Success(verificationDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify payment callback for order {PaymobOrderId}", request.PaymobOrderId);
            return Result<PaymentVerificationDto>.Failure("An error occurred during payment verification");
        }
    }
}