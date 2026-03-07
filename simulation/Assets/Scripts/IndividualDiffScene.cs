using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Scene 3: Individual Differences
/// Three panels: ADHD | Normal | Hyperfocus
/// Paper: S(t) = S₀ + A·sin(ωt) + ε(t); A = oscillation amplitude
/// </summary>
public class IndividualDiffScene : MonoBehaviour, ISceneController
{
    private struct Panel
    {
        public Magnet magnet;
        public List<IronFiling> filings;
        public float xCenter;
        public string label;
        public float S0, sigma;
    }

    private Panel[] panels = new Panel[3];
    private List<GameObject> sceneObjects = new List<GameObject>();
    private MFASimulator sim;

    private const int FILINGS_PER_PANEL = 150;
    private const float PANEL_WIDTH = 6f;
    private const float FORCE_SCALE = 0.6f;

    void Start()
    {
        sim = MFASimulator.Instance;
        sim.SetTitle("Scene 3: Individual Diff");
        sim.SetDescription("S(t) = S\u2080 + A\u00B7sin(\u03C9t)\nADHD: high A (unstable)\nHyperfocus: low A (stable)");

        // Define three profiles
        float[] xCenters = { -5.5f, 0f, 5.5f };
        string[] labels = { "ADHD", "Normal", "Hyperfocus" };
        float[] s0Values = { 40f, 50f, 90f };
        float[] sigmaValues = { 35f, 5f, 2f };
        Color[] magnetColors = {
            new Color(0.9f, 0.4f, 0.3f),   // ADHD: warm red
            MFASimulator.MainMagnetColor,    // Normal: blue
            new Color(0.3f, 0.8f, 0.5f)     // Hyperfocus: green
        };

        for (int p = 0; p < 3; p++)
        {
            panels[p] = new Panel
            {
                xCenter = xCenters[p],
                label = labels[p],
                S0 = s0Values[p],
                sigma = sigmaValues[p],
                filings = new List<IronFiling>()
            };

            // Create magnet
            var magnetGO = SpriteFactory.CreateMagnet(
                new Vector2(xCenters[p], 0), s0Values[p], magnetColors[p], false);
            var mag = magnetGO.GetComponent<Magnet>();
            mag.sigma = sigmaValues[p];
            mag.omega = 3f + p * 0.5f; // slightly different frequencies
            panels[p].magnet = mag;
            sceneObjects.Add(magnetGO);

            // Create filings
            for (int i = 0; i < FILINGS_PER_PANEL; i++)
            {
                Vector2 pos = new Vector2(xCenters[p], 0) + Random.insideUnitCircle * 3.5f;
                var go = SpriteFactory.CreateFiling(pos, MFASimulator.FilingColor);
                var filing = go.GetComponent<IronFiling>();
                panels[p].filings.Add(filing);
                sceneObjects.Add(go);
            }

            // Create label
            CreateWorldLabel(labels[p], new Vector3(xCenters[p], 4.5f, 0));

            // Create panel dividers
            if (p < 2)
            {
                float divX = (xCenters[p] + xCenters[p + 1]) / 2f;
                CreateDivider(divX);
            }
        }
    }

    void Update()
    {
        string info = "";

        for (int p = 0; p < 3; p++)
        {
            var panel = panels[p];
            if (panel.magnet == null) continue;

            Vector2 magnetPos = panel.magnet.transform.position;
            float S = panel.magnet.CurrentS;

            foreach (var filing in panel.filings)
            {
                if (filing == null) continue;

                Vector2 fPos = filing.transform.position;
                Vector2 force = MFACore.ForceVector(fPos, magnetPos, S);

                // Add jitter proportional to sigma (visual instability)
                float jitter = panel.sigma * 0.015f;
                force += Random.insideUnitCircle * jitter;

                // Keep filings in their panel's zone
                float distFromCenter = Mathf.Abs(fPos.x - panel.xCenter);
                if (distFromCenter > PANEL_WIDTH * 0.45f)
                {
                    float pushBack = (fPos.x > panel.xCenter) ? -2f : 2f;
                    force += new Vector2(pushBack, 0);
                }

                // Soft repulsion from magnet center
                float dist = Vector2.Distance(fPos, magnetPos);
                if (dist < 0.3f) force += (fPos - magnetPos).normalized * 1.5f;

                filing.ApplyForce(force * FORCE_SCALE);
                filing.UpdateBrightness(MFACore.AttentionField(S, dist));
            }

            info += $"{panel.label}: S={S:F0} (A={panel.sigma:F0})\n";
        }

        sim.SetInfo(info + "\nFormula: S(t) = S\u2080 + A\u00B7sin(\u03C9t)");
    }

    void CreateWorldLabel(string text, Vector3 position)
    {
        var go = new GameObject("Label_" + text);
        go.transform.position = position;

        // Use TextMesh for world-space text
        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 40;
        tm.characterSize = 0.12f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.white;
        tm.fontStyle = FontStyle.Bold;

        sceneObjects.Add(go);
    }

    void CreateDivider(float x)
    {
        var go = new GameObject("Divider");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.startWidth = 0.02f;
        lr.endWidth = 0.02f;
        lr.startColor = new Color(1, 1, 1, 0.2f);
        lr.endColor = new Color(1, 1, 1, 0.2f);
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.positionCount = 2;
        lr.SetPosition(0, new Vector3(x, -6, 0));
        lr.SetPosition(1, new Vector3(x, 6, 0));
        lr.sortingOrder = 0;

        sceneObjects.Add(go);
    }

    public void Cleanup()
    {
        foreach (var go in sceneObjects)
            if (go != null) Destroy(go);
        sceneObjects.Clear();
        for (int i = 0; i < 3; i++)
            panels[i].filings?.Clear();
    }

    void OnDestroy() => Cleanup();
}
