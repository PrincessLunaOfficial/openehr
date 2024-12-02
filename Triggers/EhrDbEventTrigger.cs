namespace OpenEhr.Triggers;

public class EhrDbEventTriggerAQLs
{
    public string aql { get; set; }
}

public class EhrDbEventTrigger
{
    public int? id { get; set; }
    public string? name { get; set; }
    public bool? active { get; set; }
    public int eventTimeToLiveHours { get; set; }
    public int repeatIntervalMinutes { get; set; }
    public bool runImmediately { get; set; }
    public EhrDbEventTriggerAQLs aqls { get; set; }
    public string[] archetypeIds { get; set; }
    public string[] templateIds { get; set; }
    public string? httpResultUrl { get; set; }
    public bool population { get; set; }
    public DateTime? created { get; set; }
    public string? createdBy { get; set; }
    public DateTime? updated { get; set; }
    public string? updatedBy { get; set; }
    public string? kafkaServers { get; set; }
    public string? kafkaResultTopic { get; set; }
    public int? parentTriggerId { get; set; }
}