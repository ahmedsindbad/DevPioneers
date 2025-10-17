// ============================================
// File: DevPioneers.Application/Common/Behaviors/ValidationBehavior.cs
// ============================================
using DevPioneers.Application.Common.Exceptions;
using FluentValidation;
using MediatR;

namespace DevPioneers.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior for automatic validation
/// Validates all requests that have validators registered
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
        {
            throw new DevPioneers.Application.Common.Exceptions.ValidationException(failures);
        }

        return await next();
    }
}
