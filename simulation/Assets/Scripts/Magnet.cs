using UnityEngine;

/// <summary>
/// 磁铁（注意力焦点）— 可拖拽，产生磁场
/// </summary>
public class Magnet : MonoBehaviour
{
    [Header("Field Parameters")]
    public float S = 50f;
    public float sigma = 0f;      // stability oscillation amplitude (σ)
    public float omega = 3f;      // oscillation frequency
    public Color magnetColor = new Color(0.2f, 0.4f, 0.8f);
    public bool isDraggable = true;

    /// <summary>
    /// When true, scene controls the visual (scale & color) externally.
    /// Magnet still computes CurrentS (field strength) but won't touch transform.localScale or sr.color.
    /// </summary>
    public bool externalVisual = false;

    // Runtime
    [System.NonSerialized] public float currentS;
    public float CurrentS { get { return currentS; } private set { currentS = value; } }

    private bool isDragging = false;
    private Vector3 dragOffset;
    private SpriteRenderer sr;
    private float phase;

    // For Lenz's Law: track velocity
    private Vector3 lastPosition;
    public Vector2 Velocity { get; private set; }

    void Start()
    {
        CurrentS = S;
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // Paper: magnet core (S) vs glow (filing count) — core darker for visual separation
            if (externalVisual)
                sr.color = magnetColor;  // Scene controls visuals
            else
                sr.color = new Color(magnetColor.r * 0.55f, magnetColor.g * 0.55f, magnetColor.b * 0.55f, magnetColor.a);
        }
        phase = Random.Range(0f, Mathf.PI * 2f);
        lastPosition = transform.position;
    }

    void Update()
    {
        // Track velocity for Lenz's Law
        Velocity = (transform.position - lastPosition) / Mathf.Max(Time.deltaTime, 0.001f);
        lastPosition = transform.position;

        // Stability oscillation (Formula 6)
        if (sigma > 0.01f)
        {
            CurrentS = MFACore.StabilityS(S, sigma, omega, Time.time, phase);
        }
        else
        {
            CurrentS = S;
        }

        // Drag handling
        if (isDraggable) HandleDrag();

        // Visual: only if NOT externally controlled
        if (!externalVisual)
        {
            // Scale by strength
            float scale = Mathf.Lerp(0.3f, 0.8f, Mathf.Clamp01(CurrentS / 100f));
            transform.localScale = Vector3.one * scale;

            // Pulsate when sigma is high (visual indicator of instability)
            if (sigma > 5f && sr != null)
            {
                float pulse = 0.7f + 0.3f * Mathf.Sin(Time.time * omega);
                sr.color = magnetColor * pulse;
            }
        }
    }

    void HandleDrag()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mp.z = 0;
            if (Vector2.Distance(mp, transform.position) < 1f)
            {
                isDragging = true;
                dragOffset = transform.position - mp;
            }
        }

        if (Input.GetMouseButton(0) && isDragging)
        {
            Vector3 mp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mp.z = 0;
            transform.position = mp + dragOffset;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }

    /// <summary>
    /// 计算该磁铁在指定位置产生的力向量
    /// </summary>
    public Vector2 GetForceAt(Vector2 position)
    {
        return MFACore.ForceVector(position, transform.position, CurrentS);
    }
}
