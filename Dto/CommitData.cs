using System.Text.Json.Serialization;

namespace OpenEhr.Dto;

public class CommitData
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    [JsonPropertyName("href")]
    public string Href { get; set; }
    [JsonPropertyName("action")]
    public string Action { get; set; }
    public string CompositionId => Id.Split(":").FirstOrDefault() ?? string.Empty;
}