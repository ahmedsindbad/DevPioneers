// ============================================
// File: DevPioneers.Application/Common/Mappings/PaymentMappings.cs
// ============================================
using DevPioneers.Application.Features.Payments.DTOs;
using DevPioneers.Domain.Entities;

namespace DevPioneers.Application.Common.Mappings;

public static class PaymentMappings
{
    public static PaymentDto ToDto(this Payment payment)
    {
        return new PaymentDto
        {
            Id = payment.Id,
            UserId = payment.UserId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            Status = payment.Status.ToString(),
            PaymentMethod = payment.PaymentMethod.ToString(),
            Description = payment.Description,
            PaymobOrderId = payment.PaymobOrderId,
            PaymobTransactionId = payment.PaymobTransactionId,
            PaidAt = payment.PaidAt,
            FailedAt = payment.FailedAt,
            FailureReason = payment.FailureReason,
            RefundedAt = payment.RefundedAt,
            RefundAmount = payment.RefundAmount,
            RefundReason = payment.RefundReason,
            CreatedAtUtc = payment.CreatedAtUtc,
            UpdatedAtUtc = payment.UpdatedAtUtc
        };
    }
}
