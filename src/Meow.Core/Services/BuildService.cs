using Meow.Core.Models;
using Meow.Core.Compilers;
using System.Linq;

namespace Meow.Core.Services;

/// <summary>
/// Implementation of build service (compiler-agnostic)
/// </summary>
public class BuildService : IBuildService
{
    private readonly IConfigService _configService;
    
    public BuildService(IConfigService configService)
    {
        _configService = configService;
    }

    /// <summary>
    /// Debug an executable using the specified compiler implementation.
    /// This will create the compiler instance and call its DebugAsync method.
    /// </summary>
    public async Task<bool> DebugExecutableAsync(string compilerName, string executable, string? stdinFile = null)
    {
        try
        {
            var compiler = CreateCompiler(compilerName);
            if (compiler != null)
            {
                return await compiler.DebugAsync(executable, stdinFile);
            }

            var runner = CreateRunner(compilerName);
            if (runner != null)
            {
                return await runner.DebugAsync(executable, stdinFile);
            }

            Console.WriteLine($"Unknown compiler/runner: {compilerName}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error debugging {executable}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Run an executable using the specified compiler implementation.
    /// </summary>
    public async Task<bool> RunExecutableAsync(string compilerName, string executable, string? stdinFile = null)
    {
        try
        {
            var compiler = CreateCompiler(compilerName);
            if (compiler != null)
            {
                return await compiler.RunAsync(executable, stdinFile);
            }

            var runner = CreateRunner(compilerName);
            if (runner != null)
            {
                return await runner.RunAsync(executable, stdinFile);
            }

            Console.WriteLine($"Unknown compiler/runner: {compilerName}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error running {executable}: {ex.Message}");
            return false;
        }
    }
    

    /// <inheritdoc />
    public async Task<bool> BuildProjectAsync(string projectPath, bool clean = false)
    {
        try
        {
            // Load configuration
            var configPath = Path.Combine(projectPath, "meow.yaml");
            if (!_configService.ConfigExists(configPath))
            {
                throw new FileNotFoundException($"Configuration file not found: {configPath}");
            }

            var config = await _configService.LoadConfigAsync(configPath);

            // Clean if requested
            if (clean)
            {
                CleanBuildDirectory(projectPath, config.Build);
            }

            // Create output directories
            var buildDir = Path.Combine(projectPath, config.Build.Output);
            var objDir = Path.Combine(projectPath, config.Build.Objdir);
            Directory.CreateDirectory(buildDir);
            Directory.CreateDirectory(objDir);

            // Create compiler or runner based on config
            var compiler = CreateCompiler(config.Build.Compiler);
            var runner = compiler == null ? CreateRunner(config.Build.Compiler) : null;
            if (compiler == null && runner == null)
            {
                Console.WriteLine($"Unknown compiler/runner: {config.Build.Compiler}");
                return false;
            }

            // Check dependency categories against compiler supported categories and warn if unsupported
            if (compiler != null)
            {
                var supportedCats = new HashSet<string>(compiler.SupportedDependencyCategories.Select(s => s.ToLowerInvariant()));

                foreach (var dep in config.Dependencies.Keys)
                {
                    if (config.DependencyCategories != null && config.DependencyCategories.TryGetValue(dep, out var cat) && !string.IsNullOrWhiteSpace(cat))
                    {
                        var catLower = cat.ToLowerInvariant();
                        if (!supportedCats.Contains(catLower))
                        {
                            Console.WriteLine($"Warning: compiler '{compiler.Name}' does not declare support for dependency category '{cat}' required by '{dep}'.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Warning: dependency '{dep}' has no category specified in 'DependencyCategories'.");
                    }
                }

                // DevDependencies - warn but indicate dev-only
                foreach (var dep in config.DevDependencies.Keys)
                {
                    if (config.DependencyCategories != null && config.DependencyCategories.TryGetValue(dep, out var cat) && !string.IsNullOrWhiteSpace(cat))
                    {
                        var catLower = cat.ToLowerInvariant();
                        if (!supportedCats.Contains(catLower))
                        {
                            Console.WriteLine($"Warning: compiler '{compiler.Name}' does not declare support for dev dependency category '{cat}' required by '{dep}' (dev dependency). This may be okay for build-only compilers.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Warning: dev dependency '{dep}' has no category specified in 'DependencyCategories'.");
                    }
                }
            }
            else
            {
                // Runner-based projects currently do not declare dependency categories in the same way; skip category checks.
            }

            // Get source files using the selected compiler
            var sourceFiles = GetSourceFiles(projectPath, config, compiler, runner);

            if (sourceFiles.Count == 0)
            {
                Console.WriteLine("No source files found to build.");
                return false;
            }

            Console.WriteLine($"Found {sourceFiles.Count} source file(s) to build:");
            foreach (var file in sourceFiles)
            {
                Console.WriteLine($"  - {file}");
            }

            // If this is a runner-based project, we do not assemble/link; simply validate main/source presence
            if (runner != null)
            {
                Console.WriteLine($"Runner-based project using '{runner.Name}'. Found {sourceFiles.Count} source file(s).");
                foreach (var file in sourceFiles)
                {
                    Console.WriteLine($"  - {file}");
                }

                // For now, building an interpreted project is a no-op beyond validation.
                // Future: support packaging, dependency install, or bytecode caching.
                return true;
            }

            // Assemble each source file to object file using the selected compiler
            var objectFiles = new List<string>();
            foreach (var sourceFile in sourceFiles)
            {
                var objFile = await compiler!.AssembleAsync(projectPath, sourceFile, objDir, config.Build);
                if (objFile != null)
                {
                    objectFiles.Add(objFile);
                }
                else
                {
                    Console.WriteLine($"Failed to assemble: {sourceFile}");
                    return false;
                }
            }

            Console.WriteLine($"\nAssembled {objectFiles.Count} object file(s):");
            foreach (var objFile in objectFiles)
            {
                Console.WriteLine($"  - {objFile}");
            }

            // Link if enabled
            if (config.Build.Link && objectFiles.Count > 0)
            {
                var outputExt = compiler!.Name == "masm" ? ".masi" : "";
                var outputFile = Path.Combine(projectPath, config.Build.Output,
                    $"{config.Name}{outputExt}");

                if (await compiler.LinkAsync(objectFiles, outputFile, config.Build))
                {
                    Console.WriteLine($"\nLinked output: {Path.GetFileName(outputFile)}");
                }
                else
                {
                    Console.WriteLine("\nFailed to link object files.");
                    return false;
                }
            }
            else if (!config.Build.Link)
            {
                Console.WriteLine("\nLinking disabled - object files only.");
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Build failed: {ex.Message}");
            return false;
        }
    }

    private List<string> GetSourceFiles(string projectPath, MeowConfig config, ICompiler? compiler, IRunner? runner)
    {
        var sourceFiles = new List<string>();
        var srcDir = Path.Combine(projectPath, "src");

        if (!Directory.Exists(srcDir))
        {
            Console.WriteLine($"Source directory not found: {srcDir}");
            return sourceFiles;
        }

        IEnumerable<string> extensions;
        if (compiler != null)
        {
            extensions = compiler.SourceExtensions;
        }
        else if (runner != null)
        {
            extensions = runner.SourceExtensions;
        }
        else
        {
            extensions = new[] { ".masm" };
        }

        if (config.Build.Wildcard)
        {
            // Get all matching files recursively for supported extensions
            foreach (var ext in extensions)
            {
                var pattern = "*" + ext;
                var files = Directory.GetFiles(srcDir, pattern, SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    sourceFiles.Add(Path.GetRelativePath(projectPath, file));
                }
            }
        }
        else
        {
            // Use main file only
            var mainFile = Path.Combine(projectPath, config.Main);
            if (File.Exists(mainFile))
            {
                sourceFiles.Add(config.Main);
            }
        }

        return sourceFiles;
    }

    // MASM-specific assembly/linking logic moved to MasmCompiler
    /// <summary>
    /// Runs a MASI executable using masm --run
    /// </summary>
    /// <param name="masiFile">Path to the .masi file</param>
    /// <param name="stdinFile">Optional path to stdin file</param>
    /// <returns>True if run succeeded, false otherwise</returns>
    public bool RunMasiFile(string masiFile, string? stdinFile = null)
    {
        try
        {
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "masm";
            var args = $"\"{masiFile}\" --run";
            if (!string.IsNullOrEmpty(stdinFile))
            {
                args += $" --stdin-from \"{stdinFile}\"";
            }
            process.StartInfo.Arguments = args;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Console.WriteLine(output);
            if (process.ExitCode != 0)
            {
                Console.WriteLine($"masm run error: {error}");
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error running {masiFile}: {ex.Message}");
            return false;
        }
    }

    private void CleanBuildDirectory(string projectPath, BuildConfig buildConfig)
    {
        var buildDir = Path.Combine(projectPath, buildConfig.Output);
        
        if (Directory.Exists(buildDir))
        {
            Console.WriteLine($"Cleaning build directory: {buildConfig.Output}");
            Directory.Delete(buildDir, true);
        }
    }

    public ICompiler? CreateCompiler(string compilerName)
    {
        if (string.IsNullOrWhiteSpace(compilerName))
            return new MasmCompiler();

            return compilerName.ToLowerInvariant() switch
        {
            "masm" => new MasmCompiler(),
            "sharpir" => new SharpIrCompiler(),
            "uhigh" => new UhighCompiler(),
            "objectfortran" => new ObjectFortranCompiler(),
            "c" => new CCompiler(),
            "cpp" => new CppCompiler(),
            "fortran" => new FortranCompiler(),
            "go" => new GoCompiler(),
            "rust" => new RustCompiler(),
            "csharp" => new CSharpCompiler(),
            "objectivepascal" => new ObjectivePascalCompiler(),
            "fusion" => new FusionCompiler(),
            // future compilers: "nasm" => new NasmCompiler(),
            _ => null
        };
    }

    public IRunner? CreateRunner(string runnerName)
    {
        if (string.IsNullOrWhiteSpace(runnerName))
            return null;

        return runnerName.ToLowerInvariant() switch
        {
            "python" => new Meow.Core.Compilers.PythonRunner(),
            "node" => new Meow.Core.Compilers.NodeRunner(),
            // future runners: "node" => new NodeRunner(),
            _ => null
        };
    }
}
