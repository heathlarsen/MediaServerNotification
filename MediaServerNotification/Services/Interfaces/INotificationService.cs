using MediaServerNotification.Models;

namespace MediaServerNotification.Services.Interfaces;

public interface INotificationService
{
    void EnsureChannels();

    void ShowOrUpdateSummaryNotification(int enabledServerCount);

    void ShowOrUpdateServerNotification(MediaServer server);

    void RemoveServerNotification(Guid serverId);

    void RemoveSummaryNotification();
}
