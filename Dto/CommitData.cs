using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace OpenEhr.Dto;

public class CommitData
{
    [JsonPropertyName("id")]
    [JsonProperty("id")]
    public string Id { get; set; }
    [JsonPropertyName("href")]
    [JsonProperty("href")]
    public string Href { get; set; }
    [JsonPropertyName("action")]
    [JsonProperty("action")]
    public string Action { get; set; }
    public string CompositionId => Id.Split(":").FirstOrDefault() ?? string.Empty;
}
