// ============================================
// File: DevPioneers.Infrastructure/Services/DateTimeService.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;

namespace DevPioneers.Infrastructure.Services;

public class DateTimeService : IDateTime
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Now => DateTime.Now;
}