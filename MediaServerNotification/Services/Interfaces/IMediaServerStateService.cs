using MediaServerNotification.Models;

namespace MediaServerNotification.Services.Interfaces;

public interface IMediaServerStateService
{
    event Action<MediaServer>? ServerUpdated;
    event Action<int>? EnabledServerCountUpdated;

    /// <summary>
    /// Starts polling enabled servers (Settings.EnableNotification == true) on each server's configured cadence.
    /// Safe to call multiple times; subsequent calls will return the existing running task.
    /// </summary>
    Task StartAsync(bool isForeground, CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests shutdown of the polling loop.
    /// </summary>
    void Stop();

    Task RefreshServerStateAsync(MediaServer server, CancellationToken cancellationToken = default);
}
