using System.Text.Json.Serialization;

namespace OpenEhr.Views;

public class EhrDbView
{
    [JsonPropertyName("id")]
    public int? Id { get; set; }
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    [JsonPropertyName("importExportName")]
    public string? ImportExportName { get; set; }
    [JsonPropertyName("js")]
    public string? Js { get; set; }
    [JsonPropertyName("cacheable")]
    public bool Cacheable { get; set; }
    [JsonPropertyName("created")]
    public DateTime? Created { get; set; }
    [JsonPropertyName("createdBy")]
    public string? CreatedBy { get; set; }
    [JsonPropertyName("updated")]
    public DateTime? Updated { get; set; }
    [JsonPropertyName("updatedBy")]
    public string? UpdatedBy { get; set; }
    [JsonPropertyName("parentId")]
    public int? ParentId { get; set; }
}