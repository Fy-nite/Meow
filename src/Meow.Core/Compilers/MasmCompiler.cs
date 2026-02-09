using System.Diagnostics;
using Meow.Core.Models;

namespace Meow.Core.Services;

public class MasmCompiler : ICompiler
{
    public string Name => "masm";

    public IEnumerable<string> SourceExtensions => new[] { ".masm" };
    public IEnumerable<string> SupportedDependencyCategories => new[] { "assembly", "native", "runtime" };

    public async Task<string?> AssembleAsync(string projectPath, string sourcePath, string objDir, BuildConfig buildConfig)
    {
        try
        {
            var fullSourcePath = Path.Combine(projectPath, sourcePath);
            var relativePath = sourcePath.Replace("src/", "").Replace("src\\", "");
            var objectFileName = relativePath
                .Replace(Path.DirectorySeparatorChar, '_')
                .Replace(Path.AltDirectorySeparatorChar, '_')
                .Replace(".masm", ".masi");
            var objectFilePath = Path.Combine(objDir, objectFileName);

            var process = new Process();
            process.StartInfo.FileName = "masm";
            process.StartInfo.Arguments = $"\"{fullSourcePath}\" -o \"{objectFilePath}\"";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                Console.WriteLine($"masm error: {error}");
                return null;
            }
            if (error.Length > 0 )
            {
                Console.WriteLine($"Assembly errors in {sourcePath}:\n{error}");
                return null;
            }
            return objectFilePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error assembling {sourcePath}: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> LinkAsync(IEnumerable<string> objectFiles, string outputFile, BuildConfig buildConfig)
    {
        try
        {
            var process = new Process();
            process.StartInfo.FileName = "masm";
            var objArgs = string.Join(" ", objectFiles.Select(f => $"\"{f}\""));
            process.StartInfo.Arguments = $"link {objArgs} -o \"{outputFile}\"";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            Console.WriteLine(output);
            if (process.ExitCode != 0)
            {
                Console.WriteLine($"masm link error: {error}");
                return false;
            }
            if (error.Length > 0 )
            {
                Console.WriteLine($"Linking warnings/errors:\n{error}");
            }
            if (output.Contains("Link failed"))
            {
                Console.WriteLine("Linking failed due to errors in object files.");
                Console.WriteLine(error);
                return false;
            }
            Console.WriteLine(output);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error linking: {ex.Message}");
            return false;
        }
    }

    public Task<bool> RunAsync(string executable, string? stdinFile = null)
    {
        try
        {
            var process = new Process();
            process.StartInfo.FileName = "masm";
            var args = $"\"{executable}\" --run";
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
            process.StartInfo.FileName = "masm";
            var args = $"\"{executable}\" --debug";
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
                Console.WriteLine($"masm debug error: {error}");
                return Task.FromResult(false);
            }
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error debugging {executable}: {ex.Message}");
            return Task.FromResult(false);
        }
    }
}
