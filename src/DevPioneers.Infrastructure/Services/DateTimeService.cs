// ============================================
// File: DevPioneers.Infrastructure/Services/DateTimeService.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using System;

namespace DevPioneers.Infrastructure.Services
{
    public class DateTimeService : IDateTime
    {
        public DateTime UtcNow => DateTime.UtcNow;
        public DateTime Now => DateTime.Now;

        // DateOnly requires .NET 6+
        public DateOnly UtcToday => DateOnly.FromDateTime(DateTime.UtcNow);
        public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

        public long UnixTimestamp => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>
        /// Add business days excluding Sat/Sun. Supports negative businessDays.
        /// </summary>
        public DateTime AddBusinessDays(DateTime date, int businessDays)
        {
            if (businessDays == 0) return date;

            var direction = businessDays > 0 ? 1 : -1;
            var remaining = Math.Abs(businessDays);
            var current = date;

            while (remaining > 0)
            {
                current = current.AddDays(direction);
                if (!IsWeekend(current))
                {
                    remaining--;
                }
            }

            return current;
        }

        public bool IsWeekend(DateTime date)
        {
            var d = date.DayOfWeek;
            return d == DayOfWeek.Saturday || d == DayOfWeek.Sunday;
        }

        /// <summary>
        /// Return start of day at 00:00:00 (local)
        /// </summary>
        public DateTime StartOfDay(DateTime date)
        {
            return date.Date;
        }

        /// <summary>
        /// Return end of day as the last representable tick before next day (local).
        /// Uses DateTime kind of the provided date except we keep DateTimeKind.Unspecified/Local/UTC as input.
        /// </summary>
        public DateTime EndOfDay(DateTime date)
        {
            // Use AddDays(1).AddTicks(-1) for the last tick of the day
            return date.Date.AddDays(1).AddTicks(-1);
        }
    }
}
