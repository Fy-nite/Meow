using System.Collections.Generic;

namespace Meow.Core.Models;

/// <summary>
/// Represents the meow.yaml configuration file structure
/// </summary>
public class MeowConfig
{
    /// <summary>
    /// Project name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Project version
    /// </summary>
    public string Version { get; set; } = "0.1.0";

    /// <summary>
    /// Project description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Authors of the project
    /// </summary>
    public List<string> Authors { get; set; } = new();

    /// <summary>
    /// Project type (e.g., "masm", "executable", "library")
    /// </summary>
    public string Type { get; set; } = "masm";

    /// <summary>
    /// Main entry point file
    /// </summary>
    public string Main { get; set; } = "";

    /// <summary>
    /// Build configuration
    /// </summary>
    public BuildConfig Build { get; set; } = new();

    /// <summary>
    /// Dependencies from PurrNet
    /// </summary>
    public Dictionary<string, string> Dependencies { get; set; } = new();

    /// <summary>
    /// Optional categories for dependencies. Key = package name, Value = category name.
    /// Useful for letting compilers declare supported dependency categories.
    /// </summary>
    public Dictionary<string, string> DependencyCategories { get; set; } = new();

    /// <summary>
    /// Development dependencies
    /// </summary>
    public Dictionary<string, string> DevDependencies { get; set; } = new();

    /// <summary>
    /// Custom scripts
    /// </summary>
    public Dictionary<string, string> Scripts { get; set; } = new();
}

/// <summary>
/// Build configuration options
/// </summary>
public class BuildConfig
{
    /// <summary>
    /// Build mode (debug or release)
    /// </summary>
    public string Mode { get; set; } = "debug";

    /// <summary>
    /// Output directory
    /// </summary>
    public string Output { get; set; } = "build";

    /// <summary>
    /// Target platform
    /// </summary>
    public string Target { get; set; } = "default";

    /// <summary>
    /// Compiler to use for the project (e.g., "masm", "nasm")
    /// </summary>
    public string Compiler { get; set; } = "masm";

    /// <summary>
    /// Enable incremental builds
    /// </summary>
    public bool Incremental { get; set; } = true;

    /// <summary>
    /// Enable wildcard selection of all .masm files in src/ directory
    /// </summary>
    public bool Wildcard { get; set; } = false;

    /// <summary>
    /// Enable linking of object files into a single output file
    /// </summary>
    public bool Link { get; set; } = false;

    /// <summary>
    /// Object file directory
    /// </summary>
    public string Objdir { get; set; } = "build/obj";

    /// <summary>
    /// Test entrypoint file (relative to project root). If empty, the CLI will attempt to
    /// discover a tests/test_main.&lt;ext&gt; file based on the compiler's source extensions.
    /// </summary>
    public string TestMain { get; set; } = string.Empty;

    /// <summary>
    /// Extra arguments to pass to the compiler when building tests. These are appended
    /// to `build.extraArgs` for test builds only.
    /// </summary>
    public List<string> TestExtraArgs { get; set; } = new();
    /// <summary>
    /// Fusion-specific target language (passed to fut as `-l`)
    /// </summary>
    public string FutLanguage { get; set; } = string.Empty;

    /// <summary>
    /// Name to pass to fut `-n` (namespace/package/name prefix). Defaults to project name when empty.
    /// </summary>
    public string FutName { get; set; } = string.Empty;

    /// <summary>
    /// Conditional defines to pass to fut as `-D NAME`.
    /// </summary>
    public List<string> FutDefines { get; set; } = new();

    /// <summary>
    /// Include/resource search paths to pass to fut as `-I DIR`.
    /// </summary>
    public List<string> FutIncludes { get; set; } = new();

    /// <summary>
    /// Files to read but not emit (-r FILE.fu)
    /// </summary>
    public List<string> FutReadOnly { get; set; } = new();

    /// <summary>
    /// Extra arbitrary arguments to append to the fut invocation.
    /// </summary>
    public List<string> FutExtraArgs { get; set; } = new();

    /// <summary>
    /// General extra arguments to append to compiler/linker invocations.
    /// These will be appended to the external tool command line where applicable.
    /// </summary>
    public List<string> ExtraArgs { get; set; } = new();

    /// <summary>
    /// Number of parallel jobs to use when compiling source files. Use 1 for sequential builds.
    /// </summary>
    public int Jobs { get; set; } = 1;

    // --- Platform-specific args ---

    /// <summary>
    /// Extra compiler/linker arguments appended only when building on Windows.
    /// </summary>
    public List<string> WinArgs { get; set; } = new();

    /// <summary>
    /// Extra compiler/linker arguments appended only when building on Linux.
    /// </summary>
    public List<string> LinuxArgs { get; set; } = new();

    /// <summary>
    /// Extra compiler/linker arguments appended only when building on macOS.
    /// </summary>
    public List<string> MacArgs { get; set; } = new();

    // --- C/C++ convenience flags ---

    /// <summary>
    /// Directories to add as -I include search paths (C/C++ compilers).
    /// </summary>
    public List<string> IncludeDirs { get; set; } = new();

    /// <summary>
    /// Preprocessor macro definitions to pass as -D flags (C/C++ compilers).
    /// </summary>
    public List<string> Defines { get; set; } = new();

    /// <summary>
    /// Language standard to pass to the compiler (e.g., "c++17", "c11", "c++20").
    /// Translates to --std=VALUE.
    /// </summary>
    public string Std { get; set; } = string.Empty;

    // --- IDE integration ---

    /// <summary>
    /// When true, generates a compile_commands.json compilation database in the project root
    /// after each build. Useful for IDE/clangd/IntelliSense integration.
    /// </summary>
    public bool GenerateCompileCommands { get; set; } = false;
}
