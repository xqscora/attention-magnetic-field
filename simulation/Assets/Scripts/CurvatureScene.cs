using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Scene 7: Geometric Curvature Visualization
/// Core insight: MFA field equations are the weak-curvature limit of a geometric
/// theory where attention curves neural connectivity space.
///
/// References:
/// - PrabhatSdr/spacetime-visualization: Z = -2/sqrt(R) gravity well formula
/// - timhutton/GravityIsNotAForce: geodesic visualization technique
/// - Flamm's paraboloid for the cross-section profile
/// </summary>
public class CurvatureScene : MonoBehaviour, ISceneController
{
    private MFASimulator sim;
    private Magnet magnet;
    private List<IronFiling> filings = new List<IronFiling>();
    private List<GameObject> sceneObjects = new List<GameObject>();

    // Grid (top-down view, upper portion of screen)
    private const int GRID_SIZE = 23;
    private const float GRID_EXTENT = 5f;
    private const float GRID_Y_OFFSET = 1.2f;
    private Vector2[,] originalPos;
    private LineRenderer[] hLines;
    private LineRenderer[] vLines;

    // Cross-section well profile (bottom of screen)
    private LineRenderer wellProfile;
    private LineRenderer wellProfileFlat;
    private const float WELL_Y = -4.2f;
    private const float WELL_HEIGHT = 2.5f;
    private const int WELL_POINTS = 120;

    // Geodesic paths
    private List<LineRenderer> geodesics = new List<LineRenderer>();

    // Labels
    private List<GameObject> labelObjects = new List<GameObject>();

    // Animation
    private float targetCurvature = 50f;
    private float currentCurvature = 0f;

    private const int FILING_COUNT = 100;
    private const float FORCE_SCALE = 0.6f;

    void Start()
    {
        sim = MFASimulator.Instance;
        sim.SetTitle("Scene 7: Curvature");
        sim.SetDescription(
            "\"Attention is not a force;\n" +
            " it is curvature.\"\n\n" +
            "Top: connectivity manifold\n" +
            "Bottom: cross-section well\n" +
            "Pink: geodesic paths"
        );

        // Grid positions (centered above midpoint)
        originalPos = new Vector2[GRID_SIZE, GRID_SIZE];
        float step = 2f * GRID_EXTENT / (GRID_SIZE - 1);
        for (int i = 0; i < GRID_SIZE; i++)
            for (int j = 0; j < GRID_SIZE; j++)
                originalPos[i, j] = new Vector2(
                    -GRID_EXTENT + j * step,
                    -GRID_EXTENT + i * step + GRID_Y_OFFSET
                );

        CreateGrid();
        CreateWellProfile();
        CreateGeodesicPaths();

        var magnetGO = SpriteFactory.CreateMagnet(
            new Vector2(0, GRID_Y_OFFSET), targetCurvature, MFASimulator.MainMagnetColor);
        magnet = magnetGO.GetComponent<Magnet>();
        sceneObjects.Add(magnetGO);

        SpawnFilings();

        sim.AddSlider("S (Curvature)", 0f, 150f, 50f, (v) =>
        {
            targetCurvature = v;
            magnet.S = v;
        });
    }

    // ═══════════════════════════════════════════════════
    // Grid (top-down Flamm's paraboloid view)
    // ═══════════════════════════════════════════════════
    void CreateGrid()
    {
        hLines = new LineRenderer[GRID_SIZE];
        vLines = new LineRenderer[GRID_SIZE];
        for (int i = 0; i < GRID_SIZE; i++)
        {
            hLines[i] = MakeGridLine("H_" + i, -1);
            hLines[i].positionCount = GRID_SIZE;
            vLines[i] = MakeGridLine("V_" + i, -1);
            vLines[i].positionCount = GRID_SIZE;
        }
    }

    // ═══════════════════════════════════════════════════
    // Cross-section well profile (side view, Flamm's paraboloid)
    // Z(r) = -depth / sqrt(r + epsilon), inspired by PrabhatSdr
    // ═══════════════════════════════════════════════════
    void CreateWellProfile()
    {
        // Flat reference line (no curvature)
        wellProfileFlat = MakeGridLine("WellFlat", 2);
        wellProfileFlat.positionCount = 2;
        wellProfileFlat.startColor = new Color(0.3f, 0.3f, 0.5f, 0.4f);
        wellProfileFlat.endColor = new Color(0.3f, 0.3f, 0.5f, 0.4f);
        wellProfileFlat.startWidth = 0.02f;
        wellProfileFlat.endWidth = 0.02f;
        wellProfileFlat.SetPosition(0, new Vector3(-GRID_EXTENT, WELL_Y, 0));
        wellProfileFlat.SetPosition(1, new Vector3(GRID_EXTENT, WELL_Y, 0));

        // Curved well profile
        wellProfile = MakeGridLine("WellCurve", 3);
        wellProfile.positionCount = WELL_POINTS;
        wellProfile.startWidth = 0.05f;
        wellProfile.endWidth = 0.05f;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.2f, 0.4f, 0.8f), 0f),
                new GradientColorKey(new Color(0.7f, 0.2f, 0.9f), 0.45f),
                new GradientColorKey(new Color(0.7f, 0.2f, 0.9f), 0.55f),
                new GradientColorKey(new Color(0.2f, 0.4f, 0.8f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.7f, 0f),
                new GradientAlphaKey(1f, 0.5f),
                new GradientAlphaKey(0.7f, 1f)
            }
        );
        wellProfile.colorGradient = gradient;
    }

    // ═══════════════════════════════════════════════════
    // Geodesic paths
    // ═══════════════════════════════════════════════════
    void CreateGeodesicPaths()
    {
        float[] startAngles = { 20f, 80f, 160f, 220f, 310f };
        foreach (float angle in startAngles)
        {
            var go = new GameObject("Geodesic");
            go.transform.SetParent(transform);
            sceneObjects.Add(go);

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.startWidth = 0.04f;
            lr.endWidth = 0.015f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.sortingOrder = 0;

            Gradient geo_grad = new Gradient();
            geo_grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.95f, 0.35f, 0.65f), 0f),
                    new GradientColorKey(new Color(0.95f, 0.35f, 0.65f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.65f, 0f),
                    new GradientAlphaKey(0.08f, 1f)
                }
            );
            lr.colorGradient = geo_grad;
            geodesics.Add(lr);
        }
    }

    void SpawnFilings()
    {
        for (int i = 0; i < FILING_COUNT; i++)
        {
            Vector2 pos = Random.insideUnitCircle * GRID_EXTENT * 0.9f;
            pos.y += GRID_Y_OFFSET;
            if ((pos - new Vector2(0, GRID_Y_OFFSET)).magnitude < 0.6f)
                pos = (pos - new Vector2(0, GRID_Y_OFFSET)).normalized * 0.6f + new Vector2(0, GRID_Y_OFFSET);

            var go = SpriteFactory.CreateFiling(pos, MFASimulator.FilingColor);
            filings.Add(go.GetComponent<IronFiling>());
            sceneObjects.Add(go);
        }
    }

    LineRenderer MakeGridLine(string name, int sortingOrder)
    {
        var go = new GameObject("GridLine_" + name);
        go.transform.SetParent(transform);
        sceneObjects.Add(go);

        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.startWidth = 0.03f;
        lr.endWidth = 0.03f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder = sortingOrder;
        lr.startColor = new Color(0.2f, 0.35f, 0.6f, 0.5f);
        lr.endColor = new Color(0.2f, 0.35f, 0.6f, 0.5f);

        return lr;
    }

    // ═══════════════════════════════════════════════════
    // Update Loop
    // ═══════════════════════════════════════════════════
    void Update()
    {
        if (magnet == null) return;

        currentCurvature = Mathf.Lerp(currentCurvature, targetCurvature, Time.deltaTime * 3f);

        Vector2 magnetPos = magnet.transform.position;
        float S = currentCurvature;

        UpdateGrid(magnetPos, S);
        UpdateWellProfile(magnetPos.x, S);
        UpdateGeodesics(magnetPos, S);
        UpdateFilings(magnetPos, magnet.CurrentS);
        UpdateInfo(S);
    }

    void UpdateGrid(Vector2 center, float S)
    {
        float curvatureScale = S * 0.007f;

        for (int i = 0; i < GRID_SIZE; i++)
        {
            for (int j = 0; j < GRID_SIZE; j++)
            {
                Vector2 orig = originalPos[i, j];
                Vector2 toCenter = center - orig;
                float r = toCenter.magnitude;

                // Potential-gradient pull: stronger and smoother near center
                // Inspired by Flamm's paraboloid: dr/dR = 1/(1 + Rs/r)
                float pull = curvatureScale / (r + 0.4f) * Mathf.Min(r * 0.6f, 2f);
                Vector2 deformed = orig + toCenter.normalized * pull;

                hLines[i].SetPosition(j, new Vector3(deformed.x, deformed.y, 0));
                vLines[j].SetPosition(i, new Vector3(deformed.x, deformed.y, 0));

                // Color: blue (flat) → purple (high curvature)
                float intensity = S / (r * r + 1f);
                float t = Mathf.Clamp01(intensity / 25f);
                Color c = Color.Lerp(
                    new Color(0.15f, 0.30f, 0.55f, 0.35f),
                    new Color(0.6f, 0.15f, 0.85f, 0.9f),
                    t
                );
                hLines[i].startColor = c;
                hLines[i].endColor = c;
                vLines[j].startColor = c;
                vLines[j].endColor = c;
            }
        }
    }

    void UpdateWellProfile(float centerX, float S)
    {
        // Cross-section: Z(x) = WELL_Y - depth * S / sqrt(|x - cx| + epsilon)
        // This is the Flamm's paraboloid / PrabhatSdr formula adapted
        float depthScale = S * 0.003f;

        for (int i = 0; i < WELL_POINTS; i++)
        {
            float frac = (float)i / (WELL_POINTS - 1);
            float x = -GRID_EXTENT + frac * 2f * GRID_EXTENT;
            float dx = Mathf.Abs(x - centerX);

            float wellDepth = depthScale / Mathf.Sqrt(dx + 0.3f);
            wellDepth = Mathf.Min(wellDepth, WELL_HEIGHT * 0.9f);

            float y = WELL_Y - wellDepth;
            wellProfile.SetPosition(i, new Vector3(x, y, 0));
        }
    }

    void UpdateGeodesics(Vector2 center, float S)
    {
        float[] startAngles = { 20f, 80f, 160f, 220f, 310f };
        int steps = 90;
        float dt = 0.04f;

        for (int g = 0; g < geodesics.Count; g++)
        {
            var lr = geodesics[g];
            lr.positionCount = steps;

            float angle = startAngles[g] * Mathf.Deg2Rad;
            Vector2 pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * GRID_EXTENT;

            Vector2 radial = (center - pos).normalized;
            Vector2 tangent = new Vector2(-radial.y, radial.x);
            Vector2 vel = tangent * 2f;

            int validSteps = steps;
            for (int s = 0; s < steps; s++)
            {
                lr.SetPosition(s, new Vector3(pos.x, pos.y, 0));

                Vector2 toCenter = center - pos;
                float r = toCenter.magnitude;

                if (r < 0.25f) { validSteps = s + 1; break; }

                // Geodesic acceleration = gradient of potential = S/(r^2)
                float accel = S * 0.12f / (r * r + 0.4f);
                vel += toCenter.normalized * accel * dt;
                pos += vel * dt;
            }
            lr.positionCount = validSteps;
        }
    }

    void UpdateFilings(Vector2 magnetPos, float S)
    {
        foreach (var filing in filings)
        {
            if (filing == null) continue;

            Vector2 fPos = filing.transform.position;
            Vector2 force = MFACore.ForceVector(fPos, magnetPos, S);
            force += Random.insideUnitCircle * 0.2f;

            float dist = Vector2.Distance(fPos, magnetPos);
            if (dist < 0.4f)
                force += (fPos - magnetPos).normalized * 2f;

            filing.ApplyForce(force * FORCE_SCALE);
            filing.UpdateBrightness(MFACore.AttentionField(S, dist));
        }
    }

    void UpdateInfo(float S)
    {
        string mode = S < 1f ? "FLAT (no task)" : "CURVED (task active)";
        float maxCurv = S / (0.15f * 0.15f + 1f);
        sim.SetInfo(
            $"Mode: {mode}\n" +
            $"Curvature S = {S:F1}\n" +
            $"Peak curvature = {maxCurv:F1}\n\n" +
            "Grid: connectivity manifold\n" +
            "Purple: high curvature\n" +
            "Pink curves: geodesics\n" +
            "Bottom: cross-section well\n\n" +
            "F = S/r\u00B2 is weak-curvature\n" +
            "limit of this geometry.\n" +
            "\u2014 cf. Newton \u2192 Einstein"
        );
    }

    public void Cleanup()
    {
        foreach (var go in sceneObjects)
            if (go != null) Destroy(go);
        sceneObjects.Clear();
        filings.Clear();
        geodesics.Clear();
    }

    void OnDestroy() => Cleanup();
}
