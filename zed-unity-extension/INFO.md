*AI Generated: Believe at your peril.*
# Zed Extension for Unity

**Status: Non-Functional / Work in Progress**

This Zed extension is currently not functional due to blocking process management issues in zed_extension_api v0.7.0.

## Current State

The extension builds successfully and can be installed in Zed, but it cannot automatically start the Unity log proxy because:

1. `zed::process::Command::output()` blocks until the process completes
2. There is no non-blocking process spawning API available in v0.7.0
3. No lifecycle hooks are implemented to manage background processes

## Workaround

Run the proxy manually:

```powershell
.\zed-unity-extension\proxy\target\release\unity-zed-proxy.exe
```

This will:
- Connect to Unity Editor on localhost:12345
- Stream Unity logs to your terminal
- Display logs with timestamps and error levels

## Future Implementation

To make this extension fully functional, we need to:

1. Research newer versions of zed_extension_api for async process support
2. Implement proper lifecycle hooks for proxy management
3. Integrate proxy output with Zed's terminal/task system
4. Add file/line navigation from log entries

## Installation

While non-functional, you can still install it for development:

1. In Zed: Press `Ctrl+Shift+P` (Windows/Linux) or `Cmd+Shift+P` (macOS)
2. Select "Zed: Install Dev Extensions"
3. Navigate to and select the `zed-unity-extension` folder
4. Reload Zed or restart the extension

## Building

```powershell
.\build.ps1
```

This builds both the WASM extension and the native proxy.
