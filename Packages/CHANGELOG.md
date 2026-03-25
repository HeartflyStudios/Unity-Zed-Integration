# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-03-26
### Added
- Initial *experimental* release of the Zed Editor package for Unity.
- **Feature**: Launch Zed process from Unity Editor. Automatic Zed installation path finding on Linux, Windows and MacOS.
- **Feature**: Generate `.sln` and `.csproj` files for "Local", "Embedded", "Unity Registry", "Git", "Built-In", "Player" and "LocalTarBall" assemblies, as well as unknown sources where possible.
- **Feature**: Integrates [Microsoft.Unity.Analyzers](https://github.com/microsoft/Microsoft.Unity.Analyzers) *v1.26.0*. These add Unity-specific diagnostics and/or remove general C# diagnostics that do not apply to Unity projects.  
- **Feature**: Unity Logs to Proxy that you can connect terminal to read from.

### Known Issues
- **Code Analysis**: [IDE0130](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/ide0130) is triggered for some folders.
- **Unity Editor**: `Debug.LogWarning` is triggered by /Tests/Editor/`HFS.ZedEditor.Editor.Tests.asmdef` as the tests folder is empty.
- **Zed Extension**: Basically does not work. It will display logs to terminal but only after making Unity Editor the focus application/task.
