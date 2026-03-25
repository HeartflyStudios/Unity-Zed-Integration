use anyhow::{Context, Result};
use zed_extension_api as zed;

struct UnityLogExtension;

impl UnityLogExtension {
    // TODO: Implement background process spawning for proxy
    // Current zed_extension_api v0.7.0 doesn't support non-blocking process execution
    // Run proxy manually: .\zed-unity-extension\proxy\target\release\unity-zed-proxy.exe
    //
    // Future implementation should:
    // 1. Use proper lifecycle hooks from zed::Extension trait
    // 2. Spawn proxy in background without blocking
    // 3. Handle proxy lifecycle (start/stop/restart)
    // 4. Integrate proxy output with Zed UI

    #[allow(dead_code)]
    fn start_proxy() -> Result<()> {
        // Get proxy binary path from environment variable
        let proxy_cmd = std::env::var("UNITY_ZED_PROXY_PATH")
            .with_context(|| "UNITY_ZED_PROXY_PATH environment variable not set")?;

        eprintln!("[Unity-Zed] Starting proxy: {}", proxy_cmd);

        // Start proxy process
        // Note: This will wait for completion, which isn't ideal for long-running
        // We'll need to figure out proper background process management
        let mut command = zed::process::Command::new(&proxy_cmd);

        // BLOCKING: command.output() waits for process to complete
        // This cannot be used in extension startup without blocking the UI
        match command.output() {
            Ok(output) => {
                if let Some(code) = output.status {
                    eprintln!("[Unity-Zed] Proxy exited with code: {}", code);
                } else {
                    eprintln!("[Unity-Zed] Proxy exited without status");
                }

                if !output.stderr.is_empty() {
                    eprintln!(
                        "[Unity-Zed] Proxy stderr: {}",
                        String::from_utf8_lossy(&output.stderr)
                    );
                }
            }
            Err(e) => {
                eprintln!("[Unity-Zed] Failed to start proxy: {}", e);
                return Err(anyhow::anyhow!(e));
            }
        }

        Ok(())
    }
}

impl zed::Extension for UnityLogExtension {
    fn new() -> Self {
        Self
    }
}

zed::register_extension!(UnityLogExtension);
