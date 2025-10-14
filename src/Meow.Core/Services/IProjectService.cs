namespace Meow.Core.Services;

/// <summary>
/// Service for initializing new Meow projects
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// Initialize a new MASM project
    /// </summary>
    /// <param name="name">Project name</param>
    /// <param name="path">Directory path</param>
    /// <param name="author">Project author</param>
    /// <returns>True if successful</returns>
    Task<bool> InitializeMasmProjectAsync(string name, string path, string? author = null);

    /// <summary>
    /// Check if a directory is already a Meow project
    /// </summary>
    /// <param name="path">Directory path</param>
    /// <returns>True if it's already a project</returns>
    bool IsExistingProject(string path);
}
