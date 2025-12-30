using MediaServerNotification.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaServerNotification.Services.Interfaces;

internal interface IMediaServerStateService
{
    Task RefreshServerState(MediaServer server);
}
