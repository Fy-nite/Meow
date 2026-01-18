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
    /// <returns>True if build succeeded</returns>
    Task<bool> BuildProjectAsync(string projectPath, bool clean = false);
}
