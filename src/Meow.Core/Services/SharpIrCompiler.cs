using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Meow.Core.Models;

namespace Meow.Core.Services;

public class SharpIrCompiler : ICompiler
{
    public string Name => "sharpir";
    public BuildConfig BuildConfig { get; set; }

    public IEnumerable<string> SourceExtensions => new[] { ".sir", ".cs" };
    public static string ObjectDirectory;

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

            var sourceContent = await File.ReadAllTextAsync(fullSourcePath);
            ObjectDirectory = objDir;
            // store provided build config on the instance
            this.BuildConfig = buildConfig;

            // Ensure we pass absolute paths to the sharpir tool so it emits the JSON into the
            // project's build directory (objDir may be relative to projectPath).
            var relativeOirPath = relativePath + ".oir";
            string oirOutputPath = Path.IsPathRooted(objDir)
                ? Path.Combine(objDir, relativeOirPath)
                : Path.Combine(projectPath, objDir, relativeOirPath);

            // Ensure output directory exists for the .oir file
            Directory.CreateDirectory(Path.GetDirectoryName(oirOutputPath) ?? objDir);

            GenerateObjectFileContent(fullSourcePath, oirOutputPath, "SharpIR Object");
            // Ensure object file directory exists as well
            Directory.CreateDirectory(Path.GetDirectoryName(objectFilePath) ?? objDir);
        
            return objectFilePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SharpIR assemble error: {ex.Message}");
            return null;
        }
    }

    public Task<bool> LinkAsync(IEnumerable<string> objectFiles, string outputFile, BuildConfig buildConfig)
    {
        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# SharpIR Linked Output");
            foreach (var f in objectFiles)
            {
                sb.AppendLine($"// Included: {Path.GetFileName(f)}");
                sb.AppendLine(File.ReadAllText(f));
            }
            Directory.CreateDirectory(Path.GetDirectoryName(outputFile) ?? Path.GetDirectoryName(objectFiles.First()) ?? ".");
            File.WriteAllText(outputFile, sb.ToString());
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SharpIR link error: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    public Task<bool> RunAsync(string executable, string? stdinFile = null)
    {
        Console.WriteLine("Run not implemented for SharpIR projects.");
        return Task.FromResult(true);
    }

    public Task<bool> DebugAsync(string executable, string? stdinFile = null)
    {
        Console.WriteLine("Debug not implemented for SharpIR projects.");
        return Task.FromResult(true);
    }

    private void GenerateObjectFileContent(string sourceFilePath, string oirOutputPath, string header)
    {
        // Run sharpir with absolute/quoted paths so it emits the JSON (.oir) into the desired build directory
        Process compiler = new Process();
        compiler.StartInfo.FileName = "sharpir";
        compiler.StartInfo.Arguments = $"\"{sourceFilePath}\" --emit-json --out \"{oirOutputPath}\"";
        compiler.StartInfo.RedirectStandardOutput = true;
        compiler.StartInfo.RedirectStandardError = true;
        compiler.StartInfo.UseShellExecute = false;
        compiler.StartInfo.CreateNoWindow = true;
        compiler.Start();

        // Read stdout
        while (!compiler.StandardOutput.EndOfStream)
        {
            string? line = compiler.StandardOutput.ReadLine();
            if (!string.IsNullOrEmpty(line)) Console.WriteLine(line);
        }

        compiler.WaitForExit();

        if (compiler.ExitCode != 0)
        {
            string error = compiler.StandardError.ReadToEnd();
            Console.WriteLine($"SharpIR compile error: {error}");
        }
    }
}
