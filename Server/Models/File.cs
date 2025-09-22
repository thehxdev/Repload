namespace Server.Models;

public class File(string uuid, uint size, uint written, string? name = null)
{
    public string Uuid { get; set; } = uuid;
    public string? Name { get; set; } = name;
    public uint Size { get; set; } = size;
    public uint Written { get; set; } = written;
}