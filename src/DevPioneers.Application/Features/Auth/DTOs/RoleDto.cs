// ============================================
// File: DevPioneers.Application/Features/Auth/DTOs/RoleDto.cs
// ============================================
using DevPioneers.Application.Common.Models;

namespace DevPioneers.Application.Features.Auth.DTOs;

public class RoleDto : BaseDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
}
