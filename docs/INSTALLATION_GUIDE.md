# Installation Guide — BNBC ULTIMATE PRO

## System Requirements
- **Operating System:** Windows 10 (64-bit, build 1809+) or Windows 11 (64-bit)
- **Runtime:** Bundled self-contained .NET 8 Windows Desktop runtime (no separate .NET installation required)
- **Structural Software (for live model integration):** CSI ETABS 22 (64-bit) installed locally

## Step-by-Step Installation
1. Download `BNBC_Ultimate_Pro_Setup.exe` from GitHub Actions Artifacts or GitHub Releases.
2. Double-click `BNBC_Ultimate_Pro_Setup.exe`. If Windows SmartScreen prompts because the installer is self-built, click **More info** -> **Run anyway**.
3. Follow the setup wizard:
   - Default installation folder: `C:\Program Files\BNBC ULTIMATE PRO`
   - Optionally check **Create a desktop shortcut**.
4. Click **Install** and then **Finish** to launch **BNBC ULTIMATE PRO**.

## Connecting to ETABS 22
1. Open your structural model in **CSI ETABS 22** first.
2. In **BNBC ULTIMATE PRO**, click **Detect & Connect ETABS 22** in the top header bar (or open **Tab 5: ETABS 22 Automation & Reactions**).
3. Verify that `ETABSv1.dll` is detected and click **Connect Active Model**.
4. Always click **Preview Changes** before clicking **Confirm & Push to ETABS 22**.
