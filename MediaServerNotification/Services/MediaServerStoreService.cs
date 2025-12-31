using MediaServerNotification.Models;
using MediaServerNotification.Services.Interfaces;
using Microsoft.Maui.Storage;
using System.Text.Json;

namespace MediaServerNotification.Services;

public class MediaServerStoreService : IMediaServerStoreService
{
    private const string StorageKey = "MediaServers";
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        options.Converters.Add(new MediaServerSettingsJsonConverter());
        return options;
    }

    public List<MediaServer> GetAll()
    {
        var json = Preferences.Get(StorageKey, string.Empty);

        if (string.IsNullOrEmpty(json))
            return new List<MediaServer>();

        var list = JsonSerializer.Deserialize<List<MediaServer>>(json, SerializerOptions)
                   ?? new List<MediaServer>();

        return list;
    }

    public MediaServer? GetById(Guid id)
    {
        var all = GetAll();
        return all.FirstOrDefault(s => s.Id == id);
    }

    public void AddOrUpdate(MediaServer server)
    {
        var all = GetAll();

        var existing = all.FirstOrDefault(s => s.Id == server.Id);
        if (existing != null)
        {
            all.Remove(existing);
        }

        all.Add(server);
        Save(all);
    }

    public void Delete(Guid id)
    {
        var all = GetAll();
        all.RemoveAll(s => s.Id == id);
        Save(all);
    }

    private void Save(List<MediaServer> list)
    {
        var json = JsonSerializer.Serialize(list, SerializerOptions);
        Preferences.Set(StorageKey, json);
    }
}


