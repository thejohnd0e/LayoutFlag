# Repository instructions

- LayoutFlag is a Windows-only .NET 8 WinForms tray app (`LayoutFlag/`); tests live in `LayoutFlag.Tests/`. There is no solution or workspace file.
- Run tests with `dotnet test LayoutFlag.Tests/LayoutFlag.Tests.csproj`. To focus a test class, add `--filter "FullyQualifiedName~ClassName"`; tests use xUnit.
- Publish a quick framework-dependent build from the repository root with `dotnet publish LayoutFlag/LayoutFlag.csproj -c Release -p:PublishSingleFile=true --self-contained false -o publish` (requires the .NET 8 runtime to run).
- The installer ships a self-contained build. Build it with `dotnet publish LayoutFlag/LayoutFlag.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o installer/publish`, then `ISCC.exe installer\LayoutFlag.iss` (output: `installer/LayoutFlag-Setup.exe`). The script is `installer/LayoutFlag.iss`.
- The version lives in three places that must be bumped together: `LayoutFlag/LayoutFlag.csproj`, `Strings.Version` in `LayoutFlag/Strings.cs`, and `MyAppVersion` in `installer/LayoutFlag.iss`.
- App settings are persisted as JSON under `%AppData%\LayoutFlag\settings.json`; `AppSettings.Load(string)` / `Save(string)` allow tests to use isolated paths.
- `TrayAppContext` owns the tray menu, badge windows, and layout polling. Keep layout/geometry/placement logic in the corresponding focused classes, which have unit tests.
