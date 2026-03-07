using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

/// <summary>
/// Quantitative data extraction from MFA simulation.
/// Measures filing density vs distance, fits 1/r², Gaussian, and exponential,
/// computes R² for each, and exports CSV.
/// Attach to any scene with a Magnet and IronFilings.
/// </summary>
public class FieldDataAnalyzer : MonoBehaviour
{
    [Header("Analysis Settings")]
    public int numBins = 12;
    public float maxRadius = 8f;
    public float sampleInterval = 2f;

    // Results
    private float[] binCenters;
    private float[] binDensities;
    private float r2_InvSq, r2_Gauss, r2_Exp;
    private float fit_a_InvSq, fit_a_Gauss, fit_sigma_Gauss, fit_a_Exp, fit_lambda_Exp;
    private int totalFilings;
    private float currentS;
    private bool hasData = false;

    private float timer;
    private List<string> csvRows = new List<string>();
    private int snapshotCount = 0;

    // UI
    private bool showPanel = true;
    private Vector2 scrollPos;
    private string lastCsvPath = "";

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= sampleInterval)
        {
            timer = 0f;
            CollectAndAnalyze();
        }
    }

    void CollectAndAnalyze()
    {
        var magnets = FindObjectsOfType<Magnet>();
        var filings = FindObjectsOfType<IronFiling>();
        if (magnets.Length == 0 || filings.Length == 0) return;

        Magnet primary = magnets[0];
        float bestS = 0;
        foreach (var m in magnets)
            if (m.CurrentS > bestS) { primary = m; bestS = m.CurrentS; }

        currentS = primary.CurrentS;
        totalFilings = filings.Length;
        Vector2 center = primary.transform.position;

        float binWidth = maxRadius / numBins;
        binCenters = new float[numBins];
        binDensities = new float[numBins];
        int[] binCounts = new int[numBins];

        for (int i = 0; i < numBins; i++)
            binCenters[i] = (i + 0.5f) * binWidth;

        foreach (var f in filings)
        {
            if (f == null) continue;
            float r = Vector2.Distance(f.transform.position, center);
            int bin = Mathf.Clamp((int)(r / binWidth), 0, numBins - 1);
            binCounts[bin]++;
        }

        for (int i = 0; i < numBins; i++)
        {
            float rInner = i * binWidth;
            float rOuter = (i + 1) * binWidth;
            float area = Mathf.PI * (rOuter * rOuter - rInner * rInner);
            binDensities[i] = binCounts[i] / Mathf.Max(area, 0.01f);
        }

        FitModels();
        hasData = true;
    }

    void FitModels()
    {
        // Least-squares fitting for three models using binCenters/binDensities
        // 1/r² : y = a / r²
        // Gaussian : y = a * exp(-r²/(2σ²))
        // Exponential : y = a * exp(-λr)

        float[] r = binCenters;
        float[] y = binDensities;
        int n = r.Length;

        // Skip bins with zero density (outer bins with no filings)
        int validN = 0;
        for (int i = 0; i < n; i++)
            if (y[i] > 0.001f) validN = i + 1;
        if (validN < 3) validN = n;

        // --- 1/r² fit: y = a/r² → a = Σ(y·r²·(1/r²)) / Σ((1/r²)²) = Σ(y) / Σ(1/r⁴) ... 
        // Simpler: minimize Σ(y - a/r²)² → a = Σ(y/r²) / Σ(1/r⁴)
        {
            float sumYX = 0, sumXX = 0;
            for (int i = 0; i < validN; i++)
            {
                float x = 1f / (r[i] * r[i]);
                sumYX += y[i] * x;
                sumXX += x * x;
            }
            fit_a_InvSq = sumXX > 0 ? sumYX / sumXX : 1f;
            r2_InvSq = ComputeR2(r, y, validN, (ri) => fit_a_InvSq / (ri * ri));
        }

        // --- Gaussian fit: ln(y) = ln(a) - r²/(2σ²) → linear in r²
        {
            float sumX = 0, sumY2 = 0, sumXY = 0, sumXX2 = 0;
            int cnt = 0;
            for (int i = 0; i < validN; i++)
            {
                if (y[i] <= 0.001f) continue;
                float lny = Mathf.Log(y[i]);
                float rsq = r[i] * r[i];
                sumX += rsq;
                sumY2 += lny;
                sumXY += rsq * lny;
                sumXX2 += rsq * rsq;
                cnt++;
            }
            if (cnt >= 2)
            {
                float slope = (cnt * sumXY - sumX * sumY2) / (cnt * sumXX2 - sumX * sumX + 1e-10f);
                float intercept = (sumY2 - slope * sumX) / cnt;
                fit_a_Gauss = Mathf.Exp(intercept);
                fit_sigma_Gauss = Mathf.Sqrt(Mathf.Abs(-1f / (2f * slope + 1e-10f)));
                r2_Gauss = ComputeR2(r, y, validN,
                    (ri) => fit_a_Gauss * Mathf.Exp(-ri * ri / (2f * fit_sigma_Gauss * fit_sigma_Gauss)));
            }
        }

        // --- Exponential fit: ln(y) = ln(a) - λr → linear in r
        {
            float sumX = 0, sumY2 = 0, sumXY = 0, sumXX2 = 0;
            int cnt = 0;
            for (int i = 0; i < validN; i++)
            {
                if (y[i] <= 0.001f) continue;
                float lny = Mathf.Log(y[i]);
                sumX += r[i];
                sumY2 += lny;
                sumXY += r[i] * lny;
                sumXX2 += r[i] * r[i];
                cnt++;
            }
            if (cnt >= 2)
            {
                float slope = (cnt * sumXY - sumX * sumY2) / (cnt * sumXX2 - sumX * sumX + 1e-10f);
                float intercept = (sumY2 - slope * sumX) / cnt;
                fit_a_Exp = Mathf.Exp(intercept);
                fit_lambda_Exp = -slope;
                r2_Exp = ComputeR2(r, y, validN,
                    (ri) => fit_a_Exp * Mathf.Exp(-fit_lambda_Exp * ri));
            }
        }
    }

    float ComputeR2(float[] r, float[] y, int n, System.Func<float, float> model)
    {
        float meanY = 0;
        for (int i = 0; i < n; i++) meanY += y[i];
        meanY /= n;

        float ssTot = 0, ssRes = 0;
        for (int i = 0; i < n; i++)
        {
            float pred = model(r[i]);
            ssTot += (y[i] - meanY) * (y[i] - meanY);
            ssRes += (y[i] - pred) * (y[i] - pred);
        }
        if (ssTot < 1e-10f) return 0f;
        return 1f - ssRes / ssTot;
    }

    public void ExportCSV()
    {
        if (!hasData) return;

        snapshotCount++;
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string dir = Path.Combine(Application.dataPath, "..", "DataExport");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, $"MFA_density_{timestamp}.csv");

        var sb = new StringBuilder();
        sb.AppendLine("bin_center_r,density,count_approx,predicted_inv_sq,predicted_gaussian,predicted_exponential");

        for (int i = 0; i < numBins; i++)
        {
            float pred_inv = fit_a_InvSq / (binCenters[i] * binCenters[i]);
            float pred_gauss = fit_a_Gauss * Mathf.Exp(-binCenters[i] * binCenters[i] / (2f * fit_sigma_Gauss * fit_sigma_Gauss));
            float pred_exp = fit_a_Exp * Mathf.Exp(-fit_lambda_Exp * binCenters[i]);

            sb.AppendLine($"{binCenters[i]:F2},{binDensities[i]:F4},{binDensities[i]:F4},{pred_inv:F4},{pred_gauss:F4},{pred_exp:F4}");
        }

        sb.AppendLine();
        sb.AppendLine($"# S = {currentS:F1}");
        sb.AppendLine($"# Total filings = {totalFilings}");
        sb.AppendLine($"# R2_inverse_square = {r2_InvSq:F4}");
        sb.AppendLine($"# R2_gaussian = {r2_Gauss:F4}");
        sb.AppendLine($"# R2_exponential = {r2_Exp:F4}");
        sb.AppendLine($"# Best fit = {GetBestFit()}");

        File.WriteAllText(path, sb.ToString());
        lastCsvPath = path;
        Debug.Log($"[FieldDataAnalyzer] Exported to {path}");
    }

    string GetBestFit()
    {
        if (r2_InvSq >= r2_Gauss && r2_InvSq >= r2_Exp) return "1/r² (inverse-square)";
        if (r2_Gauss >= r2_Exp) return "Gaussian";
        return "Exponential";
    }

    void OnGUI()
    {
        if (!showPanel || !hasData) return;

        float panelW = 280f;
        float panelH = 320f;
        float x = Screen.width - panelW - 20f;
        float y = Screen.height - panelH - 60f;

        GUI.Box(new Rect(x, y, panelW, panelH), "");

        GUILayout.BeginArea(new Rect(x + 8, y + 4, panelW - 16, panelH - 8));

        GUIStyle header = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 13 };
        GUIStyle mono = new GUIStyle(GUI.skin.label) { fontSize = 11 };
        GUIStyle best = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };

        GUILayout.Label("Model Comparison", header);
        GUILayout.Space(4);

        Color origColor = GUI.contentColor;
        bool invBest = r2_InvSq >= r2_Gauss && r2_InvSq >= r2_Exp;
        bool gauBest = !invBest && r2_Gauss >= r2_Exp;

        if (invBest) GUI.contentColor = Color.green;
        GUILayout.Label($"1/r² (MFA):     R² = {r2_InvSq:F4}", invBest ? best : mono);
        GUI.contentColor = origColor;

        if (gauBest) GUI.contentColor = new Color(1f, 0.8f, 0.3f);
        GUILayout.Label($"Gaussian:       R² = {r2_Gauss:F4}", gauBest ? best : mono);
        GUI.contentColor = origColor;

        if (!invBest && !gauBest) GUI.contentColor = new Color(1f, 0.8f, 0.3f);
        GUILayout.Label($"Exponential:    R² = {r2_Exp:F4}", mono);
        GUI.contentColor = origColor;

        GUILayout.Space(6);
        GUILayout.Label($"Winner: {GetBestFit()}", best);
        GUILayout.Label($"S = {currentS:F1}  |  N = {totalFilings}", mono);

        GUILayout.Space(8);
        GUILayout.Label("Density by distance:", mono);
        if (binCenters != null)
        {
            int show = Mathf.Min(6, numBins);
            for (int i = 0; i < show; i++)
                GUILayout.Label($"  r={binCenters[i]:F1}: {binDensities[i]:F2}", mono);
            if (numBins > show)
                GUILayout.Label($"  ... ({numBins - show} more bins)", mono);
        }

        GUILayout.Space(6);
        if (GUILayout.Button("Export CSV"))
            ExportCSV();
        if (!string.IsNullOrEmpty(lastCsvPath))
            GUILayout.Label($"Saved: {Path.GetFileName(lastCsvPath)}", mono);

        GUILayout.EndArea();
    }
}
