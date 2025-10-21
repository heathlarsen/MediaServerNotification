using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaServerNotification.Models;

public class StreamSession
{
    public StreamType StreamType;
}

public enum StreamType
{
    DirectPlay,
    DirectStream,
    Transcode
}
