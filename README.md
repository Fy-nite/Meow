# Meow 🐱

[![License: AGPL v3](https://img.shields.io/badge/License-AGPL%20v3-blue.svg)](https://www.gnu.org/licenses/agpl-3.0)
[![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)](https://dotnet.microsoft.com/)

**Meow** is a modern build orchestration tool for MicroAssembly (MASM) projects, designed to unify project management and streamline the developer experience. Built with C# and designed as both a CLI tool and a library, Meow integrates with the Rust MASM interpreter and PurrNet for dependency management.

```
  __  __                    
 |  \/  | ___  _____      __
 | |\/| |/ _ \/ _ \ \ /\ / /
 | |  | |  __/ (_) \ V  V / 
 |_|  |_|\___|\___/ \_/\_/  
```

## Features

### Phase 1: Project Setup ✅ (Current)
- ✅ Project initialization with `meow init`
- ✅ YAML-based configuration (`meow.yaml`)
- ✅ Starter MASM project templates
- ✅ CLI framework with help and version commands
- ✅ Library API for extensibility

### Phase 2: Build & Execution (Coming Soon)
- 🔄 MASM build orchestration via Rust MASM interpreter
- 🔄 Debug/release build modes
- 🔄 Incremental build support
- 🔄 `meow build` and `meow run` commands

### Phase 3: Package Management (Planned)
- 📋 PurrNet integration for dependencies
- 📋 `meow install`, `meow update`, and `meow publish` commands
- 📋 Dependency tree display and lock file

### Phase 4: Developer Experience (Planned)
- 📋 Colorized CLI output and progress indicators
- 📋 Script support in config file
- 📋 Pre/post build hooks
- 📋 Comprehensive documentation

## Installation

### From Source

```bash
git clone https://github.com/Fy-nite/Meow.git
cd Meow
dotnet build
dotnet pack src/Meow.CLI/Meow.CLI.csproj
dotnet tool install --global --add-source ./src/Meow.CLI/bin/Debug meow.cli
```

### As a .NET Tool (Once Published)

```bash
dotnet tool install --global meow.cli
```

## Quick Start

### Create a New MASM Project

```bash
# Create a new project in a new directory
meow init my-masm-app

# Or initialize in the current directory
cd my-project
meow init
```

This creates:
- `meow.yaml` - Project configuration
- `src/main.masm` - Main entry point
- `README.md` - Project documentation
- `.gitignore` - Git ignore patterns

### Project Structure

```
my-masm-app/
├── meow.yaml           # Project configuration
├── src/
│   └── main.masm       # Main MASM source file
├── tests/              # Test files
├── build/              # Build output
├── README.md           # Project documentation
└── .gitignore          # Git ignore file
```

### Configuration (meow.yaml)

```yaml
name: my-masm-app
version: 0.1.0
description: A my-masm-app MASM project
authors:
  - Your Name
type: masm
main: src/main.masm
build:
  mode: debug
  output: build
  target: default
  incremental: true
dependencies: {}
devDependencies: {}
scripts: {}
```

## Usage

```bash
# Show version
meow --version

# Show help
meow --help

# Initialize a project
meow init [project-name]

# Build the project (Phase 2)
meow build

# Run the project (Phase 2)
meow run

# Run tests (Phase 4)
meow test

# Install dependencies (Phase 3)
meow install

# Update dependencies (Phase 3)
meow update

# Publish to PurrNet (Phase 3)
meow publish
```

## Using Meow as a Library

Meow is designed to be extensible and can be used as a library in your own toolchain extensions:

```csharp
using Meow.Core.Services;
using Meow.Core.Models;

// Initialize services
var configService = new ConfigService();
var projectService = new ProjectService(configService);

// Create a new project programmatically
await projectService.InitializeMasmProjectAsync(
    name: "my-project",
    path: "/path/to/project",
    author: "Your Name"
);

// Load and modify configuration
var config = await configService.LoadConfigAsync("meow.yaml");
config.Version = "0.2.0";
await configService.SaveConfigAsync(config, "meow.yaml");
```

### Available Services

- **`IConfigService`** / **`ConfigService`**: Load and save `meow.yaml` configuration files
- **`IProjectService`** / **`ProjectService`**: Initialize new MASM projects

## Architecture

Meow is built with a modular architecture:

- **Meow.Core**: Core library with reusable services and models
  - `Models`: Configuration data models
  - `Services`: Business logic and project management
  - `Templates`: Project templates and scaffolding

- **Meow.CLI**: Command-line interface
  - `Commands`: CLI command handlers
  - Thin wrapper around Meow.Core services

This separation allows Meow to be used both as a standalone CLI tool and as a library for building custom tooling.

## Development Roadmap

### Phase 1: Project Setup ✅
- [x] Establish C# solution structure and CLI framework
- [x] Implement basic CLI commands: `meow --version`, `meow --help`
- [x] Project initialization: `meow init` for MASM projects with templates
- [x] Generate `meow.yaml` configuration and starter MASM file structure
- [x] Create library API for extensibility

### Phase 2: Build & Execution
- [ ] MASM build orchestration via Rust MASM interpreter
- [ ] Debug/release build modes and incremental build support
- [ ] `meow build` and `meow run` commands

### Phase 3: Package Management
- [ ] PurrNet integration for MASM dependencies
- [ ] `meow install`, `meow update`, and `meow publish` commands
- [ ] Dependency tree display and lock file for reproducible builds

### Phase 4: Developer Experience
- [ ] Colorized CLI output, progress indicators, and improved error messages
- [ ] Script support in config file, pre/post build hooks
- [ ] Comprehensive documentation and example projects

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the GNU Affero General Public License v3.0 or later (AGPL-3.0-or-later). See the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Built for the MicroAssembly (MASM) ecosystem
- Integrates with the Rust MASM interpreter
- Uses PurrNet for package management