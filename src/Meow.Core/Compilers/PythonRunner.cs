using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Meow.Core.Services;

namespace Meow.Core.Compilers;

public class PythonRunner : IRunner
{
    public string Name => "python";

    public IEnumerable<string> SourceExtensions => new[] { ".py" };

    private string? FindPythonExecutable()
    {
        var candidates = new[] { "python", "python3" };
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
        Console.WriteLine("Python debug is not implemented by PythonRunner. Use an external debugger.");
        return Task.FromResult(false);
    }

    public async Task<bool> RunAsync(string scriptPath, string? stdinFile = null)
    {
        try
        {
            var python = FindPythonExecutable();
            if (python == null)
            {
                Console.WriteLine("Python interpreter not found in PATH.");
                return false;
            }

            var psi = new ProcessStartInfo(python, $"\"{scriptPath}\"")
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
                Console.WriteLine("Failed to start python process.");
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
                Console.WriteLine($"python run error: {error}");
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error running python script {scriptPath}: {ex.Message}");
            return false;
        }
    }
}
