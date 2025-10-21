using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaServerNotification.Models;

//public class MediaServerSettings : IMediaServerSettings
//{
//    IPreferences settings;

//    public MediaServerSettings(IPreferences settings)
//    {
//        this.settings = settings;
//    }

//    public MediaServerType ServerType
//    {
//        get
//        {
//            var typeString = settings.Get("Server.Type", String.Empty);
//            if (Enum.TryParse<MediaServerType>(typeString, out var type))
//                return type;
//            throw new ApplicationException($"Unknown media server type {typeString}");
//        }
//        set => settings.Set("Server.Type", value.ToString() ?? String.Empty);
//    }

//    public string ServerAddress
//    {
//        get => settings.Get("Server.Address", String.Empty);
//        set => settings.Set("Server.Address", value);
//    }

//    public int HostMemoryCapactity
//    {
//        get => settings.Get("Host.Memory", 0);
//        set => settings.Set("Host.Memory", value);
//    }

//    public string PlexToken
//    {
//        get => settings.Get("Server.Token", String.Empty);
//        set => settings.Set("Server.Token", value);
//    }

//    public bool EnableNotification
//    {
//        get => settings.Get("App.EnableNotification", false);
//        set => settings.Set("App.EnableNotification", value);
//    }

//    public int NotificationUpdateFrequency
//    {
//        get => settings.Get("App.NotificationUpdateFrequency", 60);
//        set => settings.Set("App.NotificationUpdateFrequency", value);
//    }
//}

public abstract class MediaServerSettings
{
    public string Name { get; set; } = string.Empty;
    public string ServerAddress { get; set; } = string.Empty;
    public int HostMemoryCapactity { get; set; }
    public bool EnableNotification { get; set; }
    public int NotificationUpdateFrequency { get; set; }
}

public class PlexMediaServerSettings : MediaServerSettings
{
    public string PlexToken { get; set; }
}

public class EmbyMediaServerSettings : MediaServerSettings
{
}

public class JellyfinMediaServerSettings : MediaServerSettings
{
}