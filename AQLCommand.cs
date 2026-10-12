using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenEhr.Dto;

namespace OpenEhr;

/// <summary>An AQL query with immutable source text and explicitly bound literal values.</summary>
public class AqlCommand
{
    private readonly string _query;
    private readonly EhrConnection _connection;

    public AQLParameterCollection Parameters = new();

    public AqlCommand(string query, EhrConnection connection)
    {
        _query = !string.IsNullOrWhiteSpace(query) ? query : throw new ArgumentException("AQL is empty", nameof(query));
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public string GetQueryModel() => BindLiterals(_query, Parameters.InternalList);

    public string GetQueryModelEscapeDataString() => Uri.EscapeDataString(GetQueryModel());

    public Task<AQLDataReader> ExecuteReaderAsync() => ExecuteReaderAsync(CancellationToken.None);

    public async Task<AQLDataReader> ExecuteReaderAsync(CancellationToken cancellationToken)
    {
        string query = GetQueryModel();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_connection.Url}query");
        request.Headers.TryAddWithoutValidation("Authorization", _connection.GetCredentials());
        request.Content = new StringContent(
            new JObject { ["aql"] = query, ["aqlParameters"] = new JObject() }.ToString(Formatting.None),
            Encoding.UTF8,
            "application/json");
        using HttpResponseMessage response = await _connection.Client.SendAsync(request, cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.StatusCode != HttpStatusCode.OK)
            throw new EhrApiException("AQL query failed", response.StatusCode);

        AqlResponse? payload;
        try { payload = JsonConvert.DeserializeObject<AqlResponse>(body); }
        catch (JsonException ex) { throw new EhrContractException("Invalid AQL response JSON", ex); }
        if (payload?.ResultSet == null)
            throw new EhrContractException("AQL response has no resultSet");
        return new AQLDataReader(payload.ResultSet, response.StatusCode, body);
    }

    public Task<List<JObject>> ExecuteRawQueryAsync() => ExecuteRawQueryAsync(CancellationToken.None);

    public async Task<List<JObject>> ExecuteRawQueryAsync(CancellationToken cancellationToken)
    {
        var reader = await ExecuteReaderAsync(cancellationToken);
        var result = new List<JObject>();
        while (await reader.ReadAsync()) result.Add(reader.GetObject());
        return result;
    }

    internal static string BindLiterals(string query, IReadOnlyList<AqlParameter> parameters)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var parameter in parameters)
        {
            string name = parameter.Name.TrimStart(':');
            if (name.Length == 0 || !values.TryAdd(name, parameter.Value))
                throw new ArgumentException($"Invalid or duplicate AQL parameter: {parameter.Name}");
        }

        var used = new HashSet<string>(StringComparer.Ordinal);
        var result = new StringBuilder(query.Length);
        bool inString = false;
        for (int i = 0; i < query.Length; i++)
        {
            char ch = query[i];
            if (ch == '\'')
            {
                result.Append(ch);
                if (inString && i + 1 < query.Length && query[i + 1] == '\'')
                    result.Append(query[++i]);
                else
                    inString = !inString;
                continue;
            }
            if (!inString && ch == ':' && i + 1 < query.Length &&
                (char.IsAsciiLetter(query[i + 1]) || query[i + 1] == '_'))
            {
                int start = ++i;
                while (i + 1 < query.Length &&
                       (char.IsAsciiLetterOrDigit(query[i + 1]) || query[i + 1] == '_')) i++;
                string name = query[start..(i + 1)];
                if (!values.TryGetValue(name, out string? literal))
                    throw new ArgumentException($"Missing AQL parameter: {name}");
                used.Add(name);
                result.Append(literal);
                continue;
            }
            result.Append(ch);
        }
        if (inString) throw new ArgumentException("Unclosed AQL string literal");
        foreach (string name in values.Keys)
            if (!used.Contains(name)) throw new ArgumentException($"Unused AQL parameter: {name}");
        return result.ToString();
    }
}
