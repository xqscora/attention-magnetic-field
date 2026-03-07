using UnityEngine;

/// <summary>
/// MFA 核心公式库 — 所有 6 个公式的静态实现
/// 对应论文: Attention as a Magnetic Field (MFA)
/// α=2 基于 conservation-based geometric spreading（守恒几何扩散），非 dipole 物理推导
/// </summary>
public static class MFACore
{
    /// <summary>衰减指数 α，默认 2（inverse-square）。可改为 1.5、2.5 等用于 model comparison。</summary>
    public static float Alpha = 2f;

    // ═══════════════════════════════════════════════════
    // Formula 1: Attentional Gradient — F(r) = S / r^α
    // α=2: conservation-based geometric spreading in 3D (area ∝ r² → intensity ∝ 1/r²)
    // ═══════════════════════════════════════════════════
    public static float AttentionField(float S, float r)
    {
        float rSafe = Mathf.Max(r, 0.15f);
        return S / Mathf.Pow(rSafe, Alpha);
    }

    // ═══════════════════════════════════════════════════
    // Formula 2: Attention Shift Condition — F_p > S / r^α
    // ═══════════════════════════════════════════════════
    public static bool ShouldShift(float Fp, float Smain, float r)
    {
        return Fp > AttentionField(Smain, r);
    }

    // ═══════════════════════════════════════════════════
    // Formula 3: Magnet Strength — S = k × I × n
    // ═══════════════════════════════════════════════════
    public static float MagnetStrength(float k, float I, float n)
    {
        return k * I * n;
    }

    // ═══════════════════════════════════════════════════
    // Formula 4: Resource Conservation — E₁ + E₂ ≤ E_total - E_min
    // ═══════════════════════════════════════════════════
    public static float AvailableEnergy(float Etotal, float Emin)
    {
        return Mathf.Max(0f, Etotal - Emin);
    }

    public static bool ExceedsEnergy(float E1, float E2, float Etotal, float Emin)
    {
        return (E1 + E2) > AvailableEnergy(Etotal, Emin);
    }

    // ═══════════════════════════════════════════════════
    // Formula 5: Superposition — B_total = Σ Sᵢ / rᵢ^α
    // ═══════════════════════════════════════════════════
    public static float SuperposedField(Vector2 point, Vector2[] positions, float[] strengths)
    {
        float total = 0f;
        for (int i = 0; i < positions.Length; i++)
        {
            float r = Vector2.Distance(point, positions[i]);
            total += AttentionField(strengths[i], r);
        }
        return total;
    }

    // ═══════════════════════════════════════════════════
    // Formula 6: Stability — S(t) = S₀ + A·sin(ωt + φ)
    // ═══════════════════════════════════════════════════
    public static float StabilityS(float S0, float amplitude, float omega, float t, float phase = 0f)
    {
        return Mathf.Max(1f, S0 + amplitude * Mathf.Sin(omega * t + phase));
    }

    // ═══════════════════════════════════════════════════
    // Utility: Force vector from filing to magnet
    // ═══════════════════════════════════════════════════
    public static Vector2 ForceVector(Vector2 filingPos, Vector2 magnetPos, float S)
    {
        Vector2 dir = magnetPos - filingPos;
        float r = dir.magnitude;
        if (r < 0.15f) return Vector2.zero;
        float force = AttentionField(S, r);
        return dir.normalized * force;
    }

    // ═══════════════════════════════════════════════════
    // Utility: Threshold distance where F_p = S/r^α → r_crit = (S/F_p)^(1/α)
    // ═══════════════════════════════════════════════════
    public static float ThresholdRadius(float S, float Fp)
    {
        if (Fp <= 0.001f) return 999f;
        return Mathf.Pow(S / Fp, 1f / Alpha);
    }
}
