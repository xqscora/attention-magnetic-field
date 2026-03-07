using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Model Comparison Scene: Power Law (MFA) vs Gaussian vs Exponential
/// Split-screen showing how different decay functions distribute cognitive resources.
/// Left: F = S/r^α (MFA prediction — heavy tail, resources persist at periphery)
/// Right: F = S·exp(-r²/2σ²) (Gaussian — sharp cutoff, periphery empty)
/// This directly demonstrates why power-law matters for attention science.
/// </summary>
public class ModelComparisonScene : MonoBehaviour, ISceneController
{
    private MFASimulator sim;
    private List<GameObject> sceneObjects = new List<GameObject>();

    // Two magnets (same S, different field equations)
    private Magnet mfaMagnet;
    private Magnet gaussMagnet;

    // Filings for each model
    private List<IronFiling> mfaFilings = new List<IronFiling>();
    private List<IronFiling> gaussFilings = new List<IronFiling>();

    // Visual divider
    private LineRenderer divider;

    // Labels
    private const float LEFT_X = -3.5f;
    private const float RIGHT_X = 3.5f;
    private const float MAGNET_Y = 0f;
    private const int FILING_COUNT = 150;
    private const float SPAWN_RADIUS = 4.5f;
    private const float FORCE_SCALE = 0.7f;

    private float gaussSigma = 3f;
    private float fieldStrength = 50f;

    void Start()
    {
        sim = MFASimulator.Instance;
        sim.SetTitle("Model Comparison");
        sim.SetDescription(
            "LEFT: MFA (F = S/r\u00B2)\n" +
            "RIGHT: Gaussian (bell curve)\n\n" +
            "Notice: MFA resources persist\n" +
            "at edges (heavy tail).\n" +
            "Gaussian cuts off sharply."
        );

        CreateDivider();

        // MFA magnet (left)
        var mfaGO = SpriteFactory.CreateMagnet(new Vector2(LEFT_X, MAGNET_Y), fieldStrength,
            new Color(0.2f, 0.5f, 0.9f));
        mfaMagnet = mfaGO.GetComponent<Magnet>();
        mfaMagnet.isDraggable = false;
        sceneObjects.Add(mfaGO);

        // Gaussian magnet (right)
        var gaussGO = SpriteFactory.CreateMagnet(new Vector2(RIGHT_X, MAGNET_Y), fieldStrength,
            new Color(0.9f, 0.4f, 0.2f));
        gaussMagnet = gaussGO.GetComponent<Magnet>();
        gaussMagnet.isDraggable = false;
        sceneObjects.Add(gaussGO);

        // Spawn filings for each half
        SpawnFilings(mfaFilings, LEFT_X, new Color(0.5f, 0.7f, 1f));
        SpawnFilings(gaussFilings, RIGHT_X, new Color(1f, 0.7f, 0.4f));

        // Labels
        CreateLabel("F = S / r\u00B2", new Vector2(LEFT_X, 5f), new Color(0.4f, 0.7f, 1f));
        CreateLabel("Power Law (MFA)", new Vector2(LEFT_X, 4.4f), new Color(0.4f, 0.7f, 1f));
        CreateLabel("F = S\u00B7e^(-r\u00B2/2\u03C3\u00B2)", new Vector2(RIGHT_X, 5f), new Color(1f, 0.6f, 0.3f));
        CreateLabel("Gaussian", new Vector2(RIGHT_X, 4.4f), new Color(1f, 0.6f, 0.3f));

        sim.AddSlider("S (Strength)", 10f, 150f, 50f, (v) =>
        {
            fieldStrength = v;
            mfaMagnet.S = v;
            gaussMagnet.S = v;
        });

        sim.AddSlider("Gaussian \u03C3", 0.5f, 8f, 3f, (v) => gaussSigma = v);
    }

    void SpawnFilings(List<IronFiling> list, float centerX, Color color)
    {
        for (int i = 0; i < FILING_COUNT; i++)
        {
            Vector2 offset = Random.insideUnitCircle * SPAWN_RADIUS;
            Vector2 pos = new Vector2(centerX + offset.x, MAGNET_Y + offset.y);
            if (offset.magnitude < 0.5f) offset = offset.normalized * 0.5f;
            pos = new Vector2(centerX + offset.x, MAGNET_Y + offset.y);

            // Clamp to own half
            if (centerX < 0) pos.x = Mathf.Min(pos.x, -0.15f);
            else pos.x = Mathf.Max(pos.x, 0.15f);

            var go = SpriteFactory.CreateFiling(pos, color);
            var filing = go.GetComponent<IronFiling>();
            filing.baseColor = color;
            list.Add(filing);
            sceneObjects.Add(go);
        }
    }

    void CreateDivider()
    {
        var go = new GameObject("Divider");
        go.transform.SetParent(transform);
        sceneObjects.Add(go);

        divider = go.AddComponent<LineRenderer>();
        divider.useWorldSpace = true;
        divider.startWidth = 0.04f;
        divider.endWidth = 0.04f;
        divider.material = new Material(Shader.Find("Sprites/Default"));
        divider.sortingOrder = 10;
        divider.startColor = new Color(1f, 1f, 1f, 0.3f);
        divider.endColor = new Color(1f, 1f, 1f, 0.3f);
        divider.positionCount = 2;
        divider.SetPosition(0, new Vector3(0, -6, 0));
        divider.SetPosition(1, new Vector3(0, 6, 0));
    }

    void CreateLabel(string text, Vector2 worldPos, Color color)
    {
        var go = new GameObject("Label_" + text);
        go.transform.SetParent(transform);
        go.transform.position = new Vector3(worldPos.x, worldPos.y, 0);
        sceneObjects.Add(go);

        var tm = go.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 36;
        tm.characterSize = 0.12f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;
        tm.fontStyle = FontStyle.Bold;

        var mr = go.GetComponent<MeshRenderer>();
        mr.sortingOrder = 10;
    }

    void Update()
    {
        if (mfaMagnet == null || gaussMagnet == null) return;

        Vector2 mfaPos = mfaMagnet.transform.position;
        Vector2 gaussPos = gaussMagnet.transform.position;

        // MFA filings: F = S / r^α (using MFACore)
        int mfaNearCount = 0, mfaFarCount = 0;
        foreach (var filing in mfaFilings)
        {
            if (filing == null) continue;
            Vector2 fPos = filing.transform.position;
            Vector2 force = MFACore.ForceVector(fPos, mfaPos, fieldStrength);
            force += Random.insideUnitCircle * 0.25f;

            float dist = Vector2.Distance(fPos, mfaPos);
            if (dist < 0.4f) force += (fPos - mfaPos).normalized * 2f;

            // Keep on left side
            filing.ApplyForce(force * FORCE_SCALE);
            ClampToHalf(filing, true);
            filing.UpdateBrightness(MFACore.AttentionField(fieldStrength, dist));

            if (dist < 2f) mfaNearCount++;
            else mfaFarCount++;
        }

        // Gaussian filings: F = S · exp(-r²/2σ²)
        int gaussNearCount = 0, gaussFarCount = 0;
        foreach (var filing in gaussFilings)
        {
            if (filing == null) continue;
            Vector2 fPos = filing.transform.position;
            Vector2 dir = gaussPos - fPos;
            float dist = dir.magnitude;

            float gaussForce = fieldStrength * Mathf.Exp(-dist * dist / (2f * gaussSigma * gaussSigma));
            Vector2 force = (dist > 0.15f) ? dir.normalized * gaussForce : Vector2.zero;
            force += Random.insideUnitCircle * 0.25f;

            if (dist < 0.4f) force += (fPos - gaussPos).normalized * 2f;

            filing.ApplyForce(force * FORCE_SCALE);
            ClampToHalf(filing, false);

            float brightness = Mathf.Clamp01(gaussForce / 40f);
            filing.UpdateBrightness(gaussForce);

            if (dist < 2f) gaussNearCount++;
            else gaussFarCount++;
        }

        sim.SetInfo(
            $"S = {fieldStrength:F0}   \u03C3 = {gaussSigma:F1}\n\n" +
            $"MFA (Power Law):\n" +
            $"  Near (<2): {mfaNearCount}\n" +
            $"  Far (>2):  {mfaFarCount}\n\n" +
            $"Gaussian:\n" +
            $"  Near (<2): {gaussNearCount}\n" +
            $"  Far (>2):  {gaussFarCount}\n\n" +
            "Heavy tail = peripheral\n" +
            "awareness persists (MFA).\n" +
            "Gaussian predicts none."
        );
    }

    void ClampToHalf(IronFiling filing, bool leftHalf)
    {
        Vector3 pos = filing.transform.position;
        if (leftHalf)
            pos.x = Mathf.Min(pos.x, -0.1f);
        else
            pos.x = Mathf.Max(pos.x, 0.1f);
        filing.transform.position = pos;
    }

    public void Cleanup()
    {
        foreach (var go in sceneObjects)
            if (go != null) Destroy(go);
        sceneObjects.Clear();
        mfaFilings.Clear();
        gaussFilings.Clear();
    }

    void OnDestroy() => Cleanup();
}
