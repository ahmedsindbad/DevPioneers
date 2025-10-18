// ============================================
// File: DevPioneers.Application/Features/Subscriptions/Queries/GetSubscriptionAnalyticsQuery.cs
// ============================================
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Subscriptions.DTOs;
using MediatR;

namespace DevPioneers.Application.Features.Subscriptions.Queries;

public record GetSubscriptionAnalyticsQuery(DateTime FromDate, DateTime ToDate) : IRequest<Result<SubscriptionAnalyticsDto>>;
