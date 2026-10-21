# InputFlow — Dependency Inventory

| # | Package | Version | Purpose | License | Source |
|---|---------|---------|---------|---------|--------|
| 1 | `Microsoft.WindowsAppSDK` | 1.7.4 | WinUI 3 UI framework, Windows composition, Mica material,NavigationView, Dialogs | MIT | NuGet |
| 2 | `CommunityToolkit.Mvvm` | 8.2.2 | MVVM helpers: `[ObservableProperty]`, `[RelayCommand]`, `INotifyPropertyChanged` | MIT | NuGet |
| 3 | `Microsoft.Data.Sqlite.Core` | 9.0.6 | ADO.NET SQLite provider for local aggregate data storage | PDDL-1.0 / BSD | NuGet |
| 4 | `SQLitePCLRaw.bundle_e_sqlcipher` | 2.1.10 | SQLCipher-backed encrypted SQLite (ESQLCIPHER) for storing sensitive aggregates | Apache 2.0 | NuGet |
| 5 | `Serilog` | 4.2.0 | Structured local logging framework | Apache 2.0 | NuGet |
| 6 | `Serilog.Sinks.File` | 6.0.0 | File sink for Serilog (structured JSON logs, no user input data) | Apache 2.0 | NuGet |
| 7 | `Microsoft.Windows.CsWin32` | 0.3.106 | Source-generated Win32 P/Invoke bindings (Raw Input API, GetPhysicalCursorPos) | MIT | NuGet |
| 8 | `Microsoft.NET.Test.Sdk` | 17.12.0 | xUnit test host and runner infrastructure | MIT | NuGet |
| 9 | `xunit` | 2.9.3 | Unit test framework with assertions and theory support | Apache 2.0 | NuGet |
| 10 | `xunit.runner.visualstudio` | 3.0.2 | VS/CLI test runner for xUnit | Apache 2.0 | NuGet |
| 11 | `Moq` | 4.20.72 | Mocking framework for unit tests | BSD-3 | NuGet |

## Native Dependencies (Redistributed)

| Component | Source | License | Notes |
|-----------|--------|---------|-------|
| SQLCipher native library | SQLitePCLRaw bundle | Apache 2.0 / OpenSSL | Embedded in NuGet package |
| .NET 10 Runtime | Microsoft | MIT | Required for framework-dependent deployment |
| Windows App SDK native DLLs | Microsoft | MIT | Ships with Windows 11; separate install on Win10 |

## Third-Party Notices (Summary)

All licenses are permissive (MIT, Apache 2.0, BSD). No copyleft (GPL/LGPL) dependencies are included.
Full license texts are available at the package source URLs above.
