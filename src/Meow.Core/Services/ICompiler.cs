using System;
using System.Collections.Generic;
using System.IO;
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
    Task<string?> AssembleAsync(string projectPath, string sourcePath, string objDir, BuildConfig buildConfig, IProgressReporter? reporter = null);

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

    /// <summary>
    /// Return the expected object path for a given source file when assembled by this compiler.
    /// Implementations may provide language-specific rules; a default heuristic is provided.
    /// </summary>
    string GetObjectPath(string projectPath, string sourcePath, string objDir, BuildConfig buildConfig)
    {
        try
        {
            var rel = sourcePath.Replace("src/", "").Replace("src\\", "").Replace("tests/", "").Replace("tests\\", "");
            var comp = (Name ?? string.Empty).ToLowerInvariant();

            if (comp == "fusion")
            {
                var relativeDir = Path.GetDirectoryName(rel) ?? string.Empty;
                var ext = string.IsNullOrWhiteSpace(buildConfig.FutLanguage) ? (string.IsNullOrWhiteSpace(buildConfig.Target) || buildConfig.Target == "default" ? "obj" : buildConfig.Target) : buildConfig.FutLanguage;
                var fileName = Path.GetFileNameWithoutExtension(rel) + "." + ext;
                var objectDirFull = Path.Combine(objDir, relativeDir);
                return Path.Combine(objectDirFull, fileName);
            }

            var flat = rel.Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_');

            if (comp == "masm")
            {
                flat = flat.Replace(".masm", ".masi");
                return Path.Combine(objDir, flat);
            }

            if (comp == "csharp")
            {
                var outName = Path.GetFileNameWithoutExtension(rel) + ".exe";
                return Path.Combine(objDir, outName);
            }

            return Path.Combine(objDir, flat + ".o");
        }
        catch
        {
            return Path.Combine(objDir, Path.GetFileName(sourcePath) + ".o");
        }
    }
}
