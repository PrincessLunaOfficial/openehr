using Newtonsoft.Json.Linq;

namespace OpenEhr;

public class OpenEhrProtocolModel
{
    public string domain { get; set; }
    public Guid ehrUid { get; set; }
    public string operationType { get; set; }
    public string eventName { get; set; }
    public string compositionId { get; set; }
    public JArray oldResults { get; set; }
    public JArray newResults { get; set; }
}