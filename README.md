# InputFlow

**A private Windows keyboard and cursor statistics application.**

InputFlow gives you an engaging view of your own keyboard activity and estimated cursor travel distance — polished, immersive, and privacy-respecting.

![InputFlow Dashboard](https://img.shields.io/badge/Status-Building-blue)
![License](https://img.shields.io/badge/License-MIT-green)
![Platform](https://img.shields.io/badge/Platform-Windows_11-orange)

## Features

- **Keyboard Tracking**: Counts initial key-press transitions (modifiers, nav keys, all ordinary keys). Excludes auto-repeat. Does NOT record what you type.
- **Cursor Distance**: Estimates pointer travel across calibrated displays in cm, m, km, or Auto unit display.
- **30-Second Updates**: Aggregated statistics refresh every 30 seconds.
- **Privacy First**: No text output, no clipboard reading, no screenshots, no window titles. All data stays on your device.
- **Offline-First**: Works without any account, network connection, or cloud sync.
- **Display Calibration**: Per-display calibration with manual entry and ruler-based methods.
- **System Tray**: Minimize to tray option for background tracking.

## Supported Platforms

| Platform | Status |
|----------|--------|
| Windows 11 (build 22621+) x64 | Fully supported |
| Windows 11 ARM64 | Planned — pending native dependency validation |
| Windows 10 (build 19041+) | Assessment needed |

**Supported OS**: Windows 11 version 22H2 (build 22621) and later, x64 architecture.  
ARM64 support planned after validating all native dependencies.

## Architecture

```
InputFlow/              ← WinUI 3 desktop app (XAML pages, viewmodels, app lifecycle)
├── Views/
│   ├── WelcomeView     Onboarding with consent and toggle choices
│   ├── DashboardView   Main odometer + keyboard counter
│   ├── HistoryView     Date-filtered daily summaries  
│   ├── CalibrationView Per-display physical-size calibration
│   ├── PrivacyView     Data controls, export, deletion
│   └── SettingsView    Units, theme, startup behavior
├── Themes/
│   ├── Colors.xaml     Brand palette (#0B1020 bg, #38BDF8 cyan accent)
│   └── Styles.xaml     Typography and common styles
└── app.manifest        DPI awareness, execution level, Windows 11 support

InputFlow.Core/         ← Input capture, measurement, storage services
├── Capture/            ← RawInputCapturer, PointerCapturer
├── Measurement/        ← DisplayTopology, calibration logic
├── Aggregation/        ← SessionManager (30s reporting cadence)
└── Storage/            ← AggregateStore (SQLCipher-encrypted SQLite), SettingsStore
    └── ViewModels/     ← Dashboard, History, Calibration, Settings VMs

InputFlow.Shared/       ← Domain models (calibration, settings, metrics)
InputFlow.Tests/        ← xUnit acceptance tests for measurement & privacy
```

## Building

### Prerequisites

1. **Windows 11** (build 22621+) with .NET 10 SDK (10.0.400+)
2. Visual Studio 2022 17.12+ or VS Code with C# Dev Kit
3. Git
4. Microsoft Windows App SDK 1.7 runtime (included with .NET 10 SDK workloads)

### Build Commands

```bash
cd InputFlow

# Restore NuGet packages
dotnet restore

# Build in Debug mode (for development)
dotnet build --configuration Debug

# Build in Release mode
dotnet build --configuration Release

# Run the application directly
dotnet run

# Publish for distribution
dotnet publish -c Release -r win-x64 --self-contained false -o ./dist
```

The output will be in `./dist/` — run `InputFlow.exe` to launch.

### Installing on a Clean Machine

1. Install the [.NET 10 Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (if not using self-contained).
2. Run `dotnet install InputFlow.dll` or copy from the publish output and run `InputFlow.exe`.
3. MSIX packaging instructions are in [INSTALL-GUIDE.md](./INSTALL-GUIDE.md).

## Measurement Definitions

- **Key Presses**: Observed initial key-down transitions. Counts modifiers, navigation keys, ordinary keys. Excludes OS auto-repeat and release events. Does NOT count characters typed, words, or productivity metrics.
- **Cursor Distance**: Accumulated estimated distance traveled by the pointer across calibrated display surfaces. Formula: `dx_cm = Δx × displayWidthCm / pixelWidth`, `dy_cm = Δy × displayHeightCm / pixelHeight`, `segment = √(dx² + dy²)`. Sum of all valid segments. An out-and-back path counts both legs.
- **Units**: cm (centimeters), m (meters, /100), km (kilometers, /100000). Auto: cm below 100cm, m below 1000m, km at/above 1000m.

## Privacy Notice

> **Counts key presses without recording what you type.**

- Only aggregate counts are stored (key presses, clicks, scroll events, distance)
- No individual key names, scan codes, virtual-key codes, or typed characters
- No clipboard contents, screenshots, window titles, document names, URLs, or browser history  
- Pointer position segments exist only briefly in memory for distance calculation; never persisted as coordinate trails
- Database uses SQLCipher encryption at rest with per-user DPAPI-derived keys
- All data is local; no analytics, crash reporting, or cloud features
- Runs at standard user privileges — no admin access required

See [PRIVACY.md](./PRIVACY.md) for the complete privacy policy.

## Privacy Invariants (Verified by Tests)

| Test | Result |
|------|--------|
| 100 initial key presses | Count increases by 100 |
| Hold one key across auto-repeat | Main count increases once |
| Two keyboards | Transient states remain independent |
| Calibrated conversion (100px → 5cm) | Verified in MeasurementTests |
| Out-and-back path | Both legs counted |
| Unit conversions (100cm=1m, 100000cm=1km) | Verified |
| Privacy: no key names/sequences in DB/logs/exports | Verified by PrivacyTests |

## Known Limitations

- Does not count on-screen keyboard input, IME composed events, macros, or virtual device activity  
- Distance is an estimate based on display pixel calibration; actual physical pointer paths may differ due to sampling rate
- Cannot distinguish between human and automated cursor movement
- Fast pointer movement between samples may have sub-pixel inaccuracy
- Game cursor captures (exclusive mouse control) are not tracked
- Remote desktop and RDP sessions have incomplete coverage

## Third-Party Dependencies

| Package | Purpose | License |
|---------|---------|---------|
| Microsoft.WindowsAppSDK 1.7.4 | WinUI 3, Windows desktop SDK | MIT |
| CommunityToolkit.Mvvm 8.2.2 | MVVM framework (ObservableObject, RelayCommand) | MIT |
| Microsoft.Data.Sqlite.Core 9.0.6 | SQLite database access | PDDL-1.0 / BSD variants |
| SQLitePCLRaw.bundle_e_sqlcipher 2.1.10 | Encrypted SQLite storage | Apache 2.0 |
| Serilog 4.2.0 + Sink.File 6.0.0 | Structured local logging | Apache 2.0 |

## License

MIT License — see [LICENSE](./LICENSE)
