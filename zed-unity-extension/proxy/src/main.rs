use anyhow::{Context, Result};
use serde::Deserialize;
use std::io::{BufRead, BufReader};
use std::net::TcpStream;
use std::thread;
use std::time::Duration;

#[derive(Debug, Deserialize)]
struct UnityLogEntry {
    time: String,
    #[serde(rename = "type")]
    log_type: String,
    message: String,
    file: String,
    line: i32,
}

fn main() -> Result<()> {
    let unity_addr = "127.0.0.1:12345";

    eprintln!("[Unity-Zed] Starting Unity log listener...");
    eprintln!("[Unity-Zed] Connecting to Unity at: {}", unity_addr);

    let mut retry_count = 0;

    loop {
        eprintln!(
            "[Unity-Zed] Attempting to connect... (retry {})",
            retry_count + 1
        );

        match connect_and_stream(unity_addr) {
            Ok(_) => {
                // Connection successful, reset retry state
                retry_count = 0;
                eprintln!("[Unity-Zed] Connection closed, will retry...");
            }
            Err(e) => {
                eprintln!("[Unity-Zed] Connection failed: {}", e);

                retry_count += 1;
                let delay = Duration::from_millis(
                    1000 * 2_u64.pow(retry_count.min(5).try_into().unwrap_or(5)),
                );
                eprintln!("[Unity-Zed] Retrying in {} seconds...", delay.as_secs());
                thread::sleep(delay);
            }
        }
    }
}

fn connect_and_stream(addr: &str) -> Result<()> {
    let parsed_addr = addr.parse()?;
    let stream = TcpStream::connect_timeout(&parsed_addr, Duration::from_secs(5))
        .context("Connection timeout")?;

    println!("[Unity-Zed] ✓ Connected to Unity - Streaming logs...");
    eprintln!("[Unity-Zed] ✓ Connected successfully!");

    let reader = BufReader::new(stream);

    for line_result in reader.lines() {
        let raw_json = match line_result {
            Ok(l) => l,
            Err(e) => {
                eprintln!("[Unity-Zed] Error reading line: {}", e);
                return Err(e.into());
            }
        };

        if let Ok(log) = serde_json::from_str::<UnityLogEntry>(&raw_json) {
            print_log(&log);
        } else {
            eprintln!("[Unity-Zed] Failed to parse log: {}", raw_json);
        }
    }

    Ok(())
}

fn print_log(log: &UnityLogEntry) {
    let (icon, level) = match log.log_type.as_str() {
        "Error" | "Exception" | "Assert" => ("❌", "ERROR"),
        "Warning" => ("⚠️", "WARN"),
        _ => ("ℹ️", "INFO"),
    };

    println!("[{}] {}", icon, level);
    println!("  Timestamp: {}", log.time);
    println!("  Message: {}", log.message);

    if !log.file.is_empty() {
        println!("  Location: {} line {}", log.file, log.line);
    }

    println!(); // Blank line for readability
}
