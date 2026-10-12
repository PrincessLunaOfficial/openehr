using System.Net;
using EMIAS.DateTimeExtensions;
using Newtonsoft.Json.Linq;

namespace OpenEhr;

public class AQLDataReader
{

    public AQLDataReader(IEnumerable<JObject> resultSet, HttpStatusCode? httpStatusCode, string? content)
    {
        m_ResultSet = resultSet.ToList();
        m_ResultSetLength = m_ResultSet.Count();
        if (m_ResultSetLength > 0)
            FieldCount = m_ResultSet.First().Properties().Count();

        HttpStatusCode = httpStatusCode;
        Content = content;
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

    private JToken Required(string propertyName)
    {
        JProperty? property = m_CurrentRowProperties?.FirstOrDefault(p => p.Name == propertyName);
        if (property?.Value == null || property.Value.Type == JTokenType.Null)
            throw new EhrContractException($"AQL result has no required field: {propertyName}");
        return property.Value;
    }

    public object GetValue(string propertyName)
    {
        return Required(propertyName);
    }

    public Guid GetGuid(string propertyName)
    {
        return Required(propertyName).ToObject<Guid>();
    }

    public string GetString(string propertyName)
    {
        return Required(propertyName).ToString();
    }

    public DateTime GetDateTime(string propertyName, bool utc = true)
    {
        if (utc)
            return Required(propertyName).ToObject<DateTimeOffset>().UtcDateTime;
        return Required(propertyName).ToObject<DateTime>();
    }

    public double GetDouble(string propertyName)
    {
        return Required(propertyName).ToObject<double>();
    }

    public double GetInt32(string propertyName)
    {
        return Required(propertyName).ToObject<int>();
    }

    public bool IsNull(string propertyName)
    {
        return m_CurrentRowProperties.All(p => p.Name != propertyName) ||
               m_CurrentRowProperties.First(p => p.Name == propertyName).Value.Type == JTokenType.Null ||
               string.IsNullOrEmpty(m_CurrentRowProperties.First(p => p.Name == propertyName).Value.ToString()) ||
               m_CurrentRowProperties.First(p => p.Name == propertyName).Value.ToString() == "''";
    }

    public JObject GetObject()
    {
        return m_CurrentRowResult;
    }

    public T GetAs<T>()
    {
        return Extensions.ApplyConversion<T>(m_CurrentRowResult);
    }
}
