using System.Collections.Generic;

namespace Meow.Core.Models;

public class PurrSearchResult
{
    public int TotalCount { get; set; }
    public List<PurrPackage> Packages { get; set; } = new();
}
