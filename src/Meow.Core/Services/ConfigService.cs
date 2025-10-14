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

    public ConfigService()
    {
        _serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    public async Task<Models.MeowConfig> LoadConfigAsync(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Configuration file not found: {path}");
        }

        var yaml = await File.ReadAllTextAsync(path);
        return _deserializer.Deserialize<Models.MeowConfig>(yaml);
    }

    public async Task SaveConfigAsync(Models.MeowConfig config, string path)
    {
        var yaml = _serializer.Serialize(config);
        await File.WriteAllTextAsync(path, yaml);
    }

    public bool ConfigExists(string path)
    {
        return File.Exists(path);
    }
}
