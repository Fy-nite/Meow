using Meow.Core.Services;

namespace Meow.CLI.Commands;

/// <summary>
/// Main command handler that routes CLI commands
/// </summary>
public class CommandHandler
{
    private const string Version = "0.1.0";
    
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
            "install" => HandleInstall(),
            "update" => HandleUpdate(),
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
        var configService = new ConfigService();
        var projectService = new ProjectService(configService);

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

        if (projectService.IsExistingProject(projectPath))
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

        var success = await projectService.InitializeProjectAsync(projectName, projectPath, compiler, author);

        if (success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Project '{projectName}' initialized successfully!");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Created:");
            Console.WriteLine($"  - meow.yaml");
            Console.WriteLine($"  - src/main.masm");
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
        var configService = new ConfigService();
        var buildService = new BuildService(configService);

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

        if (!configService.ConfigExists(configPath))
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
            var config = await configService.LoadConfigAsync(configPath);
            config.Build.Mode = mode;
            await configService.SaveConfigAsync(config, configPath);
        }

        Console.WriteLine("Building project...");
        Console.WriteLine();

        var success = await buildService.BuildProjectAsync(projectPath, clean);

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
                var config = await configService.LoadConfigAsync(configPath);
                // Determine output file based on compiler
                var outputExt = config.Build.Compiler == "masm" ? ".masi" : "";
                var outputFile = Path.Combine(projectPath, config.Build.Output, $"{config.Name}{outputExt}");
                if (File.Exists(outputFile))
                {
                    Console.WriteLine($"\nStarting debugger for {outputFile} ...\n");
                    // For MASM we have a helper in BuildService; for other compilers use compiler.DebugAsync via BuildService if exposed
                    var debugSuccess = await buildService.DebugExecutableAsync(config.Build.Compiler, outputFile);
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
        Console.WriteLine("Run command is not yet implemented (Phase 2)");
        Console.ResetColor();
        Console.WriteLine("This will execute projects using the configured compiler's Run functionality");
        return 1;
    }

    private int HandleTest()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Test command is not yet implemented (Phase 4)");
        Console.ResetColor();
        return 1;
    }

    private int HandleInstall()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Install command is not yet implemented (Phase 3)");
        Console.ResetColor();
        Console.WriteLine("This will install dependencies from PurrNet");
        return 1;
    }

    private int HandleUpdate()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Update command is not yet implemented (Phase 3)");
        Console.ResetColor();
        Console.WriteLine("This will update dependencies from PurrNet");
        return 1;
    }

    private int HandlePublish()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Publish command is not yet implemented (Phase 3)");
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
