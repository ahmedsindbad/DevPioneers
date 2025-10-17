// ============================================
// File: DevPioneers.Application/Common/Models/Result.cs
// ============================================
namespace DevPioneers.Application.Common.Models;

/// <summary>
/// Generic result wrapper for operation outcomes
/// Implements Result pattern for better error handling
/// </summary>
public class Result
{
    protected Result(bool succeeded, IEnumerable<string> errors)
    {
        Succeeded = succeeded;
        Errors = errors.ToArray();
    }

    public bool Succeeded { get; }
    public string[] Errors { get; }
    public bool Failed => !Succeeded;

    public static Result Success() => new(true, Array.Empty<string>());
    public static Result Failure(IEnumerable<string> errors) => new(false, errors);
    public static Result Failure(params string[] errors) => new(false, errors);
    public static Result Failure(string error) => new(false, new[] { error });

    public static implicit operator Result(string error) => Failure(error);
}

/// <summary>
/// Generic result with data
/// </summary>
public class Result<T> : Result
{
    protected internal Result(bool succeeded, T? data, IEnumerable<string> errors)
        : base(succeeded, errors)
    {
        Data = data;
    }

    public T? Data { get; }

    public static Result<T> Success(T data) => new(true, data, Array.Empty<string>());
    public static new Result<T> Failure(IEnumerable<string> errors) => new(false, default, errors);
    public static new Result<T> Failure(params string[] errors) => new(false, default, errors);
    public static new Result<T> Failure(string error) => new(false, default, new[] { error });

    public static implicit operator Result<T>(T data) => Success(data);
    public static implicit operator Result<T>(string error) => Failure(error);
}
