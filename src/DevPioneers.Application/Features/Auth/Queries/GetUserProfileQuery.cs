// ============================================
// File: DevPioneers.Application/Features/Auth/Queries/GetUserProfileQuery.cs
// ============================================
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Auth.DTOs;
using MediatR;

namespace DevPioneers.Application.Features.Auth.Queries;

public record GetUserProfileQuery(int UserId) : IRequest<Result<UserProfileDto>>;
