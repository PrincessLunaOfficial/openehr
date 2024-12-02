using System.Globalization;

namespace OpenEhr;

public class OpenEhrTagsContainer
{
    public OpenEhrTag[]? tags { get; set; }
}
public class OpenEhrTag
{
    public string tag { get; set; } = null!;
    public string value { get; set; }= null!;
    public string aqlPath { get; set; }= null!;
}