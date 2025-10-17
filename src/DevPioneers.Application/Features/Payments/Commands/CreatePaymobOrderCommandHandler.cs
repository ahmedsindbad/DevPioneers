// ============================================
// File: DevPioneers.Application/Features/Payments/Commands/CreatePaymobOrderCommandHandler.cs
// ============================================
using DevPioneers.Application.Common.Exceptions;
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Payments.DTOs;
using DevPioneers.Domain.Entities;
using DevPioneers.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Application.Features.Payments.Commands;

public class CreatePaymobOrderCommandHandler : IRequestHandler<CreatePaymobOrderCommand, Result<PaymobOrderDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<CreatePaymobOrderCommandHandler> _logger;
    private readonly IDateTime _dateTime;

    public CreatePaymobOrderCommandHandler(
        IApplicationDbContext context,
        IPaymentService paymentService,
        ILogger<CreatePaymobOrderCommandHandler> logger,
        IDateTime dateTime)
    {
        _context = context;
        _paymentService = paymentService;
        _logger = logger;
        _dateTime = dateTime;
    }

    public async Task<Result<PaymobOrderDto>> Handle(CreatePaymobOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Verify user exists
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null)
            {
                return Result<PaymobOrderDto>.Failure("User not found");
            }

            // Verify subscription plan if provided
            SubscriptionPlan? subscriptionPlan = null;
            if (request.SubscriptionPlanId.HasValue)
            {
                subscriptionPlan = await _context.SubscriptionPlans
                    .FirstOrDefaultAsync(sp => sp.Id == request.SubscriptionPlanId.Value && sp.IsActive, 
                        cancellationToken);

                if (subscriptionPlan == null)
                {
                    return Result<PaymobOrderDto>.Failure("Subscription plan not found or not active");
                }

                // Verify amount matches plan price
                if (Math.Abs(request.Amount - subscriptionPlan.Price) > 0.01m)
                {
                    return Result<PaymobOrderDto>.Failure("Amount does not match subscription plan price");
                }
            }

            // Create payment record
            var payment = new Payment
            {
                UserId = request.UserId,
                SubscriptionPlanId = request.SubscriptionPlanId,
                Amount = request.Amount,
                Currency = request.Currency,
                Status = PaymentStatus.Pending,
                PaymentMethod = PaymentMethod.CreditCard, // Default to credit card
                Description = request.Description,
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent,
                CreatedAtUtc = _dateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync(cancellationToken);

            // Create Paymob order
            var createOrderRequest = new CreatePaymentOrderRequest(
                UserId: request.UserId,
                Amount: request.Amount,
                Currency: request.Currency,
                Description: request.Description,
                SubscriptionPlanId: request.SubscriptionPlanId,
                Metadata: new Dictionary<string, object>
                {
                    { "payment_id", payment.Id },
                    { "user_email", user.Email },
                    { "user_name", user.FullName }
                }
            );

            var paymobResult = await _paymentService.CreateOrderAsync(createOrderRequest, cancellationToken);

            if (!paymobResult.Success || string.IsNullOrEmpty(paymobResult.PaymobOrderId))
            {
                payment.MarkAsFailed(paymobResult.ErrorMessage ?? "Failed to create Paymob order");
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogError("Failed to create Paymob order for payment {PaymentId}: {Error}", 
                    payment.Id, paymobResult.ErrorMessage);

                return Result<PaymobOrderDto>.Failure(paymobResult.ErrorMessage ?? "Failed to create payment order");
            }

            // Update payment with Paymob order ID
            payment.PaymobOrderId = paymobResult.PaymobOrderId;
            payment.Status = PaymentStatus.Processing;
            await _context.SaveChangesAsync(cancellationToken);

            var orderDto = new PaymobOrderDto
            {
                PaymentId = payment.Id,
                PaymobOrderId = paymobResult.PaymobOrderId,
                PaymentUrl = paymobResult.PaymentUrl ?? string.Empty,
                Amount = request.Amount,
                Currency = request.Currency,
                Description = request.Description,
                Status = "Processing",
                ExpiresAt = _dateTime.UtcNow.AddMinutes(30), // Paymob orders typically expire in 30 minutes
                CreatedAt = payment.CreatedAtUtc
            };

            _logger.LogInformation("Paymob order created successfully for payment {PaymentId}, order {PaymobOrderId}", 
                payment.Id, paymobResult.PaymobOrderId);

            return Result<PaymobOrderDto>.Success(orderDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Paymob order for user {UserId}", request.UserId);
            return Result<PaymobOrderDto>.Failure("An error occurred while creating payment order");
        }
    }
}