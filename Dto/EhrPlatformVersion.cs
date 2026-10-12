using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace OpenEhr.Dto;

public class EhrPlatformVersion
{
    [JsonPropertyName("buildTime")]
    [JsonProperty("buildTime")]
    public string BuildTime { get; set; }
    [JsonPropertyName("version")]
    [JsonProperty("version")]
    public string Version { get; set; }
}
