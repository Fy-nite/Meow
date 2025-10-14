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
        Console.WriteLine("Build orchestration tool for MASM projects");
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

Meow - Build orchestration tool for MASM projects

USAGE:
    meow [COMMAND] [OPTIONS]

COMMANDS:
    init        Initialize a new MASM project
    build       Build the project
    run         Run the project
    test        Run project tests
    install     Install dependencies from PurrNet
    update      Update dependencies
    publish     Publish package to PurrNet
    help        Show this help message
    --version   Show version information

PHASE 1 - PROJECT SETUP (CURRENT):
    meow init [name]        Create a new MASM project
    meow --version          Display version information
    meow --help             Display this help

EXAMPLES:
    meow init my-project    Create a new project called 'my-project'
    meow init               Create a project in the current directory
    meow build              Build the current project
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

        Console.WriteLine($"Initializing new MASM project: {projectName}");

        if (projectService.IsExistingProject(projectPath))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Warning: Directory already contains a meow.yaml file");
            Console.ResetColor();
            return 1;
        }

        var author = Environment.GetEnvironmentVariable("USER") ?? 
                    Environment.GetEnvironmentVariable("USERNAME");

        var success = await projectService.InitializeMasmProjectAsync(projectName, projectPath, author);

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
        string? mode = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--clean":
                    clean = true;
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

        Console.WriteLine("Building MASM project...");
        Console.WriteLine();

        var success = await buildService.BuildProjectAsync(projectPath, clean);

        if (success)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Build completed successfully!");
            Console.ResetColor();
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
        Console.WriteLine("This will execute MASM projects using the Rust MASM interpreter");
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
