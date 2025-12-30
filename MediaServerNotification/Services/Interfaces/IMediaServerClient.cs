using MediaServerNotification.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaServerNotification.Services.Interfaces;

public interface IMediaServerClient<TMediaServerSettings>
    where TMediaServerSettings : MediaServerSettings
{
    Task<List<StreamSession>> GetStreamSessionsAsync(TMediaServerSettings settings, CancellationToken cancellationToken = default);

    Task<ServerResources> GetResourcesAsync(TMediaServerSettings settings, CancellationToken cancellationToken = default);
}
