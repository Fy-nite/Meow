namespace Meow.Core.Models;

/// <summary>
/// Represents the meow.yaml configuration file structure
/// </summary>
public class MeowConfig
{
    /// <summary>
    /// Project name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Project version
    /// </summary>
    public string Version { get; set; } = "0.1.0";

    /// <summary>
    /// Project description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Authors of the project
    /// </summary>
    public List<string> Authors { get; set; } = new();

    /// <summary>
    /// Project type (e.g., "masm", "executable", "library")
    /// </summary>
    public string Type { get; set; } = "masm";

    /// <summary>
    /// Main entry point file
    /// </summary>
    public string Main { get; set; } = "src/main.masm";

    /// <summary>
    /// Build configuration
    /// </summary>
    public BuildConfig Build { get; set; } = new();

    /// <summary>
    /// Dependencies from PurrNet
    /// </summary>
    public Dictionary<string, string> Dependencies { get; set; } = new();

    /// <summary>
    /// Development dependencies
    /// </summary>
    public Dictionary<string, string> DevDependencies { get; set; } = new();

    /// <summary>
    /// Custom scripts
    /// </summary>
    public Dictionary<string, string> Scripts { get; set; } = new();
}

/// <summary>
/// Build configuration options
/// </summary>
public class BuildConfig
{
    /// <summary>
    /// Build mode (debug or release)
    /// </summary>
    public string Mode { get; set; } = "debug";

    /// <summary>
    /// Output directory
    /// </summary>
    public string Output { get; set; } = "build";

    /// <summary>
    /// Target platform
    /// </summary>
    public string Target { get; set; } = "default";

    /// <summary>
    /// Enable incremental builds
    /// </summary>
    public bool Incremental { get; set; } = true;
}
