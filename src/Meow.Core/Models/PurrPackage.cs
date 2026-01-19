using System.Collections.Generic;

namespace Meow.Core.Models;

public class PurrPackage
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Authors { get; set; } = new();
    public Dictionary<string, string>? Dependencies { get; set; }
    public string? Category { get; set; }
    public string? Homepage { get; set; }
    public string? DownloadUrl { get; set; }
    public long Downloads { get; set; }
}
