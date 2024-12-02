using System.Diagnostics;
using System.Globalization;
using EMIAS.DateTimeExtensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenEhr.Library;

namespace OpenEhr;

public static class Extensions
{
    public static DateTime? GetSignDt(this OpenEhrTagsContainer tagsContainer)
    {
        if (tagsContainer.tags == null) return null;
        foreach (var tag in tagsContainer.tags)
        {
            if (tag.tag != "sign") continue;
            foreach (var value in tag.value.Split("|"))
            {
                // if (DateTime.TryParseExact(value, "yyyy-MM-dd" + "T" + "HH:mm:ss" + ".000Z", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                if (DateTime.TryParseExact(value, "yyyy-MM-ddTHH:mm:ss.FFF'Z'", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                    return result;
            }
        }

        return null;
    }

    public static DateTime? GetSignDt(this JObject tagsContainer)
    {
        var parsed = JsonConvert.DeserializeObject<OpenEhrTagsContainer>(tagsContainer.ToString());
        if (parsed?.tags == null) return null;
        foreach (var tag in parsed.tags)
        {
            if (tag.tag != "sign") continue;
            foreach (var value in tag.value.Split("|"))
            {
                if (DateTime.TryParseExact(value, "yyyy-MM-dd" + "T" + "HH:mm:ss" + ".000Z", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                    return result;
            }
        }

        return null;
    }

    public static async Task<List<T>> GetEhrProtocols<T>(this EhrConnection con, string query, IEnumerable<Guid> ehrId, IEnumerable<Guid> ehrCaseId) where T : IEhrProtocol
    {
        con.EhrConnectionLogger.LogDebug("Preparing AqlCommand");
        var com = new AqlCommand(query, con);
        con.EhrConnectionLogger.LogDebug("Adding :ehr_id");
        com.Parameters.AddWithValue(":ehr_id", ehrId);
        con.EhrConnectionLogger.LogDebug("Adding :ehr_case_id");
        com.Parameters.AddWithValue(":ehr_case_id", ehrCaseId);
        con.EhrConnectionLogger.LogDebug("Opening connection");
        con.Open();
        var stopwatch = Stopwatch.StartNew();
        con.EhrConnectionLogger.LogDebug("Executing reader");
        var reader = await com.ExecuteReaderAsync();
        stopwatch.Stop();
        con.EhrConnectionLogger.LogInformation("Received {m_ResultSetLength} documents", reader.m_ResultSetLength);
        con.EhrConnectionLogger.LogInformation("Time taken for ExecuteReaderAsync: {Seconds} seconds", stopwatch.Elapsed.Seconds);
        var resultList = new List<T>();
        while (await reader.ReadAsync())
        {
            con.EhrConnectionLogger.LogDebug("Adding value to resultList");
            var protocol = reader.GetAs<T>();
            try
            {
                protocol.row_ts = DateTime.Now.SetKindUtc();
                if (protocol.tags_str != null)
                    protocol.tags_sign_dt = protocol.tags_str.GetSignDt().SetKindUtc();
            }
            catch (Exception e)
            {
                con.EhrConnectionLogger.LogError("Exception for protocol sign_dt: {ExMessage}", e.Message);
            }

            resultList.Add(protocol);
        }

        con.EhrConnectionLogger.LogDebug("Filtering results");
        var filteredResultList = resultList.Where(x => ehrCaseId.Contains(x.ehr_case_id)).ToList();
        con.EhrConnectionLogger.LogInformation("Filtered {filteredResultList} documents", filteredResultList.Count);
        return filteredResultList;
    }
    
    public static T ApplyConversion<T>(JObject targetObject, ref ILogger<EhrConnection> ehrConnectionLogger)
    {
        var properties = targetObject.Properties();
        // Check and update properties that end with `_str` or `_string`
        foreach (var property in properties.Where(p => p.Name.EndsWith("_str") || p.Name.EndsWith("_string")))
        {
            ehrConnectionLogger.LogDebug("Found technical parameter {ParamName} with value '{ParamValue}'", property.Name, targetObject[property.Name]?.ToString(Formatting.None));
            targetObject[property.Name] = targetObject[property.Name]?.ToString(Formatting.None);
        }

        var convertedObject = targetObject.ToObject<T>();
        if (convertedObject == null) throw new InvalidOperationException();
        foreach (var propertyInfo in convertedObject.GetType().GetProperties())
        {
            var propertyValue = propertyInfo.GetValue(convertedObject);
            if (propertyValue == null) continue;
            var stringValue = propertyValue.ToString();
            if (string.IsNullOrEmpty(stringValue) || stringValue == "''" || stringValue == "null")
                propertyInfo.SetValue(convertedObject, null);
            if (propertyValue is DateTime time) propertyInfo.SetValue(convertedObject, time.SetKindUtc());
        }

        return convertedObject;
    }
}