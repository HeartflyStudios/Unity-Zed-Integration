*AI Generated*
# Existing Code Analysis

## Unity Package: com.heartflystudios.zededitor

### Files
- `ZedLogStreamer.cs` - **Functional** - TCP log server
- `ZedLogEntry.cs` - **Functional** - Log entry struct
- `ZedCodeEditor.cs` - **Functional** - Implements IExternalCodeEditor
- `ZedPathLocator.cs` - **Functional** - Cross-platform Zed executable locator
- `ZedProjectGenerator.cs` - **Functional** - Generates .csproj/.sln and .zed/settings.json

### ZedLogStreamer.cs (WORKING)

**Purpose:** Streams Unity Editor logs to connected clients via TCP

**Implementation:**
- Port: 12345 (localhost)
- Listens for TCP connections
- Hooks into `Application.logMessageReceivedThreaded`
- Queue-based: `ConcurrentQueue<string>`

**Log Entry Structure (JSON):**
```json
{
  "time": "HH:mm:ss",
  "type": "LogType",
  "message": "message content",
  "stack": "stack trace",
  "file": "path/to/file.cs",
  "line": 42
}
```

**Key Methods:**
```csharp
- StartServer() // TCP listener loop
- HandleLog(string, string, LogType) // Log hook
- ExtractFilePath(string) // Regex: \(at (.+):(\d+)\)
- ExtractLineNumber(string) // Extracts line number
- Cleanup() // Stops listener, unsubscribes from events
```

**Socket Configuration:**
```csharp
_listener.Server.SetSocketOption(
    SocketOptionLevel.Socket,
    SocketOptionName.ReuseAddress,
    true
);
```
Critical for allowing rebind after Unity script reloads.

**Lifecycle:**
- `[InitializeOnLoad]` - Auto-starts on Unity load
- Cleanup on: `AppDomain.CurrentDomain.DomainUnload`, `EditorApplication.quitting`

### ZedLogEntry.cs

**Structure:**
```csharp
public struct ZedLogEntry {
    public string time;
    public string type;
    public string message;
    public string stack;
    public string file;
    public int line;
}
```
Simple POCO for JSON serialization via Newtonsoft.Json.

### ZedCodeEditor.cs

**Purpose:** Implements Unity's `IExternalCodeEditor` interface to integrate Zed as an external code editor

**Key Features:**
- Registers Zed as external code editor on Unity load
- Detects Zed installation via `ZedPathLocator`
- Opens Zed with specific file and line number support
- Generates .csproj/.sln files on asset changes
- Provides generation settings in Unity preferences

**Key Methods:**
```csharp
- Initialize(string) // Sets up project generator with project directory
- OpenProject(string, int, int) // Launches Zed with optional line/column
- SyncAll() // Full regeneration of all project files
- SyncIfNeeded(string[], ...) // Incremental sync on asset changes
- OnGUI() // Renders Unity preferences UI for project generation flags
- TryGetInstallationForPath(string, out Installation) // Validates Zed installation
```

**Project Generation Flags:**
```csharp
[Flags]
public enum ProjectGenerationFlag {
    None = 0,
    Local = 1 << 0,           // Local packages
    Embedded = 1 << 1,        // Embedded packages
    OpenedPackages = 1 << 2,  // Registry packages
    Git = 1 << 3,             // Git packages
    BuiltInPackages = 1 << 4, // Built-in packages
    Unknown = 1 << 5,         // Unknown sources
    PlayerAssemblies = 1 << 6,// Player projects
    LocalTarBall = 1 << 7,    // Local tarball packages
    All = ~0
}
```

**Zed Invocation:**
```csharp
zed "<project_dir>" "<file>:<line>:<column>"
```
Supports opening with file path only, file:line, or file:line:column

**Asset Change Detection:**
Filters for .cs, .asmdef, and .asmref file changes
Performs structural updates (solution regen) when files are added/moved/deleted or asmdefs change

### ZedPathLocator.cs

**Purpose:** Finds Zed executable across Windows, macOS, and Linux platforms

**Windows Detection:**
1. Checks Registry key: `Software\Classes\zed\DefaultIcon`
2. Checks Local AppData: `%LOCALAPPDATA%\zed\bin\zed.exe`
3. Fallback: `zed.exe` (from PATH)

**macOS Detection:**
1. Checks standard path: `/Applications/Zed.app/Contents/MacOS/zed`
2. Checks user path: `~/Applications/Zed.app/Contents/MacOS/zed`
3. Fallback: `zed` (from PATH)

**Linux Detection:**
1. Uses `which zed` command to find binary
2. Fallback: `zed` (from PATH)

### ZedProjectGenerator.cs

**Purpose:** Generates Visual Studio-compatible .csproj and .sln files for Zed's Roslyn LSP integration

**Key Methods:**
```csharp
- GenerateAll(Assembly[], ProjectGenerationFlag) // Main entry point
- GenerateCsproj(Assembly, Assembly[]) // Creates .csproj for single assembly
- GenerateSolution(Assembly[]) // Creates .sln solution file
- GenerateZedSettings() // Creates .zed/settings.json
- GenerateDirectoryBuildProps() // Creates Directory.Build.props
- ShouldGenerate(Assembly, ProjectGenerationFlag) // Filters assemblies
- CleanupOrphanedProjects(List<string>) // Removes old .csproj files
- GetDeterministicGuid(string) // Generates consistent GUIDs
- GetAnalyzerPath() // Finds Unity analyzers
```

**Project File Generation:**
- Uses MSBuild SDK format (`Microsoft.NET.Sdk`)
- Absolute paths for source files and references
- Project references to other generated projects
- Assembly references to Unity engine DLLs
- Includes Unity analyzers if available
- Retries file writes (10 attempts) to handle Unity file locking

**Solution File Generation:**
- Old Visual Studio format (version 12.00) for maximum compatibility
- Configures Debug|Any CPU and Release|Any CPU platforms
- Build configurations for all projects

**Zed Settings Generation (.zed/settings.json):**
- Merges with existing settings if present
- Configures Roslyn LSP options:
  - `automaticWorkspaceInit: false` - Manual workspace initialization
  - `analyzeOpenDocumentsOnly: true/false` - Performance tuning
  - `enableRoslynAnalyzers: true` - Enable static analysis
- Configures file scan exclusions:
  - Unity-specific: `.meta`, `.dll`, `.asset`, `.prefab`, `.unity`
  - Generated files: `.csproj`, `.sln`, `.slnx`
  - Build directories: `Library/`, `Temp/`, `Logs/`, `Obj/`, `ProjectSettings/`
  - Config directories: `UIElementsSchema/`, `Directory.Build.props`

**Directory.Build.props:**
- Sets target framework to `netstandard2.1`
- C# language version `9.0`
- Disables NuGet restore
- Enables unsafe blocks
- Disables default compile items
- Ensures deterministic builds

## Zed Extension: zed-unity-extension

### Current State: MINIMAL IMPLEMENTATION

**Files:**
- `extension.toml` - Extension metadata (correct)
- `Cargo.toml` - Dependencies (correct)
- `src/lib.rs` - Basic extension structure with proxy startup
- `proxy/src/main.rs` - TCP client that connects to Unity
- `proxy/Cargo.toml` - Proxy dependencies
- `build.ps1` - Build script for both proxy and extension

### src/lib.rs

**Purpose:** Zed extension that starts the native proxy process

**Current Implementation:**
```rust
struct UnityLogExtension;

impl UnityLogExtension {
    fn start_proxy() -> Result<()> {
        let proxy_cmd = std::env::var("UNITY_ZED_PROXY_PATH")
            .with_context(|| "UNITY_ZED_PROXY_PATH environment variable not set")?;

        let mut command = zed::process::Command::new(&proxy_cmd);
        match command.output() {
            Ok(output) => { /* logs output */ }
            Err(e) => { /* logs error */ }
        }
        Ok(())
    }
}

impl zed::Extension for UnityLogExtension {
    fn new() -> Self { Self }
}

zed::register_extension!(UnityLogExtension);
```

**Issues:**
- `command.output()` blocks until process completion (can't run in background)
- No lifecycle management for the proxy process
- No error recovery or restart logic
- No UI integration

### proxy/src/main.rs

**Purpose:** Standalone TCP client that connects to Unity and streams logs

**Log Entry Structure:**
```rust
#[derive(Debug, Deserialize)]
struct UnityLogEntry {
    time: String,
    #[serde(rename = "type")]
    log_type: String,
    message: String,
    file: String,
    line: i32,
}
```

**Implementation:**
```rust
fn main() -> Result<()> {
    let unity_addr = "127.0.0.1:12345";
    loop {
        match connect_and_stream(unity_addr) {
            Ok(_) => retry_count = 0,
            Err(e) => {
                retry_count += 1;
                // Exponential backoff: 1s, 2s, 4s, 8s, 16s, 32s
                let delay = Duration::from_millis(
                    1000 * 2_u64.pow(retry_count.min(5).try_into().unwrap_or(5))
                );
                thread::sleep(delay);
            }
        }
    }
}

fn connect_and_stream(addr: &str) -> Result<()> {
    let stream = TcpStream::connect_timeout(&parsed_addr, Duration::from_secs(5))?;
    let reader = BufReader::new(stream);

    for line_result in reader.lines() {
        if let Ok(log) = serde_json::from_str::<UnityLogEntry>(&raw_json) {
            print_log(&log);
        }
    }
}
```

**Log Output Format:**
```
[❌] ERROR
  Timestamp: 14:23:45
  Message: NullReferenceException
  Location: Assets/Scripts/GameManager.cs line 42
```

**Features:**
- Auto-reconnection with exponential backoff (max 32s)
- JSON parsing with error handling
- Icon-based log level indicators (❌ ERROR, ⚠️ WARN, ℹ️ INFO)
- 5-second connection timeout
- Blank line separator for readability

### README Context

**Key Points:**
- Experimental project (not production ready)
- Inspired by SuperJura and Maligan projects
- Goal: Make Zed "first-class" for Unity development
- Proxy pattern mentioned for native binary

**Installation Requirements:**
1. Unity package via UPM or git
2. Zed extension (optional but recommended)
3. Build Rust proxy: `cargo build --release`
4. Binary must be in PATH or extension.toml updated with absolute path

**Features (with Zed Extension):**
- Stream Unity Debug Log to Zed interface - *Wishlist/Backlog*
- Notify via GUI about compilation progress - *Wishlist/Backlog*

## Issues Identified

### Unity Side
✅ **Working:**
- TCP server
- Log streaming
- JSON serialization
- Stack trace parsing
- Socket reuse configuration

**VERIFIED:** ZedCodeEditor.cs is fully functional
- Implements IExternalCodeEditor correctly
- Opens Zed with file/line/column support
- Performs incremental project file generation
- Provides Unity GUI solution gen preferences toggles
- Detects Zed installation via ZedPathLocator

**VERIFIED:** ZedPathLocator.cs is fully functional
- Windows: Registry + Local AppData detection
- macOS: Standard and user Applications folder
- Linux: `which` command lookup
- Proper fallback to PATH in all cases

**VERIFIED:** ZedProjectGenerator.cs is fully functional
- Generates .csproj files with MSBuild SDK format
- Generates .sln solution files
- Creates .zed/settings.json with LSP configuration
- Generates Directory.Build.props
- Handles Unity file locking with retries
- Cleans up orphaned .csproj files

### Zed Extension Side

**REVIEWED:** 
- zed-unity-extension is MINIMAL but FUNCTIONAL
- Extension builds successfully (WASM binary)
- Proxy builds successfully (native binary)
- Basic structure is in place
- Proxy connects to Unity and streams logs to stdout

### Integration Gaps
1. No file/line navigation from log entries
2. No compilation completion detection
3. Blocking `command.output()` prevents background proxy operation

## Next Steps for Rewrite

### High Priority
1. **Implement Background Process Management**
   - Research `zed::process` API for non-blocking process spawning
   - Call `start_proxy()` from extension lifecycle
   - Implement daemon pattern for proxy lifecycle
   - Add restart logic on proxy failure

2. **Add Zed UI Integration**
   - Research Zed Tasks API for terminal panel creation
   - Alternative: HTTP-based dashboard
   - Alternative: Stream to existing terminal
   - Display logs with syntax highlighting and filtering

3. **Implement File/Line Navigation**
   - Parse proxy output for file/line information
   - Create Zed slash command for navigation
   - Use `zed` CLI to open files at specific lines
   - Integrate with Zed's workspace API

### Medium Priority
4. **Unity Compilation Detection**
   - Identify Unity completion log patterns
   - Implement state machine for tracking compilation
   - Display notification to user when compilation completes
   - Show compilation progress in Zed UI

5. **Error Handling & UX Improvements**
   - Graceful proxy failure recovery
   - Connection status indicator in Zed
   - User feedback for common issues
   - Configuration options for proxy behavior
