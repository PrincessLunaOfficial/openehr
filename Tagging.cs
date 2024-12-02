namespace OpenEhr;

public class Tagging
{
    public string compositionUid { get; set; }
    public OpenEhrTag[]? tags { get; set; }

    public Tagging(string compositionUid, OpenEhrTag[]? tags)
    {
        this.compositionUid = compositionUid;
        this.tags = tags;
    }
}