using Autofac.Extensions.DependencyInjection;
using CivicSync.Application.Services.Sync;
using CivicSync.Core.Configuration;
using CivicSync.Web.Host;
using CivicSync.Web.Core.Infrastructure.Errors;
using CivicSync.Web.Host.Infrastructure.Persistence.Seed;
using CivicSync.Web.Core.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Volo.Abp;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseAutofac();
builder.Services.AddApplication<CivicSyncWebHostModule>();

var app = builder.Build();

await app.InitializeApplicationAsync();

StartAutomaticSyncLoop(app);

app.UseApiExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CivicSync ABP Node API v1");
        options.RoutePrefix = "swagger";
    });

    await app.SeedNodeDataAsync();
}

app.UseHttpsRedirection();

app.UseCors("CivicSyncFrontend");

app.UseApiKeyAuthentication();

app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapControllers();

app.Run();

static void StartAutomaticSyncLoop(WebApplication app)
{
    var options = app.Services.GetRequiredService<IOptions<AutomaticSyncOptions>>().Value;
    if (!options.Enabled)
    {
        app.Logger.LogInformation("Automatic CivicSync node synchronization is disabled.");
        return;
    }

    app.Lifetime.ApplicationStarted.Register(() =>
    {
        _ = Task.Run(async () =>
        {
            var stoppingToken = app.Lifetime.ApplicationStopping;
            await DelaySafelyAsync(TimeSpan.FromSeconds(Math.Max(0, options.InitialDelaySeconds)), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await RunAutomaticSyncCycleAsync(app.Services, app.Logger, stoppingToken);
                await DelaySafelyAsync(TimeSpan.FromSeconds(Math.Max(5, options.IntervalSeconds)), stoppingToken);
            }
        });
    });
}

static async Task RunAutomaticSyncCycleAsync(
    IServiceProvider services,
    ILogger logger,
    CancellationToken cancellationToken)
{
    try
    {
        using var scope = services.CreateScope();
        var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();

        await syncService.PublishPendingOutboxEventsAsync(cancellationToken);
        await syncService.ApplyPendingInboxEntriesAsync(cancellationToken);
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
    }
    catch (Exception exception)
    {
        logger.LogWarning(exception, "Automatic CivicSync node synchronization cycle failed.");
    }
}

static async Task DelaySafelyAsync(TimeSpan delay, CancellationToken cancellationToken)
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
