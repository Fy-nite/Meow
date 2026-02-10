using System.Collections.Generic;
using System.Threading.Tasks;

namespace Meow.Core.Services;

/// <summary>
/// Interface for interpreted-language runners (scripts/executables that are executed by an interpreter)
/// </summary>
public interface IRunner
{
    /// <summary>
    /// Name of the runner (e.g., "python")
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Source file extensions supported by this runner (include leading dot)
    /// </summary>
    IEnumerable<string> SourceExtensions { get; }

    /// <summary>
    /// Run the provided script/file. Returns true on success.
    /// </summary>
    Task<bool> RunAsync(string scriptPath, string? stdinFile = null);

    /// <summary>
    /// Optional: debug the provided script/file. Returns true on success.
    /// </summary>
    Task<bool> DebugAsync(string scriptPath, string? stdinFile = null);
}
