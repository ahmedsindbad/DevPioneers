// ============================================
// File: DevPioneers.Application/Features/Payments/Queries/GetPaymentOrderStatusQuery.cs
// ============================================
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Payments.DTOs;
using MediatR;

namespace DevPioneers.Application.Features.Payments.Queries;

public record GetPaymentOrderStatusQuery(string OrderId, int UserId) : IRequest<Result<PaymentOrderStatusDto?>>;
