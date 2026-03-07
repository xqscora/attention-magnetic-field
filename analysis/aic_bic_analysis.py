"""
AIC/BIC Model Comparison for MFA Reanalysis
=============================================
Adds information-theoretic model selection to complement R².
"""

import numpy as np
from scipy.optimize import curve_fit
import warnings
warnings.filterwarnings('ignore')


def compute_r2(obs, pred):
    ss_res = np.sum((obs - pred) ** 2)
    ss_tot = np.sum((obs - np.mean(obs)) ** 2)
    return 1 - ss_res / ss_tot if ss_tot > 1e-10 else 0.0


def compute_aic_bic(n, k, rss):
    """n=data points, k=free parameters, rss=residual sum of squares"""
    if rss <= 0 or n <= k:
        return float('inf'), float('inf')
    log_likelihood = -n / 2 * (np.log(2 * np.pi * rss / n) + 1)
    aic = 2 * k - 2 * log_likelihood
    bic = k * np.log(n) - 2 * log_likelihood
    return aic, bic


# All models: 3 free parameters (S, shape, baseline)
K = 3

# ================================================================
# DATASET 1: Handy et al. (1996) Exp 3 - Suprathreshold RT
# ================================================================
print("=" * 70)
print("Handy et al. (1996) Exp 3 - Suprathreshold RT (N=8, 6 points)")
print("=" * 70)

dist = np.array([2.5, 5.0, 7.5, 10.0, 12.5])
rt = np.array([262.8, 293.875, 304.833, 313.75, 327.0])
n = len(dist)


def pw(r, S, a, b): return b - S / (r ** a + 0.1)
def ga(r, S, s, b): return b - S * np.exp(-r ** 2 / (2 * s ** 2))
def ex(r, S, l, b): return b - S * np.exp(-l * r)


for name, func, p0, bounds in [
    ("Power law", pw, [50, 1.0, 340], ([0, 0.01, 200], [50000, 10, 500])),
    ("Gaussian",  ga, [50, 5, 340],   ([0, 0.1, 200], [50000, 100, 500])),
    ("Exponential", ex, [50, 0.3, 340], ([0, 0.001, 200], [50000, 10, 500])),
]:
    popt, _ = curve_fit(func, dist, rt, p0=p0, maxfev=10000, bounds=bounds)
    pred = func(dist, *popt)
    r2 = compute_r2(rt, pred)
    rss = np.sum((rt - pred) ** 2)
    aic, bic = compute_aic_bic(n, K, rss)
    print(f"  {name:15s}: R2={r2:.4f}, AIC={aic:.1f}, BIC={bic:.1f}, RSS={rss:.2f}")
    if name == "Power law":
        print(f"                   alpha={popt[1]:.3f}, 95% CI needs bootstrap")

# ================================================================
# DATASET 2: Handy et al. (1996) Exp 1 - Threshold RT
# ================================================================
print("\n" + "=" * 70)
print("Handy et al. (1996) Exp 1 - Threshold RT (N=16, 6 points)")
print("=" * 70)

dist2 = np.array([2.5, 5.0, 7.5, 10.0, 12.5])
rt2 = np.array([552.8, 565.75, 575.0, 570.5, 564.5])
n2 = len(dist2)

for name, func, p0, bounds in [
    ("Power law", pw, [50, 1.0, 580], ([0, 0.01, 400], [50000, 10, 700])),
    ("Gaussian",  ga, [50, 5, 580],   ([0, 0.1, 400], [50000, 100, 700])),
    ("Exponential", ex, [50, 0.3, 580], ([0, 0.001, 400], [50000, 10, 700])),
]:
    popt, _ = curve_fit(func, dist2, rt2, p0=p0, maxfev=10000, bounds=bounds)
    pred = func(dist2, *popt)
    r2 = compute_r2(rt2, pred)
    rss = np.sum((rt2 - pred) ** 2)
    aic, bic = compute_aic_bic(n2, K, rss)
    print(f"  {name:15s}: R2={r2:.4f}, AIC={aic:.1f}, BIC={bic:.1f}")

# ================================================================
# DATASET 3: Lavie & Cox (1997) Exp 2 - Load effect
# ================================================================
print("\n" + "=" * 70)
print("Lavie & Cox (1997) Exp 2 - Load effect (4 set sizes)")
print("=" * 70)

ss = np.array([1, 2, 4, 6], dtype=float)
ce = np.array([74, 47, 20, 0], dtype=float)
n3 = len(ss)
K_load = 2  # only 2 free params for load models


def pl_l(s, a, b): return a / (s ** b + 0.01)
def ex_l(s, a, l): return a * np.exp(-l * s)
def li_l(s, a, b): return np.maximum(a - b * s, 0)


for name, func, p0, bounds, k in [
    ("Power law",    pl_l, [77, 1.0], ([0, 0.01], [500, 10]), 2),
    ("Exponential",  ex_l, [100, 0.5], ([0, 0.01], [500, 10]), 2),
    ("Linear",       li_l, [90, 15], ([0, 0], [500, 100]), 2),
]:
    popt, _ = curve_fit(func, ss, ce, p0=p0, maxfev=10000, bounds=bounds)
    pred = func(ss, *popt)
    r2 = compute_r2(ce, pred)
    rss = np.sum((ce - pred) ** 2)
    aic, bic = compute_aic_bic(n3, k, rss)
    print(f"  {name:15s}: R2={r2:.4f}, AIC={aic:.1f}, BIC={bic:.1f}")

# Step function (1 param: threshold)
best_aic = float('inf')
for thresh in np.arange(1.5, 6.0, 0.1):
    above = ce[ss < thresh]
    pred_step = np.where(ss < thresh, np.mean(above) if len(above) > 0 else 74, 0)
    rss = np.sum((ce - pred_step) ** 2)
    aic, bic = compute_aic_bic(n3, 2, rss)
    if aic < best_aic:
        best_aic = aic
        best_bic = bic
        best_r2 = compute_r2(ce, pred_step)

print(f"  {'Step function':15s}: R2={best_r2:.4f}, AIC={best_aic:.1f}, BIC={best_bic:.1f}")

# ================================================================
# SUMMARY
# ================================================================
print("\n" + "=" * 70)
print("SUMMARY: Delta-AIC (relative to best model in each dataset)")
print("=" * 70)
print("  Positive delta = worse; delta > 10 = essentially no support")
print("  (Burnham & Anderson, 2002)")
