using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaServerNotification.Models;

public abstract class MediaServer
{
    public MediaServer(MediaServerType serverType)
    {
        Id = Guid.NewGuid();
        ServerType = serverType;
    }

    public Guid Id { get; set; }
    public MediaServerType ServerType { get; set; }
    public MediaServerStats Stats { get; set; } = new();
    public abstract MediaServerSettings Settings { get; } // Using Get only because of dervived covarient types
}

public class PlexMediaServer : MediaServer
{
    public PlexMediaServer(PlexMediaServerSettings settings)
        : base(MediaServerType.Plex)
    {
        // Ensure the deserialized settings object is applied to the read-only property
        Settings = settings ?? new PlexMediaServerSettings();
    }

    public override PlexMediaServerSettings Settings { get; } = new();
}

public class EmbyMediaServer : MediaServer
{
    public EmbyMediaServer(EmbyMediaServerSettings settings)
        : base(MediaServerType.Emby)
    {
        // Ensure the deserialized settings object is applied to the read-only property
        Settings = settings ?? new EmbyMediaServerSettings();
    }

    public override EmbyMediaServerSettings Settings { get; } = new();
}

public class JellyfinMediaServer : MediaServer
{
    public JellyfinMediaServer(JellyfinMediaServerSettings settings)
        : base(MediaServerType.Jellyfin)
    {
        // Ensure the deserialized settings object is applied to the read-only property
        Settings = settings ?? new JellyfinMediaServerSettings();
    }

    public override JellyfinMediaServerSettings Settings { get; } = new();
}
