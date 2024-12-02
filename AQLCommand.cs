using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OpenEhr;

public class AqlCommand
{
    public AQLParameterCollection Parameters = new();

    public AqlCommand(string query, EhrConnection connection)
    {
        this.Query = query;
        this.Connection = connection;
    }

    public bool Debug { get; set; }
    private string Query { get; set; }
    private EhrConnection Connection { get; }

    private void ApplyParametersToQuery()
    {
        foreach (var parameter in Parameters.InternalList)
            Query = Query.Replace(!parameter.Name.StartsWith(":") ? $":{parameter.Name}" : $"{parameter.Name}", parameter.Value);
    }

    public string GetQueryModel()
    {
        ApplyParametersToQuery();
        return Query;
    }

    public string GetQueryModelEscapeDataString()
    {
        ApplyParametersToQuery();
        return Uri.EscapeDataString(Query);
    }

    public async Task<AQLDataReader> ExecuteReaderAsync()
    {
        var sendContent = new JObject();
        Connection.EhrConnectionLogger.LogDebug("ApplyParametersToQuery");
        ApplyParametersToQuery();
        sendContent["aql"] = Query;
        sendContent["aqlParameters"] = new JObject();
        Connection.EhrConnectionLogger.LogDebug("Content: {content}", sendContent.ToString());
        var content = new StringContent(sendContent.ToString(), Encoding.UTF8, "application/json");
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{Connection.Url}query");
        requestMessage.Headers.TryAddWithoutValidation("Authorization", Connection.GetCredentials());
        requestMessage.Content = content;
        Connection.EhrConnectionLogger.LogDebug("requestMessage is null ? {requestMessage}", requestMessage == null);
        Connection.EhrConnectionLogger.LogDebug("Connection.Client is null ? {ConnectionClient}", Connection.Client == null);
        Connection.EhrConnectionLogger.LogDebug("Sending requestMessage");
        var response = await Connection.Client.SendAsync(requestMessage);
        var responseBody = await response.Content.ReadAsStringAsync();
        Connection.EhrConnectionLogger.LogDebug("responseBody: {responseBody}", responseBody.ToString());
        if (response.StatusCode != HttpStatusCode.OK)
            return new AQLDataReader(Array.Empty<JObject>(), response.StatusCode, responseBody, ref Connection.EhrConnectionLogger);
        var aqlResponse = JsonConvert.DeserializeObject<AQLResponse>(responseBody);
        Connection.EhrConnectionLogger.LogDebug("StatusCode: {StatusCode}", response.StatusCode);
        Connection.EhrConnectionLogger.LogDebug("aqlResponse is null ? {aqlResponse}", aqlResponse == null);
        return new AQLDataReader(aqlResponse!.resultSet, response.StatusCode, responseBody, ref Connection.EhrConnectionLogger);
    }

    public async Task<List<JObject>> ExecuteRawQueryAsync()
    {
        var responseBody = string.Empty;
        ApplyParametersToQuery();

        var resultSet = new List<JObject>();
        try
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{Connection.Url}?aql={Uri.EscapeUriString(Query)}");
            var response = await Connection.Client.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            responseBody = await response.Content.ReadAsStringAsync();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var aqlResponse = JsonConvert.DeserializeObject<AQLResponse>(responseBody);
                foreach (var queryObject in aqlResponse.resultSet) resultSet.Add(queryObject);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"AQLCommand exception: {ex.Message}");
        }

        return resultSet;
    }
}