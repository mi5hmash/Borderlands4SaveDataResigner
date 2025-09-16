# VS Code Development Setup

This workspace now includes comprehensive development tools for the Borderlands 4 Save Data Resigner project.

## Available Tasks

Access these tasks via **Terminal > Run Task...** or **Ctrl+Shift+P > Tasks: Run Task**

### Build Tasks
- **Build Debug** (Default) - Build in Debug configuration
- **Build Release** - Build in Release configuration
- **Clean** - Clean all build outputs
- **Restore** - Restore NuGet packages
- **Rebuild** - Clean and rebuild the project
- **Watch Build** - Automatically rebuild on file changes

### Run Tasks
- **Run Application** - Launch the WPF GUI application
- **Publish Release** - Create a self-contained executable

## Quick Shortcuts

- **Ctrl+Shift+B** - Quick build (Build Debug)
- **F5** - Run with debugger (if C# extension is installed)
- **Ctrl+F5** - Run without debugger

## Recommended Extensions

Install these extensions for the best development experience:
- C# Dev Kit (ms-dotnettools.csdevkit)
- C# (ms-dotnettools.csharp)
- PowerShell (ms-vscode.powershell)
- EditorConfig (editorconfig.editorconfig)

VS Code will automatically suggest these when you open the workspace.

## Project Structure

```
Borderlands4SaveDataResigner/
├── .vscode/                    # VS Code workspace configuration
│   ├── tasks.json             # Build and run tasks
│   ├── launch.json            # Debug configurations
│   ├── settings.json          # Workspace settings
│   └── extensions.json        # Recommended extensions
├── Borderlands4SaveDataResigner/  # Main project
│   ├── *.cs                   # C# source files
│   └── *.csproj              # Project file
└── *.sln                      # Solution file
```

## Usage

1. **Build**: Press `Ctrl+Shift+B` or run "Build Debug" task
2. **Run**: Use "Run Application" task or press F5
3. **Publish**: Use "Publish Release" task to create distributable executable
4. **Clean**: Use "Clean" task when needed to clear build artifacts

The tasks are configured to work with the dual-mode application (GUI by default, console mode with arguments).
