*AI Generated*
# Zed Extension API Research (v0.7.0)

## Key Findings

### 1. Process Management

**Using Rust:**
```rust
use zed_extension_api::process::Command;

// Execute command and wait for completion
let mut command = Command::new("binary-name")
    .arg("--arg")
    .env("KEY", "value");

let output = command.output()?;
// Returns: Output { status, stdout, stderr }
```

**Limitation:** `output()` blocks until process completes - not suitable for streaming

**Solution:** Use proxy process pattern - native binary handles streaming, WASM manages lifecycle

### 2. HTTP Client

**Streaming (Critical for this project):**
```rust
use zed_extension_api::http_client::{fetch_stream, HttpRequest, HttpMethod};

let request = HttpRequest::new()
    .url("http://localhost:12345/logs")
    .method(HttpMethod::Get);

let stream = fetch_stream(&request)?;
for chunk in stream {
    // Process chunks as they arrive
}
```

**Non-streaming:**
```rust
use zed_extension_api::http_client::fetch;
let response = fetch(&request)?;
```

### 3. Worktree & Project

**Access current worktree:**
```rust
use zed_extension_api::Worktree;

let worktree = Worktree::current()?;
let root_path = worktree.root_path()?;
let content = worktree.read_text_file("path/to/file")?;
```

**Project information:**
```rust
use zed_extension_api::Project;

let project = Project::from_worktree(worktree)?;
```

### 4. Settings

**Access language settings:**
```rust
use zed_extension_api::settings::LanguageSettings;

let settings = LanguageSettings::get(LanguageServerId("language-id".to_string()))?;
```

### 5. Utility Functions

**File operations:**
```rust
use zed_extension_api::{download_file, make_file_executable, node_binary_path};

// Download from URL
download_file(url, save_path)?;

// Make executable
make_file_executable(path)?;

// Get Node.js binary path
let node = node_binary_path()?;
```

## API Limitations

### UI Capabilities
**NOT Available in v0.7.0:**
- Custom panels
- Throbbers/progress indicators
- Bottom dock UI elements
- Custom log viewers

**Workarounds:**
- Use terminal panel (native)
- HTTP dashboard (custom)
- Wait for future UI APIs?

### Process Monitoring
**Limitation:** Only supports completion-based execution
**Workaround:** Proxy process pattern

### File Operations
**Limited to:**
- Reading text files from worktree
- Basic utilities (download, make executable)
- No direct file system access outside worktree

## Extension Structure

### Basic Template
```rust
use zed_extension_api as zed;

struct MyExtension {
    // State fields
}

impl zed::Extension for MyExtension {
    fn new() -> Self {
        Self
    }
    
    // Hook methods available:
    // on_spawn() - Process spawning
    // on_start() - Extension start
    // etc.
}

zed::register_extension!(MyExtension);
```

### Extension.toml
```toml
id = "extension-id"
name = "Extension Name"
version = "0.1.0"
schema_version = 1
authors = ["Author Name"]
description = "Description"

[lib]
name = "crate_name"
```

### Cargo.toml
```toml
[package]
name = "crate-name"
version = "0.1.0"
edition = "2021"

[lib]
crate-type = ["cdylib"]

[dependencies]
zed_extension_api = "0.7.0"
```

## Async Capabilities

The extension trait supports async operations implicitly through the Wasmtime runtime's async support, but the current API surface appears synchronous. The `http_client::fetch_stream` is the primary async streaming mechanism available.

## Best Practices

1. **Minimize Dependencies** - Only use `zed_extension_api` and common well maintained crates.
3. **Error Handling** - Use `Result<T>` throughout
4. **Resource Cleanup** - Implement proper cleanup in extension lifecycle
5. **Synchronous APIs** - Most APIs are synchronous despite Wasmtime's async backend
