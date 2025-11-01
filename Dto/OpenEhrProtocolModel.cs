using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace OpenEhr.Dto;

public class OpenEhrProtocolModel
{
    [JsonPropertyName("domain")]
    public string Domain { get; set; }
    [JsonPropertyName("ehrUid")]
    public Guid EhrUid { get; set; }
    [JsonPropertyName("operationType")]
    public string OperationType { get; set; }
    [JsonPropertyName("eventName")]
    public string EventName { get; set; }
    [JsonPropertyName("compositionId")]
    public string CompositionId { get; set; }
    [JsonPropertyName("oldResults")]
    public JArray OldResults { get; set; }
    [JsonPropertyName("newResults")]
    public JArray NewResults { get; set; }
}