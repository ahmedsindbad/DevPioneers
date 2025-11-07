// ============================================
// File: DevPioneers.Application/Features/Payments/Queries/GetPaymentByIdQuery.cs
// Query to get a specific payment by ID with authorization check
// ============================================
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Payments.DTOs;
using MediatR;

namespace DevPioneers.Application.Features.Payments.Queries;

/// <summary>
/// Query to retrieve a specific payment by its ID
/// </summary>
/// <param name="PaymentId">The ID of the payment to retrieve</param>
/// <param name="UserId">The ID of the user making the request (for authorization)</param>
/// <param name="IsAdmin">Whether the requesting user is an admin (can access any payment)</param>
public record GetPaymentByIdQuery(
    int PaymentId,
    int UserId,
    bool IsAdmin = false
) : IRequest<Result<PaymentDto>>;
