using DevPioneers.Application.Common.Attributes;
using DevPioneers.Application.Common.Models;
using DevPioneers.Domain.Entities;
using MediatR;

[RequireAuditAccess]
[RequirePermission("CanViewAuditTrail")]
// [RequireTimeWindow("08:00", "18:00")] // Only during extended business hours
public class GetAuditTrailQuery : IRequest<Result<PaginatedList<AuditTrail>>>
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? EntityType { get; set; }
    public string? Action { get; set; }
    public int? UserId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}