using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaServerNotification.Models;

public class MediaServerStats
{
    public ServerResources Resources { get; set; } = new();
    public List<StreamSession> Streams { get; set; } = new();
}
