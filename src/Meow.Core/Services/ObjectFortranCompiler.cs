using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Meow.Core.Models;

namespace Meow.Core.Services;

public class ObjectFortranCompiler : ICompiler
{
    public string Name => "objectfortran";
    public IEnumerable<string> SourceExtensions => new[] { ".fobj" };
    public IEnumerable<string> SupportedDependencyCategories => new[] { "fortran", "runtime" };

    public async Task<string?> AssembleAsync(string projectPath, string sourcePath, string objDir, BuildConfig buildConfig)
    {
        try
        {
            var fullSourcePath = Path.Combine(projectPath, sourcePath);
            var relativePath = sourcePath.Replace("src/", "").Replace("src\\", "");
            var objectFileName = relativePath
                .Replace(Path.DirectorySeparatorChar, '_')
                .Replace(Path.AltDirectorySeparatorChar, '_')
                + ".fo";
            var objectFilePath = Path.Combine(objDir, objectFileName);

            var sourceContent = await File.ReadAllTextAsync(fullSourcePath);
            var content = GenerateObjectFileContent(sourcePath, sourceContent, "ObjectFortran Object");
            Directory.CreateDirectory(Path.GetDirectoryName(objectFilePath) ?? objDir);
            await File.WriteAllTextAsync(objectFilePath, content);
            return objectFilePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ObjectFortran assemble error: {ex.Message}");
            return null;
        }
    }

    public Task<bool> LinkAsync(IEnumerable<string> objectFiles, string outputFile, BuildConfig buildConfig)
    {
        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("! ObjectFortran Linked Output");
            foreach (var f in objectFiles)
            {
                sb.AppendLine($"! Included: {Path.GetFileName(f)}");
                sb.AppendLine(File.ReadAllText(f));
            }
            Directory.CreateDirectory(Path.GetDirectoryName(outputFile) ?? Path.GetDirectoryName(objectFiles.First()) ?? ".");
            File.WriteAllText(outputFile, sb.ToString());
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ObjectFortran link error: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    public Task<bool> RunAsync(string executable, string? stdinFile = null)
    {
        Console.WriteLine("Run not implemented for ObjectFortran projects.");
        return Task.FromResult(true);
    }

    public Task<bool> DebugAsync(string executable, string? stdinFile = null)
    {
        Console.WriteLine("Debug not implemented for ObjectFortran projects.");
        return Task.FromResult(true);
    }

    private string GenerateObjectFileContent(string sourceFile, string sourceContent, string header)
    {
        var timestamp = DateTime.UtcNow.ToString("o");
        return $"! {header}\n! Source: {sourceFile}\n! Assembled: {timestamp}\n\n{sourceContent}\n";
    }
}
