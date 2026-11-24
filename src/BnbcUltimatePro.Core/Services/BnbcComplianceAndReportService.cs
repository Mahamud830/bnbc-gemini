using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BnbcUltimatePro.Core.Models;

namespace BnbcUltimatePro.Core.Services;

public sealed class BnbcComplianceChecker
{
    private readonly BnbcDatabaseProvider _dbProvider;

    public BnbcComplianceChecker(BnbcDatabaseProvider dbProvider)
    {
        _dbProvider = dbProvider ?? throw new ArgumentNullException(nameof(dbProvider));
    }

    public List<ComplianceCheckItem> RunChecks(
        BuildingProject project,
        SeismicCalculationResult seismic,
        WindCalculationResult wind,
        bool etabsConnected,
        double? maxStoryDriftRatio = null,
        double? maxWindDriftRatio = null,
        double? torsionalIrregularityRatio = null)
    {
        var items = new List<ComplianceCheckItem>();

        bool hasSoilClass = !string.IsNullOrWhiteSpace(project.GeotechnicalSiteClass);
        bool hasSoilRef = !string.IsNullOrWhiteSpace(project.GeotechnicalReportRef);
        items.Add(new ComplianceCheckItem
        {
            CheckId = "BNBC-GEO-01",
            Category = "Geotechnical Input",
            Title = "Site-Specific Geotechnical Soil Classification",
            Clause = "BNBC 2020 Part 6, Table 6.2.13",
            Status = !hasSoilClass
                ? ComplianceStatus.FAIL
                : (hasSoilRef ? ComplianceStatus.PASS : ComplianceStatus.WARNING),
            MeasuredValue = hasSoilClass ? $"Site Class {project.GeotechnicalSiteClass}" : "Not Specified",
            LimitValue = "Verified SA–SE from Soil Report",
            EngineeringRemarks = !hasSoilClass
                ? "Soil class must be entered from a geotechnical report; never guessed from district."
                : (hasSoilRef ? $"Verified via report: {project.GeotechnicalReportRef}" : "Soil class selected, but geotechnical report reference is blank.")
        });

        var sys = _dbProvider.GetStructuralSystem(project.StructuralSystemCode);
        var district = _dbProvider.GetDistrictInfo(project.District);
        if (sys != null && district != null)
        {
            bool omrfProhibited = sys.Code == "OMRF_RC" && district.Zone >= 2;
            bool imrfProhibitedInZone4 = sys.Code == "IMRF_RC" && district.Zone == 4;
            bool heightLimitExceeded = sys.Code == "IMRF_RC" && district.Zone == 3 && project.BuildingHeightMeters > 40.0;

            ComplianceStatus sysStatus = (omrfProhibited || imrfProhibitedInZone4 || heightLimitExceeded)
                ? ComplianceStatus.FAIL
                : ComplianceStatus.PASS;

            items.Add(new ComplianceCheckItem
            {
                CheckId = "BNBC-SYS-02",
                Category = "Seismic Detailing",
                Title = "Structural System Eligibility & Height Limit",
                Clause = "BNBC 2020 Part 6, Table 6.2.19",
                Status = sysStatus,
                MeasuredValue = $"{sys.Code} in Zone {district.Zone} (H = {project.BuildingHeightMeters:F1} m)",
                LimitValue = district.Zone == 4 ? "SMRF or Dual System Required" : "Permitted per Table 6.2.19",
                EngineeringRemarks = omrfProhibited
                    ? "Ordinary RC Moment Frame (OMRF) is not permitted in Seismic Zones 2, 3, or 4."
                    : (imrfProhibitedInZone4
                        ? "Intermediate RC Moment Frame (IMRF) is prohibited in Seismic Zone 4 (Z = 0.36). Use SMRF or Dual System."
                        : (heightLimitExceeded
                            ? "IMRF height exceeds 40m limit in Seismic Zone 3."
                            : "Structural system is permitted for the selected seismic zone and building height."))
            });
        }
        else
        {
            items.Add(new ComplianceCheckItem
            {
                CheckId = "BNBC-SYS-02",
                Category = "Seismic Detailing",
                Title = "Structural System Eligibility & Height Limit",
                Clause = "BNBC 2020 Part 6, Table 6.2.19",
                Status = ComplianceStatus.NOT_VERIFIED,
                MeasuredValue = "Missing System or District",
                LimitValue = "Valid BNBC System & District",
                EngineeringRemarks = "Select valid district and structural system."
            });
        }

        items.Add(new ComplianceCheckItem
        {
            CheckId = "BNBC-EQ-03",
            Category = "Seismic Base Shear",
            Title = "Minimum Design Base Shear Coefficient (Sa >= Sa,min)",
            Clause = "BNBC 2020 Part 6, Eq. 6.2.36",
            Status = !seismic.IsValid ? ComplianceStatus.NOT_VERIFIED : ComplianceStatus.PASS,
            MeasuredValue = seismic.IsValid ? $"Governing Sa = {seismic.GoverningBaseShearCoeff:F5} (V = {seismic.DesignBaseShearKn:F1} kN)" : "N/A",
            LimitValue = seismic.IsValid ? $"Sa,min = {seismic.MinimumBaseShearCoeffSaMin:F5}" : "N/A",
            EngineeringRemarks = seismic.IsValid
                ? (seismic.DesignBaseShearCoeffSa < seismic.MinimumBaseShearCoeffSaMin
                    ? "Minimum base shear limit Eq. 6.2.36 governs over period-dependent Sa."
                    : "Period-dependent Sa governs and exceeds Sa,min.")
                : seismic.ValidationMessage
        });

        bool hasStories = project.Stories.Count > 0 && project.Stories.All(s => s.SeismicWeightKn > 0);
        items.Add(new ComplianceCheckItem
        {
            CheckId = "BNBC-MOD-04",
            Category = "Model Integrity",
            Title = "Story Elevations & Seismic Mass Definition",
            Clause = "BNBC 2020 Part 6, Sec 2.5.5.1",
            Status = hasStories ? ComplianceStatus.PASS : ComplianceStatus.FAIL,
            MeasuredValue = $"{project.Stories.Count} stories (Total W = {project.Stories.Sum(s => s.SeismicWeightKn):F1} kN)",
            LimitValue = ">= 1 story with positive seismic weight",
            EngineeringRemarks = hasStories
                ? "All stories have positive seismic weight (DL + 25% LL minimum where applicable)."
                : "One or more stories have zero or undefined seismic weight."
        });

        double driftLimit = project.OccupancyCategory is "III" or "IV" ? 0.015 : 0.020;
        if (maxStoryDriftRatio.HasValue)
        {
            bool driftOk = maxStoryDriftRatio.Value <= driftLimit;
            items.Add(new ComplianceCheckItem
            {
                CheckId = "BNBC-DRF-05",
                Category = "Seismic Drift",
                Title = "Amplified Inelastic Story Drift Ratio (Cd * delta_e / h)",
                Clause = "BNBC 2020 Part 6, Table 6.2.21 & Eq. 6.2.44",
                Status = driftOk ? ComplianceStatus.PASS : ComplianceStatus.FAIL,
                MeasuredValue = $"{maxStoryDriftRatio.Value:F5}",
                LimitValue = $"<= {driftLimit:F3}",
                EngineeringRemarks = driftOk
                    ? "Amplified inelastic story drift is within BNBC 2020 Table 6.2.21 allowable limit."
                    : "Amplified story drift exceeds BNBC 2020 Table 6.2.21 limit. Increase lateral stiffness."
            });
        }
        else
        {
            items.Add(new ComplianceCheckItem
            {
                CheckId = "BNBC-DRF-05",
                Category = "Seismic Drift",
                Title = "Amplified Inelastic Story Drift Ratio (Cd * delta_e / h)",
                Clause = "BNBC 2020 Part 6, Table 6.2.21 & Eq. 6.2.44",
                Status = ComplianceStatus.NOT_VERIFIED,
                MeasuredValue = etabsConnected ? "Analysis Not Run / No Drift Data" : "ETABS Not Connected",
                LimitValue = $"<= {driftLimit:F3}",
                EngineeringRemarks = "Connect to an analyzed ETABS 22 model and extract story displacements to verify drift."
            });
        }

        if (maxWindDriftRatio.HasValue)
        {
            bool windOk = maxWindDriftRatio.Value <= 0.002;
            items.Add(new ComplianceCheckItem
            {
                CheckId = "BNBC-WND-06",
                Category = "Wind Serviceability",
                Title = "Overall Wind Sway Deflection Ratio (Delta_max / H)",
                Clause = "BNBC 2020 Part 6, Sec 1.5.6.2",
                Status = windOk ? ComplianceStatus.PASS : ComplianceStatus.FAIL,
                MeasuredValue = $"{maxWindDriftRatio.Value:F5} (H/{(maxWindDriftRatio.Value > 0 ? 1.0 / maxWindDriftRatio.Value : 9999):F0})",
                LimitValue = "<= 0.00200 (H/500)",
                EngineeringRemarks = windOk
                    ? "Top wind sway satisfies H/500 serviceability limit."
                    : "Top wind sway exceeds H/500 limit."
            });
        }
        else
        {
            items.Add(new ComplianceCheckItem
            {
                CheckId = "BNBC-WND-06",
                Category = "Wind Serviceability",
                Title = "Overall Wind Sway Deflection Ratio (Delta_max / H)",
                Clause = "BNBC 2020 Part 6, Sec 1.5.6.2",
                Status = ComplianceStatus.NOT_VERIFIED,
                MeasuredValue = "Not Extracted from ETABS",
                LimitValue = "<= 0.00200 (H/500)",
                EngineeringRemarks = "Run wind analysis in ETABS 22 and extract top joint deflection."
            });
        }

        if (torsionalIrregularityRatio.HasValue)
        {
            ComplianceStatus torStatus = torsionalIrregularityRatio.Value > 1.4
                ? ComplianceStatus.FAIL
                : (torsionalIrregularityRatio.Value > 1.2 ? ComplianceStatus.WARNING : ComplianceStatus.PASS);
            items.Add(new ComplianceCheckItem
            {
                CheckId = "BNBC-TOR-07",
                Category = "Plan Irregularity",
                Title = "Torsional Irregularity Ratio (Delta_max / Delta_avg)",
                Clause = "BNBC 2020 Part 6, Sec 2.5.3.2",
                Status = torStatus,
                MeasuredValue = $"{torsionalIrregularityRatio.Value:F3}",
                LimitValue = "<= 1.20 (Regular), <= 1.40 (Extreme Limit)",
                EngineeringRemarks = torsionalIrregularityRatio.Value > 1.4
                    ? "Extreme torsional irregularity (> 1.4). Rebalance shear walls/columns."
                    : (torsionalIrregularityRatio.Value > 1.2
                        ? "Torsional irregularity (> 1.2) detected. Apply accidental torsion amplification factor Ax."
                        : "Plan exhibits torsional regularity.")
            });
        }
        else
        {
            items.Add(new ComplianceCheckItem
            {
                CheckId = "BNBC-TOR-07",
                Category = "Plan Irregularity",
                Title = "Torsional Irregularity Ratio (Delta_max / Delta_avg)",
                Clause = "BNBC 2020 Part 6, Sec 2.5.3.2",
                Status = ComplianceStatus.NOT_VERIFIED,
                MeasuredValue = "Not Extracted from ETABS",
                LimitValue = "<= 1.20",
                EngineeringRemarks = "Requires diaphragm corner displacement results from ETABS 22."
            });
        }

        return items;
    }
}

public sealed class BnbcReportGenerator
{
    public string GenerateMarkdownReport(
        BuildingProject project,
        SeismicCalculationResult seismic,
        WindCalculationResult wind,
        IReadOnlyList<LoadCombinationDefinition> combinations,
        IReadOnlyList<ComplianceCheckItem> complianceItems,
        IReadOnlyList<FoundationReactionSummary> reactions)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# BNBC ULTIMATE PRO — Structural Engineering Calculation Report");
        sb.AppendLine($"**Design Code:** Bangladesh National Building Code (BNBC 2020), Part 6");
        sb.AppendLine($"**Generated On:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("## 1. Project & Building Metadata");
        sb.AppendLine($"- **Project Name:** {project.ProjectName} (`{project.ProjectNumber}`)");
        sb.AppendLine($"- **Client / Engineer:** {project.ClientName} / {project.EngineerName}");
        sb.AppendLine($"- **Location (District):** {project.District}");
        sb.AppendLine($"- **Geotechnical Soil Class:** {project.GeotechnicalSiteClass} (Ref: {project.GeotechnicalReportRef})");
        sb.AppendLine($"- **Occupancy Category:** {project.OccupancyCategory}");
        sb.AppendLine($"- **Structural System:** {project.StructuralSystemCode}");
        sb.AppendLine($"- **Dimensions (H x Lx x Ly):** {project.BuildingHeightMeters:F2} m x {project.PlanDimensionXMeters:F2} m x {project.PlanDimensionYMeters:F2} m");
        sb.AppendLine();

        sb.AppendLine("## 2. Seismic Hazard & Equivalent Static Analysis (BNBC 2020 Sec 2.5)");
        if (seismic.IsValid)
        {
            sb.AppendLine($"- **Seismic Zone & Z:** Zone {seismic.SeismicZone} ($Z = {seismic.ZoneCoefficientZ:F2}$)");
            sb.AppendLine($"- **Soil Factor & Corner Periods:** $S = {seismic.SoilFactorS:F2}$, $T_B = {seismic.TbSeconds:F2}\text{{ s}}$, $T_C = {seismic.TcSeconds:F2}\text{{ s}}$, $T_D = {seismic.TdSeconds:F2}\text{{ s}}$");
            sb.AppendLine($"- **System Factors:** $R = {seismic.ResponseReductionFactorR:F1}$, $\Omega_0 = {seismic.OverstrengthFactorOmega0:F1}$, $C_d = {seismic.DeflectionAmplificationCd:F1}$, $I = {seismic.ImportanceFactorI:F2}$");
            sb.AppendLine($"- **Fundamental Period ($T_a$):** {seismic.FundamentalPeriodTaSeconds:F4} s");
            sb.AppendLine($"- **Governing Base Shear Coefficient ($S_a$):** {seismic.GoverningBaseShearCoeff:F5}");
            sb.AppendLine($"- **Total Seismic Weight ($W$):** {seismic.TotalSeismicWeightKn:F2} kN");
            sb.AppendLine($"- **Design Base Shear ($V$):** **{seismic.DesignBaseShearKn:F2} kN**");
            sb.AppendLine();
            sb.AppendLine("| Story | Elevation (m) | Weight W_i (kN) | C_vx | Lateral Force F_x (kN) | Story Shear V_x (kN) |");
            sb.AppendLine("|---|---:|---:|---:|---:|---:|");
            foreach (var sf in seismic.StoryForces.OrderByDescending(x => x.ElevationMeters))
            {
                sb.AppendLine($"| {sf.StoryName} | {sf.ElevationMeters:F2} | {sf.WeightKn:F2} | {sf.DistributionFactorCvx:F4} | {sf.LateralForceFxKn:F2} | {sf.CumulativeStoryShearVxKn:F2} |");
            }
        }
        else
        {
            sb.AppendLine($"> **Validation Error:** {seismic.ValidationMessage}");
        }
        sb.AppendLine();

        sb.AppendLine("## 3. Wind Hazard Analysis (BNBC 2020 Sec 2.4)");
        if (wind.IsValid)
        {
            sb.AppendLine($"- **Basic Wind Speed ($V$):** {wind.BasicWindSpeedMs:F1} m/s (Exposure {project.WindExposure})");
            sb.AppendLine($"- **Factors:** $I = {wind.WindImportanceFactorI:F2}$, $K_d = {wind.DirectionalityFactorKd:F2}$, $K_{{zt}} = {wind.TopographicFactorKzt:F2}$, $G = {wind.GustFactorG:F2}$");
            sb.AppendLine($"- **Roof Velocity Pressure ($q_h$):** {wind.RoofVelocityPressureQzKnM2:F4} kN/m²");
            sb.AppendLine($"- **Total Base Wind Force:** $F_{{wx}} = {wind.TotalWindForceXKn:F2}\text{{ kN}}$, $F_{{wy}} = {wind.TotalWindForceYKn:F2}\text{{ kN}}$");
        }
        sb.AppendLine();

        sb.AppendLine("## 4. BNBC 2020 Compliance Checklist");
        sb.AppendLine("| ID | Check | Clause | Status | Measured | Limit | Remarks |");
        sb.AppendLine("|---|---|---|---|---|---|---|");
        foreach (var c in complianceItems)
        {
            sb.AppendLine($"| {c.CheckId} | {c.Title} | {c.Clause} | **{c.Status}** | {c.MeasuredValue} | {c.LimitValue} | {c.EngineeringRemarks} |");
        }
        sb.AppendLine();

        sb.AppendLine($"## 5. Generated Load Combinations ({combinations.Count} Total)");
        foreach (var combo in combinations)
        {
            string terms = string.Join(" + ", combo.Factors.Select(kv => $"{kv.Value:F3}({kv.Key})"));
            sb.AppendLine($"- **{combo.Name}** [{combo.DesignType}]: `{terms}` — *{combo.ClauseReference}*");
        }
        sb.AppendLine();

        if (reactions.Count > 0)
        {
            sb.AppendLine("## 6. Extracted Foundation Reactions");
            sb.AppendLine("| Joint | Combo | Fz (kN) | Fx (kN) | Fy (kN) | Mx (kN-m) | My (kN-m) |");
            sb.AppendLine("|---|---|---:|---:|---:|---:|---:|");
            foreach (var r in reactions)
            {
                sb.AppendLine($"| {r.JointLabel} | {r.LoadCombination} | {r.FzKn:F2} | {r.FxKn:F2} | {r.FyKn:F2} | {r.MxKnM:F2} | {r.MyKnM:F2} |");
            }
        }

        return sb.ToString();
    }
}
