# User Manual — BNBC ULTIMATE PRO

## Module Workflow
1. **Project & Location Setup:** Enter project metadata, choose the Bangladesh district (for BNBC 2020 Table 6.2.15 Zone $Z$ and Table 6.2.8 basic wind speed $V$), select Occupancy Category (I–IV), Structural System (`IMRF_RC`, `SMRF_RC`, `DUAL_SMRF_SW`, etc.), and enter the **Geotechnical Site Class (`SA`–`S2`)** directly from your soil investigation report.
2. **Seismic & Wind Hazard:** Review period $T_a$, normalized spectral acceleration $C_s$, governing base shear coefficient $S_a$, total design base shear $V$, story lateral force distribution $F_x$, and story wind pressures $q_z$.
3. **Gravity, Occupancy & Wall Loads:** Calculate floor slab dead loads, partition masonry wall line loads (deducting openings), and manage floor-by-floor occupancy and seismic weights.
4. **Load Combinations:** Automatically generate BNBC 2020 Section 2.7.3 Strength (USD) and Section 2.7.2 Serviceability (ASD) load combinations, including vertical seismic load effect $E_v = 0.5 a_h D$.
5. **ETABS 22 Automation & Reactions:** Detect ETABS 22, connect to an active model, inspect existing patterns/combos to avoid duplicates, preview changes, push patterns/mass source/combinations after explicit confirmation, and extract base joint foundation reactions.
6. **Compliance & Engineering Report:** Review automated `PASS`, `FAIL`, `WARNING`, and `NOT_VERIFIED` checks and export a formatted Markdown engineering calculation report.
