using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Meow.Core.Models;
using Meow.Core.Services;

namespace Meow.Core.Compilers;

public class RustCompiler : ICompiler
{
    public string Name => "rust";

    public IEnumerable<string> SourceExtensions => new[] { ".rs" };

    public IEnumerable<string> SupportedDependencyCategories => new[] { "runtime" };

    public async Task<string?> AssembleAsync(string projectPath, string sourcePath, string objDir, BuildConfig buildConfig, IProgressReporter? reporter = null)
    {
        try
        {
            // Prefer cargo if Cargo.toml present; else use rustc
            var absSource = Path.Combine(projectPath, sourcePath);
            string? outputPath = null;

            var cargoToml = Path.Combine(projectPath, "Cargo.toml");
            if (File.Exists(cargoToml))
            {
                var extraArgs = buildConfig?.ExtraArgs != null && buildConfig.ExtraArgs.Count > 0 ? " " + string.Join(" ", buildConfig.ExtraArgs) : string.Empty;
                var jobsArg = buildConfig != null && buildConfig.Jobs > 1 ? $" -j {buildConfig.Jobs}" : string.Empty;
                var psi = new ProcessStartInfo("cargo", "build --release" + jobsArg + extraArgs)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = projectPath
                };
                using var p = Process.Start(psi);
                if (p == null) return null;
                var stdout = await p.StandardOutput.ReadToEndAsync();
                var stderr = await p.StandardError.ReadToEndAsync();
                p.WaitForExit();
                Console.WriteLine(stdout);
                if (p.ExitCode != 0)
                {
                    Console.WriteLine($"cargo build error: {stderr}");
                    return null;
                }
                // Cargo places binaries in target/release
                var exeName = Path.GetFileNameWithoutExtension(projectPath);
                var candidate = Path.Combine(projectPath, "target", "release", exeName + (OperatingSystem.IsWindows() ? ".exe" : ""));
                if (File.Exists(candidate)) outputPath = candidate;
            }
            else
            {
                var outName = Path.GetFileNameWithoutExtension(sourcePath) + (OperatingSystem.IsWindows() ? ".exe" : "");
                var outPath = Path.Combine(objDir, outName);
                var extraArgs = buildConfig?.ExtraArgs != null && buildConfig.ExtraArgs.Count > 0 ? " " + string.Join(" ", buildConfig.ExtraArgs) : string.Empty;
                var psi = new ProcessStartInfo("rustc", $"-o \"{outPath}\" \"{absSource}\"" + extraArgs)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                if (p == null) return null;
                var stdout = await p.StandardOutput.ReadToEndAsync();
                var stderr = await p.StandardError.ReadToEndAsync();
                p.WaitForExit();
                Console.WriteLine(stdout);
                if (p.ExitCode != 0)
                {
                    Console.WriteLine($"rustc error: {stderr}");
                    return null;
                }
                outputPath = outPath;
            }

            if (outputPath == null) return null;
            return Path.GetRelativePath(projectPath, outputPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error building rust source {sourcePath}: {ex.Message}");
            return null;
        }
    }

    public Task<bool> LinkAsync(IEnumerable<string> objectFiles, string outputFile, BuildConfig buildConfig)
    {
        try
        {
            var first = objectFiles.FirstOrDefault();
            if (first == null) return Task.FromResult(false);
            File.Copy(first, outputFile, true);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error linking rust objects: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    public Task<bool> RunAsync(string executable, string? stdinFile = null)
    {
        try
        {
            var psi = new ProcessStartInfo(executable)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p == null) return Task.FromResult(false);
            var outp = p.StandardOutput.ReadToEnd();
            var err = p.StandardError.ReadToEnd();
            p.WaitForExit();
            Console.WriteLine(outp);
            if (p.ExitCode != 0)
            {
                Console.WriteLine($"rust run error: {err}");
                return Task.FromResult(false);
            }
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error running rust executable {executable}: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    public Task<bool> DebugAsync(string executable, string? stdinFile = null)
    {
        Console.WriteLine("Rust debug not implemented; use lldb/gdb/delve externally.");
        return Task.FromResult(false);
    }

    // Template provider: generate a basic Rust main
    public Task<bool> CreateMainAsync(string projectPath, string mainRelativePath)
    {
        try
        {
            var fullPath = Path.Combine(projectPath, mainRelativePath);
            var dir = Path.GetDirectoryName(fullPath) ?? Path.Combine(projectPath, "src");
            Directory.CreateDirectory(dir);
            var content = "fn main() {\n    println!(\"Hello, world!\");\n}\n";
            File.WriteAllText(fullPath, content);
            Console.WriteLine($"Created Rust main template: {mainRelativePath}");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to create Rust main template: {ex.Message}");
            return Task.FromResult(false);
        }
    }
}
