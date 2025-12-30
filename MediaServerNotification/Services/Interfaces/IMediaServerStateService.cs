using MediaServerNotification.Models;

namespace MediaServerNotification.Services.Interfaces;

public interface IMediaServerStateService
{
    event Action<MediaServer>? ServerUpdated;
    event Action<Guid>? ServerDeleted;
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

    /// <summary>
    /// Persists the server and immediately publishes update events so notifications/UI can reflect changes
    /// without waiting for the next polling cycle (e.g., name/IP/token/notification toggles).
    /// </summary>
    void UpsertServer(MediaServer server);

    /// <summary>
    /// Deletes the server and publishes deletion events so platform notifications can be removed immediately.
    /// </summary>
    void DeleteServer(Guid id);
}
