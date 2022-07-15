namespace MongoConcurrency.Data.Models;

public class VersionTrackerClass
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public int Value  { get; set; }
}