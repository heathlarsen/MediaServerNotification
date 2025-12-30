using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using MediaServerNotification.Models;
using MediaServerNotification.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;

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

        public override void OnCreate()
        {
            base.OnCreate();
            EnsureNotificationChannel();
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            var enabledServers = GetEnabledServers();

            // One foreground notification is required; keep it as a group summary.
            var summaryNotification = BuildSummaryNotification(enabledServers.Count);
            StartForeground(ForegroundSummaryNotificationId, summaryNotification);

            // Post one ongoing notification per server (these can be many).
            var manager = GetSystemService(NotificationService) as NotificationManager;
            if (manager is not null)
            {
                foreach (var server in enabledServers)
                {
                    var serverId = server.Id.ToString();
                    var serverName = server.Settings?.Name ?? "Media Server";
                    var serverNotification = BuildServerNotification(serverId, serverName);
                    manager.Notify(NotificationIdForServer(server.Id), serverNotification);
                }
            }

            // If you want the service to continue running until explicitly stopped, use Sticky.
            return StartCommandResult.Sticky;
        }

        public override void OnDestroy()
        {
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
            var activityIntent = new Intent(this, typeof(global::MediaServerNotification.MainActivity));
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

        private Notification BuildServerNotification(string serverId, string serverName)
        {
            var notificationId = TryGetGuid(serverId, out var guid)
                ? NotificationIdForServer(guid)
                : ForegroundSummaryNotificationId + 1;

            var builder = new NotificationCompat.Builder(this, NotificationChannelId);
            builder.SetSmallIcon(Resource.Mipmap.appicon);
            builder.SetContentTitle(serverName);
            builder.SetContentText("Monitoring active");
            builder.SetOngoing(true);
            builder.SetAutoCancel(false);
            builder.SetOnlyAlertOnce(true);
            builder.SetGroup(NotificationGroupKey);

            // Optional: tap notification to open the app
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

        private static bool TryGetGuid(string value, out System.Guid guid) => System.Guid.TryParse(value, out guid);

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