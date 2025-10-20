# Background Jobs Documentation

## Overview
This document describes the Hangfire background jobs implemented in the DevPioneers system.

## Implemented Jobs

### 1. ExpireSubscriptionsJob
**Purpose**: Automatically expire subscriptions that have passed their end date.

**Schedule**: Runs every hour

**Queue**: `normal`

**What it does**:
- Finds all active, trial, or grace period subscriptions that have passed their end date
- Updates their status to `Expired`
- Disables auto-renewal
- Sends expiration notification email to users

**Location**:
- Interface: `DevPioneers.Application/Common/Interfaces/IBackgroundJobService.cs`
- Implementation: `DevPioneers.Infrastructure/Services/BackgroundJobs/ExpireSubscriptionsJob.cs`

---

### 2. ReconcilePaymentsJob
**Purpose**: Reconcile pending/processing payments with the Paymob payment gateway.

**Schedule**: Runs every 30 minutes

**Queue**: `critical`

**What it does**:
- Finds payments that have been pending/processing for more than 30 minutes
- Checks their status with Paymob payment gateway
- Updates payment status accordingly (completed or failed)
- Ensures payment data is synchronized with the gateway

**Location**:
- Interface: `DevPioneers.Application/Common/Interfaces/IBackgroundJobService.cs`
- Implementation: `DevPioneers.Infrastructure/Services/BackgroundJobs/ReconcilePaymentsJob.cs`

---

### 3. SendEmailJob
**Purpose**: Send queued emails asynchronously.

**Schedule**: Called on-demand via Hangfire

**Queue**: `default`

**What it does**:
- Sends individual emails asynchronously
- Sends bulk emails to multiple recipients
- Sends subscription expiry reminder emails (7 days before expiration)

**Additional Methods**:
- `SendEmailAsync(email, subject, body, isHtml)` - Send single email
- `SendBulkEmailsAsync(recipients, subject, body, isHtml)` - Send bulk emails
- `SendSubscriptionExpiryRemindersAsync(context)` - Send expiry reminders

**Location**:
- Interface: `DevPioneers.Application/Common/Interfaces/IBackgroundJobService.cs`
- Implementation: `DevPioneers.Infrastructure/Services/BackgroundJobs/SendEmailJob.cs`

---

### 4. CleanOldAuditTrailJob
**Purpose**: Clean old audit trail records to maintain database performance.

**Schedule**: Runs daily at 2:00 AM UTC

**Queue**: `low`

**What it does**:
- Deletes audit trail records older than 90 days (configurable)
- Processes deletions in batches of 1000 to avoid locking the database
- Maintains system performance by preventing audit trail table growth

**Configuration**:
- Default retention period: 90 days
- Batch size: 1000 records per batch
- Delay between batches: 100ms

**Location**:
- Interface: `DevPioneers.Application/Common/Interfaces/IBackgroundJobService.cs`
- Implementation: `DevPioneers.Infrastructure/Services/BackgroundJobs/CleanOldAuditTrailJob.cs`

---

## Configuration

### Enable/Disable Background Jobs

Background jobs can be enabled or disabled via the `FeatureFlags` configuration:

```json
{
  "FeatureFlags": {
    "EnableBackgroundJobs": true
  }
}
```

### Hangfire Settings

Configure Hangfire behavior in `appsettings.json`:

```json
{
  "HangfireSettings": {
    "SchedulePollingInterval": "00:00:15",
    "WorkerCount": 2,
    "Queues": ["default", "critical", "normal", "low"],
    "ServerName": "DevPioneers-Dev-Server",
    "EnableDashboard": true,
    "DashboardPath": "/hangfire",
    "RequireAuthentication": false,
    "RetryAttempts": 3,
    "RetryDelayInMinutes": 5
  }
}
```

## Job Queues

The system uses multiple queues with different priorities:

1. **critical** - High priority jobs (e.g., payment reconciliation)
2. **normal** - Normal priority jobs (e.g., subscription expiration)
3. **low** - Low priority jobs (e.g., audit trail cleanup)
4. **default** - Default queue for on-demand jobs

## Monitoring

Access the Hangfire dashboard at `/hangfire` to monitor:
- Job execution history
- Failed jobs
- Recurring job schedules
- Queue statistics
- Server statistics

**Dashboard Access**:
- Development: No authentication required
- Production: Requires Admin role

## Manual Job Execution

Jobs can be manually triggered via the Hangfire dashboard or programmatically:

```csharp
// Trigger job immediately
BackgroundJob.Enqueue<IExpireSubscriptionsJob>(job => job.ExecuteAsync(default));

// Schedule job for later
BackgroundJob.Schedule<IReconcilePaymentsJob>(
    job => job.ExecuteAsync(default),
    TimeSpan.FromMinutes(5));

// Send email on-demand
BackgroundJob.Enqueue<ISendEmailJob>(
    job => job.SendEmailAsync("user@example.com", "Subject", "Body", true, default));
```

## Error Handling

All jobs implement comprehensive error handling:
- Individual item failures don't stop the entire job
- Errors are logged with detailed context
- Failed jobs can be retried manually from the dashboard
- Automatic retry is configured based on `HangfireSettings:RetryAttempts`

## Logging

All jobs log:
- Start/completion times
- Number of items processed
- Success/failure counts
- Detailed error information
- Performance metrics

Check application logs for job execution details.

## Dependencies

Jobs are registered in the Dependency Injection container:

**File**: `DevPioneers.Infrastructure/DependencyInjection.cs`

```csharp
services.AddScoped<IExpireSubscriptionsJob, ExpireSubscriptionsJob>();
services.AddScoped<IReconcilePaymentsJob, ReconcilePaymentsJob>();
services.AddScoped<ISendEmailJob, SendEmailJob>();
services.AddScoped<ICleanOldAuditTrailJob, CleanOldAuditTrailJob>();
```

## Job Scheduling

Jobs are scheduled in `Program.cs`:

**File**: `DevPioneers.Api/Program.cs`

```csharp
RecurringJob.AddOrUpdate<IExpireSubscriptionsJob>(
    "expire-subscriptions",
    job => job.ExecuteAsync(default),
    Cron.Hourly,
    new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc, QueueName = "normal" });
```

## Best Practices

1. **Job Design**:
   - Keep jobs idempotent (safe to run multiple times)
   - Process items in batches for large datasets
   - Use appropriate queue priorities
   - Log detailed execution information

2. **Performance**:
   - Use batching for bulk operations
   - Add delays between batches to avoid database locks
   - Monitor job execution times
   - Adjust worker count based on load

3. **Error Recovery**:
   - Implement retry logic for transient failures
   - Log errors with sufficient context
   - Monitor failed jobs in the dashboard
   - Set up alerts for critical job failures

4. **Testing**:
   - Test jobs with various data volumes
   - Verify error handling scenarios
   - Check performance under load
   - Validate email notifications

## Future Enhancements

Potential improvements for background jobs:

1. Add email queue table for reliable email delivery
2. Implement job result notifications
3. Add more granular scheduling options
4. Create admin API endpoints for job management
5. Add job execution metrics and analytics
6. Implement job dependency chains
7. Add support for job cancellation
8. Create dashboard widgets for job statistics
