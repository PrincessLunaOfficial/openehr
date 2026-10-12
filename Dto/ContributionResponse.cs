using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace OpenEhr.Dto;

public class ContributionResponse
{
    [JsonPropertyName("commitData")]
    [JsonProperty("commitData")]
    public CommitData[] CommitData { get; set; }
}
