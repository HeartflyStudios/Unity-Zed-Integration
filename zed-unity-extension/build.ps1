# --- Configuration ---
$ErrorActionPreference = "Stop"
$LogPrefix = "[BUILDER]"
$RootDir = Get-Location

Write-Host "$LogPrefix Starting build for Unity-Zed Integration..." -ForegroundColor Cyan

# Check if Rust/Cargo is installed
if (-not (Get-Command cargo -ErrorAction SilentlyContinue)) {
    Write-Error "Cargo (Rust) not found in PATH. Please install Rust from https://rustup.rs/"
    exit 1
}

# 1. Build the Native Proxy (Windows)
Write-Host "$LogPrefix Building Native Proxy..." -ForegroundColor Yellow
Set-Location "$RootDir/proxy"
cargo build --release
if ($LASTEXITCODE -ne 0) {
    Write-Error "Proxy build failed with exit code $LASTEXITCODE"
    Set-Location $RootDir
    exit $LASTEXITCODE
}
Set-Location $RootDir

# Explicitly find the EXE inside the proxy's local target
$ProxyExePath = Join-Path $RootDir "proxy\target\release\unity-zed-proxy.exe"
if (-not (Test-Path $ProxyExePath)) {
    Write-Error "Proxy executable not found at: $ProxyExePath"
    Set-Location $RootDir
    exit 1
}
$ProxyExe = Get-Item $ProxyExePath
$FullProxyPath = $ProxyExe.FullName
Write-Host "$LogPrefix Proxy built successfully: $FullProxyPath" -ForegroundColor Green
Set-Location $RootDir

# 2. Build the WASM Extension (Zed)
Write-Host "$LogPrefix Building WASM Extension..." -ForegroundColor Yellow
Set-Location $RootDir
cargo build --release --target wasm32-wasip1
if ($LASTEXITCODE -ne 0) {
    Write-Error "Extension build failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

# Verify WASM file was created
$WasmFile = Join-Path $RootDir "target\wasm32-wasip1\release\unity_zed_extension.wasm"
if (-not (Test-Path $WasmFile)) {
    Write-Error "WASM file not found at: $WasmFile"
    exit 1
}
Write-Host "$LogPrefix WASM Extension built successfully: $WasmFile" -ForegroundColor Green

# 3. Set/Update Environment Variable
Write-Host "$LogPrefix Updating UNITY_ZED_PROXY_PATH..." -ForegroundColor Green
try {
    [Environment]::SetEnvironmentVariable("UNITY_ZED_PROXY_PATH", $FullProxyPath, "User")
    $env:UNITY_ZED_PROXY_PATH = $FullProxyPath
    Write-Host "$LogPrefix Environment variable set successfully" -ForegroundColor Green
} catch {
    Write-Warning "Failed to set environment variable (may require admin privileges): $_"
    Write-Host "You can manually set UNITY_ZED_PROXY_PATH to: $FullProxyPath" -ForegroundColor Yellow
}

Write-Host "`n=== Build Summary ===" -ForegroundColor Cyan
Write-Host "[OK] Proxy Executable: $FullProxyPath" -ForegroundColor Green
Write-Host "[OK] WASM Extension: $(Join-Path $RootDir 'target\wasm32-wasip1\release\unity_zed_extension.wasm')" -ForegroundColor Green
Write-Host "`nNext Steps:" -ForegroundColor Yellow
Write-Host "1. In Zed, press Ctrl+Shift+P (Cmd+Shift+P on Mac)" -ForegroundColor White
Write-Host "2. Type 'Zed: Install Dev Extensions' and select it" -ForegroundColor White
Write-Host "3. Navigate to and select the folder: $RootDir" -ForegroundColor White
Write-Host "4. Reload Zed or restart the extension" -ForegroundColor White
Write-Host "`nNote: The extension will use the proxy at: $FullProxyPath" -ForegroundColor Gray
