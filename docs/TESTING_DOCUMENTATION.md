# Testing & Validation Documentation

## Automated Test Suite (`BnbcUltimatePro.Tests`)
Executed automatically on every GitHub Actions build via `dotnet test`:
1. `Database_Loads_Verified_Bnbc2020_Seismic_Zones` — Verifies Zone 1 (0.12), Zone 2 (0.20), Zone 3 (0.28), and Zone 4 (0.36) coefficients.
2. `SeismicCalculator_Rejects_Missing_Geotechnical_SoilClass` — Ensures soil site class is never guessed from district when omitted.
3. `SeismicCalculator_Computes_Accurate_BaseShear_And_StoryDistribution` — Verifies $T_a$, $C_s$, $S_a$, $V$, and vertical distribution $\sum F_x = V$.
4. `WindCalculator_Computes_Bnbc2020_VelocityPressure_And_StoryForces` — Verifies $K_z$ power-law exposure profile and $q_z = 0.000613 K_z K_{zt} K_d V^2 I$.
5. `LoadCombinationGenerator_Includes_VerticalSeismic_And_Prevents_Duplicates` — Verifies unique combination names and vertical seismic factor $E_v$.
6. `ComplianceChecker_Distinguishes_Pass_Fail_Warning_NotVerified` — Verifies Zone 4 structural system restrictions, missing report warnings, and unverified drift status when ETABS is offline.
7. `EtabsConnector_Builds_DuplicateFree_PushPreview` — Verifies duplicate load pattern and combination filtering prior to ETABS push.

## Scope Boundary
- **Validated in Package & CI:** C# source structure, YAML syntax, Inno Setup path alignment, BNBC 2020 mathematical formulas, compliance state transitions, and Windows installer compilation on `windows-latest`.
- **Requires Local Workstation Verification:** Live COM attachment to `CSI.ETABS.API.ETABSObject` requires a licensed local installation of CSI ETABS 22 on Windows 10/11.
