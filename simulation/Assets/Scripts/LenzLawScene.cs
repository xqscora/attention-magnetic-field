using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Scene 5: Lenz's Law — Attentional Inertia
/// When the magnet moves fast, filings resist the change
/// (brief counter-movement before following)
/// Maps to task-switch cost in cognitive psychology
/// </summary>
public class LenzLawScene : MonoBehaviour, ISceneController
{
    private Magnet magnet;
    private List<IronFiling> filings = new List<IronFiling>();
    private List<GameObject> sceneObjects = new List<GameObject>();
    private MFASimulator sim;

    // Lenz parameters
    private float lenzStrength = 0.5f;
    private Vector2 lastMagnetPos;
    private Vector2 magnetVelocity;

    // Visual: trail showing magnet movement
    private LineRenderer trailRenderer;
    private Queue<Vector3> trailPositions = new Queue<Vector3>();
    private const int TRAIL_LENGTH = 30;

    private const int FILING_COUNT = 300;
    private const float FORCE_SCALE = 0.8f;

    void Start()
    {
        sim = MFASimulator.Instance;
        sim.SetTitle("Scene 5: Lenz's Law");
        sim.SetDescription(
            "Drag magnet FAST!\n" +
            "Filings resist change.\n" +
            "F_inertia = \u2212k\u00B7I\u00B7(dS/dt)\n" +
            "= task-switch cost");

        // Create magnet
        var magnetGO = SpriteFactory.CreateMagnet(Vector2.zero, 60f, MFASimulator.MainMagnetColor);
        magnet = magnetGO.GetComponent<Magnet>();
        sceneObjects.Add(magnetGO);
        lastMagnetPos = Vector2.zero;

        // Create filings with Lenz inertia enabled
        for (int i = 0; i < FILING_COUNT; i++)
        {
            Vector2 pos = Random.insideUnitCircle * 5f;
            if (pos.magnitude < 0.5f) pos = pos.normalized * 0.5f;

            var go = SpriteFactory.CreateFiling(pos, MFASimulator.FilingColor);
            var filing = go.GetComponent<IronFiling>();
            filing.useLenzInertia = true;
            filing.lenzFactor = lenzStrength;
            filings.Add(filing);
            sceneObjects.Add(go);
        }

        // Trail renderer
        var trailGO = new GameObject("Trail");
        trailRenderer = trailGO.AddComponent<LineRenderer>();
        trailRenderer.useWorldSpace = true;
        trailRenderer.startWidth = 0.05f;
        trailRenderer.endWidth = 0.01f;
        trailRenderer.startColor = new Color(0.5f, 0.7f, 1f, 0.5f);
        trailRenderer.endColor = new Color(0.5f, 0.7f, 1f, 0f);
        trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
        trailRenderer.sortingOrder = 0;
        sceneObjects.Add(trailGO);

        // Velocity indicator
        var velGO = new GameObject("VelocityIndicator");
        var velLR = velGO.AddComponent<LineRenderer>();
        velLR.useWorldSpace = true;
        velLR.startWidth = 0.06f;
        velLR.endWidth = 0.02f;
        velLR.startColor = MFASimulator.ThresholdColor;
        velLR.endColor = new Color(1f, 0.53f, 0f, 0.3f);
        velLR.material = new Material(Shader.Find("Sprites/Default"));
        velLR.positionCount = 2;
        velLR.sortingOrder = 3;
        sceneObjects.Add(velGO);

        // UI
        sim.AddSlider("S (Field Strength)", 10f, 120f, 60f, (v) => magnet.S = v);
        sim.AddSlider("Lenz Strength", 0f, 1f, 0.5f, (v) =>
        {
            lenzStrength = v;
            foreach (var f in filings)
                if (f != null) f.lenzFactor = v;
        });
    }

    void Update()
    {
        if (magnet == null) return;

        Vector2 currentPos = magnet.transform.position;

        // Calculate magnet velocity
        magnetVelocity = (currentPos - lastMagnetPos) / Mathf.Max(Time.deltaTime, 0.001f);
        lastMagnetPos = currentPos;

        float magnetSpeed = magnetVelocity.magnitude;
        float S = magnet.CurrentS;

        // Update trail
        UpdateTrail(currentPos);

        // Update velocity indicator arrow
        UpdateVelocityArrow(currentPos, magnetVelocity);

        foreach (var filing in filings)
        {
            if (filing == null) continue;

            Vector2 fPos = filing.transform.position;
            Vector2 force = MFACore.ForceVector(fPos, currentPos, S);

            // Lenz's Law: when magnet moves fast, filings RESIST the change
            // They briefly push OPPOSITE to magnet's movement direction
            // The closer to the magnet, the stronger the resistance
            if (magnetSpeed > 0.3f)
            {
                float dist = Vector2.Distance(fPos, currentPos);
                // Resistance is strong near magnet, fades with distance
                float proximity = Mathf.Clamp01(3f / Mathf.Max(dist, 0.5f));
                // Opposing force: push filings BACKWARD (opposite to magnet movement)
                Vector2 lenzForce = -magnetVelocity.normalized * magnetSpeed * lenzStrength * proximity * 1.5f;
                force += lenzForce;
            }

            // Small jitter
            force += Random.insideUnitCircle * 0.15f;

            // Soft repulsion from center
            float d = Vector2.Distance(fPos, currentPos);
            if (d < 0.4f) force += (fPos - currentPos).normalized * 2f;

            filing.ApplyForce(force * FORCE_SCALE);

            // Color: red tint when experiencing strong Lenz resistance
            float resistance = magnetSpeed * lenzStrength;
            Color filingColor = Color.Lerp(
                MFASimulator.FilingColor,
                new Color(1f, 0.4f, 0.2f),
                Mathf.Clamp01(resistance * 0.05f));
            filing.SetColor(filingColor);
            filing.UpdateBrightness(MFACore.AttentionField(S, d));
        }

        // Info (paper: F_inertia = −k·I_engaged·(dS/dt))
        sim.SetInfo(
            $"S = {S:F1}\n" +
            $"|dS/dt| \u2248 speed: {magnetSpeed:F1}\n" +
            $"k (Lenz factor): {lenzStrength:F2}\n" +
            $"Resistance: {magnetSpeed * lenzStrength:F1}\n" +
            $"\nF_inertia = \u2212k\u00B7I\u00B7(dS/dt)\n" +
            "Fast drag = high task-switch cost"
        );
    }

    void UpdateTrail(Vector2 pos)
    {
        trailPositions.Enqueue(pos);
        while (trailPositions.Count > TRAIL_LENGTH)
            trailPositions.Dequeue();

        var positions = trailPositions.ToArray();
        trailRenderer.positionCount = positions.Length;
        for (int i = 0; i < positions.Length; i++)
            trailRenderer.SetPosition(i, positions[i]);
    }

    void UpdateVelocityArrow(Vector2 origin, Vector2 velocity)
    {
        var velGO = sceneObjects.Count > FILING_COUNT + 2 ? sceneObjects[FILING_COUNT + 2] : null;
        if (velGO == null) return;

        var lr = velGO.GetComponent<LineRenderer>();
        if (lr == null) return;

        if (velocity.magnitude > 0.5f)
        {
            lr.enabled = true;
            Vector2 end = origin + velocity.normalized * Mathf.Min(velocity.magnitude * 0.1f, 2f);
            lr.SetPosition(0, origin);
            lr.SetPosition(1, end);
        }
        else
        {
            lr.enabled = false;
        }
    }

    public void Cleanup()
    {
        foreach (var go in sceneObjects)
            if (go != null) Destroy(go);
        sceneObjects.Clear();
        filings.Clear();
        trailPositions.Clear();
    }

    void OnDestroy() => Cleanup();
}
