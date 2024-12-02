namespace OpenEhr.Library;

public class TriageDetails : IEhrProtocol
{
    public string cuid { get; set; }
    public Guid ehr_case_id { get; set; }

    public string urgency { get; set; }
    
    public DateTime start_time { get; set; }
    
    public OpenEhrTagsContainer tags_str { get; set; }
    
    public DateTime row_ts { get; set; }
    
    public DateTime? tags_sign_dt { get; set; }
}