using CivicSync.Application.Services.Sync;
using CivicSync.Core.Configuration;
using Microsoft.Extensions.Options;

namespace CivicSync.Web.Host.Infrastructure.Sync;

public sealed class AutomaticSyncHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutomaticSyncHostedService> _logger;
    private readonly AutomaticSyncOptions _options;

    public AutomaticSyncHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<AutomaticSyncOptions> options,
        ILogger<AutomaticSyncHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Automatic CivicSync node synchronization is disabled.");
            return;
        }

        await DelaySafelyAsync(TimeSpan.FromSeconds(Math.Max(0, _options.InitialDelaySeconds)), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunSyncCycleAsync(stoppingToken);
            await DelaySafelyAsync(TimeSpan.FromSeconds(Math.Max(5, _options.IntervalSeconds)), stoppingToken);
        }
    }

    private async Task RunSyncCycleAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();

            await syncService.PublishPendingOutboxEventsAsync(cancellationToken);
            await syncService.ApplyPendingInboxEntriesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Automatic CivicSync node synchronization cycle failed.");
        }
    }

    private static async Task DelaySafelyAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        if (delay <= TimeSpan.Zero)
        {
            return;
        }

        try
        {
            await Task.Delay(delay, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }
}
