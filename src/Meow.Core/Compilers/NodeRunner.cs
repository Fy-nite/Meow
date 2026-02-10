using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Meow.Core.Services;

namespace Meow.Core.Compilers;

public class NodeRunner : IRunner
{
    public string Name => "node";

    public IEnumerable<string> SourceExtensions => new[] { ".js", ".mjs", ".cjs" };

    private string? FindNodeExecutable()
    {
        var candidates = new[] { "node", "deno" };
        foreach (var c in candidates)
        {
            try
            {
                var psi = new ProcessStartInfo(c, "--version")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi);
                if (p == null) continue;
                p.WaitForExit(2000);
                if (p.ExitCode == 0)
                    return c;
            }
            catch
            {
                // try next
            }
        }
        return null;
    }

    public Task<bool> DebugAsync(string scriptPath, string? stdinFile = null)
    {
        Console.WriteLine("Node debug is not implemented. Use 'node inspect' or an external debugger.");
        return Task.FromResult(false);
    }

    public async Task<bool> RunAsync(string scriptPath, string? stdinFile = null)
    {
        try
        {
            var node = FindNodeExecutable();
            if (node == null)
            {
                Console.WriteLine("Node (or deno) not found in PATH.");
                return false;
            }

            var psi = new ProcessStartInfo(node, $"\"{scriptPath}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = stdinFile != null,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                Console.WriteLine("Failed to start node process.");
                return false;
            }

            if (stdinFile != null)
            {
                try
                {
                    using var sr = new StreamReader(stdinFile);
                    var content = await sr.ReadToEndAsync();
                    await process.StandardInput.WriteAsync(content);
                    process.StandardInput.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading stdin file: {ex.Message}");
                }
            }

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await Task.WhenAll(outputTask, errorTask);
            process.WaitForExit();

            var output = outputTask.Result;
            var error = errorTask.Result;
            Console.WriteLine(output);
            if (process.ExitCode != 0)
            {
                Console.WriteLine($"node run error: {error}");
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error running node script {scriptPath}: {ex.Message}");
            return false;
        }
    }

    // Template provider support for Node/JS
    public Task<bool> CreateMainAsync(string projectPath, string mainRelativePath)
    {
        try
        {
            var fullPath = Path.Combine(projectPath, mainRelativePath);
            var dir = Path.GetDirectoryName(fullPath) ?? Path.Combine(projectPath, "src");
            Directory.CreateDirectory(dir);
            var content = "console.log('Hello, world!');\n";
            File.WriteAllText(fullPath, content);
            Console.WriteLine($"Created Node main template: {mainRelativePath}");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to create Node main template: {ex.Message}");
            return Task.FromResult(false);
        }
    }
}
