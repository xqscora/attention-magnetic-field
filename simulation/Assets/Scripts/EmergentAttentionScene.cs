using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Scene 6: Emergent Attention — Paper-accurate simulation
///
/// KEY PHYSICS (from manuscript):
/// 1. E_total is ALWAYS 100% allocated: E_min + E_perm + E_electro = E_total
/// 2. Permanent magnets (automatized) → consume ~6% each (walking, blinking need a little attention)
/// 3. Electromagnets (controlled) → S proportional to energy share from E_available
/// 4. New stimulus appears → competes for E_available → others weaken → filings flow
/// 5. σ oscillation → stability variance on top of energy-based S
///
/// Magnet visual: core = darker (magnet itself); glow = filing count (resources attracted).
/// </summary>
public class EmergentAttentionScene : MonoBehaviour, ISceneController
{
    // ─── Objects ───
    private List<IronFiling> filings = new List<IronFiling>();
    private List<EmergentMagnetInfo> magnetInfos = new List<EmergentMagnetInfo>();
    private List<GameObject> sceneObjects = new List<GameObject>();
    private MFASimulator sim;

    // ─── Energy System (Formula ④) ───
    // E_total is ALWAYS fully allocated: E_min + E_perm + E_electro = E_total
    private float E_total = 100f;
    private float E_min = 10f;         // Reserved for peripheral monitoring (evolutionary)
    private float E_perm_cost = 0.06f;  // Permanent magnets consume ~6% each (walking, blinking need a little attention)
    private float E_available {
        get {
            float permCost = magnetInfos.Count(m => m.isPermanent && !m.isDead) * E_perm_cost * E_total;
            return Mathf.Max(0f, E_total - E_min - permCost);
        }
    }

    // ─── Parameters ───
    private float sigmaBase = 10f;
    private float spawnInterval = 6f;
    private float noveltyDecay = 0.03f; // How fast new stimuli lose novelty
    private float engagementBonusStrength = 1.5f;  // Winner-take-more: when >50% filings, S grows (flow/engagement deepening)
    private float dominantInertiaStrength = 1.2f;   // Lenz: dominant magnet resists capture (harder to distract from hyperfocus)
    private EmergentMagnetInfo _lastDominantMI;      // Dominant from previous frame (for inertia)

    private const int FILING_COUNT = 350;
    private const float FORCE_SCALE = 0.8f;
    private const int MAX_TEMP_MAGNETS = 5;
    private const float K_CONSTANT = 1f;    // k in S = k × E_i

    // ─── Spawn ───
    private float spawnTimer;
    private int tempMagnetCounter = 0;

    // ─── Labels (independent, NOT parented to magnets) ───
    private Dictionary<EmergentMagnetInfo, GameObject> labelObjects
        = new Dictionary<EmergentMagnetInfo, GameObject>();
    private Dictionary<EmergentMagnetInfo, TextMesh> labelTexts
        = new Dictionary<EmergentMagnetInfo, TextMesh>();

    // ─── Colors ───
    private static readonly Color PERM_COLOR_1 = new Color(0.85f, 0.65f, 0.15f);
    private static readonly Color PERM_COLOR_2 = new Color(0.75f, 0.2f, 0.2f);
    private static readonly Color[] TEMP_COLORS = {
        new Color(0.2f, 0.5f, 0.9f),
        new Color(0.2f, 0.75f, 0.4f),
        new Color(0.7f, 0.25f, 0.7f),
        new Color(0.1f, 0.7f, 0.7f),
        new Color(0.85f, 0.5f, 0.2f),
    };

    void Start()
    {
        sim = MFASimulator.Instance;
        sim.SetTitle("Scene 6: Emergent Attention");
        sim.SetDescription(
            "Paper-accurate simulation!\n" +
            "E_total is conserved:\n" +
            "one focus strengthens =\n" +
            "others must weaken.");

        // ─── Permanent magnets (don't consume E_total) ───
        SpawnPermanent(new Vector2(-5.5f, 0f), 30f, PERM_COLOR_1, "Auto A");
        SpawnPermanent(new Vector2(5.5f, 0f), 25f, PERM_COLOR_2, "Auto B");

        // ─── Filings ───
        for (int i = 0; i < FILING_COUNT; i++)
        {
            Vector2 pos = new Vector2(Random.Range(-7.5f, 7.5f), Random.Range(-4f, 4f));
            var go = SpriteFactory.CreateFiling(pos, MFASimulator.FilingColor);
            var filing = go.GetComponent<IronFiling>();
            filing.damping = 0.87f;
            filings.Add(filing);
            sceneObjects.Add(go);
        }

        // ─── First temp stimulus ───
        SpawnRandomTemp();
        spawnTimer = spawnInterval * 0.3f;

        // ─── UI ───
        sim.AddSlider("E_total", 40f, 200f, E_total, (v) => E_total = v);
        sim.AddSlider("Fluctuation σ", 0f, 30f, sigmaBase, (v) =>
        {
            sigmaBase = v;
            foreach (var mi in magnetInfos)
                if (mi.magnet != null)
                    mi.magnet.sigma = mi.isPermanent ? v * 0.1f : v;
        });
        sim.AddSlider("Spawn Interval", 2f, 15f, spawnInterval, (v) => spawnInterval = v);
        sim.AddSlider("Novelty Decay", 0f, 0.1f, noveltyDecay, (v) => noveltyDecay = v);
        sim.AddSlider("Engagement Bonus", 0f, 3f, engagementBonusStrength, (v) => engagementBonusStrength = v);
        sim.AddSlider("Dominant Inertia", 0f, 3f, dominantInertiaStrength, (v) => dominantInertiaStrength = v);
        sim.AddSlider("Perm cost %", 1f, 15f, E_perm_cost * 100f, (v) => E_perm_cost = v / 100f);
    }

    void Update()
    {
        // ═══ 1. Update stimulus lifecycles ═══
        UpdateLifecycles();

        // ═══ 2. ENERGY ALLOCATION (the core of MFA!) ═══
        AllocateEnergy();

        // ═══ 3. Spawn new stimuli ═══
        spawnTimer += Time.deltaTime;
        int tempCount = magnetInfos.Count(m => !m.isPermanent && !m.isDead);
        if (spawnTimer >= spawnInterval && tempCount < MAX_TEMP_MAGNETS)
        {
            SpawnRandomTemp();
            spawnTimer = 0f;
        }

        // ═══ 4. Physics: move filings ═══
        UpdateFilings();

        // ═══ 5. Visuals ═══
        UpdateVisuals();

        // ═══ 6. Info ═══
        UpdateInfo();
    }

    // ══════════════════════════════════════════════
    //  ENERGY ALLOCATION — Formula ④
    //  This is what makes attention flow REAL
    // ══════════════════════════════════════════════

    void AllocateEnergy()
    {
        // Collect all active electromagnets (NOT permanent magnets)
        var electros = magnetInfos.Where(m => !m.isPermanent && !m.isDead && m.magnet != null).ToList();
        if (electros.Count == 0) return;

        // Filing counts from previous frame (updated in UpdateFilings)
        int totalElectroFilings = electros.Sum(m => m.filingCount);
        if (totalElectroFilings < 1) totalElectroFilings = 1;

        // Winner-take-more (engagement deepening): Flow state is self-reinforcing
        // When a magnet has >50% of filings, it gets an S bonus → hyperfocus
        // Psychology: Csikszentmihalyi — flow creates positive feedback; engagement deepens
        float totalEffectiveAttract = 0f;
        foreach (var mi in electros)
        {
            float filingShare = (float)mi.filingCount / totalElectroFilings;
            float engagementBonus = (filingShare > 0.5f)
                ? 1f + engagementBonusStrength * (filingShare - 0.5f)  // 50%→1.0, 100%→1.75
                : 1f;
            float effectiveAttract = mi.attractiveness * engagementBonus;
            mi.effectiveAttractiveness = effectiveAttract;
            totalEffectiveAttract += effectiveAttract;
        }
        if (totalEffectiveAttract < 0.01f) totalEffectiveAttract = 0.01f;

        float totalBaseAttract = electros.Sum(m => m.attractiveness);
        if (totalBaseAttract < 0.01f) totalBaseAttract = 0.01f;

        float eAvail = E_available;
        foreach (var mi in electros)
        {
            // Base allocation (no engagement bonus) — for magnet SIZE = intrinsic pull
            float baseShare = mi.attractiveness / totalBaseAttract;
            mi.baseAllocatedS = K_CONSTANT * eAvail * baseShare;

            // Effective allocation (with engagement bonus) — for PHYSICS
            // Sum of allocated energy = E_available exactly (energy always 100% used)
            float effectiveShare = mi.effectiveAttractiveness / totalEffectiveAttract;
            float energy = eAvail * effectiveShare;
            mi.allocatedEnergy = energy;  // For display (avoids sigma oscillation bug)
            mi.magnet.S = K_CONSTANT * energy;

            float maxS = K_CONSTANT * energy * 1.15f;
            if (mi.magnet.CurrentS > maxS)
                mi.magnet.currentS = maxS;
        }
    }

    // ══════════════════════════════════════════════
    //  LIFECYCLES
    // ══════════════════════════════════════════════

    void UpdateLifecycles()
    {
        for (int i = magnetInfos.Count - 1; i >= 0; i--)
        {
            var mi = magnetInfos[i];
            if (mi.isDead || mi.isPermanent) continue;

            mi.age += Time.deltaTime;

            // Attractiveness lifecycle:
            // Rise phase → Peak → Gradual novelty decay
            float lifeMult;
            if (mi.age < mi.riseTime)
            {
                // Rising: attractiveness grows
                mi.phase = MagnetPhase.Rising;
                float t = mi.age / mi.riseTime;
                lifeMult = t * t;
            }
            else
            {
                // After rise: sustained but novelty slowly decays
                mi.phase = MagnetPhase.Peak;
                float timeSincePeak = mi.age - mi.riseTime;
                lifeMult = Mathf.Max(0.02f, 1f - noveltyDecay * timeSincePeak);

                // If attractiveness drops very low, it fades away
                if (lifeMult < 0.05f && mi.attractiveness * lifeMult < 1f)
                {
                    mi.phase = MagnetPhase.Dead;
                    mi.isDead = true;
                    DestroyMagnetVisuals(mi);
                    continue;
                }
            }

            mi.attractiveness = mi.baseAttractiveness * lifeMult;
        }
    }

    // ══════════════════════════════════════════════
    //  FILING PHYSICS
    // ══════════════════════════════════════════════

    void UpdateFilings()
    {
        var active = magnetInfos.Where(m => !m.isDead && m.magnet != null).ToList();
        if (active.Count == 0) return;

        // Reset distribution counter
        foreach (var mi in active) mi.filingCount = 0;

        // Lenz inertia: use dominant from PREVIOUS frame (filings not yet recounted this frame)
        EmergentMagnetInfo dominantForInertia = _lastDominantMI;

        foreach (var filing in filings)
        {
            if (filing == null) continue;

            Vector2 fPos = filing.transform.position;
            Vector2 totalForce = Vector2.zero;
            float nearestDist = float.MaxValue;
            EmergentMagnetInfo nearestMI = null;

            foreach (var mi in active)
            {
                Vector2 mPos = mi.magnet.transform.position;
                float dist = Vector2.Distance(fPos, mPos);

                if (dist < nearestDist) { nearestDist = dist; nearestMI = mi; }

                // Standard MFA force: F = S/r² direction
                totalForce += MFACore.ForceVector(fPos, mPos, mi.magnet.CurrentS);

                // Soft center repulsion
                if (dist < 0.5f)
                    totalForce += (fPos - mPos).normalized * 2.5f;
            }

            // Organic jitter
            totalForce += Random.insideUnitCircle * 0.08f;

            // Sparse inter-filing repulsion
            if (Random.value < 0.06f)
            {
                for (int j = 0; j < 3; j++)
                {
                    int ri = Random.Range(0, filings.Count);
                    if (filings[ri] == null || filings[ri] == filing) continue;
                    Vector2 diff = fPos - (Vector2)filings[ri].transform.position;
                    float d = diff.magnitude;
                    if (d < 0.4f && d > 0.01f)
                        totalForce += diff.normalized * (0.4f - d) * 2f;
                }
            }

            // Lenz inertia: when filing is "owned" by dominant magnet, add extra pull (resists capture)
            if (dominantForInertia != null && nearestMI == dominantForInertia)
            {
                Vector2 toDominant = (Vector2)dominantForInertia.magnet.transform.position - fPos;
                float distToDom = toDominant.magnitude;
                if (distToDom > 0.1f)
                {
                    float inertiaPull = dominantInertiaStrength * (1f - Mathf.Clamp01(distToDom / 4f));
                    totalForce += toDominant.normalized * inertiaPull;
                }
            }

            filing.ApplyForce(totalForce * FORCE_SCALE);

            // Track distribution + color
            if (nearestMI != null)
            {
                nearestMI.filingCount++;
                Color c = Color.Lerp(MFASimulator.FilingColor, nearestMI.color,
                    Mathf.Clamp01(1f - nearestDist * 0.15f));
                filing.SetColor(c);
                filing.UpdateBrightness(MFACore.AttentionField(nearestMI.magnet.CurrentS, nearestDist));
            }
        }

        // Update dominant for next frame (Lenz inertia)
        int totalF = active.Sum(m => m.filingCount);
        _lastDominantMI = null;
        if (totalF > 0)
        {
            foreach (var mi in active)
            {
                if ((float)mi.filingCount / totalF > 0.5f)
                {
                    _lastDominantMI = mi;
                    break;
                }
            }
        }
    }

    // ══════════════════════════════════════════════
    //  VISUALS — smooth, no bouncing, paper-accurate
    // ══════════════════════════════════════════════

    void UpdateVisuals()
    {
        foreach (var mi in magnetInfos)
        {
            if (mi.isDead || mi.magnet == null) continue;

            // ─── Magnet size = S (intrinsic pull); Glow = filingCount (actual capture) ───
            // Size uses baseAllocatedS for electros (intrinsic demand) so it's visually distinct from glow
            float targetScale;
            if (mi.isPermanent)
                targetScale = Mathf.Lerp(0.35f, 0.55f, Mathf.Clamp01(mi.magnet.CurrentS / 60f));
            else
                targetScale = Mathf.Lerp(0.12f, 0.65f, Mathf.Clamp01(mi.baseAllocatedS / 90f));

            float curScale = mi.gameObject.transform.localScale.x;
            float smooth = Mathf.Lerp(curScale, targetScale, Time.deltaTime * 2f);
            mi.gameObject.transform.localScale = Vector3.one * smooth;

            // ─── Magnet CORE: darker color (distinct from glow = filings around) ───
            // Core = the magnet itself; glow = field/filings attracted to it
            if (mi.spriteRenderer != null)
            {
                Color coreColor = mi.color * 0.55f;  // Darker core (magnet itself)
                coreColor.a = 1f;
                if (mi.isPermanent)
                {
                    mi.spriteRenderer.color = coreColor;
                }
                else
                {
                    float alpha = Mathf.Clamp01(mi.attractiveness / mi.baseAttractiveness + 0.2f);
                    coreColor.a = alpha;
                    mi.spriteRenderer.color = coreColor;
                }
            }

            // ─── Glow: filing count (filings around magnet) ───
            if (mi.glowSR != null)
            {
                float ratio = (float)mi.filingCount / Mathf.Max(FILING_COUNT, 1);
                float targetGlow = Mathf.Lerp(0.5f, 6f, ratio);
                mi.currentGlowScale = Mathf.Lerp(mi.currentGlowScale, targetGlow, Time.deltaTime * 2f);
                mi.glowGO.transform.localScale = Vector3.one * mi.currentGlowScale;

                float glowA = Mathf.Lerp(0.02f, 0.18f, ratio);
                if (!mi.isPermanent) glowA *= Mathf.Clamp01(mi.attractiveness / mi.baseAttractiveness + 0.1f);
                Color gc = mi.color; gc.a = glowA;
                mi.glowSR.color = gc;
            }

            // ─── Label (independent, NOT child of magnet) ───
            if (labelObjects.ContainsKey(mi) && labelObjects[mi] != null)
            {
                // Follow magnet position
                labelObjects[mi].transform.position =
                    mi.gameObject.transform.position + Vector3.up * 0.8f;

                if (labelTexts.ContainsKey(mi))
                {
                    var tm = labelTexts[mi];
                    if (mi.isPermanent)
                    {
                        tm.text = $"[perm] {mi.name}";
                        tm.color = mi.color;
                    }
                    else
                    {
                        string phase = mi.phase == MagnetPhase.Rising ? "+" :
                                       mi.phase == MagnetPhase.Peak ? "" : "";
                        float energyPct = (E_available > 0.01f) ? (mi.allocatedEnergy / E_available) * 100f : 0f;
                        tm.text = $"{mi.name} {phase} ({energyPct:F0}%)";
                        Color lc = mi.color;
                        lc.a = mi.spriteRenderer != null ? mi.spriteRenderer.color.a : 1f;
                        tm.color = lc;
                    }
                }
            }
        }
    }

    // ══════════════════════════════════════════════
    //  SPAWNING
    // ══════════════════════════════════════════════

    void SpawnPermanent(Vector2 pos, float fixedS, Color color, string name)
    {
        var go = SpriteFactory.CreateMagnet(pos, fixedS, color, false);
        var magnet = go.GetComponent<Magnet>();
        magnet.sigma = sigmaBase * 0.1f;
        magnet.omega = Random.Range(0.8f, 1.5f);
        magnet.externalVisual = true;
        go.transform.localScale = Vector3.one * 0.45f;

        var mi = new EmergentMagnetInfo {
            magnet = magnet, gameObject = go,
            spriteRenderer = go.GetComponent<SpriteRenderer>(),
            isPermanent = true, name = name, color = color,
            baseAllocatedS = fixedS,  // Permanent: fixed S
            phase = MagnetPhase.Peak
        };

        // Glow (child of magnet — moves with it, glow scale is relative)
        CreateGlow(mi, go);
        // Permanent ring
        CreatePermRing(mi, go);
        // Label (NOT child of magnet!)
        CreateLabel(mi, pos + Vector2.up * 0.8f);

        magnetInfos.Add(mi);
        sceneObjects.Add(go);
    }

    void SpawnRandomTemp()
    {
        // Position: not too close to existing magnets
        Vector2 pos = FindSpawnPosition();
        tempMagnetCounter++;

        // Attractiveness: wide range (some dominant, some minor)
        float attract;
        float roll = Random.value;
        if (roll < 0.12f)
            attract = Random.Range(60f, 100f);  // 12%: dominant stimulus
        else if (roll < 0.30f)
            attract = Random.Range(5f, 15f);    // 18%: minor distraction
        else
            attract = Random.Range(18f, 50f);   // 70%: normal

        Color color = TEMP_COLORS[(tempMagnetCounter - 1) % TEMP_COLORS.Length];
        string name = $"Stim {tempMagnetCounter}";

        var go = SpriteFactory.CreateMagnet(pos, 1f, color, false);
        var magnet = go.GetComponent<Magnet>();
        magnet.sigma = sigmaBase;
        magnet.omega = Random.Range(1.5f, 4f);
        magnet.externalVisual = true;
        go.transform.localScale = Vector3.one * 0.1f; // Start tiny

        var mi = new EmergentMagnetInfo {
            magnet = magnet, gameObject = go,
            spriteRenderer = go.GetComponent<SpriteRenderer>(),
            isPermanent = false, name = name, color = color,
            baseAttractiveness = attract, attractiveness = 0.01f,
            baseAllocatedS = 5f,  // Initial; AllocateEnergy will set properly
            age = 0f, riseTime = Random.Range(1f, 3f),
            phase = MagnetPhase.Rising
        };

        CreateGlow(mi, go);
        CreateLabel(mi, pos + Vector2.up * 0.8f);

        magnetInfos.Add(mi);
        sceneObjects.Add(go);
    }

    Vector2 FindSpawnPosition()
    {
        for (int a = 0; a < 20; a++)
        {
            Vector2 pos = new Vector2(Random.Range(-5.5f, 5.5f), Random.Range(-3.5f, 3.5f));
            bool valid = true;
            foreach (var mi in magnetInfos)
            {
                if (mi.isDead || mi.magnet == null) continue;
                if (Vector2.Distance(pos, mi.magnet.transform.position) < 2.5f)
                { valid = false; break; }
            }
            if (valid) return pos;
        }
        return new Vector2(Random.Range(-5.5f, 5.5f), Random.Range(-3.5f, 3.5f));
    }

    void CreateGlow(EmergentMagnetInfo mi, GameObject parent)
    {
        var glowGO = new GameObject("Glow");
        glowGO.transform.SetParent(parent.transform);
        glowGO.transform.localPosition = Vector3.zero;
        var sr = glowGO.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.CircleSprite;
        Color gc = mi.color; gc.a = 0.03f;
        sr.color = gc;
        sr.sortingOrder = 2;
        glowGO.transform.localScale = Vector3.one * 1f;

        mi.glowGO = glowGO;
        mi.glowSR = sr;
        mi.currentGlowScale = 1f;
    }

    void CreatePermRing(EmergentMagnetInfo mi, GameObject parent)
    {
        var ring = new GameObject("Ring");
        ring.transform.SetParent(parent.transform);
        ring.transform.localPosition = Vector3.zero;
        var sr = ring.AddComponent<SpriteRenderer>();
        sr.sprite = SpriteFactory.CircleSprite;
        Color rc = mi.color; rc.a = 0.15f;
        sr.color = rc;
        sr.sortingOrder = 3;
        ring.transform.localScale = Vector3.one * 3f;
    }

    // Label: INDEPENDENT of magnet (no parent!) → no scale inheritance
    void CreateLabel(EmergentMagnetInfo mi, Vector2 pos)
    {
        var go = new GameObject("Label_" + mi.name);
        go.transform.position = pos;
        // NOT parented to magnet!

        var tm = go.AddComponent<TextMesh>();
        tm.text = mi.name;
        tm.fontSize = 48;
        tm.characterSize = 0.045f;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = mi.color;
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 10;

        labelObjects[mi] = go;
        labelTexts[mi] = tm;
        sceneObjects.Add(go);
    }

    void DestroyMagnetVisuals(EmergentMagnetInfo mi)
    {
        if (mi.gameObject != null)
        {
            sceneObjects.Remove(mi.gameObject);
            Destroy(mi.gameObject);
        }
        if (labelObjects.ContainsKey(mi) && labelObjects[mi] != null)
        {
            sceneObjects.Remove(labelObjects[mi]);
            Destroy(labelObjects[mi]);
            labelObjects.Remove(mi);
            labelTexts.Remove(mi);
        }
    }

    // ══════════════════════════════════════════════
    //  INFO
    // ══════════════════════════════════════════════

    void UpdateInfo()
    {
        // Use allocatedEnergy (not CurrentS) to avoid sigma oscillation showing 60% or 100%+
        float electroUsed = 0f;
        var electros = magnetInfos.Where(m => !m.isPermanent && !m.isDead).ToList();
        foreach (var mi in electros) electroUsed += mi.allocatedEnergy;

        float permCount = magnetInfos.Count(m => m.isPermanent && !m.isDead);
        float permCost = permCount * E_perm_cost * E_total;
        float eAvail = E_available;
        float eIdle = Mathf.Max(0f, eAvail - electroUsed);  // Unallocated when no/few electros

        string info = $"E_total: {E_total:F0}  (100% allocated)\n";
        info += $"  E_min: {E_min:F0}  E_perm: {permCost:F1}  E_electro: {electroUsed:F1}";
        if (eIdle > 0.5f) info += $"  E_idle: {eIdle:F1}";
        info += "\n";
        info += $"E_available: {eAvail:F0}\n\n";

        info += "-- Permanent (~6% each) --\n";
        foreach (var mi in magnetInfos.Where(m => m.isPermanent && !m.isDead))
            info += $"  {mi.name}: S={mi.magnet.CurrentS:F0} ({mi.filingCount})\n";

        info += "\n-- Electro (share E_available) --\n";
        foreach (var mi in magnetInfos.Where(m => !m.isPermanent && !m.isDead))
        {
            float pct = E_available > 0.01f ? (mi.allocatedEnergy / E_available) * 100f : 0f;
            info += $"  {mi.name}: {mi.allocatedEnergy:F0} ({mi.filingCount}) [{pct:F0}%]\n";
        }

        sim.SetInfo(info);
    }

    // ══════════════════════════════════════════════
    //  CLEANUP
    // ══════════════════════════════════════════════

    public void Cleanup()
    {
        foreach (var go in sceneObjects)
            if (go != null) Destroy(go);
        sceneObjects.Clear();
        filings.Clear();
        magnetInfos.Clear();
        labelObjects.Clear();
        labelTexts.Clear();
    }

    void OnDestroy() => Cleanup();
}

// ══════════════════════════════════════════════
//  DATA STRUCTURES
// ══════════════════════════════════════════════

public enum MagnetPhase { Rising, Peak, Decaying, Dead }

public class EmergentMagnetInfo
{
    public Magnet magnet;
    public GameObject gameObject;
    public SpriteRenderer spriteRenderer;

    public bool isPermanent;
    public string name;
    public Color color;

    // Energy competition (electromagnets only)
    public float baseAttractiveness;  // Intrinsic demand (I × n)
    public float attractiveness;      // Current demand (decays with novelty)
    public float effectiveAttractiveness;  // attractiveness × engagement bonus (winner-take-more)
    public float baseAllocatedS;      // S from allocation WITHOUT engagement bonus (magnet size = intrinsic pull)
    public float allocatedEnergy;    // Energy allocated (for display; avoids sigma oscillation showing >100%)

    // Glow = filingCount (actual resources captured) — visually distinct from magnet size (S)
    public GameObject glowGO;
    public SpriteRenderer glowSR;
    public float currentGlowScale;

    // Lifecycle
    public float age;
    public float riseTime;
    public MagnetPhase phase;
    public bool isDead;

    // Distribution tracking
    public int filingCount;
}
