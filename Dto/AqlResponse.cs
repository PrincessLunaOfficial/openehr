using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OpenEhr.Dto;

internal class AqlResponse
{
    [JsonPropertyName("aql")]
    [JsonProperty("aql")]
    public string Aql { get; set; }
    [JsonPropertyName("resultSet")]
    [JsonProperty("resultSet")]
    public IEnumerable<JObject> ResultSet { get; set; }
    [JsonPropertyName("meta")]
    [JsonProperty("meta")]
    public JObject Meta { get; set; }
}
