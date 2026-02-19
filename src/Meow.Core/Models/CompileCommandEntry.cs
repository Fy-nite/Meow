using System.Text.Json.Serialization;

namespace Meow.Core.Models;

/// <summary>
/// Represents a single entry in a compile_commands.json compilation database.
/// See https://clang.llvm.org/docs/JSONCompilationDatabase.html
/// </summary>
public class CompileCommandEntry
{
    /// <summary>
    /// The working directory of the compilation. All relative paths in <see cref="Command"/>
    /// and <see cref="File"/> are relative to this directory.
    /// </summary>
    [JsonPropertyName("directory")]
    public string Directory { get; set; } = string.Empty;

    /// <summary>
    /// The compile command as a single shell-escaped string.
    /// </summary>
    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// The main translation unit source processed by this compilation step.
    /// </summary>
    [JsonPropertyName("file")]
    public string File { get; set; } = string.Empty;
}
