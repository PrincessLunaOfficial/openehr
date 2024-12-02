namespace OpenEhr.Views;

public class EhrDbView
{
    public int? id { get; set; }
    public string? name { get; set; }
    public string? description { get; set; }
    public string? importExportName { get; set; }
    public string? js { get; set; }
    public bool cacheable { get; set; }
    public DateTime? created { get; set; }
    public string? createdBy { get; set; }
    public DateTime? updated { get; set; }
    public string? updatedBy { get; set; }
    public int? parentId { get; set; }
}