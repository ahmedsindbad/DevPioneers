// ============================================
// File: DevPioneers.Application/Features/Payments/Queries/GetPaymentHistoryQuery.cs
// ============================================
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Payments.DTOs;
using DevPioneers.Domain.Enums;
using MediatR;

namespace DevPioneers.Application.Features.Payments.Queries;

public record GetPaymentHistoryQuery(
    int UserId,
    int PageNumber = 1,
    int PageSize = 20,
    PaymentStatus? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : IRequest<Result<PaginatedList<PaymentDto>>>;
