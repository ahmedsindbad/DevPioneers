// ============================================
// File: DevPioneers.Application/Common/Behaviors/LoggingBehavior.cs
// ============================================
using MediatR;
using Microsoft.Extensions.Logging;
using DevPioneers.Application.Common.Interfaces;

namespace DevPioneers.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior for request/response logging
/// Logs all requests with user context and execution details
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        ICurrentUserService currentUserService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = _currentUserService.UserId;
        var userName = _currentUserService.UserFullName;

        // Log request start
        _logger.LogInformation(
            "DevPioneers Request: {Name} {@UserId} {@UserName} {@Request}",
            requestName, userId, userName, request);

        try
        {
            var response = await next();

            // Log successful response
            _logger.LogInformation(
                "DevPioneers Request Completed: {Name} {@UserId} {@Response}",
                requestName, userId, response);

            return response;
        }
        catch (Exception ex)
        {
            // Log failed request
            _logger.LogError(ex,
                "DevPioneers Request Failed: {Name} {@UserId} {@UserName} {@Request}",
                requestName, userId, userName, request);

            throw;
        }
    }
}
