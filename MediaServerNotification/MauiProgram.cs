using MediaServerNotification.Models;
using MediaServerNotification.Services;
using MediaServerNotification.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using System.Net.Http;
using Microsoft.Maui;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Radzen;

namespace MediaServerNotification;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseLocalNotification()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddRadzenComponents();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
        builder.Services.AddHttpClient(); // Should this be named etc?
        builder.Services.AddSingleton<IMediaServerManagerService, MediaServerManagerService>();
        builder.Services.AddScoped<IMediaServerService<PlexMediaServerSettings>, PlexMediaServerService>();
#endif

#if ANDROID
        //builder.Services.AddSingleton<Services.INotificationService, Platforms.Android.NotificationService>();
#endif

        return builder.Build();
    }
}
