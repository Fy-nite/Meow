# CLI Reference

Complete reference for all Meow CLI commands.

## Global Options

```
--version, -v    Show version information
--help, -h       Show help message
```

## Commands

### meow init

Initialize a new MASM project.

**Usage:**
```bash
meow init [PROJECT_NAME]
```

**Arguments:**
- `PROJECT_NAME` (optional): Name of the project. If omitted, uses the current directory name.

**Examples:**
```bash
# Create a new project in a new directory
meow init my-masm-app

# Initialize in the current directory
cd my-project
meow init
```

**What it creates:**
- `meow.yaml` - Project configuration file
- `src/main.masm` - Main MASM source file
- `tests/` - Test directory
- `build/` - Build output directory
- `README.md` - Project documentation
- `.gitignore` - Git ignore patterns

**Exit Codes:**
- `0` - Success
- `1` - Error (e.g., project already exists)

---

### meow build

Build the MASM project.

**Status:** Phase 2 (Not Yet Implemented)

**Usage:**
```bash
meow build [OPTIONS]
```

**Planned Options:**
- `--mode <debug|release>` - Build mode (default: debug)
- `--output <DIR>` - Output directory (default: build)
- `--incremental` - Enable incremental builds
- `--clean` - Clean before building

**Examples:**
```bash
# Build in debug mode
meow build

# Build in release mode
meow build --mode release

# Clean build
meow build --clean
```

---

### meow run

Run the MASM project.

**Status:** Phase 2 (Not Yet Implemented)

**Usage:**
```bash
meow run [OPTIONS] [-- ARGS]
```

**Planned Options:**
- `--mode <debug|release>` - Run mode (default: debug)
- `--` - Pass arguments to the program

**Examples:**
```bash
# Run the project
meow run

# Run with arguments
meow run -- arg1 arg2

# Run release build
meow run --mode release
```

---

### meow test

Run project tests.

**Status:** Phase 4 (Planned)

**Usage:**
```bash
meow test [OPTIONS]
```

**Planned Options:**
- `--filter <PATTERN>` - Run tests matching pattern
- `--verbose` - Verbose output

---

### meow install

Install dependencies from PurrNet.

**Status:** Phase 3 (Planned)

**Usage:**
```bash
meow install [PACKAGE]
```

**Arguments:**
- `PACKAGE` (optional): Specific package to install. If omitted, installs all dependencies from `meow.yaml`.

**Examples:**
```bash
# Install all dependencies
meow install

# Install a specific package
meow install stdlib@^1.0.0
```

---

### meow update

Update dependencies from PurrNet.

**Status:** Phase 3 (Planned)

**Usage:**
```bash
meow update [PACKAGE]
```

**Arguments:**
- `PACKAGE` (optional): Specific package to update. If omitted, updates all dependencies.

---

### meow publish

Publish package to PurrNet.

**Status:** Phase 3 (Planned)

**Usage:**
```bash
meow publish [OPTIONS]
```

**Planned Options:**
- `--tag <TAG>` - Publish with a specific tag
- `--access <public|restricted>` - Package access level

---

## Configuration File (meow.yaml)

The `meow.yaml` file configures your project:

```yaml
# Project metadata
name: my-project
version: 0.1.0
description: My MASM project
authors:
  - Your Name

# Project type
type: masm

# Main entry point
main: src/main.masm

# Build configuration
build:
  mode: debug              # debug or release
  output: build            # Output directory
  target: default          # Target platform
  incremental: true        # Enable incremental builds

# Dependencies from PurrNet
dependencies:
  stdlib: "^1.0.0"
  math-lib: "^2.1.0"

# Development dependencies
devDependencies:
  test-framework: "^1.0.0"

# Custom scripts
scripts:
  test: "meow test"
  deploy: "meow build --mode release && deploy.sh"
```

## Environment Variables

- `MEOW_CONFIG` - Path to alternative config file (default: `meow.yaml`)
- `MEOW_BUILD_DIR` - Override build directory

## Exit Codes

- `0` - Success
- `1` - General error
- `2` - Configuration error
- `3` - Build error
- `4` - Runtime error

## See Also

- [Main README](../README.md)
- [API Documentation](API.md)
