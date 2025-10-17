// ============================================
// File: DevPioneers.Application/Common/Behaviors/PerformanceBehavior.cs
// ============================================
using MediatR;
using Microsoft.Extensions.Logging;
using DevPioneers.Application.Common.Interfaces;
using System.Diagnostics;

namespace DevPioneers.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior for performance monitoring
/// Logs slow queries and tracks execution time
/// </summary>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly Stopwatch _timer;
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;

    public PerformanceBehavior(
        ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
        ICurrentUserService currentUserService)
    {
        _timer = new Stopwatch();
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _timer.Start();

        var response = await next();

        _timer.Stop();

        var elapsedMilliseconds = _timer.ElapsedMilliseconds;

        // Log if request took longer than 500ms
        if (elapsedMilliseconds > 500)
        {
            var requestName = typeof(TRequest).Name;
            var userId = _currentUserService.UserId;
            var userName = _currentUserService.UserFullName;

            _logger.LogWarning(
                "DevPioneers Slow Request: {Name} ^({ElapsedMilliseconds} ms^) {@UserId} {@UserName} {@Request}",
                requestName, elapsedMilliseconds, userId, userName, request);
        }

        return response;
    }
}
