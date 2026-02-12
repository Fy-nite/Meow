namespace Meow.Core.Services;

/// <summary>
/// Service for building projects
/// </summary>
public interface IBuildService
{
    /// <summary>
    /// Build the project
    /// </summary>
    /// <param name="projectPath">Path to the project directory</param>
    /// <param name="clean">Whether to clean before building</param>
    /// <param name="testMainRelative">Optional relative path to a test entrypoint to use instead of the configured main</param>
    /// <param name="forceLink">Optional - if set, override the configured build.link value for this build</param>
    /// <returns>True if build succeeded</returns>
    Task<bool> BuildProjectAsync(string projectPath, bool clean = false, string? testMainRelative = null, bool? forceLink = null, IEnumerable<string>? extraArgs = null);
}
