using MediaServerNotification.Models;
using MediaServerNotification.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaServerNotification.Services;

public class MediaServerStateService : IMediaServerStateService
{
    private readonly IMediaServerStoreService _storeService;
    private readonly IMediaServerClient<PlexMediaServerSettings> _plexClient;

    private PeriodicTimer? _timer;

    public event Action<object>? SummaryUpdated;

    public MediaServerStateService(
        IMediaServerStoreService storeService,
        IMediaServerClient<PlexMediaServerSettings> plexClient
    )
    {
        _storeService = storeService;
        _plexClient = plexClient;
    }

    public async Task StartAsync(bool isForeground)
    {
        var interval = new TimeSpan(0,0,10);
        _timer = new PeriodicTimer(interval);

        while (await _timer.WaitForNextTickAsync())
        {
            var servers = _storeService.GetAll();

            if (servers == null || !servers.Any())
                continue;

            //await RefreshServerState(servers.First());
            SummaryUpdated?.Invoke(new object());
        }
    }


    public void Stop() => _timer = null;

    public async Task RefreshServerState(MediaServer server)
    {
        var plexSettings = server.Settings as PlexMediaServerSettings;
        server.Stats.Streams = await _plexClient.GetStreamSessionsAsync(plexSettings);
        server.Stats.Resources = await _plexClient.GetResourcesAsync(plexSettings);
        _storeService.AddOrUpdate(server);
    }
}
