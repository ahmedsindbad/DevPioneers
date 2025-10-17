// ============================================
// File: DevPioneers.Application/Common/Models/Result.cs
// ============================================
namespace DevPioneers.Application.Common.Models;

/// <summary>
/// Generic result wrapper for operation outcomes
/// </summary>
/// <typeparam name="T">Data type</typeparam>
public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? ErrorMessage { get; private set; }
    public List<string> Errors { get; private set; } = new();

    private Result(bool isSuccess, T? data, string? errorMessage, List<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Data = data;
        ErrorMessage = errorMessage;
        Errors = errors ?? new List<string>();
    }

    /// <summary>
    /// Create successful result with data
    /// </summary>
    public static Result<T> Success(T data)
    {
        return new Result<T>(true, data, null);
    }

    /// <summary>
    /// Create successful result without data
    /// </summary>
    public static Result<T> Success()
    {
        return new Result<T>(true, default, null);
    }

    /// <summary>
    /// Create failed result with error message
    /// </summary>
    public static Result<T> Failure(string errorMessage)
    {
        return new Result<T>(false, default, errorMessage);
    }

    /// <summary>
    /// Create failed result with multiple errors
    /// </summary>
    public static Result<T> Failure(List<string> errors)
    {
        return new Result<T>(false, default, errors.FirstOrDefault(), errors);
    }

    /// <summary>
    /// Create failed result with error message and additional errors
    /// </summary>
    public static Result<T> Failure(string errorMessage, List<string> errors)
    {
        return new Result<T>(false, default, errorMessage, errors);
    }
}

/// <summary>
/// Non-generic result for operations without return data
/// </summary>
public class Result
{
    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }
    public List<string> Errors { get; private set; } = new();

    private Result(bool isSuccess, string? errorMessage, List<string>? errors = null)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        Errors = errors ?? new List<string>();
    }

    /// <summary>
    /// Create successful result
    /// </summary>
    public static Result Success()
    {
        return new Result(true, null);
    }

    /// <summary>
    /// Create failed result with error message
    /// </summary>
    public static Result Failure(string errorMessage)
    {
        return new Result(false, errorMessage);
    }

    /// <summary>
    /// Create failed result with multiple errors
    /// </summary>
    public static Result Failure(List<string> errors)
    {
        return new Result(false, errors.FirstOrDefault(), errors);
    }
}