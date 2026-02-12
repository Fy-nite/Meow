# Meow API Documentation

This document describes the public API of Meow.Core, which can be used to build custom toolchain extensions.

## Overview

Meow.Core provides a library API for managing MASM projects programmatically. This allows you to:
- Create and initialize projects
- Load and save configuration files
- Integrate Meow functionality into your own tools

## Installation

Add a reference to Meow.Core in your project:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/Meow.Core/Meow.Core.csproj" />
</ItemGroup>
```

Or once published to NuGet:

```bash
dotnet add package Meow.Core
```

## Services

### IConfigService / ConfigService

Manages loading and saving of `meow.yaml` configuration files.

```csharp
using Meow.Core.Services;

var configService = new ConfigService();
```

#### Methods

**LoadConfigAsync**
```csharp
Task<MeowConfig> LoadConfigAsync(string path)
```
Loads a configuration file from the specified path.

**SaveConfigAsync**
```csharp
Task SaveConfigAsync(MeowConfig config, string path)
```
Saves a configuration object to the specified path.

**ConfigExists**
```csharp
bool ConfigExists(string path)
```
Checks if a configuration file exists at the specified path.

#### Example

```csharp
var configService = new ConfigService();

// Load existing config
var config = await configService.LoadConfigAsync("meow.yaml");

// Modify
config.Version = "1.0.0";
config.Dependencies["new-lib"] = "^2.0.0";

// Save
await configService.SaveConfigAsync(config, "meow.yaml");
```

### IProjectService / ProjectService

Handles project initialization and management.

```csharp
using Meow.Core.Services;

var configService = new ConfigService();
var projectService = new ProjectService(configService);
```

#### Methods

**InitializeMasmProjectAsync**
```csharp
Task<bool> InitializeMasmProjectAsync(string name, string path, string? author = null)
```
Creates a new MASM project with the standard structure.

**IsExistingProject**
```csharp
bool IsExistingProject(string path)
```
Checks if the specified directory is already a Meow project.

#### Example

```csharp
var configService = new ConfigService();
var projectService = new ProjectService(configService);

// Create a new project
var success = await projectService.InitializeMasmProjectAsync(
    name: "my-library",
    path: "/path/to/project",
    author: "John Doe"
);

if (success)
{
    Console.WriteLine("Project created successfully!");
}
```

## Models

### MeowConfig

Represents the `meow.yaml` configuration structure.

```csharp
public class MeowConfig
{
    public string Name { get; set; }
    public string Version { get; set; }
    public string Description { get; set; }
    public List<string> Authors { get; set; }
    public string Type { get; set; }
    public string Main { get; set; }
    public BuildConfig Build { get; set; }
    public Dictionary<string, string> Dependencies { get; set; }
    public Dictionary<string, string> DevDependencies { get; set; }
    public Dictionary<string, string> Scripts { get; set; }
}
```

### BuildConfig

Build configuration options.

```csharp
public class BuildConfig
{
    public string Mode { get; set; }        // "debug" or "release"
    public string Output { get; set; }       // Output directory
    public string Target { get; set; }       // Target platform
    public bool Incremental { get; set; }    // Enable incremental builds
    public bool Wildcard { get; set; }       // Enable wildcard source selection
    public bool Link { get; set; }           // Enable linking of object files
    public string Objdir { get; set; }       // Object file directory
    public List<string> TestExtraArgs { get; set; } // Extra args to append for test builds
}
```

### IBuildService / BuildService

Handles building MASM projects.

```csharp
using Meow.Core.Services;

var configService = new ConfigService();
var buildService = new BuildService(configService);
```

#### Methods

**BuildProjectAsync**
```csharp
Task<bool> BuildProjectAsync(string projectPath, bool clean = false, string? testMainRelative = null, bool? forceLink = null, IEnumerable<string>? extraArgs = null)
```
Builds the MASM project at the specified path.

#### Example

```csharp
var configService = new ConfigService();
var buildService = new BuildService(configService);

// Build a project
var success = await buildService.BuildProjectAsync(
    projectPath: "/path/to/project",
    clean: false
);

if (success)
{
    Console.WriteLine("Build succeeded!");
}
```

## Complete Example: Custom Project Generator

```csharp
using Meow.Core.Services;
using Meow.Core.Models;

public class CustomProjectGenerator
{
    private readonly IConfigService _configService;
    private readonly IProjectService _projectService;

    public CustomProjectGenerator()
    {
        _configService = new ConfigService();
        _projectService = new ProjectService(_configService);
    }

    public async Task<bool> CreateProjectWithTemplate(
        string name, 
        string path, 
        string template)
    {
        // Initialize base project
        var success = await _projectService.InitializeMasmProjectAsync(
            name, path, "Custom Generator");

        if (!success) return false;

        // Customize configuration
        var configPath = Path.Combine(path, "meow.yaml");
        var config = await _configService.LoadConfigAsync(configPath);

        // Add template-specific dependencies
        config.Dependencies["stdlib"] = "^1.0.0";
        config.Scripts["test"] = "meow test";

        await _configService.SaveConfigAsync(config, configPath);

        return true;
    }
}
```

## Extensibility

The Meow.Core library is designed for extensibility. Future versions will include:
- Build orchestration APIs
- Package management interfaces
- Custom interpreter integration
- Plugin system

## See Also

- [Main README](../README.md)
- [Contributing Guide](../CONTRIBUTING.md)
