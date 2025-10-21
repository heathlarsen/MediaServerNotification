using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using MediaServerNotification.Models;

namespace MediaServerNotification.Models
{
    public class MediaServerSettingsJsonConverter : JsonConverter<MediaServer>
    {
        public override MediaServer? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            if (!root.TryGetProperty("ServerType", out var typeProp))
                throw new JsonException("Missing ServerType property");

            if (typeProp.ValueKind != JsonValueKind.Number)
                throw new InvalidOperationException($"MediaServerType value '{typeProp.ToString()}' could not be parsed.");

            var intVal = typeProp.GetInt32();
            if (!Enum.IsDefined(typeof(MediaServerType), intVal))
                throw new ArgumentOutOfRangeException($"MediaServerType value '{intVal}' not supported.");

            var type = (MediaServerType)intVal;
            Type targetType = type switch
            {
                MediaServerType.Plex => typeof(PlexMediaServer),
                MediaServerType.Emby => typeof(EmbyMediaServer),
                MediaServerType.Jellyfin => typeof(JellyfinMediaServer),
                _ => throw new JsonException($"Unknown ServerType: {type}")
            };

            var json = root.GetRawText();
            return (MediaServer?)JsonSerializer.Deserialize(json, targetType, options);
        }

        public override void Write(Utf8JsonWriter writer, MediaServer value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}
