# Zed Editor (Package For Unity)

*Zed* (the actual editor) was built by much of the team behind such succesful and admired projects as *Atom* and *Electron*.  
It's super customizable, open source, and built with Rust (which the other nerds on the internet told me is the dopest programming language since the ENIAC programmers were dropping fully-sick calculations in a basement... or something).  

You can download Zed [here.](https://zed.dev/)  
Install the C# language support by looking at the information [here](https://zed.dev/docs/languages/csharp) or just by installing the C# Extension in Zed (extension repo is [here](https://github.com/zed-extensions/csharp)).

## About this project 

Cursed squigglies appeared to tell me I wasn't, "coding good", and to be honest I am usually not, "coding good", so that is a fair assessment.  
That said, a lot of things the Roslyn LSP and Zed editor were telling me were "bad" (via annoying squigglies) were simply because the expectation of these tools is that I was writing C# code.  
But, I wasn't.  
I was writing **Unity C# Code**.

And **Unity C# Code** is.. *unique*.

Unity is in a transition towards getting onto the current CoreCLR and I have done my best to account for that but things will certainly break because Unity is actively making pretty significant API changes.
This package uses Roslyn's LSP and *not* OmniSharp, and assumes your Unity project settings are configured to use the .NET Standard 2.1 APIs - effectively it tries to pretend Unity projects are mostly normal (albeit older) .NET SDK projects.

So.
Switch to Zed and use this package to make it play nice with your Unity development endeavours.  

## Technical details
### Requirements

This version of the Zed Editor package is compatible with the following versions of the Unity Editor:

* Unity 6 (6000.0 series) and later (yeah!)
* 2023.2.0f1 to 2023.2.22f1 (probably)
* 2021.2.0f1 to 2023.1.0f1 (maybe..?)

### Dependencies

This version of the Zed Editor package requires these packages installed via UPM (Unity Package Manager):

* com.unity.nuget.newtonsoft-json (v3.0.0+)
> com.unity.nuget.newtonsoft-json:3.0.0 or above is likely already in your Unity project as it is required by quite a few internal Unity packages.

I have tried to avoid any dependencies, including the Unity packages: Visual Studio Editor and/or JetBrains Rider Editor. All you need is Newtonsoft.Json but it does have to be installed via UPM - for now...



## Package contents
### Project Folder Structure

- Zed Editor (root)
    - Analyzers
    - Documentation
    - Editor
    - Tests
        - Editor


The following table describes the project folders:

|**Folder**|**Description**|
|---|---|
|Zed Editor|Project root.|
|./Analyzers|Contains the required Roslyn Analyzers.|
|./Documentation|Where this document lives.|
|./Editor|Contains all the required C# scripts|
|./Tests|Contains tests (when I can be bothered to write some).|
|`./Tests/Editor`|Editor tests|

### Included Files List
This table contains an exhaustive list of files and a summary of their purpose. 
If you download something that isn't listed here then something is not right:

|File Name|Descriptions|
|---|---|
|`package.json`|Unity-required json file for packages.|
|`README.md`|Contains instructions.|
|`CHANGELOG.md`|Contains a log of package changes.|
|`ZedEditor.md`|This document.|
|`Microsoft.Unity.Analyzers.dll`|The .dll that contains Roslyn Analyzer rules for Unity C# projects. Source is [here.](https://github.com/microsoft/Microsoft.Unity.Analyzers)|
|`HFS.ZedEditor.asmdef`|Assembly Definition file for the package.|
|`ZedCodeEditor.cs`|The "entry-point" for the package. Implements `Unity.CodeEditor.IExternalCodeEditor`.|
|`ZedProjectGenerator`|Generates the .sln, .csproj and .Directory.Build.props files for Zed/Roslyn to use.|
|`ZedPathLocator`|Finds and provides the path to the Zed applicaion on Windows, MacOS and Linux|
|`ZedLogStreamer`|Handles connection for stream Unity Editor logs to Zed.|
|`ZedLogEntry`|Defines the ZedLogEntry struct.|



## Document revision history
Changes made to this document:

|Date|Reason|
|---|---|
|Mar 24, 2026|Document created. Matches package version 0.9.0|
