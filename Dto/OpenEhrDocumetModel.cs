using System.Text.Json.Serialization;

namespace OpenEhr.Dto;

public class OpenEhrDocumetModel
{
    [JsonPropertyName("start_time")]
    public DateTime StartTime { get; set; }
    [JsonPropertyName("ehr_case_id")] 
    public Guid EhrCaseId { get; set; }
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;
    [JsonPropertyName("cuid")]
    public string Cuid { get; set; } = null!;
    [JsonPropertyName("link")]
    public string? Link { get; set; }
    [JsonPropertyName("atid")]
    public string Atid { get; set; } = null!;
    [JsonPropertyName("meaning")]
    public string? Meaning { get; set; }
    [JsonPropertyName("composer")]
    public string Composer { get; set; } = null!;
    [JsonPropertyName("state")]
    public string State { get; set; } = null!;
    [JsonPropertyName("tags")]
    public OpenEhrTagsContainer? Tags { get; set; } = null!;
    [JsonPropertyName("ehr_id")]
    public Guid EhrId { get; set; }
    [JsonPropertyName("vo_id")]
    public Guid VoId { get; set; }
    [JsonPropertyName("template_id")]
    public string TemplateId { get; set; } = null!;
}