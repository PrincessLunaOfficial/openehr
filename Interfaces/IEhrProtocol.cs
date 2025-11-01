using System.Text.Json.Serialization;
using OpenEhr.Dto;

namespace OpenEhr.Interfaces;

public interface IEhrProtocol
{
    [JsonPropertyName("cuid")]
    public string Cuid { get; set; }
    
    [JsonPropertyName("ehr_case_id")]
    public Guid EhrCaseId { get; set; }
    
    [JsonPropertyName("start_time")]
    public DateTime StartTime { get; set; }
    
    [JsonPropertyName("tags_str")]
    public OpenEhrTagsContainer? TagsStr { get; set; }
    
    [JsonPropertyName("row_ts")]
    public DateTime RowTs { get; set; }
    
    [JsonPropertyName("tags_sign_dt")]
    public DateTime? TagsSignDt { get; set; }
}