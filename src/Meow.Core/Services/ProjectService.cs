using Meow.Core.Models;

namespace Meow.Core.Services;

/// <summary>
/// Implementation of project initialization service
/// </summary>
public class ProjectService : IProjectService
{
    private readonly IConfigService _configService;

    /// <summary>
    /// Creates a new instance of ProjectService
    /// </summary>
    /// <param name="configService">Configuration service instance</param>
    public ProjectService(IConfigService configService)
    {
        _configService = configService;
    }

    /// <inheritdoc />
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
            var masmStarter = MasmCompiler.GetStarter(name);
            await File.WriteAllTextAsync(Path.Combine(path, "src", "main.masm"), masmStarter.Content);

            // Create a README
            var readmeContent = GetReadmeTemplate(name, "masm");
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

    /// <summary>
    /// Initialize a project for the specified compiler/language
    /// </summary>
    public async Task<bool> InitializeProjectAsync(string name, string path, string compiler = "masm", string? author = null)
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

            // Determine main file and templates based on compiler
            string type = compiler.ToLowerInvariant();
            string mainFile;
            string mainContent;
            string description = $"A {name} {type} project";

            // Use reflection to find a starter template method annotated with StarterTemplateAttribute.
            var starter = GetStarterForCompiler(type, name);
            mainFile = starter.MainFile;
            mainContent = starter.Content;

            var config = new MeowConfig
            {
                Name = name,
                Version = "0.1.0",
                Description = description,
                Type = type,
                Main = mainFile,
                Build = new BuildConfig
                {
                    Mode = "debug",
                    Output = "build",
                    Target = "default",
                    Incremental = true,
                    Compiler = type,
                    Objdir = "build/obj"
                }
            };

            if (!string.IsNullOrEmpty(author))
            {
                config.Authors.Add(author);
            }

            await _configService.SaveConfigAsync(config, Path.Combine(path, "meow.yaml"));

            // Create the main file for the chosen compiler
            await File.WriteAllTextAsync(Path.Combine(path, mainFile), mainContent);

            // Create a README
            var readmeContent = GetReadmeTemplate(name, type);
            // if the readme already exists, don't overwrite it, but if it doesn't exist, create it
            var readmePath = Path.Combine(path, "README.md");
            if (!File.Exists(readmePath))
            {
            await File.WriteAllTextAsync(Path.Combine(path, "README.md"), readmeContent);
            }
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

    private static readonly Dictionary<string, System.Reflection.MethodInfo> _starterMethods = new();
    private static void EnsureStarterMethodsLoaded()
    {
        if (_starterMethods.Count > 0) return;
        var asm = typeof(ProjectService).Assembly;
        var compilerInterface = typeof(ICompiler);
        foreach (var t in asm.GetTypes())
        {
            if (!compilerInterface.IsAssignableFrom(t) || t.IsInterface || t.IsAbstract) continue;

            // find static methods annotated with StarterTemplateAttribute
            var methods = t.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            foreach (var m in methods)
            {
                var attr = m.GetCustomAttributes(typeof(StarterTemplateAttribute), false).FirstOrDefault() as StarterTemplateAttribute;
                if (attr == null) continue;

                if (attr.Names != null && attr.Names.Length > 0)
                {
                    foreach (var name in attr.Names)
                    {
                        _starterMethods[name.ToLowerInvariant()] = m;
                    }
                }
                else
                {
                    try
                    {
                        // create instance to read Name property
                        var inst = Activator.CreateInstance(t) as ICompiler;
                        if (inst != null)
                        {
                            _starterMethods[inst.Name.ToLowerInvariant()] = m;
                        }
                    }
                    catch
                    {
                        // ignore failures to instantiate
                    }
                }
            }
        }
    }

    private static (string MainFile, string Content) GetStarterForCompiler(string compilerName, string projectName)
    {
        EnsureStarterMethodsLoaded();
        if (_starterMethods.TryGetValue(compilerName.ToLowerInvariant(), out var mi))
        {
            try
            {
                var result = mi.Invoke(null, new object[] { projectName });
                if (result == null) return ("src/main.txt", $"// {projectName} - Main file for {compilerName}\n");

                var t = result.GetType();
                // Try named tuple properties
                var propMain = t.GetProperty("MainFile");
                var propContent = t.GetProperty("Content");
                if (propMain != null && propContent != null)
                {
                    var mf = propMain.GetValue(result)?.ToString() ?? "src/main.txt";
                    var ct = propContent.GetValue(result)?.ToString() ?? string.Empty;
                    return (mf, ct);
                }
                // fallback to Item1/Item2 (handle properties or fields)
                object GetMember(object obj, string memberName)
                {
                    var prop = t.GetProperty(memberName);
                    if (prop != null) return prop.GetValue(obj)!;
                    var field = t.GetField(memberName);
                    if (field != null) return field.GetValue(obj)!;
                    return null!;
                }

                var v1 = GetMember(result, "Item1");
                var v2 = GetMember(result, "Item2");
                if (v1 != null && v2 != null)
                {
                    var mf = v1?.ToString() ?? "src/main.txt";
                    var ct = v2?.ToString() ?? string.Empty;
                    return (mf, ct);
                }

                return ("src/main.txt", $"// {projectName} - Main file for {compilerName}\n");
            }
            catch
            {
                return ("src/main.txt", $"// {projectName} - Main file for {compilerName}\n");
            }
        }

        // No registered starter for this compiler
        return ("src/main.txt", $"// {projectName} - Main file for {compilerName}\n");
    }

    /// <inheritdoc />
    public bool IsExistingProject(string path)
    {
        return _configService.ConfigExists(Path.Combine(path, "meow.yaml"));
    }

    private static string GetReadmeTemplate(string name, string type)
        {
                var lang = type.ToUpperInvariant();
                return $"# {name}\n\nA {type} project created with Meow.\n\n## Getting Started\n\n### Build\nmeow build\n\n### Run\nmeow run\n\n### Test\nmeow test\n\n## About {lang}\n\nThis project was scaffolded for the {type} toolchain. Replace this text with language-specific notes as needed.\n\n## Dependencies\n\nDependencies are managed through PurrNet. Add them in meow.yaml:\n\ndependencies:\n  example-lib: \"^1.0.0\"\n\nThen run:\nmeow install\n";
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
