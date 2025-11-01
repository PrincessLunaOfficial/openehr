using System.Text.Json.Serialization;

namespace OpenEhr.Triggers;

public class EhrDbEventTriggerAqLs
{
    [JsonPropertyName("aql")]
    public string Aql { get; set; }
}

public class EhrDbEventTrigger
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("active")]
    public bool? Active { get; set; }
    [JsonPropertyName("eventTimeToLiveHours")]
    public int EventTimeToLiveHours { get; set; }
    [JsonPropertyName("repeatIntervalMinutes")]
    public int RepeatIntervalMinutes { get; set; }
    [JsonPropertyName("runImmediately")]
    public bool RunImmediately { get; set; }
    [JsonPropertyName("aqls")]
    public EhrDbEventTriggerAqLs Aqls { get; set; }
    [JsonPropertyName("archetypeIds")]
    public string[] ArchetypeIds { get; set; }
    [JsonPropertyName("templateIds")]
    public string[] TemplateIds { get; set; }
    [JsonPropertyName("httpResultUrl")]
    public string? HttpResultUrl { get; set; }
    [JsonPropertyName("population")]
    public bool Population { get; set; }
    [JsonPropertyName("created")]
    public DateTime? Created { get; set; }
    [JsonPropertyName("createdBy")]
    public string? CreatedBy { get; set; }
    [JsonPropertyName("updated")]
    public DateTime? Updated { get; set; }
    [JsonPropertyName("updatedBy")]
    public string? UpdatedBy { get; set; }
    [JsonPropertyName("kafkaServers")]
    public string? KafkaServers { get; set; }
    [JsonPropertyName("kafkaResultTopic")]
    public string? KafkaResultTopic { get; set; }
    [JsonPropertyName("parentTriggerId")]
    public int? ParentTriggerId { get; set; }
}