using Meow.Core.Models;

namespace Meow.Core.Services;

/// <summary>
/// Implementation of project initialization service
/// </summary>
public class ProjectService : IProjectService
{
    private readonly IConfigService _configService;

    public ProjectService(IConfigService configService)
    {
        _configService = configService;
    }

    public async Task<bool> InitializeMasmProjectAsync(string name, string path, string? author = null)
    {
        try
        {
            // Create project directory if it doesn't exist
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            // Create subdirectories
            Directory.CreateDirectory(Path.Combine(path, "src"));
            Directory.CreateDirectory(Path.Combine(path, "tests"));
            Directory.CreateDirectory(Path.Combine(path, "build"));

            // Create meow.yaml configuration
            var config = new MeowConfig
            {
                Name = name,
                Version = "0.1.0",
                Description = $"A {name} MASM project",
                Type = "masm",
                Main = "src/main.masm",
                Build = new BuildConfig
                {
                    Mode = "debug",
                    Output = "build",
                    Target = "default",
                    Incremental = true
                }
            };

            if (!string.IsNullOrEmpty(author))
            {
                config.Authors.Add(author);
            }

            await _configService.SaveConfigAsync(config, Path.Combine(path, "meow.yaml"));

            // Create a starter MASM file
            var masmContent = GetMasmTemplate(name);
            await File.WriteAllTextAsync(Path.Combine(path, "src", "main.masm"), masmContent);

            // Create a README
            var readmeContent = GetReadmeTemplate(name);
            await File.WriteAllTextAsync(Path.Combine(path, "README.md"), readmeContent);

            // Create .gitignore
            var gitignoreContent = GetGitignoreTemplate();
            await File.WriteAllTextAsync(Path.Combine(path, ".gitignore"), gitignoreContent);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool IsExistingProject(string path)
    {
        return _configService.ConfigExists(Path.Combine(path, "meow.yaml"));
    }

    private static string GetMasmTemplate(string name)
    {
        return $@"; {name} - Main Entry Point
; This is a starter MASM (MicroAssembly) project created with Meow

; Program entry point
start:
    ; Your code here
    push ""Hello from {name}!""
    call print
    halt

; Print function
print:
    ; Print implementation (handled by MASM interpreter)
    ret
";
    }

    private static string GetReadmeTemplate(string name)
    {
        return $@"# {name}

A MASM project created with Meow.

## Getting Started

### Build
```bash
meow build
```

### Run
```bash
meow run
```

### Test
```bash
meow test
```

## About MASM

MicroAssembly (MASM) is a simple assembly-like language designed for educational purposes and rapid prototyping.

## Dependencies

Dependencies are managed through PurrNet. Add them in `meow.yaml`:

```yaml
dependencies:
  example-lib: ""^1.0.0""
```

Then run:
```bash
meow install
```
";
    }

    private static string GetGitignoreTemplate()
    {
        return @"# Build outputs
build/
*.out
*.exe

# Dependencies
.meow/
node_modules/

# IDE
.vscode/
.idea/
*.swp
*~

# OS
.DS_Store
Thumbs.db
";
    }
}
