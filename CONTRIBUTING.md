# Contributing to Meow

Thank you for your interest in contributing to Meow! This document provides guidelines for contributing to the project.

## Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/YOUR_USERNAME/Meow.git`
3. Create a branch: `git checkout -b feature/my-feature`
4. Make your changes
5. Build and test: `dotnet build && dotnet test`
6. Commit your changes: `git commit -m "Add my feature"`
7. Push to your fork: `git push origin feature/my-feature`
8. Open a Pull Request

## Development Setup

### Prerequisites

- .NET 9.0 SDK or later
- Git
- A code editor (Visual Studio, VS Code, or Rider recommended)

### Building the Project

```bash
# Clone the repository
git clone https://github.com/Fy-nite/Meow.git
cd Meow

# Restore dependencies and build
dotnet restore
dotnet build

# Run the CLI
dotnet run --project src/Meow.CLI/Meow.CLI.csproj -- --help
```

### Project Structure

```
Meow/
├── src/
│   ├── Meow.Core/          # Core library
│   │   ├── Models/         # Configuration models
│   │   ├── Services/       # Business logic
│   │   └── Templates/      # Project templates
│   └── Meow.CLI/           # CLI application
│       └── Commands/       # Command handlers
├── tests/                  # Unit tests (future)
├── docs/                   # Documentation
├── Meow.sln               # Solution file
└── README.md              # Main documentation
```

## Coding Standards

- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation comments to public APIs
- Keep methods focused and concise
- Write unit tests for new features

## Pull Request Guidelines

- Keep PRs focused on a single feature or bug fix
- Update documentation as needed
- Add tests for new functionality
- Ensure all tests pass
- Follow the existing code style
- Write clear commit messages

## Roadmap Phases

Contributions are welcome for any phase:

### Phase 1: Project Setup (Current)
Focus on improving project initialization and templates

### Phase 2: Build & Execution
Implement MASM build and run capabilities

### Phase 3: Package Management
Add PurrNet integration

### Phase 4: Developer Experience
Enhance CLI output and usability

## Questions?

Feel free to open an issue for questions or discussions.

## License

By contributing, you agree that your contributions will be licensed under the AGPL-3.0-or-later license.
