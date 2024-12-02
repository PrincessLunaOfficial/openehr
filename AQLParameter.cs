namespace OpenEhr;

public class AQLParameter
{
    public AQLParameter(string name, string value)
    {
        Name = name;
        Value = value;
    }

    public string Name { get; set; }
    public string Value { get; set; }
}