using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Scene 1: Basic Magnetic Field Visualization
/// F(r) = S / r² — 一个磁铁 + 铁屑分布
/// </summary>
public class BasicFieldScene : MonoBehaviour, ISceneController
{
    private Magnet magnet;
    private List<IronFiling> filings = new List<IronFiling>();
    private List<GameObject> sceneObjects = new List<GameObject>();
    private MFASimulator sim;

    private const int FILING_COUNT = 300;
    private const float FORCE_SCALE = 0.8f;
    private const float SPAWN_RADIUS = 7f;

    void Start()
    {
        sim = MFASimulator.Instance;
        sim.SetTitle("Scene 1: Basic Field");
        sim.SetDescription("F(r) = S / r\u00B2\nDrag the magnet.\nAdjust S with slider.");

        // Create magnet at center
        var magnetGO = SpriteFactory.CreateMagnet(Vector2.zero, 50f, MFASimulator.MainMagnetColor);
        magnet = magnetGO.GetComponent<Magnet>();
        sceneObjects.Add(magnetGO);

        // Create iron filings in random positions
        for (int i = 0; i < FILING_COUNT; i++)
        {
            Vector2 pos = Random.insideUnitCircle * SPAWN_RADIUS;
            // Keep some distance from center for initial distribution
            if (pos.magnitude < 0.5f) pos = pos.normalized * 0.5f;

            var go = SpriteFactory.CreateFiling(pos, MFASimulator.FilingColor);
            filings.Add(go.GetComponent<IronFiling>());
            sceneObjects.Add(go);
        }

        // UI: S slider
        sim.AddSlider("S (Field Strength)", 5f, 150f, 50f, (v) => magnet.S = v);

        // Add field strength reference circles (visual guides)
        CreateFieldCircles();
    }

    void Update()
    {
        if (magnet == null) return;

        Vector2 magnetPos = magnet.transform.position;
        float S = magnet.CurrentS;

        // Update each filing
        foreach (var filing in filings)
        {
            if (filing == null) continue;

            Vector2 fPos = filing.transform.position;
            Vector2 force = MFACore.ForceVector(fPos, magnetPos, S);

            // Add small random jitter for natural look
            force += Random.insideUnitCircle * 0.3f;

            // Soft repulsion from magnet center (prevent piling)
            float dist = Vector2.Distance(fPos, magnetPos);
            if (dist < 0.4f)
            {
                Vector2 pushOut = (fPos - magnetPos).normalized * 2f;
                force += pushOut;
            }

            filing.ApplyForce(force * FORCE_SCALE);

            // Update brightness based on local field
            float localField = MFACore.AttentionField(S, dist);
            filing.UpdateBrightness(localField);
        }

        // Update info display (paper: F_att(r) = S/r^α, α=2)
        float rRef = 2f;
        float fAtRef = MFACore.AttentionField(S, rRef);
        sim.SetInfo(
            $"S = {S:F1}\n" +
            $"F(r=1) = {MFACore.AttentionField(S, 1f):F1}\n" +
            $"F(r=2) = {fAtRef:F1}\n" +
            $"F(r=5) = {MFACore.AttentionField(S, 5f):F1}\n" +
            $"\nFormula: F = S / r\u00B2 (\u03B1=2)"
        );
    }

    void CreateFieldCircles()
    {
        // Create subtle rings showing field strength zones
        float[] radii = { 1f, 2f, 3f, 5f };
        float[] alphas = { 0.15f, 0.10f, 0.07f, 0.04f };

        for (int i = 0; i < radii.Length; i++)
        {
            var ring = CreateRing(radii[i], new Color(0.3f, 0.5f, 0.8f, alphas[i]));
            sceneObjects.Add(ring);
        }
    }

    GameObject CreateRing(float radius, Color color)
    {
        var go = new GameObject("Ring_" + radius);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.startWidth = 0.03f;
        lr.endWidth = 0.03f;
        lr.startColor = color;
        lr.endColor = color;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = 0;

        int segments = 64;
        lr.positionCount = segments + 1;
        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0));
        }

        return go;
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
