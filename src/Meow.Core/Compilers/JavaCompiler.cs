using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Meow.Core.Models;

namespace Meow.Core.Services;

/// <summary>
/// Compiler implementation for Java source files using javac.
/// </summary>
public class JavaCompiler : ICompiler
{
    /// <summary>
    /// Name of the compiler implementation.
    /// </summary>
    public string Name => "java";

    /// <summary>
    /// Supported source file extensions.
    /// </summary>
    public IEnumerable<string> SourceExtensions => new[] { ".java" };

    /// <summary>
    /// Dependency categories supported by this compiler.
    /// </summary>
    public IEnumerable<string> SupportedDependencyCategories => new[] { "java", "runtime" };
    /// <summary>
    /// Compile a Java source file into class files placed under the obj directory.
    /// Returns a representative object path on success, or null on failure.
    /// </summary>
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
    /// <summary>
    /// For Java this method performs a packaging/copy step into the output directory.
    /// </summary>
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

    /// <summary>
    /// Run a Java class or jar. `executable` is expected to be the path to the class directory or jar.
    /// </summary>
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

    /// <summary>
    /// Launch the Java Debugger (jdb) against the provided class directory or jar.
    /// </summary>
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

    // Template provider: create a default Main.java in src/ if requested
    /// <summary>
    /// Create a simple `main` Java source file at the requested relative path.
    /// </summary>
    public Task<bool> CreateMainAsync(string projectPath, string mainRelativePath)
    {
        try
        {
            var fullPath = Path.Combine(projectPath, mainRelativePath);
            var dir = Path.GetDirectoryName(fullPath) ?? Path.Combine(projectPath, "src");
            Directory.CreateDirectory(dir);
            var className = Path.GetFileNameWithoutExtension(fullPath);
            if (string.IsNullOrWhiteSpace(className)) className = "Main";
            var content = $"public class {className} {{\n    public static void main(String[] args) {{\n        System.out.println(\"Hello, world!\");\n    }}\n}}\n";
            File.WriteAllText(fullPath, content);
            Console.WriteLine($"Created Java main template: {mainRelativePath}");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to create Java main template: {ex.Message}");
            return Task.FromResult(false);
        }
    }
}
