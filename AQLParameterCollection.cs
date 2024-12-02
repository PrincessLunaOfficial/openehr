namespace OpenEhr;

public sealed class AQLParameterCollection
{
    internal List<AQLParameter> InternalList = new();

    public AQLParameter AddWithValue(string parameterName, object value)
    {
        AQLParameter parameter = null;

        if (value is IEnumerable<Guid> guid_enumerable)
        {
            parameter = new AQLParameter(parameterName, string.Join(',', guid_enumerable.Select(x => $"'{x.ToString()}'")));
            InternalList.Add(parameter);
            return parameter;
        }

        if (value is IEnumerable<string> string_enumerable)
        {
            parameter = new AQLParameter(parameterName, string.Join(',', string_enumerable.Select(x => $"'{x.ToString()}'")));
            InternalList.Add(parameter);
            return parameter;
        }

        if (value is Guid guid)
        {
            parameter = new AQLParameter(parameterName, $"'{guid}'");
            InternalList.Add(parameter);
            return parameter;
        }

        if (value is string str)
        {
            parameter = new AQLParameter(parameterName, $"'{str}'");
            InternalList.Add(parameter);
            return parameter;
        }

        if (value is DateTime dt)
        {
            parameter = new AQLParameter(parameterName, $"'{dt.ToString("yyyy-MM-dd")}T{dt.ToString("HH:mm:ss")}Z'");
            InternalList.Add(parameter);
            return parameter;
        }

        parameter = new AQLParameter(parameterName, value.ToString());
        InternalList.Add(parameter);
        return parameter;
    }
}