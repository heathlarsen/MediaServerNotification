using Android.App;
using Android.Content;
using Android.OS;
using MediaServerNotification.Models;
using MediaServerNotification.Services.Interfaces;

namespace MediaServerNotification.Platforms.Android
{
    [Service(Exported = true)]
    public class MediaServerForegroundService : Service
    {
        private AndroidNotificationService? _notificationService;
        private IMediaServerStateService? _stateService;
        private bool _subscribed;

        public override void OnCreate()
        {
            base.OnCreate();

            // Resolve from MAUI DI container; fall back to direct creation if not available yet.
            var services = global::Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services;
            _notificationService = services?.GetService<AndroidNotificationService>() ?? new AndroidNotificationService(this);
            _notificationService.EnsureChannels();
        }

        public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
        {
            var enabledServers = GetEnabledServers();

            // One foreground notification is required; keep it as a group summary.
            var summaryNotification = _notificationService?.BuildSummaryNotification(enabledServers.Count);
            if (summaryNotification is not null)
                StartForeground(AndroidNotificationService.ForegroundSummaryNotificationId, summaryNotification);

            // Post one ongoing notification per server (these can be many).
            if (_notificationService is not null)
            {
                foreach (var server in enabledServers)
                {
                    _notificationService.ShowOrUpdateServerNotification(server);
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
                    _stateService.ServerDeleted -= OnServerDeleted;
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
                _stateService.ServerDeleted += OnServerDeleted;
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
                if (_notificationService is null)
                    return;

                // Update summary text.
                _notificationService.ShowOrUpdateSummaryNotification(enabledCount);

                // Keep per-server notifications in sync with enabled set (cancel any that were disabled).
                var enabledServers = GetEnabledServers();
                var enabledIds = enabledServers.Select(s => s.Id).ToHashSet();

                // Cancel notifications for servers that exist in the store but are no longer enabled.
                var allServers = GetAllServers();
                foreach (var server in allServers.Where(s => !enabledIds.Contains(s.Id)))
                {
                    _notificationService.RemoveServerNotification(server.Id);
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
                if (_notificationService is null)
                    return;

                // Only update notifications for servers that still have notifications enabled.
                if (server?.Settings?.EnableNotification != true)
                    return;

                _notificationService.ShowOrUpdateServerNotification(server);
            }
            catch
            {
                // ignore
            }
        }

        private void OnServerDeleted(Guid serverId)
        {
            try
            {
                if (_notificationService is null)
                    return;

                _notificationService.RemoveServerNotification(serverId);
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