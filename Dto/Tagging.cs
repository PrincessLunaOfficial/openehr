using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace OpenEhr.Dto;

public class Tagging
{
    [JsonPropertyName("compositionUid")]
    [JsonProperty("compositionUid")]
    public string CompositionUid { get; set; }
    [JsonPropertyName("tags")]
    [JsonProperty("tags")]
    public OpenEhrTag[]? Tags { get; set; }

    public Tagging(string compositionUid, OpenEhrTag[]? tags)
    {
        this.CompositionUid = compositionUid;
        this.Tags = tags;
    }
}
