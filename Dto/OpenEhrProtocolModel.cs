using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OpenEhr.Dto;

public class OpenEhrProtocolModel
{
    [JsonPropertyName("domain")]
    [JsonProperty("domain")]
    public string Domain { get; set; }
    [JsonPropertyName("ehrUid")]
    [JsonProperty("ehrUid")]
    public Guid EhrUid { get; set; }
    [JsonPropertyName("operationType")]
    [JsonProperty("operationType")]
    public string OperationType { get; set; }
    [JsonPropertyName("eventName")]
    [JsonProperty("eventName")]
    public string EventName { get; set; }
    [JsonPropertyName("compositionId")]
    [JsonProperty("compositionId")]
    public string CompositionId { get; set; }
    [JsonPropertyName("oldResults")]
    [JsonProperty("oldResults")]
    public JArray OldResults { get; set; }
    [JsonPropertyName("newResults")]
    [JsonProperty("newResults")]
    public JArray NewResults { get; set; }
}
