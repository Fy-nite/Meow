using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Meow.Core.Services;

/// <summary>
/// Implementation of configuration service using YamlDotNet
/// </summary>
public class ConfigService : IConfigService
{
    private readonly ISerializer _serializer;
    private readonly IDeserializer _deserializer;

    /// <summary>
    /// Creates a new instance of ConfigService with default YAML settings
    /// </summary>
    public ConfigService()
    {
        _serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    /// <inheritdoc />
    public async Task<Models.MeowConfig> LoadConfigAsync(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Configuration file not found: {path}");
        }

        var yaml = await File.ReadAllTextAsync(path);
        return _deserializer.Deserialize<Models.MeowConfig>(yaml);
    }

    /// <inheritdoc />
    public async Task SaveConfigAsync(Models.MeowConfig config, string path)
    {
        var yaml = _serializer.Serialize(config);
        await File.WriteAllTextAsync(path, yaml);
    }

    /// <inheritdoc />
    public bool ConfigExists(string path)
    {
        return File.Exists(path);
    }
}
