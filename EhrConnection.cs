using System.Net;
using System.Net.Http.Headers;
using System.Text;
using EMIAS.DateTimeExtensions;
using Newtonsoft.Json;
using OpenEhr.Dto;
using OpenEhr.Triggers;
using OpenEhr.Views;
using Serilog;

namespace OpenEhr;

/// <summary>
/// 
/// </summary>
public readonly struct EhrDbResponse
{
    /// <summary>
    /// 
    /// </summary>
    public string? EntityName { get; init; }
    /// <summary>
    /// 
    /// </summary>
    public HttpStatusCode? Code { get; init; }
    /// <summary>
    /// 
    /// </summary>
    public EhrDbOperationType OperationType { get; init; }
    /// <summary>
    /// 
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public string GetMessage()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"EhrDb: Operation {OperationType.ToString()} for {EntityName} returned {Code}");
        if (Code != HttpStatusCode.OK && Code != HttpStatusCode.NoContent)
            sb.AppendLine(Message);
        return sb.ToString();
    }
}

/// <summary>
/// 
/// </summary>
public enum EhrDbOperationType
{
    /// <summary>
    /// 
    /// </summary>
    Create,
    /// <summary>
    /// 
    /// </summary>
    Update,
    /// <summary>
    /// 
    /// </summary>
    SetStatus
}

/// <summary>
/// 
/// </summary>
public enum EhrDbTriggerStatus
{
    /// <summary>
    /// 
    /// </summary>
    ACTIVE,
    /// <summary>
    /// 
    /// </summary>
    DEACTIVATED
}

/// <summary>
/// 
/// </summary>
public class EhrConnection
{
    /// <summary>
    /// Builds a closed connection object ready to be used.
    /// </summary>
    /// <param name="url"></param>
    /// <param name="login"></param>
    /// <param name="password"></param>
    public EhrConnection(string url, string login, string password)
    {
        Url = url;
        if (!Url.EndsWith("/", StringComparison.Ordinal))
            Url += "/";
        Login = login;
        Password = password;
    }

    private string Login { get; }
    private string Password { get; }
    internal string Url { get; set; }
    internal string AdminUrl => Url.Replace("rest/v1", "admin/rest/v1");
    internal HttpClient Client { get; private set; } = null!;
    private bool Initialized { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public void Open()
    {
        if (Initialized) return;
        Log.Logger.Debug("Initializing HttpClient connection");
        Client = new HttpClient();
        Client.Timeout = TimeSpan.FromSeconds(1024);
        Client.DefaultRequestHeaders.ConnectionClose = false;
        Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        Client.DefaultRequestHeaders.TryAddWithoutValidation("Wait-For-Commit", "true");
        Client.DefaultRequestHeaders.TryAddWithoutValidation("Connection", "keep-alive");
        Initialized = true;
        Log.Logger.Debug("HttpClient has been initialized and is ready for use");
    }

    internal string GetCredentials()
    {
        return "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(Login + ":" + Password));
    }

    #region Trigger

    /// <summary>
    ///     Ensures that EHR has trigger up and running
    /// </summary>
    /// <param name="trigger"></param>
    /// <returns></returns>
    public async Task<EhrDbResponse[]> HasEventTrigger(EhrDbEventTrigger trigger)
    {
        EhrDbResponse[] result = new EhrDbResponse[2];
        EhrDbEventTrigger? eventTrigger = await GetTrigger(trigger);
        if (eventTrigger == null)
        {
            result[0] = await CreateEventTrigger(trigger);
            eventTrigger = await GetTrigger(trigger);
            if (eventTrigger == null)
            {
                Log.Logger.Error("Failed to get the event trigger after creation");
                return result;
            }
            result[1] = await SetTriggerStatus(eventTrigger, EhrDbTriggerStatus.ACTIVE);
            return result;
        }
        eventTrigger.Aqls = trigger.Aqls;
        if (!string.IsNullOrEmpty(trigger.HttpResultUrl))
            eventTrigger.HttpResultUrl = trigger.HttpResultUrl;
        eventTrigger.ArchetypeIds = trigger.ArchetypeIds;
        eventTrigger.TemplateIds = trigger.TemplateIds;
        if (!string.IsNullOrEmpty(trigger.KafkaResultTopic))
            eventTrigger.KafkaResultTopic = trigger.KafkaResultTopic;
        eventTrigger.EventTimeToLiveHours = trigger.EventTimeToLiveHours;
        eventTrigger.RepeatIntervalMinutes = trigger.RepeatIntervalMinutes;
        eventTrigger.Name = trigger.Name;
        result[0] = await UpdateTrigger(eventTrigger);
        eventTrigger = await GetTrigger(trigger);
        if (eventTrigger == null)
        {
            Log.Logger.Error("Failed to get the event trigger after updated");
            return result;
        }
        result[1] = await SetTriggerStatus(eventTrigger, EhrDbTriggerStatus.ACTIVE);
        return result;
    }

    /// <summary>
    ///     Gets EHR trigger by name
    /// </summary>
    /// <param name="trigger"></param>
    /// <returns></returns>
    private async Task<EhrDbEventTrigger?> GetTrigger(EhrDbEventTrigger trigger)
    {
        try
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{Url}trigger/?name={trigger.Name}");
            requestMessage.Headers.TryAddWithoutValidation("Authorization", GetCredentials());
            HttpResponseMessage response = await Client.SendAsync(requestMessage);
            string responseBody = await response.Content.ReadAsStringAsync();
            if (response.StatusCode == HttpStatusCode.OK)
                return JsonConvert.DeserializeObject<EhrDbEventTrigger>(responseBody);
        }
        catch (Exception ex)
        {
            Log.Logger.Error($"Failed to get trigger {trigger.Name}: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    ///     Creates EHR event trigger
    /// </summary>
    /// <param name="trigger"></param>
    /// <returns></returns>
    private async Task<EhrDbResponse> CreateEventTrigger(EhrDbEventTrigger trigger)
    {
        string serializedTrigger = JsonConvert.SerializeObject(trigger, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        HttpResponseMessage? response = null;
        try
        {
            StringContent content = new StringContent(serializedTrigger, null, "application/json");
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{Url}trigger/create");
            requestMessage.Headers.TryAddWithoutValidation("Authorization", GetCredentials());
            requestMessage.Content = content;
            response = await Client.SendAsync(requestMessage);
            return new EhrDbResponse { EntityName = trigger.Name, Code = response.StatusCode, OperationType = EhrDbOperationType.Create, Message = serializedTrigger };
        }
        catch (Exception ex)
        {
            Log.Logger.Error($"Failed to create trigger {trigger.Name}: {ex.Message}\r\n{serializedTrigger}");
            // Depends on the requirement, you may or may not want to return a response after logging an error
            return new EhrDbResponse { EntityName = trigger.Name, Code = response?.StatusCode ?? HttpStatusCode.InternalServerError, OperationType = EhrDbOperationType.Create, Message = "An error occurred while creating the trigger" };
        }
    }

    /// <summary>
    ///     Updates EHR event trigger
    /// </summary>
    /// <param name="trigger"></param>
    /// <returns></returns>
    private async Task<EhrDbResponse> UpdateTrigger(EhrDbEventTrigger trigger)
    {
        string serializedTrigger = JsonConvert.SerializeObject(trigger, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        HttpResponseMessage? response = null;
        try
        {
            int id = (int)trigger.Id!;
            trigger.Id = null;
            StringContent content = new StringContent(serializedTrigger, null, "application/json");
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Put, $"{Url}trigger/update/{id}");
            requestMessage.Headers.TryAddWithoutValidation("Authorization", GetCredentials());
            requestMessage.Content = content;
            response = await Client.SendAsync(requestMessage);
            return new EhrDbResponse { EntityName = trigger.Name, Code = response.StatusCode, OperationType = EhrDbOperationType.Update, Message = serializedTrigger };
        }
        catch (Exception ex)
        {
            Log.Logger.Error($"Failed to update trigger {trigger.Name}: {ex.Message}\r\n{serializedTrigger}");
            // Depends on the requirement, you may or may not want to return a response after logging an error
            return new EhrDbResponse { EntityName = trigger.Name, Code = response?.StatusCode ?? HttpStatusCode.InternalServerError, OperationType = EhrDbOperationType.Update, Message = "An error occurred while updating the trigger" };
        }
    }

    /// <summary>
    ///     Updates EHR trigger status
    /// </summary>
    /// <param name="trigger"></param>
    /// <param name="status"></param>
    /// <returns></returns>
    private async Task<EhrDbResponse> SetTriggerStatus(EhrDbEventTrigger trigger, EhrDbTriggerStatus status)
    {
        string serializedTrigger = JsonConvert.SerializeObject(trigger, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        HttpResponseMessage? response = null;
        try
        {
            StringContent content = new StringContent(serializedTrigger, null, "application/json");
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Put, $"{Url}trigger/{trigger.Id}/status/{status.ToString()}");
            requestMessage.Headers.TryAddWithoutValidation("Authorization", GetCredentials());
            requestMessage.Content = content;
            response = await Client.SendAsync(requestMessage);
            return new EhrDbResponse { EntityName = trigger.Name, Code = response.StatusCode, OperationType = EhrDbOperationType.SetStatus, Message = serializedTrigger };
        }
        catch (Exception ex)
        {
            Log.Logger.Error($"Failed to set status for trigger {trigger.Name}: {ex.Message}\r\n{serializedTrigger}");
            // Depends on the requirement, you may or may not want to return a response after logging an error
            return new EhrDbResponse { EntityName = trigger.Name, Code = response?.StatusCode ?? HttpStatusCode.InternalServerError, OperationType = EhrDbOperationType.SetStatus, Message = "An error occurred while setting the status of the trigger" };
        }
    }

    #endregion

    #region View

    /// <summary>
    ///     Ensures that EHR has view up and running
    /// </summary>
    /// <param name="view"></param>
    /// <returns></returns>
    public async Task<EhrDbResponse[]> HasView(EhrDbView view)
    {
        EhrDbResponse[] result = new EhrDbResponse[1];
        EhrDbView? eventView = await GetView(view);
        if (eventView == null)
        {
            result[0] = await CreateView(view);
            return result;
        }
        eventView.Js = view.Js;
        eventView.Name = view.Name;
        result[0] = await UpdateView(eventView);
        return result;
    }

    /// <summary>
    ///     Gets EHR view by name
    /// </summary>
    /// <param name="view"></param>
    /// <returns></returns>
    private async Task<EhrDbView?> GetView(EhrDbView view)
    {
        try
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{Url}view/{view.Name}");
            requestMessage.Headers.TryAddWithoutValidation("Authorization", GetCredentials());
            HttpResponseMessage response = await Client.SendAsync(requestMessage);
            string responseBody = await response.Content.ReadAsStringAsync();
            if (response.StatusCode == HttpStatusCode.OK) return JsonConvert.DeserializeObject<EhrDbView>(responseBody);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get view {view.Name}: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    ///     Creates EHR event view
    /// </summary>
    /// <param name="view"></param>
    /// <returns></returns>
    private async Task<EhrDbResponse> CreateView(EhrDbView view)
    {
        string serializedView = JsonConvert.SerializeObject(view, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        HttpResponseMessage? response = null;
        try
        {
            StringContent content = new StringContent(serializedView, null, "application/json");
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{Url}view/create");
            requestMessage.Headers.TryAddWithoutValidation("Authorization", GetCredentials());
            requestMessage.Content = content;
            response = await Client.SendAsync(requestMessage);
            return new EhrDbResponse { EntityName = view.Name, Code = response.StatusCode, OperationType = EhrDbOperationType.Create, Message = serializedView };
        }
        catch (Exception ex)
        {
            return new EhrDbResponse { EntityName = view.Name, Code = response?.StatusCode, OperationType = EhrDbOperationType.Create, Message = $"{ex.Message}\r\n{serializedView}" };
        }
    }

    /// <summary>
    ///     Updates EHR event view
    /// </summary>
    /// <param name="view"></param>
    /// <returns></returns>
    private async Task<EhrDbResponse> UpdateView(EhrDbView view)
    {
        string serializedView = JsonConvert.SerializeObject(view, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        HttpResponseMessage? response = null;
        try
        {
            int id = (int)view.Id!;
            view.Id = null;
            StringContent content = new StringContent(serializedView, null, "application/json");
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Put, $"{Url}view/update/{id}");
            requestMessage.Headers.TryAddWithoutValidation("Authorization", GetCredentials());
            requestMessage.Content = content;
            response = await Client.SendAsync(requestMessage);
            return new EhrDbResponse { EntityName = view.Name, Code = response.StatusCode, OperationType = EhrDbOperationType.Update, Message = serializedView };
        }
        catch (Exception ex)
        {
            return new EhrDbResponse { EntityName = view.Name, Code = response?.StatusCode, OperationType = EhrDbOperationType.Update, Message = $"{ex.Message}\r\n{serializedView}" };
        }
    }

    #endregion

    /// <summary>
    ///     Marks target composition as deleted
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task DeleteComposition(Guid id)
    {
        try
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Delete, $"{Url}composition/{id}");
            requestMessage.Headers.TryAddWithoutValidation("Authorization", GetCredentials());
            HttpResponseMessage response = await Client.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to delete composition {id}: {ex.Message}");
        }
    }

    /// <summary>
    ///     Restores target composition
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task RestoreComposition(Guid id)
    {
        try
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Put, $"{Url}composition/{id}/restore");
            requestMessage.Headers.TryAddWithoutValidation("Authorization", GetCredentials());
            HttpResponseMessage response = await Client.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to restore composition {id}: {ex.Message}");
        }
    }

    /// <summary>
    ///     Gets all compositions for specified <paramref name="ehrId" />
    /// </summary>
    /// <param name="ehrId"></param>
    /// <returns></returns>
    public async Task<OpenEhrDocumetModel[]> GetCompositions(Guid ehrId)
    {
        List<OpenEhrDocumetModel> result = new List<OpenEhrDocumetModel>();
        const string query = @"SELECT c/context/start_time/value as start_time
                          , c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0035]/value/id as ehr_case_id
                          , c/name/value as name
                          , c/uid/value as cuid
                          , c/links/target/value as link
                          , c/archetype_details/template_id/value as atid
                          , c/links/meaning/value as meaning
                          , c/composer/name as composer
                          , vo/trunk_lifecycle_state/value as state
                          , e/ehr_id/value as ehr_id
                          , tags(c) as tags
                          , vo/uid/value as vo_id
                          , c/archetype_details/template_id/value as template_id
                        FROM EHR e
                        CONTAINS VERSIONED_OBJECT vo
                        CONTAINS COMPOSITION c 
                        WHERE e/ehr_id/value = :ehr_id
                        LIMIT 10000";
        AqlCommand com = new AqlCommand(query, this);
        com.Parameters.AddWithValue("ehr_id", ehrId);
        AQLDataReader reader = await com.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(new OpenEhrDocumetModel
            {
                StartTime = reader.GetDateTime("start_time"),
                EhrCaseId = reader.GetGuid("ehr_case_id"),
                Name = reader.GetString("name"),
                Cuid = reader.GetString("cuid"),
                Link = reader.IsNull("cuid") ? string.Empty : reader.GetString("cuid"),
                Atid = reader.GetString("atid"),
                Meaning = reader.IsNull("meaning") ? string.Empty : reader.GetString("meaning"),
                Composer = reader.GetString("composer"),
                State = reader.GetString("state"),
                EhrId = reader.GetGuid("ehr_id"),
                Tags = JsonConvert.DeserializeObject<OpenEhrTagsContainer>(reader.GetString("tags")),
                VoId = reader.GetGuid("vo_id"),
                TemplateId = reader.GetString("template_id")
            });
        return result.OrderByDescending(x => x.StartTime).ThenBy(x => x.Cuid).ToArray();
    }

    /// <summary>
    ///     Gets all compositions with complete lifecycleState for specified <paramref name="ehrId" />.
    /// </summary>
    /// <param name="ehrId"></param>
    /// <returns></returns>
    public async Task<OpenEhrDocumetModel[]> GetCompleteCompositions(Guid ehrId)
    {
        List<OpenEhrDocumetModel> result = new List<OpenEhrDocumetModel>();
        const string query = @"SELECT c/context/start_time/value as start_time
                          , c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0035]/value/id as ehr_case_id
                          , c/name/value as name
                          , c/uid/value as cuid
                          , c/links/target/value as link
                          , c/archetype_details/template_id/value as atid
                          , c/links/meaning/value as meaning
                          , c/composer/name as composer
                          , vo/trunk_lifecycle_state/value as state
                          , e/ehr_id/value as ehr_id
                          , tags(c) as tags
                          , vo/uid/value as vo_id
                          , c/archetype_details/template_id/value as template_id
                        FROM EHR e
                        CONTAINS VERSIONED_OBJECT vo
                        CONTAINS COMPOSITION c 
                        WHERE e/ehr_id/value = :ehr_id
                        AND vo/trunk_lifecycle_state/value = 'complete'
                        LIMIT 10000";
        AqlCommand com = new AqlCommand(query, this);
        com.Parameters.AddWithValue("ehr_id", ehrId);
        AQLDataReader reader = await com.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(new OpenEhrDocumetModel
            {
                StartTime = reader.GetDateTime("start_time"),
                EhrCaseId = reader.GetGuid("ehr_case_id"),
                Name = reader.GetString("name"),
                Cuid = reader.GetString("cuid"),
                Link = reader.IsNull("cuid") ? string.Empty : reader.GetString("cuid"),
                Atid = reader.GetString("atid"),
                Meaning = reader.IsNull("meaning") ? string.Empty : reader.GetString("meaning"),
                Composer = reader.GetString("composer"),
                State = reader.GetString("state"),
                EhrId = reader.GetGuid("ehr_id"),
                Tags = JsonConvert.DeserializeObject<OpenEhrTagsContainer>(reader.GetString("tags")),
                VoId = reader.GetGuid("vo_id"),
                TemplateId = reader.GetString("template_id")
            });
        return result.OrderByDescending(x => x.StartTime).ThenBy(x => x.Cuid).ToArray();
    }

    /// <summary>
    ///     Gets all compositions with complete lifecycleState for specified <paramref name="ehrCaseId" />.
    /// </summary>
    /// <param name="ehrCaseId"></param>
    /// <returns></returns>
    public async Task<OpenEhrDocumetModel[]> GetCompleteCompositionsByEhrCaseId(Guid ehrCaseId)
    {
        List<OpenEhrDocumetModel> result = new List<OpenEhrDocumetModel>();
        const string query = @"SELECT c/context/start_time/value as start_time
                          , c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0035]/value/id as ehr_case_id
                          , c/name/value as name
                          , c/uid/value as cuid
                          , e/ehr_id/value as ehr_id
                          , vo/uid/value as vo_id
                          , c/archetype_details/template_id/value as template_id
                        FROM EHR e
                        CONTAINS VERSIONED_OBJECT vo
                        CONTAINS COMPOSITION c 
                        WHERE c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0035]/value/id = :ehr_case_id
                        AND vo/trunk_lifecycle_state/value = 'complete'
                        LIMIT 10000";
        AqlCommand com = new AqlCommand(query, this);
        com.Parameters.AddWithValue("ehr_case_id", ehrCaseId);
        AQLDataReader reader = await com.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(new OpenEhrDocumetModel
            {
                StartTime = reader.GetDateTime("start_time"),
                EhrCaseId = reader.GetGuid("ehr_case_id"),
                Name = reader.GetString("name"),
                Cuid = reader.GetString("cuid"),
                EhrId = reader.GetGuid("ehr_id"),
                VoId = reader.GetGuid("vo_id"),
                TemplateId = reader.GetString("template_id")
            });
        return result.OrderByDescending(x => x.StartTime).ThenBy(x => x.Cuid).ToArray();
    }
    
    public async Task<OpenEhrDocumetModel[]> GetCompositionsByEhrCaseId(Guid ehrCaseId)
    {
        List<OpenEhrDocumetModel> result = new List<OpenEhrDocumetModel>();
        const string query = @"SELECT c/context/start_time/value as start_time
                          , c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0035]/value/id as ehr_case_id
                          , c/name/value as name
                          , c/uid/value as cuid
                          , e/ehr_id/value as ehr_id
                          , vo/uid/value as vo_id
                          , c/archetype_details/template_id/value as template_id
                          , c/links/target/value as link
                        FROM EHR e
                        CONTAINS VERSIONED_OBJECT vo
                        CONTAINS COMPOSITION c 
                        WHERE c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0035]/value/id = :ehr_case_id
                        LIMIT 10000";
        AqlCommand com = new AqlCommand(query, this);
        com.Parameters.AddWithValue("ehr_case_id", ehrCaseId);
        AQLDataReader reader = await com.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(new OpenEhrDocumetModel
            {
                StartTime = reader.GetDateTime("start_time"),
                EhrCaseId = reader.GetGuid("ehr_case_id"),
                Name = reader.GetString("name"),
                Cuid = reader.GetString("cuid"),
                EhrId = reader.GetGuid("ehr_id"),
                VoId = reader.GetGuid("vo_id"),
                TemplateId = reader.GetString("template_id"),
                Link = reader.GetString("link"),
            });
        return result.OrderByDescending(x => x.StartTime).ThenBy(x => x.Cuid).ToArray();
    }
    
    public async Task<OpenEhrDocumetModel[]> GetCompositionsByEhrCaseId(Guid ehrCaseId, string[] templateId)
    {
        List<OpenEhrDocumetModel> result = new List<OpenEhrDocumetModel>();
        const string query = @"SELECT c/context/start_time/value as start_time
                          , c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0035]/value/id as ehr_case_id
                          , c/name/value as name
                          , c/uid/value as cuid
                          , e/ehr_id/value as ehr_id
                          , vo/uid/value as vo_id
                          , c/archetype_details/template_id/value as template_id
                          , c/links/target/value as link
                        FROM EHR e
                        CONTAINS VERSIONED_OBJECT vo
                        CONTAINS COMPOSITION c 
                        WHERE c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0035]/value/id = :ehr_case_id
                        AND c/archetype_details/template_id/value MATCHES {:template_id}
                        LIMIT 10000";
        AqlCommand com = new AqlCommand(query, this);
        com.Parameters.AddWithValue("ehr_case_id", ehrCaseId);
        com.Parameters.AddWithValue("template_id", templateId);
        AQLDataReader reader = await com.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(new OpenEhrDocumetModel
            {
                StartTime = reader.GetDateTime("start_time"),
                EhrCaseId = reader.GetGuid("ehr_case_id"),
                Name = reader.GetString("name"),
                Cuid = reader.GetString("cuid"),
                EhrId = reader.GetGuid("ehr_id"),
                VoId = reader.GetGuid("vo_id"),
                TemplateId = reader.GetString("template_id"),
                Link = reader.GetString("link"),
            });
        return result.OrderByDescending(x => x.StartTime).ThenBy(x => x.Cuid).ToArray();
    }

    public async Task<OpenEhrDocumetModel[]> GetCompositionsByName(string name, DateTime dtBeg, DateTime dtEnd)
    {
        List<OpenEhrDocumetModel> result = new List<OpenEhrDocumetModel>();
        const string query = @"SELECT c/context/start_time/value as start_time
                          , c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0035]/value/id as ehr_case_id
                          , c/name/value as name
                          , c/uid/value as cuid
                          , e/ehr_id/value as ehr_id
                          , vo/uid/value as vo_id
                          , c/archetype_details/template_id/value as template_id
                        FROM EHR e
                        CONTAINS VERSIONED_OBJECT vo
                        CONTAINS COMPOSITION c 
                        WHERE c/name/value = :name
                        AND c/context/start_time/value >= :dt_beg
                        AND c/context/start_time/value <= :dt_end               
                        LIMIT 10000";
        AqlCommand com = new AqlCommand(query, this);
        com.Parameters.AddWithValue("name", name);
        com.Parameters.AddWithValue("dt_beg", dtBeg);
        com.Parameters.AddWithValue("dt_end", dtEnd);
        AQLDataReader reader = await com.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(new OpenEhrDocumetModel
            {
                StartTime = reader.GetDateTime("start_time"),
                EhrCaseId = reader.GetGuid("ehr_case_id"),
                Name = reader.GetString("name"),
                Cuid = reader.GetString("cuid"),
                EhrId = reader.GetGuid("ehr_id"),
                VoId = reader.GetGuid("vo_id"),
                TemplateId = reader.GetString("template_id")
            });
        return result.OrderByDescending(x => x.StartTime).ThenBy(x => x.Cuid).ToArray();
    }

    public async Task<OpenEhrDocumetModel[]> GetCompositionsByPeriod(DateTime dtBeg, DateTime dtEnd)
    {
        List<OpenEhrDocumetModel> result = new List<OpenEhrDocumetModel>();
        const string query = @"SELECT c/context/start_time/value as start_time
                          , c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0035]/value/id as ehr_case_id
                          , c/name/value as name
                          , c/uid/value as cuid
                          , e/ehr_id/value as ehr_id
                          , vo/uid/value as vo_id
                          , c/archetype_details/template_id/value as template_id
                        FROM EHR e
                        CONTAINS VERSIONED_OBJECT vo
                        CONTAINS COMPOSITION c 
                        WHERE c/context/start_time/value >= :dt_beg
                        AND c/context/start_time/value <= :dt_end               
                        LIMIT 10000";
        AqlCommand com = new AqlCommand(query, this);
        com.Parameters.AddWithValue("dt_beg", dtBeg);
        com.Parameters.AddWithValue("dt_end", dtEnd);
        AQLDataReader reader = await com.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(new OpenEhrDocumetModel
            {
                StartTime = reader.GetDateTime("start_time"),
                EhrCaseId = reader.GetGuid("ehr_case_id"),
                Name = reader.GetString("name"),
                Cuid = reader.GetString("cuid"),
                EhrId = reader.GetGuid("ehr_id"),
                VoId = reader.GetGuid("vo_id"),
                TemplateId = reader.GetString("template_id")
            });
        return result.OrderByDescending(x => x.StartTime).ThenBy(x => x.Cuid).ToArray();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ehrId"></param>
    /// <returns></returns>
    public async Task<OpenEhrDocumetModel[]> GetCompositionsAllVersions(Guid ehrId)
    {
        List<OpenEhrDocumetModel> result = new List<OpenEhrDocumetModel>();
        const string query = @"SELECT c/context/start_time/value as start_time
                          , c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0035]/value/id as ehr_case_id
                          , c/name/value as name
                          , c/uid/value as cuid
                          , c/links/target/value as link
                          , c/archetype_details/template_id/value as atid
                          , c/links/meaning/value as meaning
                          , c/composer/name as composer
                          , vo/trunk_lifecycle_state/value as state
                          , e/ehr_id/value as ehr_id
                          , tags(c) as tags
                          , vo/uid/value as vo_id
                          , c/archetype_details/template_id/value as template_id
                        FROM EHR e
                        CONTAINS VERSIONED_OBJECT vo
                        CONTAINS VERSION v[all_versions]
                        CONTAINS COMPOSITION c 
                        WHERE e/ehr_id/value = :ehr_id
                        LIMIT 10000";
        AqlCommand com = new AqlCommand(query, this);
        com.Parameters.AddWithValue("ehr_id", ehrId);
        AQLDataReader reader = await com.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(new OpenEhrDocumetModel
            {
                StartTime = reader.GetDateTime("start_time"),
                EhrCaseId = reader.GetGuid("ehr_case_id"),
                Name = reader.GetString("name"),
                Cuid = reader.GetString("cuid"),
                Link = reader.IsNull("cuid") ? string.Empty : reader.GetString("cuid"),
                Atid = reader.GetString("atid"),
                Meaning = reader.IsNull("meaning") ? string.Empty : reader.GetString("meaning"),
                Composer = reader.GetString("composer"),
                State = reader.GetString("state"),
                EhrId = reader.GetGuid("ehr_id"),
                Tags = JsonConvert.DeserializeObject<OpenEhrTagsContainer>(reader.GetString("tags")),
                VoId = reader.GetGuid("vo_id"),
                TemplateId = reader.GetString("template_id")
            });
        return result.OrderByDescending(x => x.StartTime).ThenBy(x => x.Cuid).ToArray();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="compositionUid"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public OpenEhrTagsContainer? GetCompositionTags(string compositionUid)
    {
        try
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{Url}tagging/{compositionUid}");
            requestMessage.Headers.TryAddWithoutValidation("Authorization", GetCredentials());
            HttpResponseMessage response = Client.Send(requestMessage);
            response.EnsureSuccessStatusCode();
            string jsonResponse = response.Content.ReadAsStringAsync().Result;
            Log.Logger.Debug("{compositionUid} tags\r\n{jsonResponse}", compositionUid, jsonResponse);
            return JsonConvert.DeserializeObject<OpenEhrTagsContainer>(jsonResponse);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to delete composition {compositionUid}: {ex.Message}");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="compositionUid"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<OpenEhrTagsContainer?> GetCompositionTagsAsync(string compositionUid)
    {
        try
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{Url}tagging/{compositionUid}");
            requestMessage.Headers.TryAddWithoutValidation("Authorization", GetCredentials());
            HttpResponseMessage response = await Client.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            string jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<OpenEhrTagsContainer>(jsonResponse);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to delete composition {compositionUid}: {ex.Message}");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ehrId"></param>
    /// <param name="dtBeg"></param>
    /// <param name="dtEnd"></param>
    /// <returns></returns>
    public async Task<OpenEhrDocumetModel[]> GetCompositionsAllVersions(Guid ehrId, DateTime dtBeg, DateTime dtEnd)
    {
        List<OpenEhrDocumetModel> result = new List<OpenEhrDocumetModel>();
        foreach (DateTime day in EMIAS.DateTimeExtensions.Extensions.EachDay(dtBeg, dtEnd))
        {
            const string query = @"SELECT c/context/start_time/value as start_time
                          , c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0035]/value/id as ehr_case_id
                          , c/name/value as name
                          , c/uid/value as cuid
                          , c/links/target/value as link
                          , c/archetype_details/template_id/value as atid
                          , c/links/meaning/value as meaning
                          , c/composer/name as composer
                          , vo/trunk_lifecycle_state/value as state
                          , e/ehr_id/value as ehr_id
                          , tags(c) as tags
                          , vo/uid/value as vo_id
                          , c/archetype_details/template_id/value as template_id
                        FROM EHR e
                        CONTAINS VERSIONED_OBJECT vo
                        CONTAINS VERSION v[all_versions]
                        CONTAINS COMPOSITION c 
                        WHERE e/ehr_id/value = :ehr_id
                        AND c/context/start_time/value >= :dt_beg
                        AND c/context/start_time/value <= :dt_end
                        LIMIT 10000";
            AqlCommand com = new AqlCommand(query, this);
            com.Parameters.AddWithValue("ehr_id", ehrId);
            com.Parameters.AddWithValue("dt_beg", day);
            com.Parameters.AddWithValue("dt_end", day.EndOfDay());
            AQLDataReader reader = await com.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add(new OpenEhrDocumetModel
                {
                    StartTime = reader.GetDateTime("start_time"),
                    EhrCaseId = reader.GetGuid("ehr_case_id"),
                    Name = reader.GetString("name"),
                    Cuid = reader.GetString("cuid"),
                    Link = reader.IsNull("cuid") ? string.Empty : reader.GetString("cuid"),
                    Atid = reader.GetString("atid"),
                    Meaning = reader.IsNull("meaning") ? string.Empty : reader.GetString("meaning"),
                    Composer = reader.GetString("composer"),
                    State = reader.GetString("state"),
                    EhrId = reader.GetGuid("ehr_id"),
                    Tags = JsonConvert.DeserializeObject<OpenEhrTagsContainer>(reader.GetString("tags")),
                    VoId = reader.GetGuid("vo_id"),
                    TemplateId = reader.GetString("template_id")
                });
        }
        return result.OrderByDescending(x => x.StartTime).ThenBy(x => x.Cuid).ToArray();
    }

    /// <summary>
    ///     Reindex target EHR Content
    /// </summary>
    /// <param name="ehrId"></param>
    /// <returns></returns>
    public async Task Reindex(Guid ehrId)
    {
        try
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{Url}ehr/{ehrId}/reindex");
            requestMessage.Headers.TryAddWithoutValidation("Authorization", GetCredentials());
            HttpResponseMessage response = await Client.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to reindex {ehrId}: {ex.Message}");
        }
    }

    /// <summary>
    ///     Repairs specified EHR by reindex, delete and restore all compositions
    /// </summary>
    /// <param name="ehrId"></param>
    /// <returns></returns>
    public async Task RepairCase(Guid ehrId)
    {
        await Reindex(ehrId);
        OpenEhrDocumetModel[] compositions = await GetCompositions(ehrId);
        foreach (OpenEhrDocumetModel composition in compositions)
        {
            await DeleteComposition(composition.VoId);
            await RestoreComposition(composition.VoId);
        }
    }

    /// <summary>
    /// Adds tags to a specific composition identified by its unique identifier.
    /// </summary>
    /// <param name="cuid">The unique identifier of the composition to be tagged.</param>
    /// <param name="tags">Container holding the tags to be added to the composition.</param>
    /// <returns>A task that represents the asynchronous operation of tagging a composition.</returns>
    public async Task Tag(string cuid, OpenEhrTagsContainer tags)
    {
        Tagging preparedTags = new Tagging(cuid, tags.Tags);
        string serializedTags = JsonConvert.SerializeObject(preparedTags, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        try
        {
            StringContent content = new StringContent(serializedTags, null, "application/json");
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{Url}tagging");
            requestMessage.Headers.TryAddWithoutValidation("Authorization", GetCredentials());
            requestMessage.Content = content;
            HttpResponseMessage response = await Client.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to tag {cuid}: {ex.Message}");
        }
    }

    /// <summary>
    /// Determines if a composition exists within the EHR system based on the specified EHR case ID and template ID.
    /// </summary>
    /// <param name="ehrCaseId">The unique identifier of the EHR case.</param>
    /// <param name="templateId">The unique identifier of the composition template.</param>
    /// <returns>A task that represents the asynchronous operation. The task result is a boolean value indicating whether
    /// the composition exists (true) or not (false).</returns>
    public async Task<bool> HasComposition(Guid ehrCaseId, string templateId)
    {
        const string query = "SELECT c/uid/value as uid FROM EHR e CONTAINS COMPOSITION c WHERE c/archetype_details/template_id/value = :template_id AND c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0035]/value/id = :ehr_case_id LIMIT 1";
        AqlCommand com = new AqlCommand(query, this);
        com.Parameters.AddWithValue("ehr_case_id", ehrCaseId);
        com.Parameters.AddWithValue("template_id", templateId);
        AQLDataReader reader = await com.ExecuteReaderAsync();
        return await reader.ReadAsync();
    }

    /// <summary>
    /// Gets server version info from api/rest/v1/system/version
    /// </summary>
    /// <returns></returns>
    public async Task<Dto.EhrPlatformVersion> Version()
    {
        try
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{Url}system/version");
            requestMessage.Headers.TryAddWithoutValidation("Authorization", GetCredentials());
            HttpResponseMessage response = await Client.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            Dto.EhrPlatformVersion? result = JsonConvert.DeserializeObject<Dto.EhrPlatformVersion>(json);
            if (result == null)
                throw new Exception("Empty response");
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get server version: {ex.Message}");
        }
    }

    /// <summary>
    /// Posts a contribution payload to api/rest/v1/composition/contribution and returns CommitData
    /// </summary>
    /// <param name="content">Raw JSON payload string</param>
    /// <returns>CommitData of the performed action</returns>
    public async Task<ContributionResponse> PostContribution(string content)
    {
        try
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{Url}composition/contribution");
            requestMessage.Headers.TryAddWithoutValidation("Authorization", GetCredentials());
            requestMessage.Content = new StringContent(content, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await Client.SendAsync(requestMessage);
            Log.Logger.Debug("Post contribution response: {@Response}", new { response.StatusCode, Body = await response.Content.ReadAsStringAsync() });
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync();
            ContributionResponse? result = JsonConvert.DeserializeObject<ContributionResponse>(json);
            if (result?.CommitData == null || result.CommitData.Length == 0)
                throw new Exception("Empty response");
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to post contribution: {ex.Message}");
        }
    }
    
    /// <summary>
    ///     Gets composition body by EHR case ID and template ID in FLAT format
    /// </summary>
    /// <param name="ehrCaseId"></param>
    /// <param name="templateId"></param>
    /// <returns></returns>
    public async Task<string?> GetCompositionBodyByEhrCaseId(Guid ehrCaseId, string templateId)
    {
        const string query = "SELECT c/uid/value as cuid FROM EHR e CONTAINS COMPOSITION c WHERE c/archetype_details/template_id/value = :template_id AND c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0035]/value/id = :ehr_case_id LIMIT 1";
        AqlCommand com = new AqlCommand(query, this);
        com.Parameters.AddWithValue("ehr_case_id", ehrCaseId);
        com.Parameters.AddWithValue("template_id", templateId);
        AQLDataReader reader = await com.ExecuteReaderAsync();
    
        if (!await reader.ReadAsync())
        {
            Log.Logger.Information("Состав не найден для ehrCaseId={EhrCaseId} и templateId={TemplateId}", ehrCaseId, templateId);
            return null;
        }
    
        string compUid = reader.GetString("cuid");
    
        try
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{Url}composition/{Uri.EscapeDataString(compUid)}?format=FLAT&meta=true");
            requestMessage.Headers.TryAddWithoutValidation("Authorization", GetCredentials());
            HttpResponseMessage response = await Client.SendAsync(requestMessage);
            string responseBody = await response.Content.ReadAsStringAsync();
        
            if (!response.IsSuccessStatusCode)
            {
                Log.Logger.Warning("Ошибка получения состава из EHR: {Status} {Body}", response.StatusCode, responseBody);
                return null;
            }
        
            return responseBody;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Ошибка получения состава из EHR: {Message}", ex.Message);
            return null;
        }
    }

    public async Task<OpenEhrDocumetModel[]> GetCompositionChildren(Guid compositionId)
    {
        List<OpenEhrDocumetModel> result = new List<OpenEhrDocumetModel>();
        const string query = @"SELECT c/context/start_time/value as start_time
                          , c/context/other_context[at0001]/items[openEHR-EHR-CLUSTER.composition_context_details*]/items[at0035]/value/id as ehr_case_id
                          , c/name/value as name
                          , c/uid/value as cuid
                          , e/ehr_id/value as ehr_id
                          , vo/uid/value as vo_id
                          , c/archetype_details/template_id/value as template_id
                          , c/links/target/value as link
                        FROM EHR e
                        CONTAINS VERSIONED_OBJECT vo
                        CONTAINS COMPOSITION c 
                        WHERE c/links/target/value = :composition_id
                        LIMIT 10000";
        AqlCommand com = new AqlCommand(query, this);
        com.Parameters.AddWithValue("compositionId", $"ehr:compositions/{compositionId.ToString()}");
        AQLDataReader reader = await com.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(new OpenEhrDocumetModel
            {
                StartTime = reader.GetDateTime("start_time"),
                EhrCaseId = reader.GetGuid("ehr_case_id"),
                Name = reader.GetString("name"),
                Cuid = reader.GetString("cuid"),
                EhrId = reader.GetGuid("ehr_id"),
                VoId = reader.GetGuid("vo_id"),
                TemplateId = reader.GetString("template_id"),
                Link = reader.GetString("link"),
            });
        return result.OrderByDescending(x => x.StartTime).ThenBy(x => x.Cuid).ToArray();
    }
}