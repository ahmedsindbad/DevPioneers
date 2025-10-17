// ============================================
// File: DevPioneers.Application/Common/Exceptions/ValidationException.cs
// ============================================
using FluentValidation.Results;

namespace DevPioneers.Application.Common.Exceptions;

/// <summary>
/// Exception for validation failures
/// </summary>
public class ValidationException : Exception
{
    public ValidationException()
        : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }

    public ValidationException(string property, string error)
        : this()
    {
        Errors = new Dictionary<string, string[]>
        {
            { property, new[] { error } }
        };
    }

    public ValidationException(Dictionary<string, string[]> errors)
        : this()
    {
        Errors = errors;
    }

    public IDictionary<string, string[]> Errors { get; }

    public string[] GetErrors(string property)
    {
        return Errors.TryGetValue(property, out var errors) ? errors : Array.Empty<string>();
    }

    public string[] GetAllErrors()
    {
        return Errors.Values.SelectMany(x => x).ToArray();
    }
}