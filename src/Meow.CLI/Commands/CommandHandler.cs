using Meow.Core.Services;
using System.Reflection;

namespace Meow.CLI.Commands;

/// <summary>
/// Main command handler that routes CLI commands
/// </summary>
public class CommandHandler
{
    private const string Version = "0.1.0";
    
    private readonly IConfigService _configService;
    private readonly IProjectService _projectService;
    private readonly BuildService _buildService;
    private readonly IInstallService _installService;

    public CommandHandler(IConfigService configService, IProjectService projectService, BuildService buildService, IInstallService installService)
    {
        _configService = configService;
        _projectService = projectService;
        _buildService = buildService;
        _installService = installService;
    }

    public async Task<int> HandleCommandAsync(string[] args)
    {
        if (args.Length == 0)
        {
            ShowHelp();
            return 0;
        }

        var command = args[0].ToLowerInvariant();

            return command switch
        {
            "--version" or "-v" => ShowVersion(),
            "--help" or "-h" or "help" => ShowHelp(),
            "init" => await HandleInitAsync(args[1..]),
            "build" => await HandleBuildAsync(args[1..]),
            "run" => HandleRun(),
            "test" => HandleTest(),
            "install" => await HandleInstallAsync(),
            "add" => await HandleAddAsync(args[1..]),
            "update" => await HandleUpdateAsync(),
            "lint" => await HandleLintAsync(),
            "publish" => HandlePublish(),
            _ => ShowUnknownCommand(command)
        };
    }

    private int ShowVersion()
    {
        Console.WriteLine($"Meow version {Version}");
        Console.WriteLine("Build orchestration tool for multiple compilers (MASM, SharpIR, Uhigh, ...)");
        return 0;
    }

    private int ShowHelp()
    {
        Console.WriteLine(@"
  __  __                    
 |  \/  | ___  _____      __
 | |\/| |/ _ \/ _ \ \ /\ / /
 | |  | |  __/ (_) \ V  V / 
 |_|  |_|\___|\___/ \_/\_/  

Meow - Build orchestration tool for multiple compilers

Supports projects targeting MASM and other compilers (SharpIR, Uhigh, ObjectFortran, ObjectivePascal)

USAGE:
    meow [COMMAND] [OPTIONS]

COMMANDS:
    init        Initialize a new project (compiler selectable)
    build       Build the project
    build --debug   Build and debug the project (if supported by compiler)
    run         Run the project
    test        Run project tests
    install     Install dependencies from PurrNet
    update      Update dependencies
    publish     Publish package to PurrNet
    help        Show this help message
    --version   Show version information

PROJECT SETUP:
    meow init [name]        Create a new project (defaults to MASM but can be changed in meow.yaml)
    meow --version          Display version information
    meow --help             Display this help

EXAMPLES:
    meow init my-project    Create a new project called 'my-project'
    meow init               Create a project in the current directory
    meow build              Build the current project
    meow build --debug      Build and debug the current project (if available)
    meow run                Run the current project

For more information, visit: https://github.com/Fy-nite/Meow
");
        return 0;
    }

    private async Task<int> HandleInitAsync(string[] args)
    {

        string projectName;
        string projectPath;

        if (args.Length > 0)
        {
            projectName = args[0];
            projectPath = Path.Combine(Directory.GetCurrentDirectory(), projectName);
        }
        else
        {
            projectPath = Directory.GetCurrentDirectory();
            projectName = Path.GetFileName(projectPath);
        }

        Console.WriteLine($"Initializing new project: {projectName}");

        if (_projectService.IsExistingProject(projectPath))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Warning: Directory already contains a meow.yaml file");
            Console.ResetColor();
            return 1;
        }

        var author = Environment.GetEnvironmentVariable("USER") ?? 
                    Environment.GetEnvironmentVariable("USERNAME");

        // allow specifying compiler as second arg: meow init name [compiler]
        string compiler = "masm";
        if (args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]))
        {
            compiler = args[1].ToLowerInvariant();
        }

        // Invoke an initialization method on the project service via reflection so different implementations are supported.
        var svcType = _projectService.GetType();
        var initMethod = svcType.GetMethod("InitializeProjectAsync")
                      ?? svcType.GetMethod("InitializeAsync")
                      ?? svcType.GetMethod("CreateProjectAsync");

        if (initMethod == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: the project service does not expose an initialization method.");
            Console.ResetColor();
            return 1;
        }

        bool success;
        var initResult = initMethod.Invoke(_projectService, new object[] { projectName, projectPath, compiler, author });
        if (initResult is Task<bool> initTask)
        {
            success = await initTask;
        }
        else if (initResult is bool syncResult)
        {
            success = syncResult;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: initialization method returned an unexpected result.");
            Console.ResetColor();
            return 1;
        }

        if (success)
        {
            // Create language-specific main template if the selected compiler/runner supports it
            try
            {
                var configPathAfter = Path.Combine(projectPath, "meow.yaml");
                if (_configService.ConfigExists(configPathAfter))
                {
                    var cfg = await _configService.LoadConfigAsync(configPathAfter);
                    var mainRel = cfg.Main ?? Path.Combine("src", "main");
                    // Ask BuildService to create a proper main template for the configured compiler
                    await _buildService.CreateMainTemplateAsync(projectPath, cfg.Build.Compiler ?? compiler, mainRel);
                }
            }
            catch
            {
                // ignore template generation errors during init
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Project '{projectName}' initialized successfully!");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Created:");
            Console.WriteLine($"  - meow.yaml");
            Console.WriteLine($"  - src/main.{compiler}");
            Console.WriteLine($"  - README.md");
            Console.WriteLine($"  - .gitignore");
            Console.WriteLine();
            Console.WriteLine("Next steps:");
            Console.WriteLine($"  cd {projectName}");
            Console.WriteLine($"  meow build");
            Console.WriteLine($"  meow run");
            return 0;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Failed to initialize project");
            Console.ResetColor();
            return 1;
        }
    }

    private async Task<int> HandleBuildAsync(string[] args)
    {
        // use injected services

        // Parse arguments
    bool clean = false;
    bool debug = false;
    string? mode = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--clean":
                    clean = true;
                    break;
                case "--debug":
                    debug = true;
                    break;
                case "--mode":
                    if (i + 1 < args.Length)
                    {
                        mode = args[i + 1];
                        i++;
                    }
                    break;
            }
        }

        var projectPath = Directory.GetCurrentDirectory();
        var configPath = Path.Combine(projectPath, "meow.yaml");

        if (!_configService.ConfigExists(configPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: No meow.yaml found in current directory.");
            Console.ResetColor();
            Console.WriteLine("Run 'meow init' to create a new project.");
            return 2;
        }

        // Override mode if specified
        if (!string.IsNullOrEmpty(mode))
        {
            var config = await _configService.LoadConfigAsync(configPath);
            config.Build.Mode = mode;
            await _configService.SaveConfigAsync(config, configPath);
        }

        Console.WriteLine("Building project...");
        Console.WriteLine();

        var success = await _buildService.BuildProjectAsync(projectPath, clean);

        if (success)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Build completed successfully!");
            Console.ResetColor();
            // If debug flag, run debugger on output file
            if (debug)
            {
                // Try to get output file path from config
                var config = await _configService.LoadConfigAsync(configPath);
                // Determine output file based on compiler
                var outputExt = config.Build.Compiler == "masm" ? ".masi" : "";
                var outputFile = Path.Combine(projectPath, config.Build.Output, $"{config.Name}{outputExt}");
                if (File.Exists(outputFile))
                {
                    Console.WriteLine($"\nStarting debugger for {outputFile} ...\n");
                    // For MASM we have a helper in BuildService; for other compilers use compiler.DebugAsync via BuildService if exposed
                    var debugSuccess = await _buildService.DebugExecutableAsync(config.Build.Compiler, outputFile);
                    if (!debugSuccess)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("✗ Debug session failed.");
                        Console.ResetColor();
                        return 4;
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: Output file not found for debugging: {outputFile}");
                    Console.ResetColor();
                    return 5;
                }
            }
            return 0;
        }
        else
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("✗ Build failed.");
            Console.ResetColor();
            return 3;
        }
    }

    private int HandleRun()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Run command is not yet implemented");
        Console.ResetColor();
        Console.WriteLine("This will execute projects using the configured compiler's Run functionality");
        return 1;
    }

    private int HandleTest()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Test command is not yet implemented");
        Console.ResetColor();
        return 1;
    }

    private async Task<int> HandleInstallAsync()
    {
        var projectPath = Directory.GetCurrentDirectory();
        Console.WriteLine("Installing dependencies from PurrNet...");
        var ok = await _installService.InstallAsync(projectPath);
        if (ok)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Install completed");
            Console.ResetColor();
            return 0;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("✗ Install failed");
            Console.ResetColor();
            return 1;
        }
    }

    private async Task<int> HandleUpdateAsync()
    {
        var projectPath = Directory.GetCurrentDirectory();
        Console.WriteLine("Updating dependencies from PurrNet...");
        var ok = await _installService.UpdateAsync(projectPath);
        if (ok)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Update completed");
            Console.ResetColor();
            return 0;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("✗ Update failed");
            Console.ResetColor();
            return 1;
        }
    }

    private async Task<int> HandleAddAsync(string[] args)
    {
        var projectPath = Directory.GetCurrentDirectory();

        if (args.Length == 0)
        {
            // alias to install all
            return await HandleInstallAsync();
        }

        // Install specific package: meow add name@version
        var pkgSpec = args[0];
        Console.WriteLine($"Installing package: {pkgSpec}");
        var ok = await _installService.InstallPackageAsync(projectPath, pkgSpec);
        if (ok)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Added {pkgSpec}");
            Console.ResetColor();
            return 0;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ Failed to add {pkgSpec}");
            Console.ResetColor();
            return 1;
        }
    }

    private async Task<int> HandleLintAsync()
    {
        var projectPath = Directory.GetCurrentDirectory();
        var configPath = Path.Combine(projectPath, "meow.yaml");
        if (!_configService.ConfigExists(configPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: No meow.yaml found in current directory.");
            Console.ResetColor();
            return 2;
        }
        var config = await _configService.LoadConfigAsync(configPath);
        var compiler = _buildService.CreateCompiler(config.Build.Compiler);
        if (compiler == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Unknown compiler: {config.Build.Compiler}");
            Console.ResetColor();
            return 3;
        }

        var supported = new HashSet<string>(compiler.SupportedDependencyCategories.Select(s => s.ToLowerInvariant()));
        var hasProblems = false;

        foreach (var dep in config.Dependencies.Keys)
        {
            if (config.DependencyCategories != null && config.DependencyCategories.TryGetValue(dep, out var cat) && !string.IsNullOrWhiteSpace(cat))
            {
                if (!supported.Contains(cat.ToLowerInvariant()))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[lint] Unsupported category: dependency '{dep}' categorized as '{cat}', but compiler '{compiler.Name}' supports: {string.Join(", ", supported)}");
                    Console.ResetColor();
                    hasProblems = true;
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[lint] Missing category: dependency '{dep}' has no category in 'dependencyCategories'.");
                Console.ResetColor();
                hasProblems = true;
            }
        }

        if (!hasProblems)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Lint passed — all dependencies match compiler categories");
            Console.ResetColor();
            return 0;
        }

        return 1;
    }

    private int HandlePublish()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Publish command is not yet implemented");
        Console.ResetColor();
        Console.WriteLine("This will publish packages to PurrNet");
        return 1;
    }

    private int ShowUnknownCommand(string command)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Unknown command: {command}");
        Console.ResetColor();
        Console.WriteLine("Run 'meow --help' for usage information");
        return 1;
    }
}
