namespace MongoConcurrency.Data.Models;

public class Counter
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public int Value  { get; set; }
}