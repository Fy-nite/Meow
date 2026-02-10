using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Meow.Core.Models;
using Meow.Core.Services;

namespace Meow.Core.Compilers;

public class CSharpCompiler : ICompiler
{
    public string Name => "csharp";

    public IEnumerable<string> SourceExtensions => new[] { ".cs" };

    public IEnumerable<string> SupportedDependencyCategories => new[] { "runtime", "nuget" };

    public async Task<string?> AssembleAsync(string projectPath, string sourcePath, string objDir, BuildConfig buildConfig)
    {
        try
        {
            // If project contains a .csproj, prefer dotnet build
            var csproj = Directory.GetFiles(projectPath, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if (!string.IsNullOrEmpty(csproj))
            {
                var psi = new ProcessStartInfo("dotnet", $"build \"{csproj}\" -c {buildConfig.Mode}")
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
                    Console.WriteLine($"dotnet build error: {stderr}");
                    return null;
                }
                // Find built assembly in bin/ directory
                var binDir = Path.Combine(projectPath, "bin", buildConfig.Mode);
                var dll = Directory.GetFiles(projectPath, "*.dll", SearchOption.AllDirectories)
                    .FirstOrDefault(f => f.Contains(Path.Combine("bin", buildConfig.Mode)));
                if (dll != null)
                {
                    return Path.GetRelativePath(projectPath, dll);
                }
                return null;
            }

            // Fallback: try csc on the single file
            var absSource = Path.Combine(projectPath, sourcePath);
            var outName = Path.GetFileNameWithoutExtension(sourcePath) + ".exe";
            var outPath = Path.Combine(objDir, outName);
            var psi2 = new ProcessStartInfo("csc", $"-out:\"{outPath}\" \"{absSource}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p2 = Process.Start(psi2);
            if (p2 == null) return null;
            var outS = await p2.StandardOutput.ReadToEndAsync();
            var errS = await p2.StandardError.ReadToEndAsync();
            p2.WaitForExit();
            Console.WriteLine(outS);
            if (p2.ExitCode != 0)
            {
                Console.WriteLine($"csc error: {errS}");
                return null;
            }
            return Path.GetRelativePath(projectPath, outPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error building csharp source {sourcePath}: {ex.Message}");
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
            Console.WriteLine($"Error linking csharp objects: {ex.Message}");
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
                Console.WriteLine($"csharp run error: {err}");
                return Task.FromResult(false);
            }
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error running csharp executable {executable}: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    public Task<bool> DebugAsync(string executable, string? stdinFile = null)
    {
        Console.WriteLine("C# debug not implemented; use `dotnet run` or VS Code debugger.");
        return Task.FromResult(false);
    }
}
