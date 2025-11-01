namespace OpenEhr.Dto;

public class AqlParameter
{
    public AqlParameter(string name, string value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; set; }
    public string Value { get; set; }
}