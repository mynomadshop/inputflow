# INSTALL-GUIDE.md

## Installing InputFlow on Windows 11

### Method 1: Direct Run (Quick Start)

After building or downloading a release:

1. Navigate to the `dist/` folder (or extracted release folder).
2. Double-click `InputFlow.exe`.
3. The application will launch with default dark theme settings.

**Requirements**: .NET 10 Desktop Runtime (x64) installed on your system.

### Method 2: MSIX Package (Store-style Installation)

```powershell
# Build an MSIX package
dotnet publish -c Release -r win-x64 ^
    -p:PublishProfile=Properties\PublishProfiles\Win10.msprops ^
    -p:GenerateAppxPackageOnBuild=true

# Install the generated .msix package
Add-AppxPackage .\dist\InputFlow_1.0.0.0_x64.msix
```

### Method 3: Self-Contained (No Runtime Required)

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./dist
```

The resulting `dist/` folder is fully self-contained and can be run on any Windows 11 machine without installing .NET separately.

### Uninstalling

- **MSIX**: Settings → Apps → InputFlow → Uninstall
- **Direct Run**: Delete the application folder and its data directory at `%LOCALAPPDATA%\InputFlow\`

## Troubleshooting

| Problem | Solution |
|---------|----------|
| App won't start, "runtime not found" | Install [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download) |
| Black screen / no UI | Ensure Windows 11 build 22621+ is installed; check GPU drivers |
| Data doesn't persist | Verify write permissions to `%LOCALAPPDATA%\InputFlow\` |
