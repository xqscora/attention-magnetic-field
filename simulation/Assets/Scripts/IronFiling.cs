using UnityEngine;

/// <summary>
/// 单个铁屑粒子 — 在磁场中被吸引和对齐
/// </summary>
public class IronFiling : MonoBehaviour
{
    [HideInInspector] public Vector2 velocity;
    [HideInInspector] public Color baseColor = new Color(0.8f, 0.67f, 0.27f);
    [HideInInspector] public float damping = 0.90f;
    [HideInInspector] public float maxSpeed = 6f;
    [HideInInspector] public bool useLenzInertia = false;
    [HideInInspector] public float lenzFactor = 0f;

    private SpriteRenderer sr;
    private Vector2 lastForce;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// 由场景控制器调用：施加磁场力
    /// </summary>
    public void ApplyForce(Vector2 force)
    {
        // Lenz inertia is now handled entirely by the scene controller
        // (only triggers when magnet actually moves, not when filings settle)

        velocity += force * Time.deltaTime;
        velocity = Vector2.ClampMagnitude(velocity, maxSpeed);

        // Align with field direction (not velocity)
        if (force.magnitude > 0.01f)
        {
            float angle = Mathf.Atan2(force.y, force.x) * Mathf.Rad2Deg;
            Quaternion target = Quaternion.Euler(0, 0, angle);
            transform.rotation = Quaternion.Lerp(transform.rotation, target, Time.deltaTime * 8f);
        }
    }

    void Update()
    {
        // Move
        transform.position += (Vector3)(velocity * Time.deltaTime);
        velocity *= damping;

        // Clamp to visible area
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, -12f, 12f);
        pos.y = Mathf.Clamp(pos.y, -6f, 6f);
        transform.position = pos;
    }

    /// <summary>
    /// 根据场强更新视觉效果：场强越大，铁屑越亮
    /// </summary>
    public void UpdateBrightness(float fieldStrength)
    {
        if (sr == null) return;
        float brightness = Mathf.Clamp01(fieldStrength / 40f);
        sr.color = Color.Lerp(baseColor * 0.25f, baseColor, 0.3f + brightness * 0.7f);
    }

    /// <summary>
    /// 设置铁屑颜色
    /// </summary>
    public void SetColor(Color color)
    {
        baseColor = color;
        if (sr != null) sr.color = color;
    }
}
