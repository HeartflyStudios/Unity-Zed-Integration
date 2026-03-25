# Unity → Zed Editor Integration

**Status: Experimental / Work in Progress**
> **Disclaimer:** This project is experimental, involved a  fair bit of "vibe-coding" (particularly around the rust side but honestly everywhere) and in general is just a toy project. ***However...***  
> 
> ...It is heavily inspired by the work of [SuperJura](https://github.com/SuperJura/unity-zed-integration) and by extension [Maligan](https://github.com/Maligan/unity-zed). Many thanks to them for giving me a starting point and sorry I didn't just contribute to your projects:pensive:.  
I started working on this project purely because I wanted to improve my knowledge of tooling such as LSP's and Debuggers - in particular those that apply to Unity game development and the .NET SDK.   
If I had just contributed and not built from scratch my brain would have taken a lot longer to understand everything, and I am too disorganized and novelty stimulated to be a good contributor (or maintainer for that matter). That said, if [@SuperJura](https://github.com/SuperJura) and/or [@Maligan](https://github.com/Maligan) want to hit me up to merge projects, I'm down for that now that I understand it a bit more.  
> 
> P.S. I also really wanted to *not* use Microsoft's VS Code and/or Visual Studio:unamused:.

## What is it?
A simple tooling project to use Zed code editor with Unity projects.

## What Works:smile:  
✅ Use Zed as external code editor in Unity with familiar UX (Preferences > External Tools > External Script Editor).  
✅ Auto-generate .csproj/.sln files.  
✅ Log streaming to terminal (manual proxy on port 12345).  
✅ AssetDatabase auto-refresh on editor focus.  

## What Doesn't Work Yet:sad:  
🚫 Logs display in external terminal, not in Zed UI.  
🚫 Double-clicking Unity Debug logs focus Zed - but don't navigate to correct file/line/
🚫 Any build script for Linux or MacOS. Also, I haven't tested outside of Windows... *sorry*.
🚫 I didn't include any cli arguments to change the port on the proxy... woops. You'll have to edit `./zed-unity-extension/proxy/src/main.rs` if Port: 12345 isn't good enough for you:laughing:

## Installation

### 1. Add Unity Package
**Method 1** manually:
- Copy `Packages/com.heartflystudios.zededitor` to your Unity project's `Packages/` folder.
- Restart Unity Editor  

**Method 2** UPM as git package:
- Open your Unity Project.
- Go to Window > Package Manager.
- Click the + (plus) icon in the top-left corner.
- Select Add package from git URL...
- Paste this URL: https://github.com/HeartflyStudios/Unity-Zed-Integration.git?path=/Packages/com.heartflystudios.zededitor 

### 2. Build Zed Extension Proxy
**Windows**

```powershell
.\zed-unity-extension\build.ps1
```
This builds both the WASM extension and native proxy if you are running on Windows.

*Linux and MacOS friends will have to manually build both*  
*Here's some instructions but you will know better than I*  

**Linux/MacOS**
- Open terminal in `/zed-unity-extension/proxy` and `cargo build --release && export UNITY_ZED_PROXY_PATH="$(pwd)/target/release/unity-zed-proxy"`

Or take the risk on my bash scripting, by...

- Opening terminal in root of entire project (`Unity-Zed-Integration`).  
- Enter the following...  

```bash 
cd ./zed-unity-extension/proxy && \
cargo build --release && \
export UNITY_ZED_PROXY_PATH="$(cd "$(dirname "./target/release/unity-zed-proxy")" && pwd)/$(basename "./target/release/unity-zed-proxy")"
```  

### 3. Configure Unity
- Restart Unity Editor if you haven't since adding the package.
- Get rid of your old .csproj, .sln, .slnx files in your Unity project folder. Delete or move - they just need to leave.
- In the Unity Editor:
    - Edit → Preferences → External Tools → Select "Zed Editor" 
    - Select the assemblies you want project files for as usual and click generate.
- Done! Either launch Zed by double clicking a script or by opening the Unity project at the root (where all the .csproj files have been generated).

## Usage

### Editing Code, Analyzers and LSP
It might take a while depending on which selections you made in the generate project files stage. But, then, you should see all the squigglies f@*k off and you should start getting proper "IntelliSense" style help that is actually relevant to Unity project code.

### Proxy Usage (look, it's barely working but that's the point of this project)

### 1. Start the Proxy (via a terminal in Zed preferably)

**Windows**
```powershell
& $env:UNITY_ZED_PROXY_PATH
```  
**Linux/MacOS**
```bash
"$UNITY_ZED_PROXY_PATH"
```

### 2. Make a .cs script change
Something that would cause an error or warning, otherwise you won't see much except connection status.

### 3. See Unity Logs in Zed Terminal
Logs will stream to the terminal window with timestamps and error levels.

### 3. Click Log Entries (Limited)
Clicking a log will focus Zed, but won't navigate to the file/line yet.

## Project File Generation Triggers
Unity automatically generates .csproj and .sln files when:
- You open Unity
- .cs files change
- .asmdef files change
- .asmref files change

## Known Issues

- **Manual proxy startup required**: Run the proxy executable manually before logging starts
- **No file navigation from Unity Log**: Clicking logs focuses Zed but doesn't open the correct file at the right line
- **Noisy Proxt Log**: It's got too many connection messsages - w.i.p.
- **Experimental status**: This is a hobby project, use at your own risk

## Troubleshooting

- Permission Denied: If the Rust proxy fails to start, ensure the binary has execution permissions.
- Port Conflict: If Unity fails to start the ZedLogStreamer, ensure port 12345 isn't being used by another application and your firewall is allowing loopback for the port.
- LSP Spam: If you still see "Restoring (0%)..." progress throbber in Zed a lot after like ~5 mins of working - close Zed, delete your Unity project obj and temp folders and restart Unity and Zed.  

## Development

### Building the Extension
```powershell
.\zed-unity-extension\build.ps1
```

### Building Just the Proxy
```powershell
cd proxy
cargo build --release
```

### Building Just the Extension
```powershell
cargo build --release --target wasm32-wasip1
```

## Contributing

This is an experimental project. Feel free to fork and experiment!

## License

MIT License - see LICENSE file for details.
