"""
Multi-Dataset Analysis: Power-law vs Gaussian vs Exponential
============================================================

Like Shepard (1987) used 12 datasets, we use multiple published
attention gradient datasets to test whether power-law decay
consistently provides the best fit.
"""

import numpy as np
from scipy.optimize import curve_fit
import matplotlib.pyplot as plt
from matplotlib import rcParams
import warnings
warnings.filterwarnings('ignore')

rcParams['font.family'] = 'Arial'
rcParams['font.size'] = 9
rcParams['axes.linewidth'] = 0.8

def power_law(r, S, alpha, baseline):
    return baseline - S / (r**alpha + 0.1)

def gaussian(r, S, sigma, baseline):
    return baseline - S * np.exp(-r**2 / (2 * sigma**2))

def exponential(r, S, lam, baseline):
    return baseline - S * np.exp(-lam * r)

def compute_r2(observed, predicted):
    ss_res = np.sum((observed - predicted)**2)
    ss_tot = np.sum((observed - np.mean(observed))**2)
    if ss_tot < 1e-10:
        return 0
    return 1 - ss_res / ss_tot

def fit_all_models(distances, performance, is_higher_better=False):
    """Fit three models. Returns dict of results."""
    d = distances[1:]  # skip r=0
    p = performance[1:]

    if is_higher_better:
        def pw(r, S, a, b): return b + S / (r**a + 0.1)
        def ga(r, S, s, b): return b + S * np.exp(-r**2 / (2*s**2))
        def ex(r, S, l, b): return b + S * np.exp(-l * r)
    else:
        pw, ga, ex = power_law, gaussian, exponential

    results = {}
    try:
        popt, _ = curve_fit(pw, d, p, p0=[50, 1.0, np.mean(p)], maxfev=10000,
                           bounds=([0, 0.01, min(p)*0.5], [50000, 10, max(p)*2]))
        pred = pw(d, *popt)
        results['power_law'] = {'alpha': popt[1], 'r2': compute_r2(p, pred), 'params': popt}
    except:
        results['power_law'] = {'alpha': None, 'r2': 0, 'params': None}

    try:
        popt, _ = curve_fit(ga, d, p, p0=[50, 5, np.mean(p)], maxfev=10000,
                           bounds=([0, 0.1, min(p)*0.5], [50000, 100, max(p)*2]))
        pred = ga(d, *popt)
        results['gaussian'] = {'r2': compute_r2(p, pred), 'params': popt}
    except:
        results['gaussian'] = {'r2': 0, 'params': None}

    try:
        popt, _ = curve_fit(ex, d, p, p0=[50, 0.3, np.mean(p)], maxfev=10000,
                           bounds=([0, 0.001, min(p)*0.5], [50000, 10, max(p)*2]))
        pred = ex(d, *popt)
        results['exponential'] = {'r2': compute_r2(p, pred), 'params': popt}
    except:
        results['exponential'] = {'r2': 0, 'params': None}

    return results


# ================================================================
# DATASET 1: Handy et al. (1996) Exp 3 - Suprathreshold RT
# ================================================================
ds1_dist = np.array([0.0, 2.5, 5.0, 7.5, 10.0, 12.5])
ds1_rt = np.array([241.833, 262.8, 293.875, 304.833, 313.75, 327.0])
ds1_name = "Handy et al. (1996) Exp 3\nSuprathreshold RT, N=8"

# ================================================================
# DATASET 2: Handy et al. (1996) Exp 1 - Threshold A'
# ================================================================
ds2_dist = np.array([0.0, 2.5, 5.0, 7.5, 10.0, 12.5])
ds2_perf = np.array([0.841, 0.792, 0.790, 0.780, 0.810, 0.807])
ds2_name = "Handy et al. (1996) Exp 1\nThreshold A', N=16"

# ================================================================
# DATASET 3: Shepherd & Muller (1989) - Peripheral cue, SOA 500ms
# Benefit/cost from neutral, distances 0, 10, 30, 40 degrees from cue
# ================================================================
ds3_dist = np.array([0.0, 10.0, 30.0, 40.0])
ds3_benefit = np.array([15, -15, -32, -48])  # positive = benefit, negative = cost
ds3_rt_from_neutral = 275 - ds3_benefit  # convert to RT (neutral baseline ~275ms)
ds3_name = "Shepherd & Muller (1989)\nPeripheral cue RT, N=9"

# ================================================================
# DATASET 4: Baruch & Goldfarb (2020) - Long SOA IES (approx.)
# ================================================================
ds4_dist = np.array([0.0, 2.3, 4.5, 6.4, 7.8, 8.7, 9.0])
ds4_ies = np.array([600, 620, 640, 660, 690, 710, 720])
ds4_name = "Baruch & Goldfarb (2020)\nLong-SOA IES, N=15"

# ================================================================
# DATASET 5: Handy et al. (1996) Exp 1 - Threshold RT
# ================================================================
ds5_dist = np.array([0.0, 2.5, 5.0, 7.5, 10.0, 12.5])
ds5_rt = np.array([494.333, 552.8, 565.75, 575.0, 570.5, 564.5])
ds5_name = "Handy et al. (1996) Exp 1\nThreshold RT, N=16"

datasets = [
    (ds1_dist, ds1_rt, ds1_name, False),
    (ds2_dist, ds2_perf, ds2_name, True),
    (ds3_dist, ds3_rt_from_neutral, ds3_name, False),
    (ds4_dist, ds4_ies, ds4_name, False),
    (ds5_dist, ds5_rt, ds5_name, False),
]

print("=" * 75)
print("MULTI-DATASET MODEL COMPARISON: Power-law vs Gaussian vs Exponential")
print("=" * 75)

all_results = []

fig, axes = plt.subplots(2, 3, figsize=(10, 6))

for idx, (dist, perf, name, higher_better) in enumerate(datasets):
    results = fit_all_models(dist, perf, higher_better)
    all_results.append((name, results))

    pl_r2 = results['power_law']['r2']
    ga_r2 = results['gaussian']['r2']
    ex_r2 = results['exponential']['r2']
    alpha = results['power_law']['alpha']

    winner = 'Power law'
    if ga_r2 > pl_r2 and ga_r2 > ex_r2: winner = 'Gaussian'
    if ex_r2 > pl_r2 and ex_r2 > ga_r2: winner = 'Exponential'

    print(f"\n--- {name.replace(chr(10), ' | ')} ---")
    print(f"  Power law:   R2={pl_r2:.4f}, alpha={alpha:.3f}" if alpha else f"  Power law:   FAILED")
    print(f"  Gaussian:    R2={ga_r2:.4f}")
    print(f"  Exponential: R2={ex_r2:.4f}")
    print(f"  WINNER: {winner}")

    # Plot
    ax = axes[idx // 3, idx % 3]
    ax.plot(dist, perf, 'ko', ms=5, zorder=5)
    short_name = name.split('\n')[0][:25]
    ax.set_title(f'{short_name}\nPL={pl_r2:.2f} G={ga_r2:.2f} E={ex_r2:.2f}', fontsize=8)
    ax.set_xlabel('Distance (deg)')

    best_r2 = max(pl_r2, ga_r2, ex_r2)
    color = '#2E7D32' if winner == 'Power law' else '#C62828'
    ax.text(0.95, 0.95, f'Best: {winner}', transform=ax.transAxes,
            ha='right', va='top', fontsize=7, fontweight='bold', color=color)

# Summary panel
ax = axes[1, 2]
names_short = [n.split('\n')[0][:20] for n, _ in all_results]
pl_r2s = [r['power_law']['r2'] for _, r in all_results]
ga_r2s = [r['gaussian']['r2'] for _, r in all_results]
ex_r2s = [r['exponential']['r2'] for _, r in all_results]

x = np.arange(len(names_short))
w = 0.25
ax.barh(x - w, pl_r2s, w, label='Power law', color='#1565C0')
ax.barh(x, ga_r2s, w, label='Gaussian', color='#F57C00')
ax.barh(x + w, ex_r2s, w, label='Exponential', color='#7B1FA2')
ax.set_yticks(x)
ax.set_yticklabels([n[:18] for n in names_short], fontsize=7)
ax.set_xlabel('R^2')
ax.set_title('Model comparison\nacross datasets', fontsize=9)
ax.legend(fontsize=6)
ax.set_xlim(0.5, 1.05)

plt.tight_layout()
plt.savefig('figures/multi_dataset_comparison.png', dpi=300, bbox_inches='tight')
print("\nFigure saved to figures/multi_dataset_comparison.png")

# Final summary
print("\n" + "=" * 75)
print("CROSS-DATASET SUMMARY")
print("=" * 75)
print(f"\n{'Dataset':<35} {'PL R2':>8} {'Gauss R2':>10} {'Exp R2':>8} {'Winner':>12} {'Alpha':>7}")
print("-" * 85)

pl_wins = 0
for name, results in all_results:
    short = name.split('\n')[0][:33]
    pl = results['power_law']['r2']
    ga = results['gaussian']['r2']
    ex = results['exponential']['r2']
    alpha = results['power_law']['alpha']
    winner = 'PL' if pl >= ga and pl >= ex else ('G' if ga >= ex else 'E')
    if winner == 'PL': pl_wins += 1
    print(f"{short:<35} {pl:>8.4f} {ga:>10.4f} {ex:>8.4f} {winner:>12} {alpha:>7.3f}" if alpha
          else f"{short:<35} {pl:>8.4f} {ga:>10.4f} {ex:>8.4f} {winner:>12}    N/A")

print(f"\nPower law wins: {pl_wins}/{len(all_results)} datasets")
print(f"Mean R2: PL={np.mean(pl_r2s):.4f}, Gauss={np.mean(ga_r2s):.4f}, Exp={np.mean(ex_r2s):.4f}")

plt.close()
