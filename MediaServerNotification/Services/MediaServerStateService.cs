using MediaServerNotification.Models;
using MediaServerNotification.Services.Interfaces;

namespace MediaServerNotification.Services;

public class MediaServerStateService : IMediaServerStateService
{
    private readonly IMediaServerStoreService _storeService;
    private readonly IMediaServerClient<PlexMediaServerSettings> _plexClient;

    private readonly object _gate = new();
    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private readonly SemaphoreSlim _wakeSignal = new(0, int.MaxValue);

    public event Action<MediaServer>? ServerUpdated;
    public event Action<Guid>? ServerDeleted;
    public event Action<int>? EnabledServerCountUpdated;

    public MediaServerStateService(
        IMediaServerStoreService storeService,
        IMediaServerClient<PlexMediaServerSettings> plexClient
    )
    {
        _storeService = storeService;
        _plexClient = plexClient;
    }

    public Task StartAsync(bool isForeground, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (_loopTask is { IsCompleted: false })
                return _loopTask;

            _cts?.Dispose();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _loopTask = RunPollingLoopAsync(isForeground, _cts.Token);
            return _loopTask;
        }
    }


    public void Stop()
    {
        lock (_gate)
        {
            try { _cts?.Cancel(); } catch { /* ignore */ }
        }
    }

    public async Task RefreshServerStateAsync(MediaServer server, CancellationToken cancellationToken = default)
    {
        if (server is null)
            return;

        // Only Plex is currently implemented.
        if (server.Settings is not PlexMediaServerSettings plexSettings)
            return;

        cancellationToken.ThrowIfCancellationRequested();

        var streams = await _plexClient.GetStreamSessionsAsync(plexSettings, cancellationToken);
        var resources = await _plexClient.GetResourcesAsync(plexSettings, cancellationToken);

        server.Stats.Streams = streams ?? new List<StreamSession>();
        server.Stats.Resources = resources ?? new ServerResources();

        _storeService.AddOrUpdate(server);
        ServerUpdated?.Invoke(server);
    }

    public void UpsertServer(MediaServer server)
    {
        if (server is null)
            return;

        _storeService.AddOrUpdate(server);

        // Immediately reflect changes in platform notifications (name/IP/enable toggles/etc).
        ServerUpdated?.Invoke(server);
        EnabledServerCountUpdated?.Invoke(GetEnabledServerCount());

        SignalWake();
    }

    public void DeleteServer(Guid id)
    {
        _storeService.Delete(id);

        // Ensure platform notifications get removed even though the server no longer exists in the store.
        ServerDeleted?.Invoke(id);
        EnabledServerCountUpdated?.Invoke(GetEnabledServerCount());

        SignalWake();
    }

    private int GetEnabledServerCount()
    {
        try
        {
            return (_storeService.GetAll() ?? new List<MediaServer>())
                .Count(s => s?.Settings?.EnableNotification == true);
        }
        catch
        {
            return 0;
        }
    }

    private void SignalWake()
    {
        try
        {
            _wakeSignal.Release();
        }
        catch (SemaphoreFullException)
        {
            // ignore: best-effort wakeup
        }
    }

    private async Task RunPollingLoopAsync(bool isForeground, CancellationToken cancellationToken)
    {
        var nextDueByServer = new Dictionary<Guid, DateTimeOffset>();

        // A small floor prevents tight loops when users set very low or invalid values.
        static TimeSpan ToInterval(MediaServerSettings settings)
        {
            var minutes = settings?.NotificationUpdateFrequency ?? 0;
            if (minutes <= 0)
                minutes = 1;
            return TimeSpan.FromMinutes(minutes);
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            List<MediaServer> enabledServers;
            try
            {
                enabledServers = (_storeService.GetAll() ?? new List<MediaServer>())
                    .Where(s => s?.Settings?.EnableNotification == true)
                    .ToList();
            }
            catch
            {
                enabledServers = new List<MediaServer>();
            }

            EnabledServerCountUpdated?.Invoke(enabledServers.Count);

            // Reconcile schedule to current enabled set.
            var enabledIds = enabledServers.Select(s => s.Id).ToHashSet();
            foreach (var stale in nextDueByServer.Keys.Where(id => !enabledIds.Contains(id)).ToList())
                nextDueByServer.Remove(stale);

            var now = DateTimeOffset.UtcNow;
            foreach (var server in enabledServers)
            {
                if (!nextDueByServer.ContainsKey(server.Id))
                    nextDueByServer[server.Id] = now; // new server: refresh immediately
            }

            // If nothing enabled, wait a bit and re-check.
            if (enabledServers.Count == 0)
            {
                await Task.WhenAny(
                    Task.Delay(TimeSpan.FromSeconds(15), cancellationToken),
                    _wakeSignal.WaitAsync(cancellationToken)
                );
                continue;
            }

            // Refresh any server that is due now.
            var due = enabledServers
                .Where(s => nextDueByServer.TryGetValue(s.Id, out var dueAt) && dueAt <= now)
                .ToList();

            foreach (var server in due)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await RefreshServerStateAsync(server, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch
                {
                    // Best-effort polling: a single server failure should not stop the loop.
                }

                // Schedule next run based on current config (pick up changes without restarting).
                var interval = ToInterval(server.Settings);
                nextDueByServer[server.Id] = DateTimeOffset.UtcNow + interval;
            }

            // Wait until the next server is due.
            var nextDue = nextDueByServer.Values.Min();
            var delay = nextDue - DateTimeOffset.UtcNow;
            if (delay < TimeSpan.FromSeconds(1))
                delay = TimeSpan.FromSeconds(1);

            await Task.WhenAny(
                Task.Delay(delay, cancellationToken),
                _wakeSignal.WaitAsync(cancellationToken)
            );
        }
    }
}
