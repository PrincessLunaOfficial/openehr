using System.Net;
using EMIAS.DateTimeExtensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OpenEhr;

public class AQLDataReader
{
    private ILogger<EhrConnection> _ehrConnectionLogger;

    public AQLDataReader(IEnumerable<JObject> resultSet, HttpStatusCode? httpStatusCode, string? content, ref ILogger<EhrConnection> ehrConnectionLogger)
    {
        m_ResultSet = resultSet.ToList();
        m_ResultSetLength = m_ResultSet.Count();
        if (m_ResultSetLength > 0)
            FieldCount = m_ResultSet.Properties().Count();

        HttpStatusCode = httpStatusCode;
        Content = content;
        this._ehrConnectionLogger = ehrConnectionLogger;
    }

    public int FieldCount { get; set; }
    private IEnumerable<JObject> m_ResultSet { get; }
    public int m_ResultSetLength { get; set; }
    private int m_CurrentRowIndex { get; set; }
    private IEnumerable<JProperty> m_CurrentRowProperties { get; set; }
    public JObject m_CurrentRowResult { get; set; }
    public HttpStatusCode? HttpStatusCode { get; set; }
    public string? Content { get; set; }

    public async Task<bool> ReadAsync()
    {
        try
        {
            if (m_CurrentRowIndex <= m_ResultSetLength - 1)
            {
                m_CurrentRowProperties = m_ResultSet.ElementAt(m_CurrentRowIndex).Properties();
                m_CurrentRowResult = m_ResultSet.ElementAt(m_CurrentRowIndex);
                m_CurrentRowIndex++;
                return await Task.FromResult(true);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"AQLDataReader exception: {ex.Message}");
        }

        return await Task.FromResult(false);
    }

    public object GetValue(string propertyName)
    {
        return m_CurrentRowProperties.FirstOrDefault(p => p.Name == propertyName).Value;
    }

    public Guid GetGuid(string propertyName)
    {
        return (Guid)m_CurrentRowProperties.FirstOrDefault(p => p.Name == propertyName).Value;
    }

    public string GetString(string propertyName)
    {
        return m_CurrentRowProperties.FirstOrDefault(p => p.Name == propertyName).Value.ToString();
    }

    public DateTime GetDateTime(string propertyName, bool utc = true)
    {
        if (utc)
            return ((DateTime)m_CurrentRowProperties.FirstOrDefault(p => p.Name == propertyName).Value).SetKindUtc();
        return (DateTime)m_CurrentRowProperties.FirstOrDefault(p => p.Name == propertyName).Value;
    }

    public double GetDouble(string propertyName)
    {
        return (double)m_CurrentRowProperties.FirstOrDefault(p => p.Name == propertyName).Value;
    }

    public double GetInt32(string propertyName)
    {
        return (int)m_CurrentRowProperties.FirstOrDefault(p => p.Name == propertyName).Value;
    }

    public bool IsNull(string propertyName)
    {
        return m_CurrentRowProperties.All(p => p.Name != propertyName) ||
               string.IsNullOrEmpty(m_CurrentRowProperties.First(p => p.Name == propertyName).Value.ToString()) ||
               m_CurrentRowProperties.First(p => p.Name == propertyName).Value.ToString() == "''";
    }

    public JObject GetObject()
    {
        return m_CurrentRowResult;
    }

    public T GetAs<T>()
    {
        return Extensions.ApplyConversion<T>(m_CurrentRowResult, ref _ehrConnectionLogger);
    }
    
    // public T GetAs<T>()
    // {
    //     var properties = m_CurrentRowResult.Properties();
    //     // Check and update properties that end with `_str` or `_string`
    //     foreach (var property in properties.Where(p => p.Name.EndsWith("_str") || p.Name.EndsWith("_string")))
    //     {
    //         _ehrConnectionLogger.LogDebug("Found technical parameter {ParamName} with value '{ParamValue}'", property.Name, m_CurrentRowResult[property.Name]?.ToString(Formatting.None));
    //         m_CurrentRowResult[property.Name] = m_CurrentRowResult[property.Name]?.ToString(Formatting.None);
    //     }
    //
    //     var result = m_CurrentRowResult.ToObject<T>();
    //     if (result == null) throw new InvalidOperationException();
    //     foreach (var propertyInfo in result.GetType().GetProperties())
    //     {
    //         var val = propertyInfo.GetValue(result);
    //         if (val == null) continue;
    //         var value = val.ToString();
    //         if (string.IsNullOrEmpty(value) || value == "''" || value == "null")
    //             propertyInfo.SetValue(result, null);
    //         if (val is DateTime time) propertyInfo.SetValue(result, time.SetKindUtc());
    //     }
    //
    //     return result;
    // }

    // public T GetAs<T>()
    // {
    //     var result = JsonConvert.DeserializeObject<T>(m_CurrentRowResult.ToString(),
    //         new JsonSerializerSettings
    //         {
    //             DateParseHandling = DateParseHandling.None // for dates
    //         });
    //
    //     if (result == null) throw new InvalidOperationException();
    //     foreach (var propertyInfo in result.GetType().GetProperties())
    //     {
    //         var val = propertyInfo.GetValue(result);
    //         switch (val)
    //         {
    //             case null:
    //                 continue;
    //             case JObject:
    //             case JToken:
    //                 propertyInfo.SetValue(result, val.ToString());
    //                 continue;
    //         }
    //
    //         var value = val.ToString();
    //         if (string.IsNullOrEmpty(value) || value == "''")
    //             propertyInfo.SetValue(result, null);
    //         if (val is DateTime time) propertyInfo.SetValue(result, time.SetKindUtc());
    //     }
    //     return result;
    // }
}