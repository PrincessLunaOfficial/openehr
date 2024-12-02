using Newtonsoft.Json.Linq;

namespace OpenEhr;

internal class AQLResponse
{
    public string aql { get; set; }
    public IEnumerable<JObject> resultSet { get; set; }
    public JObject meta { get; set; }
}