// ============================================
// File: DevPioneers.Application/Features/Subscriptions/Queries/GetActiveSubscriptionQuery.cs
// ============================================
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Subscriptions.DTOs;
using MediatR;

namespace DevPioneers.Application.Features.Subscriptions.Queries;

public record GetActiveSubscriptionQuery(int UserId) : IRequest<Result<SubscriptionDto?>>;