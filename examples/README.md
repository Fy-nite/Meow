# Meow Library Usage Examples

This directory contains examples of using Meow.Core as a library in your own applications.

## Example 1: Basic Project Creation

```csharp
using Meow.Core.Services;

var configService = new ConfigService();
var projectService = new ProjectService(configService);

// Create a new MASM project
await projectService.InitializeMasmProjectAsync(
    name: "my-game",
    path: "./my-game",
    author: "Game Developer"
);
```

## Example 2: Configuration Management

```csharp
using Meow.Core.Services;
using Meow.Core.Models;

var configService = new ConfigService();

// Load existing configuration
var config = await configService.LoadConfigAsync("meow.yaml");

// Modify configuration
config.Version = "1.0.0";
config.Dependencies["physics-engine"] = "^2.0.0";
config.Scripts["build-release"] = "meow build --mode release";

// Save changes
await configService.SaveConfigAsync(config, "meow.yaml");
```

## Example 3: Custom Project Generator

```csharp
using Meow.Core.Services;
using Meow.Core.Models;

public class GameProjectGenerator
{
    private readonly IConfigService _configService;
    private readonly IProjectService _projectService;

    public GameProjectGenerator()
    {
        _configService = new ConfigService();
        _projectService = new ProjectService(_configService);
    }

    public async Task<bool> CreateGameProject(string name, string path)
    {
        // Create base project
        var success = await _projectService.InitializeMasmProjectAsync(
            name, path, "Game Generator");

        if (!success) return false;

        // Load and customize configuration
        var configPath = Path.Combine(path, "meow.yaml");
        var config = await _configService.LoadConfigAsync(configPath);

        // Add game-specific dependencies
        config.Dependencies["game-engine"] = "^1.0.0";
        config.Dependencies["sprite-lib"] = "^2.1.0";
        config.Dependencies["audio-lib"] = "^1.5.0";

        // Add game-specific scripts
        config.Scripts["play"] = "meow run";
        config.Scripts["package"] = "meow build --mode release";

        await _configService.SaveConfigAsync(config, configPath);

        // Create additional game directories
        Directory.CreateDirectory(Path.Combine(path, "assets"));
        Directory.CreateDirectory(Path.Combine(path, "assets", "sprites"));
        Directory.CreateDirectory(Path.Combine(path, "assets", "audio"));
        Directory.CreateDirectory(Path.Combine(path, "levels"));

        return true;
    }
}
```

## Example 4: Configuration Validator

```csharp
using Meow.Core.Services;

public class ConfigValidator
{
    private readonly IConfigService _configService;

    public ConfigValidator()
    {
        _configService = new ConfigService();
    }

    public async Task<List<string>> ValidateProject(string projectPath)
    {
        var errors = new List<string>();
        var configPath = Path.Combine(projectPath, "meow.yaml");

        // Check if config exists
        if (!_configService.ConfigExists(configPath))
        {
            errors.Add("meow.yaml not found");
            return errors;
        }

        // Load and validate
        var config = await _configService.LoadConfigAsync(configPath);

        if (string.IsNullOrWhiteSpace(config.Name))
            errors.Add("Project name is required");

        if (string.IsNullOrWhiteSpace(config.Version))
            errors.Add("Project version is required");

        if (string.IsNullOrWhiteSpace(config.Main))
            errors.Add("Main entry point is required");

        // Check if main file exists
        var mainPath = Path.Combine(projectPath, config.Main);
        if (!File.Exists(mainPath))
            errors.Add($"Main file not found: {config.Main}");

        return errors;
    }
}
```

## Example 5: Batch Project Creation

```csharp
using Meow.Core.Services;

public class BatchProjectCreator
{
    private readonly IProjectService _projectService;

    public BatchProjectCreator()
    {
        var configService = new ConfigService();
        _projectService = new ProjectService(configService);
    }

    public async Task CreateMultipleProjects(string basePath, params string[] names)
    {
        foreach (var name in names)
        {
            var projectPath = Path.Combine(basePath, name);
            
            Console.WriteLine($"Creating {name}...");
            
            var success = await _projectService.InitializeMasmProjectAsync(
                name, projectPath, "Batch Creator");

            if (success)
                Console.WriteLine($"✓ {name} created successfully");
            else
                Console.WriteLine($"✗ Failed to create {name}");
        }
    }
}

// Usage
var creator = new BatchProjectCreator();
await creator.CreateMultipleProjects(
    "/projects",
    "project-alpha",
    "project-beta",
    "project-gamma"
);
```

## Running the Examples

To run these examples in your own project:

1. Add a reference to Meow.Core:
   ```xml
   <ItemGroup>
     <ProjectReference Include="path/to/Meow.Core/Meow.Core.csproj" />
   </ItemGroup>
   ```

2. Copy the example code into your application

3. Build and run:
   ```bash
   dotnet build
   dotnet run
   ```

## See Also

- [API Documentation](../docs/API.md)
- [Main README](../README.md)
