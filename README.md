# BNBC ULTIMATE PRO — BNBC 2020 & ETABS 22 Structural Suite

**BNBC ULTIMATE PRO** is a Windows 10/11 desktop engineering suite built in C# / .NET 8 (WPF) for structural analysis, load generation, code compliance verification, and live COM/API automation with **Computers and Structures ETABS 22** under the **Bangladesh National Building Code (BNBC 2020)**.

---

## Exact GitHub Upload & Automated Build Instructions

> **IMPORTANT:** Do **NOT** upload the ZIP file itself into your GitHub repository. You must **extract** the ZIP file first and upload the extracted files and folders directly into the **repository root** so GitHub Actions detects `.github/workflows/windows-build.yml`.

1. **Create a new GitHub repository**
   - Sign in to GitHub and click **New repository** (e.g., `bnbc-ultimate-pro`).
   - Do not initialize with a conflicting README or `.gitignore` (or overwrite them when uploading).
2. **Extract the ZIP file**
   - Right-click `BNBC_ULTIMATE_PRO_GITHUB_PACKAGE.zip` on your computer and choose **Extract All...**.
   - Open the extracted folder so you can see `BnbcUltimatePro.sln`, `src/`, `tests/`, `installer/`, `docs/`, and `.github/`.
3. **Upload all extracted files and folders to the repository root**
   - In your new GitHub repository, click **uploading an existing file** (or use Git CLI / GitHub Desktop).
   - Drag and drop **all extracted files and folders** (including the `.github` folder) into the repository root.
4. **Commit the files to the `main` branch**
   - Enter a commit message (e.g., `Initial commit of BNBC ULTIMATE PRO`) and click **Commit changes** to `main`.
5. **Open the Actions tab**
   - Click the **Actions** tab at the top of your GitHub repository.
6. **Select the Windows Build workflow**
   - In the left sidebar under *All workflows*, click **Windows Build & Installer Pipeline** (`.github/workflows/windows-build.yml`).
7. **Run the workflow if it does not start automatically**
   - Pushing to `main` triggers the workflow automatically.
   - To trigger manually (or create a tagged GitHub Release), click **Run workflow**, optionally enter a release version like `v1.0.0`, and click **Run workflow**.
8. **Wait for the build to finish**
   - The GitHub-hosted `windows-latest` runner will restore NuGet packages, compile the C# solution in `Release` mode, execute xUnit automated tests, publish the self-contained Windows x64 application, and compile `BNBC_Ultimate_Pro_Setup.exe` using Inno Setup.
9. **Open the completed workflow run**
   - Click the green checkmark run once completed.
10. **Download the installer from the Artifacts section**
    - Scroll down to **Artifacts** at the bottom of the workflow summary page and download **`BNBC_Ultimate_Pro_Setup_Windows_x64`**.
    - Extract the downloaded artifact ZIP to obtain **`BNBC_Ultimate_Pro_Setup.exe`**.
11. **Finding the installer if a GitHub Release was created**
    - If you entered a `release_version` (e.g., `v1.0.0`) during a manual `workflow_dispatch` run, open the **Releases** section on the right side of your repository's main page to download `BNBC_Ultimate_Pro_Setup.exe` directly under **Assets**.

---

## Architecture & Legal Compliance Notice

- **Zero Redistribution of Proprietary CSI DLLs:** This repository does not bundle copyrighted Computers and Structures (`ETABSv1.dll`) libraries. Instead, `BnbcUltimatePro.EtabsIntegration` locates the user's locally installed ETABS 22 directory (`C:\Program Files\Computers and Structures\ETABS 22` or Windows Registry) and binds `ETABSv1.dll` dynamically at runtime.
- **Verified BNBC 2020 Engineering Rules:**
  - Soil site class (`SA`, `SB`, `SC`, `SD`, `SE`, `S1`, `S2`) must always be supplied from the user's geotechnical investigation report and is never guessed from district.
  - Compliance checks distinguish `PASS`, `FAIL`, `WARNING`, and `NOT_VERIFIED`.
- **Build vs. Live ETABS Testing Transparency:** GitHub-hosted Windows runners compile the complete application, run all unit/compliance tests, and produce the Inno Setup installer. Live COM integration with ETABS 22 requires running `BNBC_Ultimate_Pro_Setup.exe` on a Windows 10/11 workstation with ETABS 22 installed.
