using MediaServerNotification.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaServerNotification.Services.Interfaces;

public interface IMediaServerManagerService
{
    List<MediaServer> GetAll();
    MediaServer? GetById(Guid id);
    void AddOrUpdate(MediaServer server);
    void Delete(Guid id);
}
