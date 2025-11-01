using System.Text.Json.Serialization;

namespace OpenEhr.Dto;

public class EhrPlatformVersion
{
    [JsonPropertyName("buildTime")]
    public string BuildTime { get; set; }
    [JsonPropertyName("version")]
    public string Version { get; set; }
}