# Repository instructions

- LayoutFlag is a Windows-only .NET 8 WinForms tray app (`LayoutFlag/`); tests live in `LayoutFlag.Tests/`. There is no solution or workspace file.
- Run tests with `dotnet test LayoutFlag.Tests/LayoutFlag.Tests.csproj`. To focus a test class, add `--filter "FullyQualifiedName~ClassName"`; tests use xUnit.
- Publish the app from the repository root with `dotnet publish LayoutFlag/LayoutFlag.csproj -c Release -p:PublishSingleFile=true --self-contained false -o publish`. This produces a framework-dependent single-file executable (requires the .NET runtime); use self-contained publishing when a standalone runtime is required.
- The Inno Setup script is `installer/LayoutFlag.iss`; its source expects the published executable under `installer/publish/`. The script's documented compiler command is `ISCC.exe installer\LayoutFlag.iss`.
- App settings are persisted as JSON under `%AppData%\LayoutFlag\settings.json`; `AppSettings.Load(string)` / `Save(string)` allow tests to use isolated paths.
- `TrayAppContext` owns the tray menu, badge windows, and layout polling. Keep layout/geometry/placement logic in the corresponding focused classes, which have unit tests.
