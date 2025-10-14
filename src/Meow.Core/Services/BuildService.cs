using Meow.Core.Models;

namespace Meow.Core.Services;

/// <summary>
/// Implementation of build service for MASM projects
/// </summary>
public class BuildService : IBuildService
{
    private readonly IConfigService _configService;

    /// <summary>
    /// Creates a new instance of BuildService
    /// </summary>
    /// <param name="configService">Configuration service instance</param>
    public BuildService(IConfigService configService)
    {
        _configService = configService;
    }

    /// <inheritdoc />
    public async Task<bool> BuildProjectAsync(string projectPath, bool clean = false)
    {
        try
        {
            // Load configuration
            var configPath = Path.Combine(projectPath, "meow.yaml");
            if (!_configService.ConfigExists(configPath))
            {
                throw new FileNotFoundException($"Configuration file not found: {configPath}");
            }

            var config = await _configService.LoadConfigAsync(configPath);

            // Clean if requested
            if (clean)
            {
                CleanBuildDirectory(projectPath, config.Build);
            }

            // Create output directories
            var buildDir = Path.Combine(projectPath, config.Build.Output);
            var objDir = Path.Combine(projectPath, config.Build.Objdir);
            Directory.CreateDirectory(buildDir);
            Directory.CreateDirectory(objDir);

            // Get source files
            var sourceFiles = GetSourceFiles(projectPath, config);

            if (sourceFiles.Count == 0)
            {
                Console.WriteLine("No source files found to build.");
                return false;
            }

            Console.WriteLine($"Found {sourceFiles.Count} source file(s) to build:");
            foreach (var file in sourceFiles)
            {
                Console.WriteLine($"  - {file}");
            }

            // Assemble each source file to object file
            var objectFiles = new List<string>();
            foreach (var sourceFile in sourceFiles)
            {
                var objFile = AssembleToObject(projectPath, sourceFile, objDir);
                if (objFile != null)
                {
                    objectFiles.Add(objFile);
                }
                else
                {
                    Console.WriteLine($"Failed to assemble: {sourceFile}");
                    return false;
                }
            }

            Console.WriteLine($"\nAssembled {objectFiles.Count} object file(s):");
            foreach (var objFile in objectFiles)
            {
                Console.WriteLine($"  - {objFile}");
            }

            // Link if enabled
            if (config.Build.Link && objectFiles.Count > 0)
            {
                var outputFile = Path.Combine(projectPath, config.Build.Output, 
                    $"{config.Name}.masi");
                
                if (LinkObjectFiles(objectFiles, outputFile))
                {
                    Console.WriteLine($"\nLinked output: {Path.GetFileName(outputFile)}");
                }
                else
                {
                    Console.WriteLine("\nFailed to link object files.");
                    return false;
                }
            }
            else if (!config.Build.Link)
            {
                Console.WriteLine("\nLinking disabled - object files only.");
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Build failed: {ex.Message}");
            return false;
        }
    }

    private List<string> GetSourceFiles(string projectPath, MeowConfig config)
    {
        var sourceFiles = new List<string>();
        var srcDir = Path.Combine(projectPath, "src");

        if (!Directory.Exists(srcDir))
        {
            Console.WriteLine($"Source directory not found: {srcDir}");
            return sourceFiles;
        }

        if (config.Build.Wildcard)
        {
            // Get all .masm files recursively
            var files = Directory.GetFiles(srcDir, "*.masm", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                sourceFiles.Add(Path.GetRelativePath(projectPath, file));
            }
        }
        else
        {
            // Use main file only
            var mainFile = Path.Combine(projectPath, config.Main);
            if (File.Exists(mainFile))
            {
                sourceFiles.Add(config.Main);
            }
        }

        return sourceFiles;
    }

    private string? AssembleToObject(string projectPath, string sourceFile, string objDir)
    {
        try
        {
            var fullSourcePath = Path.Combine(projectPath, sourceFile);
            
            // Generate object file name
            // Convert path separators to underscores for flat output
            var relativePath = sourceFile.Replace("src/", "").Replace("src\\", "");
            var objectFileName = relativePath
                .Replace(Path.DirectorySeparatorChar, '_')
                .Replace(Path.AltDirectorySeparatorChar, '_')
                .Replace(".masm", ".masi");
            
            var objectFilePath = Path.Combine(objDir, objectFileName);

            // Read source file
            var sourceContent = File.ReadAllText(fullSourcePath);

            // Simulate assembly process - in reality this would call the MASM interpreter
            // For now, we'll create a mock object file with metadata
            var objectContent = GenerateObjectFileContent(sourceFile, sourceContent);

            // Write object file
            File.WriteAllText(objectFilePath, objectContent);

            return objectFilePath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error assembling {sourceFile}: {ex.Message}");
            return null;
        }
    }

    private string GenerateObjectFileContent(string sourceFile, string sourceContent)
    {
        // Mock object file format - in reality, this would be binary/intermediate format
        // from the MASM interpreter
        var timestamp = DateTime.UtcNow.ToString("o");
        return $@"; MASM Object File
; Source: {sourceFile}
; Assembled: {timestamp}
; Format: MASI v1.0

; Original source (for demonstration):
{sourceContent}

; [In a real implementation, this would contain assembled bytecode/intermediate representation]
";
    }

    private bool LinkObjectFiles(List<string> objectFiles, string outputFile)
    {
        try
        {
            // Simulate linking process - in reality this would combine object files
            var linkedContent = "; MASM Linked Executable\n";
            linkedContent += $"; Linked: {DateTime.UtcNow:o}\n";
            linkedContent += $"; Object files: {objectFiles.Count}\n\n";

            foreach (var objFile in objectFiles)
            {
                linkedContent += $"; Including: {Path.GetFileName(objFile)}\n";
                var content = File.ReadAllText(objFile);
                linkedContent += content + "\n\n";
            }

            linkedContent += "; [In a real implementation, this would contain linked executable code]\n";

            // Write linked output
            File.WriteAllText(outputFile, linkedContent);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error linking: {ex.Message}");
            return false;
        }
    }

    private void CleanBuildDirectory(string projectPath, BuildConfig buildConfig)
    {
        var buildDir = Path.Combine(projectPath, buildConfig.Output);
        
        if (Directory.Exists(buildDir))
        {
            Console.WriteLine($"Cleaning build directory: {buildConfig.Output}");
            Directory.Delete(buildDir, true);
        }
    }
}
