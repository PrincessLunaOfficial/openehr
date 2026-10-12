using System.Globalization;
using OpenEhr.Dto;

namespace OpenEhr;

public sealed class AQLParameterCollection
{
    internal List<AqlParameter> InternalList { get; } = new();

    public AqlParameter AddWithValue(string parameterName, object value)
    {
        if (string.IsNullOrWhiteSpace(parameterName)) throw new ArgumentException("Parameter name is empty", nameof(parameterName));
        if (value == null) throw new ArgumentNullException(nameof(value));
        string literal = value switch
        {
            string text => Quote(text),
            Guid guid => Quote(guid.ToString()),
            DateTimeOffset time => Quote(time.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture)),
            DateTime time when time.Kind != DateTimeKind.Unspecified =>
                Quote(time.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture)),
            DateTime => throw new ArgumentException("AQL DateTime must have a known time zone", nameof(value)),
            IEnumerable<string> strings => Join(strings.Select(Quote)),
            IEnumerable<Guid> guids => Join(guids.Select(x => Quote(x.ToString()))),
            bool boolean => boolean ? "true" : "false",
            IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
            _ => throw new ArgumentException($"Unsupported AQL parameter type: {value.GetType().Name}", nameof(value))
        };
        var parameter = new AqlParameter(parameterName, literal);
        InternalList.Add(parameter);
        return parameter;
    }

    private static string Quote(string text) => "'" + text.Replace("'", "''", StringComparison.Ordinal) + "'";

    private static string Join(IEnumerable<string> values)
    {
        string[] items = values.ToArray();
        if (items.Length == 0) throw new ArgumentException("An empty AQL array is ambiguous");
        return string.Join(',', items);
    }
}
