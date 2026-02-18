using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Meow.Core.Models;

namespace Meow.Core.Services;

public class FusionCompiler : ICompiler
{
    public string Name => "fusion";
    public IEnumerable<string> SourceExtensions => new[] { ".fut", ".fusion" };
    public IEnumerable<string> SupportedDependencyCategories => new[] { "fusion", "library", "runtime" };

    public async Task<string?> AssembleAsync(string projectPath, string sourcePath, string objDir, BuildConfig buildConfig, IProgressReporter? reporter = null)
    {
        try
        {
            var fullSourcePath = Path.Combine(projectPath, sourcePath);
            var relativePath = sourcePath.Replace("src/", "").Replace("src\\", "");
            var relativeDir = Path.GetDirectoryName(relativePath) ?? string.Empty;
            var objectFileName = Path.GetFileNameWithoutExtension(relativePath) + $".{buildConfig.FutLanguage ?? "obj"}";
            var objectDirFull = Path.Combine(objDir, relativeDir);
            var objectFilePath = Path.Combine(objectDirFull, objectFileName);

            // Try to invoke the 'fut' compiler if available
            try
            {
                var futArgs = new List<string>();
                // language: prefer explicit FutLanguage, fallback to Target
                var lang = string.Empty;
                if (!string.IsNullOrWhiteSpace(buildConfig?.FutLanguage)) lang = buildConfig.FutLanguage;
                else if (!string.IsNullOrWhiteSpace(buildConfig?.Target) && buildConfig.Target != "default") lang = buildConfig.Target;
                if (!string.IsNullOrWhiteSpace(lang))
                {
                    futArgs.Add("-l");
                    futArgs.Add(lang);
                }

                // name (-n): prefer FutName, fallback to project folder name
                var projectName = Path.GetFileName(projectPath) ?? "project";
                var futName = !string.IsNullOrWhiteSpace(buildConfig?.FutName) ? buildConfig.FutName : projectName;
                if (!string.IsNullOrWhiteSpace(futName))
                {
                    futArgs.Add("-n");
                    futArgs.Add(futName);
                }

                // defines (-D)
                if (buildConfig?.FutDefines != null)
                {
                    foreach (var d in buildConfig.FutDefines.Where(s => !string.IsNullOrWhiteSpace(s)))
                    {
                        futArgs.Add("-D");
                        futArgs.Add(d);
                    }
                }
                
                // include/resource dirs (-I)
                if (buildConfig?.FutIncludes != null)
                {
                    foreach (var inc in buildConfig.FutIncludes.Where(s => !string.IsNullOrWhiteSpace(s)))
                    {
                        // make include path relative to project if not rooted
                        var incPath = Path.IsPathRooted(inc) ? inc : Path.Combine(projectPath, inc);
                        futArgs.Add("-I");
                        futArgs.Add(incPath);
                    }
                }

                // read-only files (-r)
                if (buildConfig?.FutReadOnly != null)
                {
                    foreach (var r in buildConfig.FutReadOnly.Where(s => !string.IsNullOrWhiteSpace(s)))
                    {
                        var rPath = Path.IsPathRooted(r) ? r : Path.Combine(projectPath, r);
                        futArgs.Add("-r");
                        futArgs.Add(rPath);
                    }
                }

                // output and source
                futArgs.Add("-o");
                futArgs.Add(objectFilePath);
                futArgs.Add(fullSourcePath);

                // extra args appended
                if (buildConfig?.FutExtraArgs != null)
                {
                    futArgs.AddRange(buildConfig.FutExtraArgs.Where(s => !string.IsNullOrWhiteSpace(s)));
                }

                var process = new Process();
                process.StartInfo.FileName = "fut";
                process.StartInfo.Arguments = string.Join(" ", futArgs.Select(a => a.Contains(' ') ? "\"" + a + "\"" : a));
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                string stdout = await process.StandardOutput.ReadToEndAsync();
                string stderr = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();
                if (!string.IsNullOrEmpty(stdout)) Console.WriteLine(stdout);
                if (process.ExitCode == 0 && File.Exists(objectFilePath))
                {
                    return objectFilePath;
                }
                if (!string.IsNullOrEmpty(stderr)) Console.WriteLine($"fut: {stderr}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"fut CLI not available or failed to run: {ex.Message}");
            }

            // Fallback: generate a simple object file containing the source and a header
            var sourceContent = await File.ReadAllTextAsync(fullSourcePath);
            Directory.CreateDirectory(objectDirFull ?? objDir);
            var content = GenerateObjectFileContent(sourcePath, sourceContent, "Fusion Object");
            await File.WriteAllTextAsync(objectFilePath, content);
            return objectFilePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fusion assemble error: {ex.Message}");
            return null;
        }
    }

    public async Task<(bool Success, string? Error)> LinkAsync(IEnumerable<string> objectFiles, string outputFile, BuildConfig buildConfig)
    {
        try
        {
            // Try to use 'fut' linker if available
            try
            {
                var futArgs = new List<string>();
                if (!string.IsNullOrWhiteSpace(buildConfig?.Target) && buildConfig.Target != "default")
                {
                    futArgs.Add("-l");
                    futArgs.Add(buildConfig.Target);
                }
                // pass output
                futArgs.Add("-o");
                futArgs.Add(outputFile);
                // add object/input files
                futArgs.AddRange(objectFiles);

                var process = new Process();
                process.StartInfo.FileName = "fut";
                process.StartInfo.Arguments = string.Join(" ", futArgs.Select(a => a.Contains(' ') ? "\"" + a + "\"" : a));
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                string stdout = await process.StandardOutput.ReadToEndAsync();
                string stderr = await process.StandardError.ReadToEndAsync();
                process.WaitForExit();
                if (!string.IsNullOrEmpty(stdout)) Console.WriteLine(stdout);
                if (process.ExitCode == 0 && File.Exists(outputFile))
                {
                    return (true, null);
                }
                if (!string.IsNullOrEmpty(stderr)) Console.WriteLine($"fut: {stderr}");
                var futErr = !string.IsNullOrEmpty(stderr) ? $"fut link error: {stderr}" : "fut link failed";
                // fallthrough to fallback linking
            }
            catch (Exception ex)
            {
                Console.WriteLine($"fut CLI link not available or failed: {ex.Message}");
            }

            // Fallback: simple concatenation into the output file
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# Fusion Linked Output");
            foreach (var f in objectFiles)
            {
                sb.AppendLine($"// Included: {Path.GetFileName(f)}");
                sb.AppendLine(File.ReadAllText(f));
            }
            Directory.CreateDirectory(Path.GetDirectoryName(outputFile) ?? Path.GetDirectoryName(objectFiles.First()) ?? ".");
            File.WriteAllText(outputFile, sb.ToString());
            return (true, null);
        }
        catch (Exception ex)
        {
            var exc = $"Fusion link error: {ex.Message}";
            Console.WriteLine(exc);
            return (false, exc);
        }
    }

    public Task<bool> RunAsync(string executable, string? stdinFile = null)
    {
        Console.WriteLine("Run not implemented for Fusion projects.");
        return Task.FromResult(true);
    }

    public Task<bool> DebugAsync(string executable, string? stdinFile = null)
    {
        Console.WriteLine("Debug not implemented for Fusion projects.");
        return Task.FromResult(true);
    }

    private string GenerateObjectFileContent(string sourceFile, string sourceContent, string header)
    {
        var timestamp = DateTime.UtcNow.ToString("o");
        return $"// {header}\n// Source: {sourceFile}\n// Assembled: {timestamp}\n\n{sourceContent}\n";
    }
}