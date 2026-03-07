using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Scene 4: Multi-Task (Multiple Magnets)
/// B_total = Σ Sᵢ / (x - xᵢ)²
/// Energy conservation: E₁ + E₂ ≤ E_total - E_min
/// Click to place magnets, drag to move, right-click to remove
/// </summary>
public class MultiTaskScene : MonoBehaviour, ISceneController
{
    private List<Magnet> magnets = new List<Magnet>();
    private List<IronFiling> filings = new List<IronFiling>();
    private List<GameObject> sceneObjects = new List<GameObject>();
    private MFASimulator sim;

    private const int FILING_COUNT = 350;
    private const float FORCE_SCALE = 0.7f;
    private const float E_TOTAL = 100f;
    private const float E_MIN = 10f;
    private const int MAX_MAGNETS = 4;

    private float defaultS = 40f;

    void Start()
    {
        sim = MFASimulator.Instance;
        sim.SetTitle("Scene 4: Multi-Task");
        sim.SetDescription(
            "Click to place magnets.\n" +
            "Right-click to remove.\n" +
            "E\u2081+E\u2082+... = E_available (100% allocated)");

        // Create initial filings
        for (int i = 0; i < FILING_COUNT; i++)
        {
            Vector2 pos = Random.insideUnitCircle * 6f;
            var go = SpriteFactory.CreateFiling(pos, MFASimulator.FilingColor);
            filings.Add(go.GetComponent<IronFiling>());
            sceneObjects.Add(go);
        }

        // Place two initial magnets
        PlaceMagnet(new Vector2(-2f, 0));
        PlaceMagnet(new Vector2(2f, 0));

        // UI
        sim.AddSlider("New Magnet S", 10f, 80f, 40f, (v) => defaultS = v);
    }

    void Update()
    {
        HandleInput();
        UpdateFilings();
        UpdateInfo();
    }

    void HandleInput()
    {
        // Left-click: place new magnet (if not dragging an existing one)
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mp.z = 0;

            // Check if clicking near existing magnet (that one handles its own drag)
            bool nearExisting = false;
            foreach (var m in magnets)
            {
                if (m != null && Vector2.Distance(mp, m.transform.position) < 1f)
                {
                    nearExisting = true;
                    break;
                }
            }

            if (!nearExisting && magnets.Count < MAX_MAGNETS)
            {
                // Check if in the sim area (not on UI)
                if (mp.x < 4f) // rough check: side panel is on the right
                {
                    PlaceMagnet(mp);
                }
            }
        }

        // Right-click: remove nearest magnet
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mp.z = 0;

            Magnet nearest = null;
            float nearestDist = 1.5f;
            foreach (var m in magnets)
            {
                if (m == null) continue;
                float d = Vector2.Distance(mp, m.transform.position);
                if (d < nearestDist)
                {
                    nearestDist = d;
                    nearest = m;
                }
            }

            if (nearest != null)
            {
                magnets.Remove(nearest);
                sceneObjects.Remove(nearest.gameObject);
                Destroy(nearest.gameObject);
                EnforceEnergyConservation();
            }
        }
    }

    void PlaceMagnet(Vector2 pos)
    {
        // Cycle colors for visual distinction
        Color[] colors = {
            MFASimulator.MainMagnetColor,
            new Color(0.3f, 0.7f, 0.3f),
            MFASimulator.DistractorColor,
            new Color(0.7f, 0.3f, 0.7f)
        };
        Color c = colors[magnets.Count % colors.Length];

        var go = SpriteFactory.CreateMagnet(pos, defaultS, c, true);
        magnets.Add(go.GetComponent<Magnet>());
        sceneObjects.Add(go);
        EnforceEnergyConservation();
    }

    void EnforceEnergyConservation()
    {
        if (magnets.Count == 0) return;

        // E₁ + E₂ + ... ≤ E_total - E_min
        float available = MFACore.AvailableEnergy(E_TOTAL, E_MIN);
        float totalS = 0;
        foreach (var m in magnets)
            if (m != null) totalS += m.S;

        // If total exceeds available, scale down proportionally
        if (totalS > available)
        {
            float scale = available / totalS;
            foreach (var m in magnets)
            {
                if (m != null) m.S *= scale;
            }
        }
    }

    void UpdateFilings()
    {
        // Collect magnet data
        List<Vector2> mPositions = new List<Vector2>();
        List<float> mStrengths = new List<float>();
        foreach (var m in magnets)
        {
            if (m == null) continue;
            mPositions.Add(m.transform.position);
            mStrengths.Add(m.CurrentS);
        }

        if (mPositions.Count == 0)
        {
            // No magnets: filings drift randomly
            foreach (var f in filings)
                if (f != null) f.ApplyForce(Random.insideUnitCircle * 0.5f);
            return;
        }

        Vector2[] posArr = mPositions.ToArray();
        float[] strArr = mStrengths.ToArray();

        foreach (var filing in filings)
        {
            if (filing == null) continue;

            Vector2 fPos = filing.transform.position;
            Vector2 totalForce = Vector2.zero;

            for (int i = 0; i < posArr.Length; i++)
            {
                totalForce += MFACore.ForceVector(fPos, posArr[i], strArr[i]);

                // Soft repulsion from magnet center
                float d = Vector2.Distance(fPos, posArr[i]);
                if (d < 0.3f) totalForce += ((Vector2)fPos - posArr[i]).normalized * 1.5f;
            }

            totalForce += Random.insideUnitCircle * 0.15f;
            filing.ApplyForce(totalForce * FORCE_SCALE);

            // Color by nearest magnet
            float fieldTotal = MFACore.SuperposedField(fPos, posArr, strArr);
            filing.UpdateBrightness(fieldTotal);
        }
    }

    void UpdateInfo()
    {
        // Use base S (not CurrentS) for energy display — avoids sigma oscillation bug
        float totalE = 0;
        string magnetInfo = "";
        for (int i = 0; i < magnets.Count; i++)
        {
            if (magnets[i] == null) continue;
            float s = magnets[i].S;  // Base S, not CurrentS
            totalE += s;
            magnetInfo += $"Task {i + 1}: S={s:F1}\n";
        }

        float available = MFACore.AvailableEnergy(E_TOTAL, E_MIN);
        float eIdle = Mathf.Max(0f, available - totalE);
        string allocNote = eIdle > 0.5f ? $" (E_idle: {eIdle:F1})" : " (100%)";

        sim.SetInfo(
            magnetInfo +
            $"\nE_used: {totalE:F1} / {available:F1}{allocNote}\n" +
            $"E_total={E_TOTAL:F0}, E_min={E_MIN:F0}\n" +
            $"Magnets: {magnets.Count}/{MAX_MAGNETS}"
        );
    }

    public void Cleanup()
    {
        foreach (var go in sceneObjects)
            if (go != null) Destroy(go);
        sceneObjects.Clear();
        filings.Clear();
        magnets.Clear();
    }

    void OnDestroy() => Cleanup();
}
