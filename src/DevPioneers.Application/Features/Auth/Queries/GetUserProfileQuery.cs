// ============================================
// File: DevPioneers.Application/Features/Users/Queries/GetUserProfileQuery.cs
// Example: User profile with ownership or admin access
// ============================================
using DevPioneers.Application.Common.Attributes;
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Auth.DTOs;
using MediatR;

[RequireUserRole] // Any authenticated user
[RequireOwnership("UserId")] // But can only access their own profile (unless Admin)
public class GetUserProfileQuery : IRequest<Result<UserProfileDto>>
{
    public int UserId { get; set; }
}