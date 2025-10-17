// ============================================
// File: DevPioneers.Application/Common/Models/ApiResponse.cs
// ============================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace DevPioneers.Application.Common.Models
{
    /// <summary>
    /// Standard API response wrapper
    /// Provides consistent response format across all endpoints
    /// </summary>
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public object? Data { get; set; }
        public string[]? Errors { get; set; }
        public ApiMetadata? Metadata { get; set; }

        [JsonIgnore]
        public int StatusCode { get; set; } = 200;

        // --------------------------
        // ✅ Factory Methods
        // --------------------------

        public static ApiResponse Ok(object? data = null, string? message = null)
            => new()
            {
                Success = true,
                Data = data,
                Message = message ?? "Success",
                StatusCode = 200
            };

        public static ApiResponse Created(object? data = null, string? message = null)
            => new()
            {
                Success = true,
                Data = data,
                Message = message ?? "Created successfully",
                StatusCode = 201
            };

        public static ApiResponse BadRequest(string message = "Bad request", string[]? errors = null)
            => new()
            {
                Success = false,
                Message = message,
                Errors = errors,
                StatusCode = 400
            };

        public static ApiResponse NotFound(string message = "Resource not found")
            => new()
            {
                Success = false,
                Message = message,
                StatusCode = 404
            };

        public static ApiResponse Unauthorized(string message = "Unauthorized access")
            => new()
            {
                Success = false,
                Message = message,
                StatusCode = 401
            };

        public static ApiResponse ServerError(string message = "An internal server error occurred")
            => new()
            {
                Success = false,
                Message = message,
                StatusCode = 500
            };

        // --------------------------
        // ✅ Integration with Result<T>
        // --------------------------

        public static ApiResponse FromResult(Result result, string? successMessage = null)
        {
            if (result.IsSuccess)
            {
                return Ok(message: successMessage);
            }

            // ✅ Fix: convert List<string> to string[]
            return BadRequest("Operation failed", result.Errors?.ToArray());
        }

        public static ApiResponse FromResult<T>(Result<T> result, string? successMessage = null)
        {
            if (result.IsSuccess)
            {
                return Ok(result.Data, successMessage);
            }

            // ✅ Fix: convert List<string> to string[]
            return BadRequest("Operation failed", result.Errors?.ToArray());
        }
    }

    /// <summary>
    /// API response metadata for pagination, request info, etc.
    /// </summary>
    public class ApiMetadata
    {
        public int? TotalCount { get; set; }
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
        public int? TotalPages { get; set; }
        public bool? HasPreviousPage { get; set; }
        public bool? HasNextPage { get; set; }
        public DateTime? Timestamp { get; set; } = DateTime.UtcNow;
        public string? RequestId { get; set; }
        public Dictionary<string, object>? AdditionalData { get; set; }
    }
}
