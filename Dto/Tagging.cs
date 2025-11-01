using System.Text.Json.Serialization;

namespace OpenEhr.Dto;

public class Tagging
{
    [JsonPropertyName("compositionUid")]
    public string CompositionUid { get; set; }
    [JsonPropertyName("tags")]
    public OpenEhrTag[]? Tags { get; set; }

    public Tagging(string compositionUid, OpenEhrTag[]? tags)
    {
        this.CompositionUid = compositionUid;
        this.Tags = tags;
    }
}