using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Meow.Core.Models;
using Meow.Core.Services;

namespace Meow.Core.Compilers;

public class GoCompiler : ICompiler
{
    public string Name => "go";

    public IEnumerable<string> SourceExtensions => new[] { ".go" };

    public IEnumerable<string> SupportedDependencyCategories => new[] { "runtime" };

    public async Task<string?> AssembleAsync(string projectPath, string sourcePath, string objDir, BuildConfig buildConfig)
    {
        try
        {
            // go builds from package; simplest approach: run `go build -o objDir/file.exe path/to/main.go`
            var absSource = Path.Combine(projectPath, sourcePath);
            var outName = Path.GetFileNameWithoutExtension(sourcePath) + (OperatingSystem.IsWindows() ? ".exe" : "");
            var outputPath = Path.Combine(objDir, outName);

            var psi = new ProcessStartInfo("go", $"build -o \"{outputPath}\" \"{absSource}\"")
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
                Console.WriteLine($"go build error: {stderr}");
                return null;
            }

            return Path.GetRelativePath(projectPath, outputPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error building go file {sourcePath}: {ex.Message}");
            return null;
        }
    }

    public Task<bool> LinkAsync(IEnumerable<string> objectFiles, string outputFile, BuildConfig buildConfig)
    {
        // Go produces final binaries during build; linking step not needed. Copy first object as output if present.
        try
        {
            var first = objectFiles.FirstOrDefault();
            if (first == null) return Task.FromResult(false);
            File.Copy(first, outputFile, true);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error linking go objects: {ex.Message}");
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
                Console.WriteLine($"go run error: {err}");
                return Task.FromResult(false);
            }
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error running go executable {executable}: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    public Task<bool> DebugAsync(string executable, string? stdinFile = null)
    {
        Console.WriteLine("Go debug not implemented; use Delve (dlv) externally.");
        return Task.FromResult(false);
    }
}
