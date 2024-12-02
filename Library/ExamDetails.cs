namespace OpenEhr.Library;

public class ExamDetails : IEhrProtocol
{
    public Guid ehr_case_id { get; set; }

    public string admission_reason { get; set; }

    public DateTime start_time { get; set; }
    
    public OpenEhrTagsContainer tags_str { get; set; }

    public DateTime row_ts { get; set; }

    public DateTime? tags_sign_dt { get; set; }
    
    public string? emp_id { get; set; }
    
    public DateTime time_created { get; set; }
    
    public string template_id { get; set; }
    
    public string dept { get; set; }
    
    public string cuid { get; set; }
    
    public string name { get; set; }
    
    public Guid uuid { get; set; }
}