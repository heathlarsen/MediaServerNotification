using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using MediaServerNotification.Extensions;
using MediaServerNotification.Models;
using MediaServerNotification.Services.Interfaces;

namespace MediaServerNotification.Platforms.Android;

public sealed class AndroidNotificationService : INotificationService
{
    public const int ForegroundSummaryNotificationId = 1000;
    private const string NotificationGroupKey = "media_servers_group";

    // Single channel for all media server notifications
    private const string NotificationChannelId = "media_servers_channel";
    private const string NotificationChannelName = "Media Servers";
    private const string NotificationChannelDesc = "Notifications for running media server monitors";

    private readonly Context _context;

    public AndroidNotificationService(Context context)
    {
        _context = context;
    }

    public void EnsureChannels()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            return;

        var manager = _context.GetSystemService(Context.NotificationService) as NotificationManager;
        if (manager is null)
            return;

#pragma warning disable CA1416 // Guarded by SDK checks above; APIs are Android O+ only.
        var existing = manager.GetNotificationChannel(NotificationChannelId);
        if (existing != null)
            return;

        var channel = new NotificationChannel(NotificationChannelId, NotificationChannelName, NotificationImportance.Low)
        {
            Description = NotificationChannelDesc
        };

        manager.CreateNotificationChannel(channel);
#pragma warning restore CA1416
    }

    public bool CanPostNotifications()
    {
        // POST_NOTIFICATIONS became a runtime permission on Android 13 (API 33).
        if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
            return true;

        try
        {
            return ContextCompat.CheckSelfPermission(_context, global::Android.Manifest.Permission.PostNotifications) == global::Android.Content.PM.Permission.Granted;
        }
        catch
        {
            return false;
        }
    }

    public void ShowOrUpdateSummaryNotification(int enabledServerCount)
    {
        if (!CanPostNotifications())
            return;

        var manager = GetManager();
        if (manager is null)
            return;

        try
        {
            manager.Notify(ForegroundSummaryNotificationId, BuildSummaryNotification(enabledServerCount));
        }
        catch
        {
            // ignore (e.g., permission not granted on Android 13+)
        }
    }

    public void ShowOrUpdateServerNotification(MediaServer server)
    {
        if (!CanPostNotifications())
            return;

        var manager = GetManager();
        if (manager is null)
            return;

        try
        {
            manager.Notify(NotificationIdForServer(server.Id), BuildServerNotification(server));
        }
        catch
        {
            // ignore (e.g., permission not granted on Android 13+)
        }
    }

    public void RemoveServerNotification(Guid serverId)
    {
        var manager = GetManager();
        if (manager is null)
            return;

        try
        {
            manager.Cancel(NotificationIdForServer(serverId));
        }
        catch
        {
            // ignore
        }
    }

    public void RemoveSummaryNotification()
    {
        var manager = GetManager();
        if (manager is null)
            return;

        try
        {
            manager.Cancel(ForegroundSummaryNotificationId);
        }
        catch
        {
            // ignore
        }
    }

    public Notification BuildSummaryNotification(int enabledServerCount)
    {
        var title = "Media Server Monitor";
        var text = enabledServerCount switch
        {
            0 => "Monitoring 0 servers",
            1 => "Monitoring 1 server",
            _ => $"Monitoring {enabledServerCount} servers"
        };

        var builder = new NotificationCompat.Builder(_context, NotificationChannelId);
        builder.SetSmallIcon(Resource.Mipmap.appicon);
        builder.SetContentTitle(title);
        builder.SetContentText(text);
        builder.SetOngoing(true);
        builder.SetAutoCancel(false);

        // Optional: tap notification to open the app
        var activityIntent = new Intent(_context, typeof(MainActivity));
        activityIntent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
        var pending = PendingIntent.GetActivity(
            _context,
            ForegroundSummaryNotificationId,
            activityIntent,
            PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
        if (pending is not null)
            builder.SetContentIntent(pending);

        // Group summary keeps the notification shade tidy.
        builder.SetGroup(NotificationGroupKey);
        builder.SetGroupSummary(true);
        builder.SetOnlyAlertOnce(true);

        return builder.Build()!;
    }

    public Notification BuildServerNotification(MediaServer server)
    {
        string Pluralize(string word, int count) => count > 1 ? word + "s" : word;
        string? StreamTextBuilder(int streams, StreamType streamType) => streams == 0 ? null : $"{streams} {Pluralize(streamType.GetDescription(), streams)}";
        string JoinStrings(string separator, string?[] values) => string.Join(separator, values.Where(x => !string.IsNullOrWhiteSpace(x)));

        var notificationId = NotificationIdForServer(server.Id);
        var serverName = server.Settings?.Name ?? "Media Server";

        var streams = server.Stats?.Streams ?? new List<StreamSession>();
        var resources = server.Stats?.Resources ?? new ServerResources();

        var directPlays = streams.Count(x => x.StreamType == StreamType.DirectPlay);
        var directStreams = streams.Count(x => x.StreamType == StreamType.DirectStream);
        var transcodes = streams.Count(x => x.StreamType == StreamType.Transcode);
        var textLine1 = JoinStrings(
            " • ",
            [StreamTextBuilder(directPlays, StreamType.DirectPlay), StreamTextBuilder(directStreams, StreamType.DirectStream), StreamTextBuilder(transcodes, StreamType.Transcode)]
        );
        var textLine2 = JoinStrings(
            " • ",
            [$"CPU {(int)Math.Round(resources.HostCpuUsagePercent, 0)}% ", $"MEM {(int)Math.Round(resources.HostMemoryUsagePercent, 0)}%", "NET placeholder"]
        );

        var builder = new NotificationCompat.Builder(_context, NotificationChannelId);
        builder.SetSmallIcon(Resource.Mipmap.appicon);
        builder.SetContentTitle($"{serverName} • {(streams.Any() ? $"{streams.Count} {Pluralize("Stream", streams.Count)}" : string.Empty)}");
        builder.SetContentText("");
        builder.SetStyle(new NotificationCompat.BigTextStyle().BigText(textLine1 + System.Environment.NewLine + textLine2));
        builder.SetOngoing(true);
        builder.SetAutoCancel(false);
        builder.SetOnlyAlertOnce(true);
        builder.SetGroup(NotificationGroupKey);
        builder.SetPriority(NotificationCompat.PriorityMin);

        // Tap notification to open the app
        var activityIntent = new Intent(_context, typeof(MainActivity));
        activityIntent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
        var pending = PendingIntent.GetActivity(
            _context,
            notificationId,
            activityIntent,
            PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
        if (pending is not null)
            builder.SetContentIntent(pending);

        return builder.Build()!;
    }

    private NotificationManager? GetManager()
        => _context.GetSystemService(Context.NotificationService) as NotificationManager;

    private static int NotificationIdForServer(Guid serverId)
    {
        // Deterministic, stable, positive int derived from GUID bytes.
        // Avoid string.GetHashCode() which is randomized across processes/runtime versions.
        var bytes = serverId.ToByteArray();
        var raw = BitConverter.ToInt32(bytes, 0) & 0x3FFFFFFF; // keep positive and avoid very large ids
        var id = 2000 + raw; // keep away from summary id
        if (id == ForegroundSummaryNotificationId)
            id++;
        return id;
    }
}


