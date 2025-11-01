using System.Text.Json.Serialization;
using Newtonsoft.Json.Linq;

namespace OpenEhr.Dto;

internal class AqlResponse
{
    [JsonPropertyName("aql")]
    public string Aql { get; set; }
    [JsonPropertyName("resultSet")]
    public IEnumerable<JObject> ResultSet { get; set; }
    [JsonPropertyName("meta")]
    public JObject Meta { get; set; }
}