using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Meow.Core.Models;

namespace Meow.Core.Services;

public class InstallService : IInstallService
{
    private readonly IConfigService _configService;
    private readonly IPurrNetService _purrNet;

    public InstallService(IConfigService configService, IPurrNetService purrNet)
    {
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _purrNet = purrNet ?? throw new ArgumentNullException(nameof(purrNet));
    }

    public async Task<bool> InstallAsync(string projectPath, bool saveCategories = true, CancellationToken ct = default)
    {
        var configPath = Path.Combine(projectPath, "meow.yaml");
        if (!_configService.ConfigExists(configPath))
        {
            Console.WriteLine("Error: meow.yaml not found");
            return false;
        }

        var config = await _configService.LoadConfigAsync(configPath);

        var packageDir = Path.Combine(projectPath, ".meow", "packages");
        Directory.CreateDirectory(packageDir);

        foreach (var kv in config.Dependencies)
        {
            var name = kv.Key;
            var version = kv.Value;
            try
            {
                var pkg = await _purrNet.GetPackageAsync(name, version, ct);
                if (pkg == null)
                {
                    Console.WriteLine($"Warning: package '{name}' not found on PurrNet");
                    continue;
                }

                // Persist a minimal package metadata file in .meow/packages/{name}.json
                var outPath = Path.Combine(packageDir, name + ".json");
                var json = JsonSerializer.Serialize(new { pkg.Name, pkg.Version, pkg.Category, pkg.Homepage }, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(outPath, json, ct);

                // If category missing in config, populate it from PurrNet package metadata
                if (saveCategories)
                {
                    if (string.IsNullOrWhiteSpace(pkg.Category))
                    {
                        Console.WriteLine($"Info: package '{name}' has no category on PurrNet");
                    }
                    else
                    {
                        if (config.DependencyCategories == null)
                            config.DependencyCategories = new System.Collections.Generic.Dictionary<string, string>();
                        if (!config.DependencyCategories.ContainsKey(name))
                        {
                            config.DependencyCategories[name] = pkg.Category!;
                            Console.WriteLine($"Info: set category '{pkg.Category}' for dependency '{name}' in meow.yaml");
                        }
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Network error fetching '{name}': {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error installing '{name}': {ex.Message}");
            }
        }

        // Save updated categories back to meow.yaml
        if (saveCategories)
        {
            await _configService.SaveConfigAsync(config, configPath);
        }

        return true;
    }

    public async Task<bool> UpdateAsync(string projectPath, CancellationToken ct = default)
    {
        // For now, update simply re-runs install which refreshes metadata and categories
        return await InstallAsync(projectPath, saveCategories: true, ct: ct);
    }
}
