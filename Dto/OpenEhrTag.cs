using System.Text.Json.Serialization;

namespace OpenEhr.Dto;

public class OpenEhrTagsContainer
{
    [JsonPropertyName("tags")]
    public OpenEhrTag[]? Tags { get; set; }
}

public class OpenEhrTag
{
    [JsonPropertyName("tag")]
    public string Tag { get; set; } = null!;
    [JsonPropertyName("value")]
    public string Value { get; set; } = null!;
    [JsonPropertyName("aqlPath")]
    public string AqlPath { get; set; } = null!;
}