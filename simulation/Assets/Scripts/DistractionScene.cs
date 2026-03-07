using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Scene 2: Distraction Simulation
/// F_p > S_main / r² → attention shift
/// Main magnet (task) + distractor magnet (distraction)
/// </summary>
public class DistractionScene : MonoBehaviour, ISceneController
{
    private Magnet mainMagnet;
    private Magnet distractorMagnet;
    private List<IronFiling> filings = new List<IronFiling>();
    private List<GameObject> sceneObjects = new List<GameObject>();
    private MFASimulator sim;
    private GameObject thresholdLine;

    private const int FILING_COUNT = 300;
    private const float FORCE_SCALE = 0.8f;
    private float distractorFp = 20f;
    private int shiftedCount = 0;

    void Start()
    {
        sim = MFASimulator.Instance;
        sim.SetTitle("Scene 2: Distraction");
        sim.SetDescription("Main task vs. distractor.\nF_p > S/r\u00B2 = shift!\nOrange line = threshold.");

        // Main magnet (task) — center-left
        var mainGO = SpriteFactory.CreateMagnet(new Vector2(-2f, 0), 60f, MFASimulator.MainMagnetColor);
        mainMagnet = mainGO.GetComponent<Magnet>();
        sceneObjects.Add(mainGO);

        // Distractor magnet — right side
        var distGO = SpriteFactory.CreateMagnet(new Vector2(4f, 0), 20f, MFASimulator.DistractorColor);
        distractorMagnet = distGO.GetComponent<Magnet>();
        sceneObjects.Add(distGO);

        // Iron filings — start clustered around main magnet
        for (int i = 0; i < FILING_COUNT; i++)
        {
            Vector2 pos = (Vector2)mainMagnet.transform.position + Random.insideUnitCircle * 4f;
            var go = SpriteFactory.CreateFiling(pos, MFASimulator.FilingColor);
            filings.Add(go.GetComponent<IronFiling>());
            sceneObjects.Add(go);
        }

        // Threshold line
        thresholdLine = CreateThresholdCircle();
        sceneObjects.Add(thresholdLine);

        // UI Sliders
        sim.AddSlider("S_main (Task)", 10f, 150f, 60f, (v) => mainMagnet.S = v);
        sim.AddSlider("F_p (Distractor)", 5f, 100f, 20f, (v) =>
        {
            distractorFp = v;
            distractorMagnet.S = v;
        });
    }

    void Update()
    {
        if (mainMagnet == null || distractorMagnet == null) return;

        Vector2 mainPos = mainMagnet.transform.position;
        Vector2 distPos = distractorMagnet.transform.position;
        float Smain = mainMagnet.CurrentS;
        float Fp = distractorMagnet.CurrentS;

        shiftedCount = 0;

        foreach (var filing in filings)
        {
            if (filing == null) continue;

            Vector2 fPos = filing.transform.position;

            // Force from main magnet
            Vector2 mainForce = MFACore.ForceVector(fPos, mainPos, Smain);

            // Force from distractor
            Vector2 distForce = MFACore.ForceVector(fPos, distPos, Fp);

            // Check if this filing should "shift" (Formula 2)
            float distToMain = Vector2.Distance(fPos, mainPos);
            float localMainField = MFACore.AttentionField(Smain, distToMain);
            float distToDist = Vector2.Distance(fPos, distPos);
            float localDistField = MFACore.AttentionField(Fp, distToDist);

            bool shifted = localDistField > localMainField;
            if (shifted) shiftedCount++;

            // Color: gold if loyal to main, red if shifted
            filing.SetColor(shifted
                ? Color.Lerp(MFASimulator.FilingColor, MFASimulator.DistractorColor, 0.7f)
                : MFASimulator.FilingColor);

            // Total force
            Vector2 totalForce = mainForce + distForce + Random.insideUnitCircle * 0.2f;

            // Soft repulsion from magnet centers
            if (distToMain < 0.4f) totalForce += (fPos - mainPos).normalized * 2f;
            if (distToDist < 0.4f) totalForce += (fPos - distPos).normalized * 2f;

            filing.ApplyForce(totalForce * FORCE_SCALE);
        }

        // Update threshold line
        UpdateThresholdLine(mainPos, Smain, Fp);

        // Update info (paper: r_crit = √(S_main / F_p))
        float threshR = MFACore.ThresholdRadius(Smain, Fp);
        float shiftPct = (float)shiftedCount / FILING_COUNT * 100f;
        sim.SetInfo(
            $"S_main = {Smain:F1}  F_p = {Fp:F1}\n" +
            $"r_crit = \u221A(S/F_p) = {threshR:F1}\n" +
            $"\nShifted: {shiftedCount}/{FILING_COUNT} ({shiftPct:F0}%)\n" +
            $"Condition: F_p > S/r\u00B2"
        );
    }

    GameObject CreateThresholdCircle()
    {
        var go = new GameObject("ThresholdLine");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.startWidth = 0.04f;
        lr.endWidth = 0.04f;
        lr.startColor = MFASimulator.ThresholdColor;
        lr.endColor = MFASimulator.ThresholdColor;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = 2;
        lr.positionCount = 65;
        return go;
    }

    void UpdateThresholdLine(Vector2 center, float S, float Fp)
    {
        if (thresholdLine == null) return;
        var lr = thresholdLine.GetComponent<LineRenderer>();

        float r = MFACore.ThresholdRadius(S, Fp);
        r = Mathf.Min(r, 20f); // cap for visibility

        for (int i = 0; i <= 64; i++)
        {
            float angle = (float)i / 64 * Mathf.PI * 2f;
            Vector3 pos = new Vector3(
                center.x + Mathf.Cos(angle) * r,
                center.y + Mathf.Sin(angle) * r, 0);
            lr.SetPosition(i, pos);
        }
    }

    public void Cleanup()
    {
        foreach (var go in sceneObjects)
            if (go != null) Destroy(go);
        sceneObjects.Clear();
        filings.Clear();
    }

    void OnDestroy() => Cleanup();
}
