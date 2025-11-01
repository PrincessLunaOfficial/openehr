using System.Text.Json.Serialization;

namespace OpenEhr.Dto;

public class ContributionResponse
{
    [JsonPropertyName("commitData")]
    public CommitData[] CommitData { get; set; }
}