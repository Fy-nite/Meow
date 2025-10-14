namespace Meow.Core.Services;

/// <summary>
/// Service for managing Meow project configuration
/// </summary>
public interface IConfigService
{
    /// <summary>
    /// Load configuration from a YAML file
    /// </summary>
    /// <param name="path">Path to the configuration file</param>
    /// <returns>The loaded configuration</returns>
    Task<Models.MeowConfig> LoadConfigAsync(string path);

    /// <summary>
    /// Save configuration to a YAML file
    /// </summary>
    /// <param name="config">Configuration to save</param>
    /// <param name="path">Path to save the configuration</param>
    Task SaveConfigAsync(Models.MeowConfig config, string path);

    /// <summary>
    /// Check if a configuration file exists
    /// </summary>
    /// <param name="path">Path to check</param>
    /// <returns>True if the file exists</returns>
    bool ConfigExists(string path);
}
