using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Attention Life Scene: Auto-evolving demonstration of all MFA phenomena
/// in one continuous, naturalistic sequence. No user interaction needed.
///
/// Timeline:
///   Phase 1 (0-8s):   Normal focus — single strong magnet, stable S
///   Phase 2 (8-16s):  Distraction — second magnet appears, competes
///   Phase 3 (16-22s): Task switch — Lenz's Law inertia resists change
///   Phase 4 (22-32s): ADHD mode — S oscillates (high σ), filings scatter
///   Phase 5 (32-42s): Hyperfocus — very high stable S, all filings locked in
///   Phase 6 (42-48s): Flow state — optimal S, low σ, balanced energy
///   Then loops.
///
/// This replaces separate Distraction, Lenz, Individual Diff, and Emergent scenes
/// with one coherent narrative that mirrors real cognitive experience.
/// </summary>
public class AttentionLifeScene : MonoBehaviour, ISceneController
{
    private MFASimulator sim;
    private List<GameObject> sceneObjects = new List<GameObject>();
    private List<IronFiling> filings = new List<IronFiling>();

    private Magnet mainMagnet;
    private Magnet distractorMagnet;
    private GameObject distractorGO;

    private const int FILING_COUNT = 250;
    private const float FORCE_SCALE = 0.7f;
    private const float SPAWN_RADIUS = 6f;
    private const float CYCLE_LENGTH = 48f;

    // Threshold circle
    private LineRenderer thresholdCircle;

    // Phase tracking
    private float timer = 0f;
    private int currentPhase = -1;

    // ADHD oscillation params
    private float sigma = 0f;

    struct PhaseInfo
    {
        public float startTime;
        public string name;
        public string description;
        public Color magnetColor;
    }

    private PhaseInfo[] phases = new PhaseInfo[]
    {
        new PhaseInfo { startTime = 0f,  name = "FOCUS",
            description = "Single task, stable S.\nResources cluster around focus.",
            magnetColor = new Color(0.2f, 0.5f, 0.9f) },
        new PhaseInfo { startTime = 8f,  name = "DISTRACTION",
            description = "Competing stimulus appears.\nCapture criterion: F_p > S/r\u00B2",
            magnetColor = new Color(0.2f, 0.5f, 0.9f) },
        new PhaseInfo { startTime = 16f, name = "TASK SWITCH",
            description = "Switching to new task.\nLenz's Law: system resists change.",
            magnetColor = new Color(0.9f, 0.6f, 0.2f) },
        new PhaseInfo { startTime = 22f, name = "ADHD (high \u03C3)",
            description = "Unstable S(t) = S\u2080 + A\u00B7sin(\u03C9t)\nField oscillates, resources scatter.",
            magnetColor = new Color(0.9f, 0.3f, 0.3f) },
        new PhaseInfo { startTime = 32f, name = "HYPERFOCUS",
            description = "Very high stable S, low \u03C3.\nAll resources locked in. No spillover.",
            magnetColor = new Color(0.4f, 0.2f, 0.9f) },
        new PhaseInfo { startTime = 42f, name = "FLOW STATE",
            description = "Optimal S, low \u03C3, full E_total.\nPeak performance: stable + efficient.",
            magnetColor = new Color(0.2f, 0.8f, 0.5f) },
    };

    void Start()
    {
        sim = MFASimulator.Instance;
        sim.SetTitle("Attention Life");
        sim.SetDescription("Auto-evolving demo.\nWatch how attention\nshifts through states.");

        // Main magnet
        var mainGO = SpriteFactory.CreateMagnet(Vector2.zero, 50f, phases[0].magnetColor);
        mainMagnet = mainGO.GetComponent<Magnet>();
        mainMagnet.isDraggable = false;
        sceneObjects.Add(mainGO);

        // Distractor magnet (hidden initially)
        distractorGO = SpriteFactory.CreateMagnet(new Vector2(4f, 3f), 30f, MFASimulator.DistractorColor);
        distractorMagnet = distractorGO.GetComponent<Magnet>();
        distractorMagnet.isDraggable = false;
        distractorGO.SetActive(false);
        sceneObjects.Add(distractorGO);

        // Threshold circle
        CreateThresholdCircle();

        // Filings
        for (int i = 0; i < FILING_COUNT; i++)
        {
            Vector2 pos = Random.insideUnitCircle * SPAWN_RADIUS;
            if (pos.magnitude < 0.5f) pos = pos.normalized * 0.5f;
            var go = SpriteFactory.CreateFiling(pos, MFASimulator.FilingColor);
            filings.Add(go.GetComponent<IronFiling>());
            sceneObjects.Add(go);
        }

        timer = 0f;
    }

    void CreateThresholdCircle()
    {
        var go = new GameObject("ThresholdCircle");
        go.transform.SetParent(transform);
        sceneObjects.Add(go);

        thresholdCircle = go.AddComponent<LineRenderer>();
        thresholdCircle.useWorldSpace = true;
        thresholdCircle.startWidth = 0.025f;
        thresholdCircle.endWidth = 0.025f;
        thresholdCircle.material = new Material(Shader.Find("Sprites/Default"));
        thresholdCircle.sortingOrder = 2;
        thresholdCircle.startColor = MFASimulator.ThresholdColor;
        thresholdCircle.endColor = MFASimulator.ThresholdColor;

        int segments = 64;
        thresholdCircle.positionCount = segments + 1;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= CYCLE_LENGTH) timer -= CYCLE_LENGTH;

        int phase = GetPhase(timer);
        if (phase != currentPhase)
        {
            currentPhase = phase;
            OnPhaseChange(phase);
        }

        float S = GetCurrentS(timer, phase);
        Vector2 mainPos = mainMagnet.transform.position;

        // Update threshold circle
        float threshRadius = MFACore.ThresholdRadius(S, 5f);
        UpdateThresholdCircle(mainPos, Mathf.Min(threshRadius, 8f));

        // Update main magnet visual
        mainMagnet.S = S;

        // Distractor visible only in phase 1 (distraction)
        bool showDistractor = (phase == 1);
        if (distractorGO.activeSelf != showDistractor)
            distractorGO.SetActive(showDistractor);

        // Move filings
        foreach (var filing in filings)
        {
            if (filing == null) continue;
            Vector2 fPos = filing.transform.position;

            // Main magnet force
            Vector2 force = MFACore.ForceVector(fPos, mainPos, S);

            // Distractor force (phase 1 only)
            if (showDistractor)
            {
                Vector2 dPos = distractorMagnet.transform.position;
                force += MFACore.ForceVector(fPos, dPos, distractorMagnet.CurrentS);
            }

            force += Random.insideUnitCircle * 0.3f;

            // Soft repulsion near center
            float dist = Vector2.Distance(fPos, mainPos);
            if (dist < 0.4f) force += (fPos - mainPos).normalized * 1.5f;

            filing.ApplyForce(force * FORCE_SCALE);

            float localField = MFACore.AttentionField(S, dist);
            filing.UpdateBrightness(localField);
        }

        // Info panel
        string phaseName = phases[phase].name;
        float phaseTime = timer - phases[phase].startTime;
        float nextPhase = (phase < phases.Length - 1) ? phases[phase + 1].startTime : CYCLE_LENGTH;
        float remaining = nextPhase - timer;

        sim.SetInfo(
            $"\u25B6 {phaseName}\n" +
            $"  S = {S:F1}   \u03C3 = {sigma:F2}\n" +
            $"  Next phase: {remaining:F0}s\n\n" +
            phases[phase].description +
            $"\n\nTime: {timer:F0}s / {CYCLE_LENGTH:F0}s"
        );
    }

    int GetPhase(float t)
    {
        for (int i = phases.Length - 1; i >= 0; i--)
            if (t >= phases[i].startTime) return i;
        return 0;
    }

    void OnPhaseChange(int phase)
    {
        // Update main magnet color
        var sr = mainMagnet.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = phases[phase].magnetColor;

        // Phase 2 (task switch): move main magnet to new position
        if (phase == 2)
            mainMagnet.transform.position = new Vector3(3f, -2f, 0);
        else if (phase != 1)
            mainMagnet.transform.position = Vector3.zero;
    }

    float GetCurrentS(float t, int phase)
    {
        switch (phase)
        {
            case 0: // Normal focus
                sigma = 0.05f;
                return 50f;

            case 1: // Distraction
                sigma = 0.1f;
                return 40f;

            case 2: // Task switch (Lenz inertia — S ramps up slowly)
                sigma = 0.15f;
                float switchProgress = (t - 16f) / 6f;
                return Mathf.Lerp(15f, 60f, Mathf.SmoothStep(0, 1, switchProgress));

            case 3: // ADHD (high sigma oscillation)
                sigma = 0.8f;
                float adhdS = 35f + 30f * Mathf.Sin(t * 2.5f) + 10f * Mathf.Sin(t * 4.1f);
                return Mathf.Max(5f, adhdS);

            case 4: // Hyperfocus
                sigma = 0.02f;
                return 120f;

            case 5: // Flow state
                sigma = 0.03f;
                return 80f;

            default:
                return 50f;
        }
    }

    void UpdateThresholdCircle(Vector2 center, float radius)
    {
        int segments = thresholdCircle.positionCount - 1;
        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float x = center.x + Mathf.Cos(angle) * radius;
            float y = center.y + Mathf.Sin(angle) * radius;
            thresholdCircle.SetPosition(i, new Vector3(x, y, 0));
        }
    }

    public void Cleanup()
    {
        foreach (var go in sceneObjects)
            if (go != null) Destroy(go);
        sceneObjects.Clear();
        filings.Clear();
        currentPhase = -1;
        timer = 0f;
    }

    void OnDestroy() => Cleanup();
}
