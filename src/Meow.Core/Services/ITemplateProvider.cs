using System.Threading.Tasks;

namespace Meow.Core.Services;

/// <summary>
/// Provides a language-specific main file template generator.
/// </summary>
public interface ITemplateProvider
{
    /// <summary>
    /// Create a main/source template file for the given project.
    /// </summary>
    /// <param name="projectPath">Absolute path to project root.</param>
    /// <param name="mainRelativePath">Path of the main file relative to project root (e.g., "src/main.py").</param>
    Task<bool> CreateMainAsync(string projectPath, string mainRelativePath);
}
