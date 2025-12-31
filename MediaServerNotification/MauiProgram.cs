using MediaServerNotification.Models;
using MediaServerNotification.Services;
using MediaServerNotification.Services.Interfaces;
using Microsoft.Extensions.Logging;
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
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddRadzenComponents();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // Register core services for storing and polling media servers
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<IMediaServerStoreService, MediaServerStoreService>();
        builder.Services.AddSingleton<IMediaServerStateService, MediaServerStateService>();
        builder.Services.AddScoped<IMediaServerClient<PlexMediaServerSettings>, PlexMediaServerClient>();

#if ANDROID
        builder.Services.AddSingleton(sp => new Platforms.Android.AndroidNotificationService(Android.App.Application.Context));
        builder.Services.AddSingleton<INotificationService>(sp => sp.GetRequiredService<Platforms.Android.AndroidNotificationService>());
#else
        builder.Services.AddSingleton<INotificationService, NoopNotificationService>();
#endif

        var app = builder.Build();

        return app;
    }
}
