using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenEhr.Dto;
using Serilog;

namespace OpenEhr;

public class AqlCommand
{
    public AQLParameterCollection Parameters = new AQLParameterCollection();

    public AqlCommand(string query, EhrConnection connection)
    {
        this.Query = query;
        this.Connection = connection;
    }

    private string Query { get; set; }
    private EhrConnection Connection { get; }

    private void ApplyParametersToQuery()
    {
        foreach (AqlParameter parameter in Parameters.InternalList)
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
        JObject sendContent = new JObject();
        Log.Logger.Debug("ApplyParametersToQuery");
        ApplyParametersToQuery();
        sendContent["aql"] = Query;
        sendContent["aqlParameters"] = new JObject();
        Log.Logger.Debug("Content: {content}", sendContent.ToString());
        StringContent content = new StringContent(sendContent.ToString(), Encoding.UTF8, "application/json");
        HttpRequestMessage? requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{Connection.Url}query");
        requestMessage.Headers.TryAddWithoutValidation("Authorization", Connection.GetCredentials());
        requestMessage.Content = content;
        Log.Logger.Debug("requestMessage is null ? {requestMessage}", requestMessage == null);
        Log.Logger.Debug("Connection.Client is null ? {ConnectionClient}", Connection.Client == null);
        Log.Logger.Debug("Sending requestMessage");
        HttpResponseMessage response = await Connection.Client.SendAsync(requestMessage);
        string responseBody = await response.Content.ReadAsStringAsync();
        Log.Logger.Debug("responseBody: {responseBody}", responseBody.ToString());
        if (response.StatusCode != HttpStatusCode.OK)
            return new AQLDataReader(Array.Empty<JObject>(), response.StatusCode, responseBody);
        AqlResponse? aqlResponse = JsonConvert.DeserializeObject<AqlResponse>(responseBody);
        Log.Logger.Debug("StatusCode: {StatusCode}", response.StatusCode);
        Log.Logger.Debug("aqlResponse is null ? {aqlResponse}", aqlResponse == null);
        return new AQLDataReader(aqlResponse!.ResultSet, response.StatusCode, responseBody);
    }

    public async Task<List<JObject>> ExecuteRawQueryAsync()
    {
        ApplyParametersToQuery();

        List<JObject> resultSet = new List<JObject>();
        try
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{Connection.Url}?aql={Uri.EscapeUriString(Query)}");
            HttpResponseMessage response = await Connection.Client.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                AqlResponse? aqlResponse = JsonConvert.DeserializeObject<AqlResponse>(responseBody);
                foreach (JObject queryObject in aqlResponse.ResultSet) resultSet.Add(queryObject);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"AQLCommand exception: {ex.Message}");
        }

        return resultSet;
    }
}