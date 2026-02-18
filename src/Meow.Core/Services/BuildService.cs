using Meow.Core.Models;
using Meow.Core.Compilers;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading;
using System.Diagnostics;

namespace Meow.Core.Services;

/// <summary>
/// Implementation of build service (compiler-agnostic)
/// </summary>
public class BuildService : IBuildService
{
    private readonly IConfigService _configService;
    
    /// <summary>
    /// Create a new BuildService using the provided config service.
    /// </summary>
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
                // If the executable file exists on disk, run it directly without redirecting
                // to avoid buffering and Ctrl+C behavior that can interfere with child process lifetime.
                try
                {
                    if (File.Exists(executable))
                    {
                        var ext = Path.GetExtension(executable).ToLowerInvariant();
                        // Skip passthrough for managed/jvm artifacts which require a host (dotnet/jar)
                        if (ext != ".dll" && ext != ".jar")
                        {
                            var psi = new ProcessStartInfo(executable)
                            {
                                UseShellExecute = true,
                                CreateNoWindow = false,
                                WorkingDirectory = Path.GetDirectoryName(executable) ?? Environment.CurrentDirectory
                            };
                            using var p = Process.Start(psi);
                            if (p == null) return false;
                            p.WaitForExit();
                            return p.ExitCode == 0;
                        }
                    }
                }
                catch
                {
                    // fallback to compiler-specific runner if passthrough fails
                }

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
    public async Task<bool> BuildProjectAsync(string projectPath, bool clean = false, string? testMainRelative = null, bool? forceLink = null, IEnumerable<string>? extraArgs = null)
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

            // If caller requested forcing link/unlink for this build, override in-memory only.
            if (forceLink.HasValue)
            {
                config.Build.Link = forceLink.Value;
            }

            // If caller provided extra args for this build (e.g., test-specific args), append them in-memory.
            if (extraArgs != null)
            {
                if (config.Build.ExtraArgs == null)
                    config.Build.ExtraArgs = new List<string>();

                foreach (var a in extraArgs)
                {
                    if (!string.IsNullOrWhiteSpace(a))
                        config.Build.ExtraArgs.Add(a);
                }
            }

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

            // Get source files using the selected compiler. If testMainRelative is provided,
            // adjust selection to include the test entry and exclude the normal main when wildcarding.
            var sourceFiles = GetSourceFiles(projectPath, config, compiler, runner, testMainRelative);

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

            // Ensure config.Build is not null
            if (config.Build == null)
            {
                Console.WriteLine("Build configuration is missing in config.");
                return false;
            }

            // Determine which sources actually need assembling (respect incremental builds)
            var toAssemble = new List<(string SourceFile, string ExpectedObj, string SourceFull)>();
            foreach (var sourceFile in sourceFiles)
            {
                string expectedObj = compiler!.GetObjectPath(projectPath, sourceFile, objDir, config.Build);
                var sourceFull = Path.Combine(projectPath, sourceFile);
                if (config.Build.Incremental && File.Exists(expectedObj))
                {
                    var objTime = File.GetLastWriteTimeUtc(expectedObj);
                    var srcTime = File.GetLastWriteTimeUtc(sourceFull);
                    if (objTime >= srcTime)
                    {
                        Console.WriteLine($"Skipping unchanged: {sourceFile}");
                        objectFiles.Add(Path.GetRelativePath(projectPath, expectedObj).Replace("\\", "/"));
                        continue;
                    }
                }

                toAssemble.Add((sourceFile, expectedObj, sourceFull));
            }

            // Progress reporter
            var reporter = new ConsoleProgressReporter();
            int total = toAssemble.Count;
            int done = 0;

            if (toAssemble.Count > 0)
            {
                var jobs = config.Build.Jobs > 0 ? config.Build.Jobs : 1;

                if (jobs <= 1)
                {
                    // Sequential assemble (legacy behavior)
                    foreach (var t in toAssemble)
                    {
                        double percent = total > 0 ? (done * 100.0 / total) : 100.0;
                        reporter.Report(t.SourceFile, percent);
                        var objFile = await compiler!.AssembleAsync(projectPath, t.SourceFile, objDir, config.Build, reporter);
                        done++;
                        if (objFile != null)
                        {
                            objectFiles.Add(objFile);
                        }
                        else
                        {
                            Console.WriteLine($"Failed to assemble: {t.SourceFile}");
                            return false;
                        }
                    }
                }
                else
                {
                    // Parallel assemble using SemaphoreSlim to limit concurrency
                    var bag = new ConcurrentBag<string>();
                    var errors = new ConcurrentBag<string>();
                    var semaphore = new SemaphoreSlim(jobs);
                    var tasks = toAssemble.Select(async t =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            double percent = total > 0 ? (Interlocked.Increment(ref done) * 100.0 / total) : 100.0;
                            reporter.Report(t.SourceFile, percent);
                            var objFile = await compiler!.AssembleAsync(projectPath, t.SourceFile, objDir, config.Build, reporter);
                            if (objFile == null)
                            {
                                errors.Add($"Failed to assemble: {t.SourceFile}");
                            }
                            else
                            {
                                bag.Add(objFile);
                            }
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Exception in assembling {t.SourceFile}: {ex.Message}");
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }).ToArray();

                    await Task.WhenAll(tasks);

                    // Add assembled object files (order may be different)
                    foreach (var f in bag)
                        objectFiles.Add(f);
                    if (!errors.IsEmpty)
                    {
                        foreach (var err in errors)
                            Console.WriteLine(err);
                        return false;
                    }
                }
            }

            Console.WriteLine($"\nAssembled {objectFiles.Count} object file(s):");
            foreach (var objFile in objectFiles)
            {
                Console.WriteLine($"  - {objFile}");
            }

            // Link if enabled
            if (config.Build != null && config.Build.Link && objectFiles.Count > 0)
            {
                var outputExt = compiler!.Name == "masm" ? ".masi" : "";
                var outputFile = Path.Combine(projectPath, config.Build.Output,
                    $"{config.Name}{outputExt}");

                var (linkOk, linkErr) = await compiler.LinkAsync(objectFiles, outputFile, config.Build);
                if (linkOk)
                {
                    Console.WriteLine($"\nLinked output: {Path.GetFileName(outputFile)}");
                }
                else
                {
                    if (!string.IsNullOrEmpty(linkErr))
                        Console.WriteLine(linkErr);
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
        return GetSourceFiles(projectPath, config, compiler, runner, null);
    }

    private List<string> GetSourceFiles(string projectPath, MeowConfig config, ICompiler? compiler, IRunner? runner, string? testMainRelative)
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

        // If a test entrypoint was provided, include it and if wildcarding, exclude the normal main file.
        if (!string.IsNullOrEmpty(testMainRelative))
        {
            var testFull = Path.Combine(projectPath, testMainRelative);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(testFull))
            {
                var norm = testMainRelative.Replace("\\", "/");
                sourceFiles.Add(norm);
                seen.Add(norm);
            }

            if (config.Build.Wildcard)
            {
                // gather src files but exclude configured Main (so test can link against other sources)
                foreach (var ext in extensions)
                {
                    var pattern = "*" + ext;
                    var files = Directory.GetFiles(srcDir, pattern, SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        var rel = Path.GetRelativePath(projectPath, file).Replace("\\", "/");
                        // skip the project's main entrypoint so tests don't compile the main program
                        if (!string.Equals(rel, config.Main.Replace("\\", "/"), StringComparison.OrdinalIgnoreCase) && seen.Add(rel))
                        {
                            sourceFiles.Add(rel);
                        }
                    }
                }

                // Also include files from tests/ directory matching supported extensions
                var testsDir = Path.Combine(projectPath, "tests");
                if (Directory.Exists(testsDir))
                {
                    foreach (var ext in extensions)
                    {
                        var pattern = "*" + ext;
                        var testFiles = Directory.GetFiles(testsDir, pattern, SearchOption.AllDirectories);
                        foreach (var file in testFiles)
                        {
                            var rel = Path.GetRelativePath(projectPath, file).Replace("\\", "/");
                            if (seen.Add(rel))
                            {
                                sourceFiles.Add(rel);
                            }
                        }
                    }
                }
            }
            else
            {
                // not wildcarding: only test entry is used (we've already added it)
            }

            return sourceFiles;
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

    /// <summary>
    /// Instantiate a compiler implementation by name.
    /// </summary>
    public ICompiler? CreateCompiler(string compilerName)
    {
        // Dynamic discovery: scan loaded types implementing ICompiler and match by Name.
        EnsureCompilerTypesLoaded();
        if (string.IsNullOrWhiteSpace(compilerName))
            compilerName = "masm";

        if (_compilerTypes.TryGetValue(compilerName.ToLowerInvariant(), out var ctor))
        {
            try
            {
                return ctor() as ICompiler;
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Instantiate a runner implementation by name.
    /// </summary>
    public IRunner? CreateRunner(string runnerName)
    {
        EnsureRunnerTypesLoaded();
        if (string.IsNullOrWhiteSpace(runnerName))
            return null;

        if (_runnerTypes.TryGetValue(runnerName.ToLowerInvariant(), out var ctor))
        {
            try
            {
                return ctor() as IRunner;
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    // Caches populated via reflection
    private static readonly Dictionary<string, Func<object?>> _compilerTypes = new();
    private static readonly Dictionary<string, Func<object?>> _runnerTypes = new();

    private static void EnsureCompilerTypesLoaded()
    {
        if (_compilerTypes.Count > 0) return;
        var asm = typeof(BuildService).Assembly;
        var compilerInterface = typeof(ICompiler);
        foreach (var t in asm.GetTypes())
        {
            if (!compilerInterface.IsAssignableFrom(t) || t.IsInterface || t.IsAbstract) continue;
            try
            {
                // Attempt to get Name property via a temporary instance if available
                var inst = Activator.CreateInstance(t) as ICompiler;
                var name = inst?.Name ?? t.Name.ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(name)) continue;

                // store a constructor delegate
                _compilerTypes[name.ToLowerInvariant()] = () => Activator.CreateInstance(t)!;
            }
            catch
            {
                // ignore types we can't construct
            }
        }
    }

    private static void EnsureRunnerTypesLoaded()
    {
        if (_runnerTypes.Count > 0) return;
        var asm = typeof(BuildService).Assembly;
        var runnerInterface = typeof(IRunner);
        foreach (var t in asm.GetTypes())
        {
            if (!runnerInterface.IsAssignableFrom(t) || t.IsInterface || t.IsAbstract) continue;
            try
            {
                var inst = Activator.CreateInstance(t) as IRunner;
                var name = inst?.Name ?? t.Name.ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(name)) continue;
                _runnerTypes[name.ToLowerInvariant()] = () => Activator.CreateInstance(t)!;
            }
            catch
            {
                // ignore
            }
        }
    }

    /// <summary>
    /// Create a language-specific main template file for a project using the named compiler/runner.
    /// </summary>
    public async Task<bool> CreateMainTemplateAsync(string projectPath, string compilerOrRunnerName, string? mainRelativePath = null)
    {
        try
        {
            var configPath = Path.Combine(projectPath, "meow.yaml");
            MeowConfig? config = null;
            if (_configService.ConfigExists(configPath))
                config = await _configService.LoadConfigAsync(configPath);

            var mainPath = mainRelativePath ?? config?.Main ?? Path.Combine("src", "main");

            // Create compiler or runner
            var compiler = CreateCompiler(compilerOrRunnerName);
            var runner = compiler == null ? CreateRunner(compilerOrRunnerName) : null;

            // Prefer template provider on compiler, then runner
            if (compiler is ITemplateProvider cp)
            {
                return await cp.CreateMainAsync(projectPath, mainPath);
            }
            if (runner is ITemplateProvider rp)
            {
                return await rp.CreateMainAsync(projectPath, mainPath);
            }

            Console.WriteLine($"Compiler/runner '{compilerOrRunnerName}' does not support main-template generation.");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to create main template: {ex.Message}");
            return false;
        }
    }
}
