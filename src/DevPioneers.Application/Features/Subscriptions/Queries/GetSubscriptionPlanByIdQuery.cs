// ============================================
// File: DevPioneers.Application/Features/Subscriptions/Queries/GetSubscriptionPlanByIdQuery.cs
// ============================================
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Subscriptions.DTOs;
using MediatR;

namespace DevPioneers.Application.Features.Subscriptions.Queries;

public record GetSubscriptionPlanByIdQuery(int Id) : IRequest<Result<SubscriptionPlanDto?>>;
