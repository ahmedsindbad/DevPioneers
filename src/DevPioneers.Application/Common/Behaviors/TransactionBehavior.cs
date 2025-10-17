// ============================================
// File: DevPioneers.Application/Common/Behaviors/TransactionBehavior.cs
// ============================================
using MediatR;
using Microsoft.Extensions.Logging;
using DevPioneers.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DevPioneers.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior for database transactions
/// Wraps commands in database transactions for data consistency
/// </summary>
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(
        IApplicationDbContext context,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only wrap commands in transactions, not queries
        var isCommand = typeof(TRequest).Name.EndsWith("Command");

        if (!isCommand)
        {
            return await next();
        }

        var requestName = typeof(TRequest).Name;

        try
        {
            if (_context is DbContext dbContext)
            {
                var strategy = dbContext.Database.CreateExecutionStrategy();
 
                return await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
 
                    _logger.LogInformation("Begin transaction for {RequestName}", requestName);

                    try
                    {
                        var response = await next();
 
                        await transaction.CommitAsync(cancellationToken);
 
                        _logger.LogInformation("Commit transaction for {RequestName}", requestName);
 
                        return response;
                    }
                    catch
                    {
                        _logger.LogInformation("Rollback transaction for {RequestName}", requestName);
                        throw;
                    }
                });
            }

            return await next();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction failed for {RequestName}", requestName);
            throw;
        }
    }
}
