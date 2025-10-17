// ============================================
// File: DevPioneers.Application/Common/Models/BaseDto.cs
// ============================================
namespace DevPioneers.Application.Common.Models;

/// <summary>
/// Base DTO with common properties
/// </summary>
public abstract class BaseDto
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

/// <summary>
/// Base auditable DTO
/// </summary>
public abstract class AuditableDto : BaseDto
{
    public int? CreatedById { get; set; }
    public string? CreatedByName { get; set; }
    public int? UpdatedById { get; set; }
    public string? UpdatedByName { get; set; }
}

/// <summary>
/// Pagination request parameters
/// </summary>
public class PaginationRequest
{
    private const int MaxPageSize = 100;
    private int _pageSize = 20;

    public int PageNumber { get; set; } = 1;
 
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }

    public string? SortBy { get; set; }
    public string? SortDirection { get; set; } = "asc";
    public string? SearchTerm { get; set; }
}
