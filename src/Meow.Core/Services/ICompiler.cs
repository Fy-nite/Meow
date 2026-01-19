using System.Threading.Tasks;
using Meow.Core.Models;

namespace Meow.Core.Services;

public interface ICompiler
{
    /// <summary>
    /// Name of the compiler (e.g., "masm")
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Source file extensions supported by this compiler (include leading dot)
    /// </summary>
    IEnumerable<string> SourceExtensions { get; }

    /// <summary>
    /// Categories of dependencies this compiler supports (e.g., "runtime", "native", "sharpir").
    /// Meow will consult this list before emitting build warnings about dependencies.
    /// </summary>
    IEnumerable<string> SupportedDependencyCategories { get; }

    /// <summary>
    /// Assemble a source file into an object file. Returns the generated object path or null on failure.
    /// </summary>
    Task<string?> AssembleAsync(string projectPath, string sourcePath, string objDir, BuildConfig buildConfig);

    /// <summary>
    /// Link object files into a final output. Returns true on success.
    /// </summary>
    Task<bool> LinkAsync(IEnumerable<string> objectFiles, string outputFile, BuildConfig buildConfig);

    /// <summary>
    /// Optional: run the produced executable (if applicable).
    /// </summary>
    Task<bool> RunAsync(string executable, string? stdinFile = null);

    /// <summary>
    /// Optional: debug the produced executable (if applicable).
    /// </summary>
    Task<bool> DebugAsync(string executable, string? stdinFile = null);
}
