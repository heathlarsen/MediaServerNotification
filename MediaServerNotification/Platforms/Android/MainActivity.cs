using Android.App;
using Android.Content.PM;
using Android.OS;

namespace MediaServerNotification
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Start the foreground monitor only after notification permission is granted (Android 13+).
            _ = TryStartForegroundMonitorAsync();
        }

        private static async Task TryStartForegroundMonitorAsync()
        {
            try
            {
                if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
                {
                    var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                    if (status != PermissionStatus.Granted)
                        status = await Permissions.RequestAsync<Permissions.PostNotifications>();

                    if (status != PermissionStatus.Granted)
                        return;
                }

                Platforms.Android.MediaServerForegroundService.StartMonitoring(Android.App.Application.Context);
            }
            catch
            {
                // Best-effort: do not crash app if permission APIs or service start fails.
            }
        }
    }
}
