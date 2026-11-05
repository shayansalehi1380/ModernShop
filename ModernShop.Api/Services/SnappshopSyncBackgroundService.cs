using Microsoft.Extensions.Options;

namespace ModernShop.Api.Services;

public class SnappshopSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly SnappshopSettings _s;
    private readonly ILogger<SnappshopSyncBackgroundService> _log;

    public SnappshopSyncBackgroundService(IServiceScopeFactory scopes,
        IOptions<SnappshopSettings> s, ILogger<SnappshopSyncBackgroundService> log)
    { _scopes = scopes; _s = s.Value; _log = log; }

    protected override async Task ExecuteAsync(CancellationToken stop)
    {
        if (!_s.Enabled) { _log.LogInformation("Snappshop sync disabled."); return; }

        await Task.Delay(TimeSpan.FromSeconds(30), stop); // بعد از استارت صبر کن
        var interval = TimeSpan.FromMinutes(Math.Max(1, _s.SyncIntervalMinutes));

        while (!stop.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                await scope.ServiceProvider.GetRequiredService<SnappshopSyncService>().SyncAllAsync(stop);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _log.LogError(ex, "Snappshop background sync failed");
            }
            await Task.Delay(interval, stop);
        }
    }
}