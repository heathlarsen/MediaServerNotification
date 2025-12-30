using System.ComponentModel;

namespace MediaServerNotification.Models;

public class StreamSession
{
    public StreamType StreamType;
}

public enum StreamType
{
    [Description("Direct Play")]
    DirectPlay,
    [Description("Direct Stream")]
    DirectStream,
    [Description("Transcode")]
    Transcode
}
