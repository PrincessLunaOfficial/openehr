using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using EMIAS.DateTimeExtensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenEhr.Dto;
using OpenEhr.Interfaces;
using Serilog;

namespace OpenEhr;

public static class Extensions
{
    public static DateTime? GetSignDt(this OpenEhrTagsContainer tagsContainer)
    {
        if (tagsContainer.Tags == null) return null;
        foreach (OpenEhrTag tag in tagsContainer.Tags)
        {
            if (tag.Tag != "sign") continue;
            foreach (string value in tag.Value.Split("|"))
            {
                // if (DateTime.TryParseExact(value, "yyyy-MM-dd" + "T" + "HH:mm:ss" + ".000Z", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                if (DateTime.TryParseExact(value, "yyyy-MM-ddTHH:mm:ss.FFF'Z'", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                    return result;
            }
        }

        return null;
    }

    public static DateTime? GetSignDt(this JObject tagsContainer)
    {
        OpenEhrTagsContainer? parsed = JsonConvert.DeserializeObject<OpenEhrTagsContainer>(tagsContainer.ToString());
        if (parsed?.Tags == null) return null;
        foreach (OpenEhrTag tag in parsed.Tags)
        {
            if (tag.Tag != "sign") continue;
            foreach (string value in tag.Value.Split("|"))
            {
                if (DateTime.TryParseExact(value, "yyyy-MM-dd" + "T" + "HH:mm:ss" + ".000Z", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                    return result;
            }
        }

        return null;
    }

    public static async Task<List<T>> GetEhrProtocols<T>(this EhrConnection con, string query, IEnumerable<Guid> ehrId, IEnumerable<Guid> ehrCaseId) where T : IEhrProtocol
    {
        Log.Logger.Debug("Preparing AqlCommand");
        AqlCommand com = new AqlCommand(query, con);
        Log.Logger.Debug("Adding :ehr_id");
        com.Parameters.AddWithValue(":ehr_id", ehrId);
        Log.Logger.Debug("Adding :ehr_case_id");
        com.Parameters.AddWithValue(":ehr_case_id", ehrCaseId);
        Log.Logger.Debug("Opening connection");
        con.Open();
        Stopwatch stopwatch = Stopwatch.StartNew();
        Log.Logger.Debug("Executing reader");
        AQLDataReader reader = await com.ExecuteReaderAsync();
        stopwatch.Stop();
        Log.Logger.Information("Received {m_ResultSetLength} documents", reader.m_ResultSetLength);
        Log.Logger.Information("Time taken for ExecuteReaderAsync: {Seconds} seconds", stopwatch.Elapsed.Seconds);
        List<T> resultList = new List<T>();
        while (await reader.ReadAsync())
        {
            Log.Logger.Debug("Adding value to resultList");
            T protocol = reader.GetAs<T>();
            try
            {
                protocol.RowTs = DateTime.Now.SetKindUtc();
                if (protocol.TagsStr != null)
                    protocol.TagsSignDt = protocol.TagsStr.GetSignDt().SetKindUtc();
            }
            catch (Exception e)
            {
                Log.Logger.Error("Exception for protocol sign_dt: {ExMessage}", e.Message);
            }

            resultList.Add(protocol);
        }

        Log.Logger.Debug("Filtering results");
        List<T> filteredResultList = resultList.Where(x => ehrCaseId.Contains(x.EhrCaseId)).ToList();
        Log.Logger.Information("Filtered {filteredResultList} documents", filteredResultList.Count);
        return filteredResultList;
    }
    
    public static T ApplyConversion<T>(JObject targetObject)
    {
        IEnumerable<JProperty> properties = targetObject.Properties();
        // Check and update properties that end with `_str` or `_string`
        foreach (JProperty property in properties.Where(p => p.Name.EndsWith("_str") || p.Name.EndsWith("_string")))
        {
            Log.Logger.Debug("Found technical parameter {ParamName} with value '{ParamValue}'", property.Name, targetObject[property.Name]?.ToString(Formatting.None));
            targetObject[property.Name] = targetObject[property.Name]?.ToString(Formatting.None);
        }

        T? convertedObject = targetObject.ToObject<T>();
        if (convertedObject == null) throw new InvalidOperationException();
        foreach (PropertyInfo propertyInfo in convertedObject.GetType().GetProperties())
        {
            object? propertyValue = propertyInfo.GetValue(convertedObject);
            if (propertyValue == null) continue;
            string? stringValue = propertyValue.ToString();
            if (string.IsNullOrEmpty(stringValue) || stringValue == "''" || stringValue == "null")
                propertyInfo.SetValue(convertedObject, null);
            if (propertyValue is DateTime time) propertyInfo.SetValue(convertedObject, time.SetKindUtc());
        }

        return convertedObject;
    }
}