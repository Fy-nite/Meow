using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Meow.Core.Models;

namespace Meow.Core.Services;

public class JavaCompiler : ICompiler
{
    public string Name => "java";
    public IEnumerable<string> SourceExtensions => new[] { ".java" };
    public IEnumerable<string> SupportedDependencyCategories => new[] { "java", "runtime" };
    public async Task<string?> AssembleAsync(string projectPath, string sourcePath, string objDir, BuildConfig buildConfig)
    {
        try
        {
            var fullSourcePath = Path.Combine(projectPath, sourcePath);
            var relativePath = sourcePath.Replace("src/", "").Replace("src\\", "");
            var objectFileName = relativePath
                .Replace(Path.DirectorySeparatorChar, '_')
                .Replace(Path.AltDirectorySeparatorChar, '_')
                + ".o";
            var objectFilePath = Path.Combine(objDir, objectFileName);

            Directory.CreateDirectory(Path.GetDirectoryName(objectFilePath) ?? objDir);

            var flags = buildConfig.Mode?.ToLower() == "debug" ? "-g -O0" : "-O2 -s";
            var process = new Process();
            process.StartInfo.FileName = "javac";
            process.StartInfo.Arguments = $"-d \"{objDir}\" {flags} \"{fullSourcePath}\"";
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
                Console.WriteLine($"java compile error: {error}");
                return null;
            }
            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine(error);
            }
            return objectFilePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Java assemble error: {ex.Message}");
            return null;
        }
    }
    public async Task<bool> LinkAsync(IEnumerable<string> objectFiles, string outputFile, BuildConfig buildConfig)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputFile) ?? Path.GetDirectoryName(objectFiles.First()) ?? ".");
            var objArgs = string.Join(" ", objectFiles.Select(f => $"\"{f}\""));
            var process = new Process();
            process.StartInfo.FileName = "javac";
            process.StartInfo.Arguments = $"-d \"{Path.GetDirectoryName(outputFile)}\" {objArgs}";
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
                Console.WriteLine($"Java link error: {error}");
                return false;
            }
            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine(error);
            }
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Java link error: {ex.Message}");
            return false;
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
                Console.WriteLine($"Java run error: {error}");
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
            process.StartInfo.FileName = "jdb";
            process.StartInfo.Arguments = $"-classpath \"{Path.GetDirectoryName(executable)}\" {Path.GetFileNameWithoutExtension(executable)}";
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
        return $"// {header}\n// Source: {sourceFile}\n// Assembled: {timestamp}\n\n{sourceContent}\n";
    }
}
