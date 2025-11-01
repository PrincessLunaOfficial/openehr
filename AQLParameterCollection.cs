using OpenEhr.Dto;

namespace OpenEhr;

public sealed class AQLParameterCollection
{
    internal List<AqlParameter> InternalList = new List<AqlParameter>();

    public AqlParameter AddWithValue(string parameterName, object value)
    {
        AqlParameter parameter = null;

        if (value is IEnumerable<Guid> guid_enumerable)
        {
            parameter = new AqlParameter(parameterName, string.Join(',', guid_enumerable.Select(x => $"'{x.ToString()}'")));
            InternalList.Add(parameter);
            return parameter;
        }

        if (value is IEnumerable<string> string_enumerable)
        {
            parameter = new AqlParameter(parameterName, string.Join(',', string_enumerable.Select(x => $"'{x.ToString()}'")));
            InternalList.Add(parameter);
            return parameter;
        }

        if (value is Guid guid)
        {
            parameter = new AqlParameter(parameterName, $"'{guid}'");
            InternalList.Add(parameter);
            return parameter;
        }

        if (value is string str)
        {
            parameter = new AqlParameter(parameterName, $"'{str}'");
            InternalList.Add(parameter);
            return parameter;
        }

        if (value is DateTime dt)
        {
            parameter = new AqlParameter(parameterName, $"'{dt.ToString("yyyy-MM-dd")}T{dt.ToString("HH:mm:ss")}Z'");
            InternalList.Add(parameter);
            return parameter;
        }

        parameter = new AqlParameter(parameterName, value.ToString());
        InternalList.Add(parameter);
        return parameter;
    }
}