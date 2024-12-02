namespace OpenEhr.Library;

public class AdmissionDetails : IEhrProtocol
{
    public string cuid { get; set; }
    public Guid ehr_case_id { get; set; }

    public string admission_reason { get; set; }

    public DateTime start_time { get; set; }

    public OpenEhrTagsContainer tags_str { get; set; }

    public DateTime row_ts { get; set; }

    public DateTime? tags_sign_dt { get; set; }
}