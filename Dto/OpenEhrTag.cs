using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace OpenEhr.Dto;

public class OpenEhrTagsContainer
{
    [JsonPropertyName("tags")]
    [JsonProperty("tags")]
    public OpenEhrTag[]? Tags { get; set; }
}

public class OpenEhrTag
{
    [JsonPropertyName("tag")]
    [JsonProperty("tag")]
    public string Tag { get; set; } = null!;
    [JsonPropertyName("value")]
    [JsonProperty("value")]
    public string Value { get; set; } = null!;
    [JsonPropertyName("aqlPath")]
    [JsonProperty("aqlPath")]
    public string AqlPath { get; set; } = null!;
}
