using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Meow.Core.Models;

namespace Meow.Core.Services;

public class CCompiler : ICompiler
{
    public string Name => "c";
    public IEnumerable<string> SourceExtensions => new[] { ".c" };
    public IEnumerable<string> SupportedDependencyCategories => new[] { "c", "runtime" };

    public async Task<string?> AssembleAsync(string projectPath, string sourcePath, string objDir, BuildConfig buildConfig, IProgressReporter? reporter = null)
    {
        try
        {
            reporter?.StartFile(sourcePath);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var fullSourcePath = Path.Combine(projectPath, sourcePath);
            var relativePath = sourcePath;
            // Support sources coming from either src/ or tests/ when building test programs
            if (relativePath.StartsWith("src/", StringComparison.OrdinalIgnoreCase) || relativePath.StartsWith("src\\", StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath.Substring(4);
            }
            else if (relativePath.StartsWith("tests/", StringComparison.OrdinalIgnoreCase) || relativePath.StartsWith("tests\\", StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath.Substring(6);
            }
            var objectFileName = relativePath
                .Replace(Path.DirectorySeparatorChar, '_')
                .Replace(Path.AltDirectorySeparatorChar, '_')
                + ".o";
            var objectFilePath = Path.Combine(objDir, objectFileName);

            Directory.CreateDirectory(Path.GetDirectoryName(objectFilePath) ?? objDir);

            var flags = buildConfig.Mode?.ToLower() == "debug" ? "-g -O0" : "-O2 -s";
            var process = new Process();
            process.StartInfo.FileName = "gcc";
            var extraArgs = buildConfig?.ExtraArgs != null && buildConfig.ExtraArgs.Count > 0 ? " " + string.Join(" ", buildConfig.ExtraArgs) : string.Empty;
            process.StartInfo.Arguments = $"-c {flags} \"{fullSourcePath}\" -o \"{objectFilePath}\"" + extraArgs;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                Console.WriteLine($"gcc compile error: {error}");
                return null;
            }
            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine(error);
            }
            sw.Stop();
            reporter?.EndFile(sourcePath, sw.Elapsed);
            return objectFilePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"C assemble error: {ex.Message}");
            return null;
        }
    }

    public async Task<(bool Success, string? Error)> LinkAsync(IEnumerable<string> objectFiles, string outputFile, BuildConfig buildConfig)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputFile) ?? Path.GetDirectoryName(objectFiles.First()) ?? ".");
            var objArgs = string.Join(" ", objectFiles.Select(f => $"\"{f}\""));
            var process = new Process();
            process.StartInfo.FileName = "gcc";
            var extraLinkArgs = buildConfig?.ExtraArgs != null && buildConfig.ExtraArgs.Count > 0 ? " " + string.Join(" ", buildConfig.ExtraArgs) : string.Empty;
            process.StartInfo.Arguments = $"{objArgs} -o \"{outputFile}\"" + extraLinkArgs;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            Console.WriteLine(output);
            if (process.ExitCode != 0)
            {
                var errMsg = $"gcc link error: {error}";
                Console.WriteLine(errMsg);
                return (false, errMsg);
            }
            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine(error);
            }
            return (true, null);
        }
        catch (Exception ex)
        {
            var exc = $"C link error: {ex.Message}";
            Console.WriteLine(exc);
            return (false, exc);
        }
    }

    public Task<bool> RunAsync(string executable, string? stdinFile = null)
    {
        try
        {
            var process = new Process();
            process.StartInfo.FileName = executable;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            if (!string.IsNullOrEmpty(stdinFile))
            {
                process.StartInfo.RedirectStandardInput = true;
            }
            process.Start();
            if (!string.IsNullOrEmpty(stdinFile))
            {
                var input = File.ReadAllText(stdinFile);
                process.StandardInput.Write(input);
                process.StandardInput.Close();
            }
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Console.WriteLine(output);
            if (process.ExitCode != 0)
            {
                Console.WriteLine($"Run error: {error}");
                return Task.FromResult(false);
            }
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error running {executable}: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    public Task<bool> DebugAsync(string executable, string? stdinFile = null)
    {
        try
        {
            var process = new Process();
            process.StartInfo.FileName = "gdb";
            process.StartInfo.Arguments = $"-q --args \"{executable}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = false;
            process.Start();
            process.WaitForExit();
            return Task.FromResult(process.ExitCode == 0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Debug error: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    private string GenerateObjectFileContent(string sourceFile, string sourceContent, string header)
    {
        var timestamp = DateTime.UtcNow.ToString("o");
        return $"/* {header}\n * Source: {sourceFile}\n * Assembled: {timestamp}\n */\n\n{sourceContent}\n";
    }

    [StarterTemplate("c")]
    public static (string MainFile, string Content) GetStarter(string name)
    {
        var mainFile = "src/main.c";
        var mainContent = $@"/* {name} - C main
       Generated by Meow */
    #include <stdio.h>
    int main(void) {{ printf(""Hello from {name}!\n""); return 0; }}
    ";
        return (mainFile, mainContent);
    }
}
