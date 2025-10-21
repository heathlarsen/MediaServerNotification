using MediaServerNotification.Models;
using MediaServerNotification.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MediaServerNotification.Services;

public class PlexMediaServerService : IMediaServerService<PlexMediaServerSettings>
{
    private readonly HttpClient httpClient;

    public PlexMediaServerService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
        //httpClient.BaseAddress = new UriBuilder(_config.ServerAddress).Uri;
    }

    public async Task<List<StreamSession>> GetStreamSessionsAsync(PlexMediaServerSettings settings)
    {
        var streams = new List<StreamSession>();

        // Request the current stream info from the Plex server
        using (var response = await httpClient.GetAsync($"{settings.ServerAddress}/status/sessions?X-Plex-Token={settings.PlexToken}"))
        {
            // Convert response into an XmlDocument
            XmlDocument xmlDoc = new XmlDocument();
            var xml = await response.Content.ReadAsStringAsync();
            xmlDoc.LoadXml(xml);

            // Access the Element and Attribute containing the stream type and turn it into a response object
            XmlNodeList xmlTags = xmlDoc.GetElementsByTagName("Part");
            foreach (XmlNode xmlTag in xmlTags)
            {
                var decision = xmlTag.Attributes["decision"]?.InnerText;
                var stream = new StreamSession()
                {
                    StreamType = ToStreamType(decision),
                };

                streams.Add(stream);
            }
        }

        return streams;
    }

    public async Task<ServerResources> GetResourcesAsync(PlexMediaServerSettings settings)
    {
        var resources = new ServerResources();

        // Request the current system resource info from the Plex server
        //httpClient.BaseAddress = new UriBuilder(settings.ServerAddress).Uri;
        using (var response = await httpClient.GetAsync($"{settings.ServerAddress}/statistics/resources?X-Plex-Token={settings.PlexToken}&timespan=6"))
        {
            // Convert response into an XmlDocument
            XmlDocument xmlDoc = new XmlDocument();
            var xml = await response.Content.ReadAsStringAsync();
            xmlDoc.LoadXml(xml);

            // Access the Element and Attribute containing each resource
            var hostCpuValues = new List<double>();
            var processCpuValues = new List<double>();
            var hostMemoryValues = new List<double>();
            var processMemoryValues = new List<double>();
            XmlNodeList xmlTags = xmlDoc.GetElementsByTagName("StatisticsResources");

            if (xmlTags.Count == 0)
            {
                return resources;
            }

            foreach (XmlNode xmlTag in xmlTags)
            {
                if (Double.TryParse(xmlTag.Attributes["hostCpuUtilization"]?.InnerText, out var hostCpuValue))
                    hostCpuValues.Add(hostCpuValue);

                if (Double.TryParse(xmlTag.Attributes["processCpuUtilization"]?.InnerText, out var processCpuValue))
                    processCpuValues.Add(processCpuValue);

                if (Double.TryParse(xmlTag.Attributes["hostMemoryUtilization"]?.InnerText, out var hostMemoryValue))
                    hostMemoryValues.Add(hostMemoryValue);

                if (Double.TryParse(xmlTag.Attributes["processMemoryUtilization"]?.InnerText, out var processMemoryValue))
                    processMemoryValues.Add(processMemoryValue);
            }

            // Turn it into a response object
            resources = new ServerResources()
            {
                HostCpuUsagePercent = hostCpuValues.Average(),
                ProcessCpuUsagePercent = processCpuValues.Average(),
                HostMemoryUsagePercent = hostMemoryValues.Average(),
                ProcessMemoryUsagePercent = processMemoryValues.Average(),
            };
        }

        return resources;
    }

    private StreamType ToStreamType(string plexStreamType) => plexStreamType switch
    {
        "directplay" => StreamType.DirectPlay,
        "directstream" => StreamType.DirectStream,
        "transcode" => StreamType.Transcode,
        _ => StreamType.DirectPlay // Sometimes Plex just doesn't give a stream decision, but it seems to indicate direct play (at least according to Tautulli)
                                   //_ => throw new ArgumentOutOfRangeException(nameof(plexStreamType), $"Unknown Plex stream type: {plexStreamType}"),
    };
}
