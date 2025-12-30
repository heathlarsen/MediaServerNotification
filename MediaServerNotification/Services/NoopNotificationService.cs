using MediaServerNotification.Models;
using MediaServerNotification.Services.Interfaces;

namespace MediaServerNotification.Services;

public sealed class NoopNotificationService : INotificationService
{
    public void EnsureChannels()
    {
        // No-op on non-Android platforms (and/or when notifications are not supported).
    }

    public void ShowOrUpdateSummaryNotification(int enabledServerCount)
    {
        // No-op
    }

    public void ShowOrUpdateServerNotification(MediaServer server)
    {
        // No-op
    }

    public void RemoveServerNotification(Guid serverId)
    {
        // No-op
    }

    public void RemoveSummaryNotification()
    {
        // No-op
    }
}


