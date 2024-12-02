using Newtonsoft.Json.Linq;

namespace OpenEhr.Library;

public class DischargeDetails : IEhrProtocol
{
    public string cuid { get; set; }
    public Guid ehr_case_id { get; set; }

    public string? emp_id { get; set; }
    
    public JObject? diag_string { get; set; }
    
    public string? additional_icd10_kods { get; set; }
    
    public string? final_icd10_kod { get; set; }

    public DateTime start_time { get; set; }

    public OpenEhrTagsContainer tags_str { get; set; }

    public DateTime row_ts { get; set; }

    public DateTime? tags_sign_dt { get; set; }
}