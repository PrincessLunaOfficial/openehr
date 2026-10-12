using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace OpenEhr.Dto;

public class OpenEhrDocumetModel
{
    [JsonPropertyName("start_time")]
    [JsonProperty("start_time")]
    public DateTime StartTime { get; set; }
    [JsonPropertyName("ehr_case_id")]
    [JsonProperty("ehr_case_id")]
    public Guid EhrCaseId { get; set; }
    [JsonPropertyName("name")]
    [JsonProperty("name")]
    public string Name { get; set; } = null!;
    [JsonPropertyName("cuid")]
    [JsonProperty("cuid")]
    public string Cuid { get; set; } = null!;
    [JsonPropertyName("link")]
    [JsonProperty("link")]
    public string? Link { get; set; }
    [JsonPropertyName("atid")]
    [JsonProperty("atid")]
    public string Atid { get; set; } = null!;
    [JsonPropertyName("meaning")]
    [JsonProperty("meaning")]
    public string? Meaning { get; set; }
    [JsonPropertyName("composer")]
    [JsonProperty("composer")]
    public string Composer { get; set; } = null!;
    [JsonPropertyName("state")]
    [JsonProperty("state")]
    public string State { get; set; } = null!;
    [JsonPropertyName("tags")]
    [JsonProperty("tags")]
    public OpenEhrTagsContainer? Tags { get; set; } = null!;
    [JsonPropertyName("ehr_id")]
    [JsonProperty("ehr_id")]
    public Guid EhrId { get; set; }
    [JsonPropertyName("vo_id")]
    [JsonProperty("vo_id")]
    public Guid VoId { get; set; }
    [JsonPropertyName("template_id")]
    [JsonProperty("template_id")]
    public string TemplateId { get; set; } = null!;
}
