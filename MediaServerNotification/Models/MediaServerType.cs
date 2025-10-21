using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaServerNotification.Models;

public enum MediaServerType
{
    Plex,
    Emby,
    Jellyfin
}

public static class MediaServerTypeExtensions
{
    public static MediaServer ToMediaServer(this MediaServerType mediaServerType)
    {
        return mediaServerType switch
        {
            MediaServerType.Plex => new PlexMediaServer(new PlexMediaServerSettings()),
            MediaServerType.Emby => new EmbyMediaServer(new EmbyMediaServerSettings()),
            MediaServerType.Jellyfin => new JellyfinMediaServer(new JellyfinMediaServerSettings()),
            _ => throw new ArgumentOutOfRangeException(nameof(mediaServerType), mediaServerType, null)
        };
    }

    public static string GetBrandColor(this MediaServerType mediaServerType)
    {
        return mediaServerType switch
        {
            MediaServerType.Plex => "#c69e00",
            MediaServerType.Emby => "#4caf50",
            MediaServerType.Jellyfin => "#a45cbd",
            _ => throw new ArgumentOutOfRangeException(nameof(mediaServerType), mediaServerType, null)
        };
    }
}
