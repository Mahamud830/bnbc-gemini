# Developer & CI/CD Build Guide

## Solution Structure
- `BnbcUltimatePro.sln` — Root Visual Studio / .NET 8 solution
- `src/BnbcUltimatePro.Core` — BNBC 2020 Part 6 database, seismic/wind/gravity/wall calculators, load combination generator, compliance engine, and report generator
- `src/BnbcUltimatePro.EtabsIntegration` — Runtime COM & `ETABSv1.dll` late-binding connector for ETABS 22
- `src/BnbcUltimatePro.Desktop` — Windows Presentation Foundation (WPF) desktop application
- `tests/BnbcUltimatePro.Tests` — Automated xUnit verification suite
- `installer/BNBC_Ultimate_Pro.iss` — Inno Setup 6 installer script producing `BNBC_Ultimate_Pro_Setup.exe`
- `.github/workflows/windows-build.yml` — Automated GitHub Actions Windows build, test, publish, and installer pipeline

## Local Command-Line Build (Windows)
```powershell
dotnet restore BnbcUltimatePro.sln
dotnet build BnbcUltimatePro.sln --configuration Release
dotnet test tests/BnbcUltimatePro.Tests/BnbcUltimatePro.Tests.csproj --configuration Release
dotnet publish src/BnbcUltimatePro.Desktop/BnbcUltimatePro.Desktop.csproj -c Release -r win-x64 --self-contained true -o publish/win-x64
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\BNBC_Ultimate_Pro.iss
```
