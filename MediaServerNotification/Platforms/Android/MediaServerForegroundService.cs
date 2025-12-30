using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using MediaServerNotification.Extensions;
using MediaServerNotification.Models;
using MediaServerNotification.Services.Interfaces;

namespace MediaServerNotification.Platforms.Android
{
    [Service(Exported = true)]
    public class MediaServerForegroundService : Service
    {
        private const int ForegroundSummaryNotificationId = 1000;
        private const string NotificationGroupKey = "media_servers_group";

        // Single channel for all media server notifications
        private const string NotificationChannelId = "media_servers_channel";
        private const string NotificationChannelName = "Media Servers";
        private const string NotificationChannelDesc = "Notifications for running media server monitors";

        private NotificationManager? _notificationManager;
        private IMediaServerStateService? _stateService;
        private bool _subscribed;

        public override void OnCreate()
        {
            base.OnCreate();
            EnsureNotificationChannel();
            _notificationManager = GetSystemService(NotificationService) as NotificationManager;
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            var enabledServers = GetEnabledServers();

            // One foreground notification is required; keep it as a group summary.
            var summaryNotification = BuildSummaryNotification(enabledServers.Count);
            StartForeground(ForegroundSummaryNotificationId, summaryNotification);

            // Post one ongoing notification per server (these can be many).
            if (_notificationManager is not null)
            {
                foreach (var server in enabledServers)
                {
                    var serverNotification = BuildServerNotification(server);
                    _notificationManager.Notify(NotificationIdForServer(server.Id), serverNotification);
                }
            }

            EnsurePollingHooked();

            // If you want the service to continue running until explicitly stopped, use Sticky.
            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
            try
            {
                if (_subscribed && _stateService is not null)
                {
                    _stateService.ServerUpdated -= OnServerUpdated;
                    _stateService.EnabledServerCountUpdated -= OnEnabledServerCountUpdated;
                    _subscribed = false;
                }

                _stateService?.Stop();
            }
            catch
            {
                // ignore
            }
            base.OnDestroy();
        }

        public override global::Android.OS.IBinder? OnBind(Intent? intent) => null;

        private void EnsureNotificationChannel()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
                return;

            var manager = GetSystemService(NotificationService) as NotificationManager;
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

        private Notification BuildSummaryNotification(int enabledServerCount)
        {
            var title = "Media Server Monitor";
            var text = enabledServerCount switch
            {
                0 => "Monitoring 0 servers",
                1 => "Monitoring 1 server",
                _ => $"Monitoring {enabledServerCount} servers"
            };

            var builder = new NotificationCompat.Builder(this, NotificationChannelId);
            builder.SetSmallIcon(Resource.Mipmap.appicon);
            builder.SetContentTitle(title);
            builder.SetContentText(text);
            builder.SetOngoing(true);
            builder.SetAutoCancel(false);

            // Optional: tap notification to open the app
            var activityIntent = new Intent(this, typeof(MainActivity));
            activityIntent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
            var pending = PendingIntent.GetActivity(
                this,
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

        private Notification BuildServerNotification(MediaServer server)
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
                [ StreamTextBuilder(directPlays, StreamType.DirectPlay), StreamTextBuilder(directStreams, StreamType.DirectStream), StreamTextBuilder(transcodes, StreamType.Transcode) ]
            );
            var textLine2 = JoinStrings(
                " • ",
                [ $"CPU {(int)Math.Round(resources.HostCpuUsagePercent, 0)}% ", $"MEM {(int)Math.Round(resources.HostMemoryUsagePercent, 0)}%" , $"NET placeholder" ]
            );

            var builder = new NotificationCompat.Builder(this, NotificationChannelId);
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
            var activityIntent = new Intent(this, typeof(global::MediaServerNotification.MainActivity));
            activityIntent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
            var pending = PendingIntent.GetActivity(
                this,
                notificationId,
                activityIntent,
                PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
            if (pending is not null)
                builder.SetContentIntent(pending);

            return builder.Build()!;
        }

        private static int NotificationIdForServer(System.Guid serverId)
        {
            // Deterministic, stable, positive int derived from GUID bytes.
            // Avoid string.GetHashCode() which is randomized across processes/runtime versions.
            var bytes = serverId.ToByteArray();
            var raw = System.BitConverter.ToInt32(bytes, 0) & 0x3FFFFFFF; // keep positive and avoid very large ids
            var id = 2000 + raw; // keep away from summary id
            if (id == ForegroundSummaryNotificationId)
                id++;
            return id;
        }

        private void EnsurePollingHooked()
        {
            if (_subscribed)
                return;

            try
            {
                var services = global::Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services;
                _stateService = services?.GetService<IMediaServerStateService>();
                if (_stateService is null)
                    return;

                _stateService.ServerUpdated += OnServerUpdated;
                _stateService.EnabledServerCountUpdated += OnEnabledServerCountUpdated;
                _subscribed = true;

                // Fire and forget: service owns its own cancellation via Stop().
                _ = _stateService.StartAsync(isForeground: true);
            }
            catch
            {
                // ignore
            }
        }

        private void OnEnabledServerCountUpdated(int enabledCount)
        {
            try
            {
                if (_notificationManager is null)
                    return;

                // Update summary text.
                _notificationManager.Notify(ForegroundSummaryNotificationId, BuildSummaryNotification(enabledCount));

                // Keep per-server notifications in sync with enabled set (cancel any that were disabled).
                var enabledServers = GetEnabledServers();
                var enabledIds = enabledServers.Select(s => s.Id).ToHashSet();

                // Cancel notifications for servers that exist in the store but are no longer enabled.
                var allServers = GetAllServers();
                foreach (var server in allServers.Where(s => !enabledIds.Contains(s.Id)))
                {
                    _notificationManager.Cancel(NotificationIdForServer(server.Id));
                }
            }
            catch
            {
                // ignore
            }
        }

        private void OnServerUpdated(MediaServer server)
        {
            try
            {
                if (_notificationManager is null)
                    return;

                // Only update notifications for servers that still have notifications enabled.
                if (server?.Settings?.EnableNotification != true)
                    return;

                _notificationManager.Notify(NotificationIdForServer(server.Id), BuildServerNotification(server));
            }
            catch
            {
                // ignore
            }
        }

        private List<MediaServer> GetAllServers()
        {
            try
            {
                var services = global::Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services;
                var store = services?.GetService<IMediaServerStoreService>();
                return store?.GetAll() ?? new List<MediaServer>();
            }
            catch
            {
                return new List<MediaServer>();
            }
        }

        private List<MediaServer> GetEnabledServers()
        {
            try
            {
                // Resolve from MAUI DI container
                var services = global::Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services;
                var store = services?.GetService<IMediaServerStoreService>();
                var servers = store?.GetAll() ?? new List<MediaServer>();
                return servers.Where(s => s?.Settings?.EnableNotification == true).ToList();
            }
            catch
            {
                return new List<MediaServer>();
            }
        }

        // Convenience helper to start the monitor service (single instance)
        public static void StartMonitoring(Context context)
        {
            var intent = new Intent(context, typeof(MediaServerForegroundService));

            // Use StartForegroundService on newer Android
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
#pragma warning disable CA1416 // Guarded by SDK checks above; APIs are Android O+ only.
                context.StartForegroundService(intent);
#pragma warning restore CA1416
            }
            else
            {
                context.StartService(intent);
            }
        }
    }
}