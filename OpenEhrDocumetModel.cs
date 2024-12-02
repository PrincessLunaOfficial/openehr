namespace OpenEhr;

public class OpenEhrDocumetModel
{
    public DateTime start_time { get; set; }
    public Guid ehr_case_id { get; set; }
    public string name { get; set; } = null!;
    public string cuid { get; set; } = null!;
    public string? link { get; set; }
    public string atid { get; set; } = null!;
    public string? meaning { get; set; }
    public string composer { get; set; } = null!;
    public string state { get; set; } = null!;
    public OpenEhrTagsContainer? tags { get; set; } = null!;
    public Guid ehr_id { get; set; }
    public Guid vo_id { get; set; }
    public string template_id { get; set; } = null!;
}