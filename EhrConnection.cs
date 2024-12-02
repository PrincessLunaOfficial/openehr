using System.Net;
using System.Net.Http.Headers;
using System.Text;
using EMIAS.DateTimeExtensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenEhr.Triggers;
using OpenEhr.Views;

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
        var sb = new StringBuilder();
        sb.AppendLine($"EhrDb: Operation {OperationType.ToString()} for {EntityName} returned {Code}");
        if (Code != HttpStatusCode.OK &&
            Code != HttpStatusCode.NoContent)
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
    /// 
    /// </summary>
    public ILogger<EhrConnection> EhrConnectionLogger;

    /// <summary>
    /// Builds a closed connection object ready to be used.
    /// </summary>
    /// <param name="url"></param>
    /// <param name="login"></param>
    /// <param name="password"></param>
    /// <param name="ehrConnectionLogger"></param>
    public EhrConnection(string url, string login, string password, ILogger<EhrConnection> ehrConnectionLogger)
    {
        this.EhrConnectionLogger = ehrConnectionLogger;
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
        EhrConnectionLogger.LogDebug("Initializing HttpClient connection");
        Client = new HttpClient();
        Client.Timeout = TimeSpan.FromSeconds(1024);
        Client.DefaultRequestHeaders.ConnectionClose = false;
        Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        Client.DefaultRequestHeaders.TryAddWithoutValidation("Wait-For-Commit", "true");
        Client.DefaultRequestHeaders.TryAddWithoutValidation("Connection", "keep-alive");
        Initialized = true;
        EhrConnectionLogger.LogDebug("HttpClient has been initialized and is ready for use");
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
        var result = new EhrDbResponse[2];
        var eventTrigger = await GetTrigger(trigger);

        if (eventTrigger == null)
        {
            result[0] = await CreateEventTrigger(trigger);
            eventTrigger = await GetTrigger(trigger);

            if (eventTrigger == null)
            {
                EhrConnectionLogger.LogError("Failed to get the event trigger after creation");
                return result;
            }

            result[1] = await SetTriggerStatus(eventTrigger, EhrDbTriggerStatus.ACTIVE);
            return result;
        }

        eventTrigger.aqls = trigger.aqls;
        if (!string.IsNullOrEmpty(trigger.httpResultUrl))
            eventTrigger.httpResultUrl = trigger.httpResultUrl;

        eventTrigger.archetypeIds = trigger.archetypeIds;
        eventTrigger.templateIds = trigger.templateIds;
        if (!string.IsNullOrEmpty(trigger.kafkaResultTopic))
            eventTrigger.kafkaResultTopic = trigger.kafkaResultTopic;
        eventTrigger.eventTimeToLiveHours = trigger.eventTimeToLiveHours;
        eventTrigger.repeatIntervalMinutes = trigger.repeatIntervalMinutes;
        eventTrigger.name = trigger.name;
        result[0] = await UpdateTrigger(eventTrigger);

        eventTrigger = await GetTrigger(trigger);

        if (eventTrigger == null)
        {
            EhrConnectionLogger.LogError("Failed to get the event trigger after updated");
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
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{Url}trigger/?name={trigger.name}");
            requestMessage.Headers.TryAddWithoutValidation("Authorization", GetCredentials());
            var response = await Client.SendAsync(requestMessage);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.OK)
                return JsonConvert.DeserializeObject<EhrDbEventTrigger>(responseBody);
        }
        catch (Exception ex)
        {
            EhrConnectionLogger.LogError($"Failed to get trigger {trigger.name}: {ex.Message}");
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
        var serializedTrigger = JsonConvert.SerializeObject(trigger, Formatting.Indented, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });
        HttpResponseMessage? response = null;
        try
        {
            var content = new StringContent(serializedTrigger, null, "application/json");
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{Url}trigger/create");
            requestMessage.Headers.TryAddWithoutValidation("Authorization", GetCredentials());
            requestMessage.Content = content;
            response = await Client.SendAsync(requestMessage);
            return new EhrDbResponse
            {
                EntityName = trigger.name,
                Code = response.StatusCode,
                OperationType = EhrDbOperationType.Create,
                Message = serializedTrigger
            };
        }
        catch (Exception ex)
        {
            EhrConnectionLogger.LogError(
                $"Failed to create trigger {trigger.name}: {ex.Message}\r\n{serializedTrigger}");
            // Depends on the requirement, you may or may not want to return a response after logging an error
            return new EhrDbResponse
            {
                EntityName = trigger.name,
                Code = response?.StatusCode ?? HttpStatusCode.InternalServerError,
                OperationType = EhrDbOperationType.Create,
                Message = "An error occurred while creating the trigger"
            };
        }
    }

    /// <summary>
    ///     Updates EHR event trigger
    /// </summary>
    /// <param name="trigger"></param>
    /// <returns></returns>
    private async Task<EhrDbResponse> UpdateTrigger(EhrDbEventTrigger trigger)
    {
        var serializedTrigger = JsonConvert.SerializeObject(trigger, Formatting.Indented, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });
        HttpResponseMessage? response = null;

        try
        {
            var id = (int)trigger.id!;
            trigger.id = null;
            var content = new StringContent(serializedTrigger, null, "application/json");
            var requestMessage = new HttpRequestMessage(HttpMethod.Put, $"{Url}trigger/update/{id}");
            requestMessage.Headers.TryAddWithoutValidation("Authorization", GetCredentials());
            requestMessage.Content = content;
            response = await Client.SendAsync(requestMessage);
            return new EhrDbResponse
            {
                EntityName = trigger.name,
                Code = response.StatusCode,
                OperationType = EhrDbOperationType.Update,
                Message = serializedTrigger
            };
        }
        catch (Exception ex)
        {
            EhrConnectionLogger.LogError(
                $"Failed to update trigger {trigger.name}: {ex.Message}\r\n{serializedTrigger}");
            // Depends on the requirement, you may or may not want to return a response after logging an error
            return new EhrDbResponse
            {
                EntityName = trigger.name,
                Code = response?.StatusCode ?? HttpStatusCode.InternalServerError,
                OperationType = EhrDbOperationType.Update,
                Message = "An error occurred while updating the trigger"
            };
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
        var serializedTrigger = JsonConvert.SerializeObject(trigger, Formatting.Indented, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });
        HttpResponseMessage? response = null;

        try
        {
            var content = new StringContent(serializedTrigger, null, "application/json");
            var requestMessage =
                new HttpRequestMessage(HttpMethod.Put, $"{Url}trigger/{trigger.id}/status/{status.ToString()}");
            requestMessage.Headers.TryAddWithoutValidation("Authorization", GetCredentials());
            requestMessage.Content = content;
            response = await Client.SendAsync(requestMessage);
            return new EhrDbResponse
            {
                EntityName = trigger.name,
                Code = response.StatusCode,
                OperationType = EhrDbOperationType.SetStatus,
                Message = serializedTrigger
            };
        }
        catch (Exception ex)
        {
            EhrConnectionLogger.LogError(
                $"Failed to set status for trigger {trigger.name}: {ex.Message}\r\n{serializedTrigger}");
            // Depends on the requirement, you may or may not want to return a response after logging an error
            return new EhrDbResponse
            {
                EntityName = trigger.name,
                Code = response?.StatusCode ?? HttpStatusCode.InternalServerError,
                OperationType = EhrDbOperationType.SetStatus,
                Message = "An error occurred while setting the status of the trigger"
            };
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
        var result = new EhrDbResponse[1];
        var eventView = await GetView(view);
        if (eventView == null)
        {
            result[0] = await CreateView(view);
            return result;
        }

        eventView.js = view.js;
        eventView.name = view.name;
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
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{Url}view/{view.name}");
            requestMessage.Headers.TryAddWithoutValidation("Authorization", GetCredentials());
            var response = await Client.SendAsync(requestMessage);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (response.StatusCode == HttpStatusCode.OK) return JsonConvert.DeserializeObject<EhrDbView>(responseBody);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to get view {view.name}: {ex.Message}");
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
        var serializedView = JsonConvert.SerializeObject(view, Formatting.Indented, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });
        HttpResponseMessage? response = null;

        try
        {
            var content = new StringContent(serializedView, null, "application/json");
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{Url}view/create");
            requestMessage.Headers.TryAddWithoutValidation("Authorization", GetCredentials());
            requestMessage.Content = content;
            response = await Client.SendAsync(requestMessage);
            return new EhrDbResponse
            {
                EntityName = view.name,
                Code = response.StatusCode,
                OperationType = EhrDbOperationType.Create,
                Message = serializedView
            };
        }
        catch (Exception ex)
        {
            return new EhrDbResponse
            {
                EntityName = view.name,
                Code = response?.StatusCode,
                OperationType = EhrDbOperationType.Create,
                Message = $"{ex.Message}\r\n{serializedView}"
            };
        }
    }

    /// <summary>
    ///     Updates EHR event view
    /// </summary>
    /// <param name="view"></param>
    /// <returns></returns>
    private async Task<EhrDbResponse> UpdateView(EhrDbView view)
    {
        var serializedView = JsonConvert.SerializeObject(view, Formatting.Indented, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });
        HttpResponseMessage? response = null;

        try
        {
            var id = (int)view.id!;
            view.id = null;
            var content = new StringContent(serializedView, null, "application/json");
            var requestMessage = new HttpRequestMessage(HttpMethod.Put, $"{Url}view/update/{id}");
            requestMessage.Headers.TryAddWithoutValidation("Authorization", GetCredentials());
            requestMessage.Content = content;
            response = await Client.SendAsync(requestMessage);
            return new EhrDbResponse
            {
                EntityName = view.name,
                Code = response.StatusCode,
                OperationType = EhrDbOperationType.Update,
                Message = serializedView
            };
        }
        catch (Exception ex)
        {
            return new EhrDbResponse
            {
                EntityName = view.name,
                Code = response?.StatusCode,
                OperationType = EhrDbOperationType.Update,
                Message = $"{ex.Message}\r\n{serializedView}"
            };
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
            var requestMessage = new HttpRequestMessage(HttpMethod.Delete, $"{Url}composition/{id}");
            requestMessage.Headers.TryAddWithoutValidation("Authorization", GetCredentials());
            var response = await Client.SendAsync(requestMessage);
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
            var requestMessage = new HttpRequestMessage(HttpMethod.Put, $"{Url}composition/{id}/restore");
            requestMessage.Headers.TryAddWithoutValidation("Authorization", GetCredentials());
            var response = await Client.SendAsync(requestMessage);
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
        var result = new List<OpenEhrDocumetModel>();
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
        var com = new AqlCommand(query, this);
        com.Parameters.AddWithValue("ehr_id", ehrId);
        var reader = await com.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(new OpenEhrDocumetModel
            {
                start_time = reader.GetDateTime("start_time"),
                ehr_case_id = reader.GetGuid("ehr_case_id"),
                name = reader.GetString("name"),
                cuid = reader.GetString("cuid"),
                link = reader.IsNull("cuid") ? string.Empty : reader.GetString("cuid"),
                atid = reader.GetString("atid"),
                meaning = reader.IsNull("meaning") ? string.Empty : reader.GetString("meaning"),
                composer = reader.GetString("composer"),
                state = reader.GetString("state"),
                ehr_id = reader.GetGuid("ehr_id"),
                tags = JsonConvert.DeserializeObject<OpenEhrTagsContainer>(reader.GetString("tags")),
                vo_id = reader.GetGuid("vo_id"),
                template_id = reader.GetString("template_id")
            });

        return result.OrderByDescending(x => x.start_time).ThenBy(x => x.cuid).ToArray();
    }

    /// <summary>
    ///     Gets all compositions with complete lifecycleState for specified <paramref name="ehrId" />.
    /// </summary>
    /// <param name="ehrId"></param>
    /// <returns></returns>
    public async Task<OpenEhrDocumetModel[]> GetCompleteCompositions(Guid ehrId)
    {
        var result = new List<OpenEhrDocumetModel>();
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
        var com = new AqlCommand(query, this);
        com.Parameters.AddWithValue("ehr_id", ehrId);
        var reader = await com.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(new OpenEhrDocumetModel
            {
                start_time = reader.GetDateTime("start_time"),
                ehr_case_id = reader.GetGuid("ehr_case_id"),
                name = reader.GetString("name"),
                cuid = reader.GetString("cuid"),
                link = reader.IsNull("cuid") ? string.Empty : reader.GetString("cuid"),
                atid = reader.GetString("atid"),
                meaning = reader.IsNull("meaning") ? string.Empty : reader.GetString("meaning"),
                composer = reader.GetString("composer"),
                state = reader.GetString("state"),
                ehr_id = reader.GetGuid("ehr_id"),
                tags = JsonConvert.DeserializeObject<OpenEhrTagsContainer>(reader.GetString("tags")),
                vo_id = reader.GetGuid("vo_id"),
                template_id = reader.GetString("template_id")
            });

        return result.OrderByDescending(x => x.start_time).ThenBy(x => x.cuid).ToArray();
    }

    /// <summary>
    ///     Gets all compositions with complete lifecycleState for specified <paramref name="ehrCaseId" />.
    /// </summary>
    /// <param name="ehrCaseId"></param>
    /// <returns></returns>
    public async Task<OpenEhrDocumetModel[]> GetCompleteCompositionsByEhrCaseId(Guid ehrCaseId)
    {
        var result = new List<OpenEhrDocumetModel>();
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
        var com = new AqlCommand(query, this);
        com.Parameters.AddWithValue("ehr_case_id", ehrCaseId);
        var reader = await com.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(new OpenEhrDocumetModel
            {
                start_time = reader.GetDateTime("start_time"),
                ehr_case_id = reader.GetGuid("ehr_case_id"),
                name = reader.GetString("name"),
                cuid = reader.GetString("cuid"),
                ehr_id = reader.GetGuid("ehr_id"),
                vo_id = reader.GetGuid("vo_id"),
                template_id = reader.GetString("template_id")
            });

        return result.OrderByDescending(x => x.start_time).ThenBy(x => x.cuid).ToArray();
    }

    public async Task<OpenEhrDocumetModel[]> GetCompositionsByName(string name, DateTime dtBeg, DateTime dtEnd)
    {
        var result = new List<OpenEhrDocumetModel>();
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
        var com = new AqlCommand(query, this);
        com.Parameters.AddWithValue("name", name);
        com.Parameters.AddWithValue("dt_beg", dtBeg);
        com.Parameters.AddWithValue("dt_end", dtEnd);
        var reader = await com.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(new OpenEhrDocumetModel
            {
                start_time = reader.GetDateTime("start_time"),
                ehr_case_id = reader.GetGuid("ehr_case_id"),
                name = reader.GetString("name"),
                cuid = reader.GetString("cuid"),
                ehr_id = reader.GetGuid("ehr_id"),
                vo_id = reader.GetGuid("vo_id"),
                template_id = reader.GetString("template_id")
            });

        return result.OrderByDescending(x => x.start_time).ThenBy(x => x.cuid).ToArray();
    }

    public async Task<OpenEhrDocumetModel[]> GetCompositionsByPeriod(DateTime dtBeg, DateTime dtEnd)
    {
        var result = new List<OpenEhrDocumetModel>();
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
        var com = new AqlCommand(query, this);
        com.Parameters.AddWithValue("dt_beg", dtBeg);
        com.Parameters.AddWithValue("dt_end", dtEnd);
        var reader = await com.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(new OpenEhrDocumetModel
            {
                start_time = reader.GetDateTime("start_time"),
                ehr_case_id = reader.GetGuid("ehr_case_id"),
                name = reader.GetString("name"),
                cuid = reader.GetString("cuid"),
                ehr_id = reader.GetGuid("ehr_id"),
                vo_id = reader.GetGuid("vo_id"),
                template_id = reader.GetString("template_id")
            });

        return result.OrderByDescending(x => x.start_time).ThenBy(x => x.cuid).ToArray();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ehrId"></param>
    /// <returns></returns>
    public async Task<OpenEhrDocumetModel[]> GetCompositionsAllVersions(Guid ehrId)
    {
        var result = new List<OpenEhrDocumetModel>();
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
        var com = new AqlCommand(query, this);
        com.Parameters.AddWithValue("ehr_id", ehrId);
        var reader = await com.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(new OpenEhrDocumetModel
            {
                start_time = reader.GetDateTime("start_time"),
                ehr_case_id = reader.GetGuid("ehr_case_id"),
                name = reader.GetString("name"),
                cuid = reader.GetString("cuid"),
                link = reader.IsNull("cuid") ? string.Empty : reader.GetString("cuid"),
                atid = reader.GetString("atid"),
                meaning = reader.IsNull("meaning") ? string.Empty : reader.GetString("meaning"),
                composer = reader.GetString("composer"),
                state = reader.GetString("state"),
                ehr_id = reader.GetGuid("ehr_id"),
                tags = JsonConvert.DeserializeObject<OpenEhrTagsContainer>(reader.GetString("tags")),
                vo_id = reader.GetGuid("vo_id"),
                template_id = reader.GetString("template_id")
            });

        return result.OrderByDescending(x => x.start_time).ThenBy(x => x.cuid).ToArray();
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
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{Url}tagging/{compositionUid}");
            requestMessage.Headers.TryAddWithoutValidation("Authorization", GetCredentials());
            var response = Client.Send(requestMessage);
            response.EnsureSuccessStatusCode();
            var jsonResponse = response.Content.ReadAsStringAsync().Result;
            EhrConnectionLogger.LogDebug("{compositionUid} tags\r\n{jsonResponse}", compositionUid, jsonResponse);
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
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{Url}tagging/{compositionUid}");
            requestMessage.Headers.TryAddWithoutValidation("Authorization", GetCredentials());
            var response = await Client.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
            var jsonResponse = await response.Content.ReadAsStringAsync();
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
        var result = new List<OpenEhrDocumetModel>();
        foreach (var day in EMIAS.DateTimeExtensions.Extensions.EachDay(dtBeg, dtEnd))
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
            var com = new AqlCommand(query, this);
            com.Parameters.AddWithValue("ehr_id", ehrId);
            com.Parameters.AddWithValue("dt_beg", day);
            com.Parameters.AddWithValue("dt_end", day.EndOfDay());
            var reader = await com.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                result.Add(new OpenEhrDocumetModel
                {
                    start_time = reader.GetDateTime("start_time"),
                    ehr_case_id = reader.GetGuid("ehr_case_id"),
                    name = reader.GetString("name"),
                    cuid = reader.GetString("cuid"),
                    link = reader.IsNull("cuid") ? string.Empty : reader.GetString("cuid"),
                    atid = reader.GetString("atid"),
                    meaning = reader.IsNull("meaning") ? string.Empty : reader.GetString("meaning"),
                    composer = reader.GetString("composer"),
                    state = reader.GetString("state"),
                    ehr_id = reader.GetGuid("ehr_id"),
                    tags = JsonConvert.DeserializeObject<OpenEhrTagsContainer>(reader.GetString("tags")),
                    vo_id = reader.GetGuid("vo_id"),
                    template_id = reader.GetString("template_id")
                });
        }

        return result.OrderByDescending(x => x.start_time).ThenBy(x => x.cuid).ToArray();
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
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{Url}ehr/{ehrId}/reindex");
            requestMessage.Headers.TryAddWithoutValidation("Authorization", GetCredentials());
            var response = await Client.SendAsync(requestMessage);
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
        var compositions = await GetCompositions(ehrId);
        foreach (var composition in compositions)
        {
            await DeleteComposition(composition.vo_id);
            await RestoreComposition(composition.vo_id);
        }
    }

    public async Task Tag(string cuid, OpenEhrTagsContainer tags)
    {
        var preparedTags = new Tagging(cuid, tags.tags);
        var serializedTags = JsonConvert.SerializeObject(preparedTags, Formatting.Indented, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });

        try
        {
            var content = new StringContent(serializedTags, null, "application/json");
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{Url}tagging");
            requestMessage.Headers.TryAddWithoutValidation("Authorization", GetCredentials());
            requestMessage.Content = content;
            var response = await Client.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to tag {cuid}: {ex.Message}");
        }
    }
}