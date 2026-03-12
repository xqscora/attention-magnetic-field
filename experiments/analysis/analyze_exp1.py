"""
Experiment 1 — Attentional Gradient Analysis
=============================================

Analyzes jsPsych output from exp1_gradient.html.
Tests MFA Prediction 1: F(r) = S / r^α (inverse-power decay).

Compares power-law, Gaussian, and exponential decay models
using AIC/BIC model comparison.

Usage:
    python analyze_exp1.py data/exp1_gradient_*.csv
    python analyze_exp1.py data/  # processes all Exp1 files in folder

Cora Zeng, 2026
"""

import sys
import os
import glob
import json
import numpy as np
import pandas as pd
from scipy.optimize import curve_fit
from scipy.stats import pearsonr, ttest_rel
import warnings
warnings.filterwarnings('ignore')

try:
    import matplotlib
    matplotlib.use('Agg')
    import matplotlib.pyplot as plt
    HAS_PLOT = True
except ImportError:
    HAS_PLOT = False


# ── Data loading ──────────────────────────────────────────────

def load_exp1_data(path):
    """Load one or more Exp1 CSV files into a single DataFrame."""
    if os.path.isdir(path):
        files = sorted(glob.glob(os.path.join(path, 'exp1_gradient_*.csv')))
    elif '*' in path:
        files = sorted(glob.glob(path))
    else:
        files = [path]

    if not files:
        raise FileNotFoundError(f"No Exp1 files found at: {path}")

    frames = []
    for f in files:
        df = pd.read_csv(f)
        df['source_file'] = os.path.basename(f)
        frames.append(df)
    return pd.concat(frames, ignore_index=True)


def preprocess(df):
    """Filter to response trials, apply RT cleaning."""
    resp = df[(df['phase'] == 'response') &
              (df['task'] == 'gradient') &
              (df['is_practice'] == False) &
              (df['is_catch'] == False)].copy()

    resp['rt'] = pd.to_numeric(resp['rt'], errors='coerce')
    resp = resp.dropna(subset=['rt'])

    n_before = len(resp)
    resp = resp[(resp['rt'] >= 150) & (resp['rt'] <= 2000)]
    resp = resp[resp['timed_out'] == False]
    n_after = len(resp)

    print(f"  Trials: {n_before} → {n_after} after RT cleaning "
          f"({n_before - n_after} removed, {100*(n_before-n_after)/max(n_before,1):.1f}%)")

    return resp


# ── Catch trial analysis ─────────────────────────────────────

def analyze_catch_trials(df):
    """Check attention check performance."""
    catch = df[(df['is_catch'] == True) & (df['phase'] == 'response')]
    if len(catch) == 0:
        print("  No catch trials found.")
        return True

    acc = catch['correct'].mean()
    n = len(catch)
    print(f"  Catch trial accuracy: {acc*100:.1f}% ({int(acc*n)}/{n})")

    if acc < 0.75:
        print("  ⚠ WARNING: Catch accuracy < 75% — participant may not be attending!")
        return False
    return True


# ── Core analysis ─────────────────────────────────────────────

def analyze_gradient(resp):
    """Compute RT and accuracy by eccentricity and validity."""
    results = {}

    for validity in ['valid', 'invalid']:
        sub = resp[resp['validity'] == validity]
        grouped = sub.groupby('target_eccentricity').agg(
            mean_rt=('rt', 'mean'),
            sd_rt=('rt', 'std'),
            accuracy=('correct', 'mean'),
            n=('rt', 'count')
        ).reset_index()
        grouped['se_rt'] = grouped['sd_rt'] / np.sqrt(grouped['n'])
        results[validity] = grouped

    valid = results['valid']
    eccs = valid['target_eccentricity'].values
    rt_means = valid['mean_rt'].values

    print(f"\n  Valid trials — RT by eccentricity:")
    print(f"  {'Ecc (°)':>8} {'RT (ms)':>9} {'SD':>7} {'Acc':>7} {'N':>5}")
    print(f"  {'─'*40}")
    for _, row in valid.iterrows():
        print(f"  {row['target_eccentricity']:>8.1f} {row['mean_rt']:>9.1f} "
              f"{row['sd_rt']:>7.1f} {row['accuracy']:>6.1%} {int(row['n']):>5}")

    return results, eccs, rt_means


# ── Model fitting ─────────────────────────────────────────────

def power_law(r, S, alpha, baseline, r0=0.5):
    return baseline - S / (r + r0) ** alpha

def gaussian(r, S, sigma, baseline):
    return baseline - S * np.exp(-r**2 / (2 * sigma**2))

def exponential(r, S, lam, baseline):
    return baseline - S * np.exp(-lam * r)

def inverse_square(r, S, baseline, r0=0.5):
    return baseline - S / (r + r0) ** 2


def compute_aic_bic(n, k, ss_res):
    """Compute AIC and BIC from residual sum of squares."""
    if ss_res <= 0 or n <= k:
        return np.inf, np.inf
    log_lik = -n/2 * np.log(2 * np.pi * ss_res / n) - n/2
    aic = 2*k - 2*log_lik
    bic = k*np.log(n) - 2*log_lik
    return aic, bic


def fit_models(eccs, rt_means):
    """Fit power-law, Gaussian, and exponential to RT gradient."""
    n = len(eccs)
    results = {}

    # 1) Power law (free alpha): RT = baseline - S/(r+r0)^alpha
    try:
        popt, _ = curve_fit(power_law, eccs, rt_means,
                            p0=[50, 2.0, np.max(rt_means)],
                            bounds=([0, 0.1, 0], [1e5, 10, 2000]),
                            maxfev=10000)
        pred = power_law(eccs, *popt)
        ss_res = np.sum((rt_means - pred)**2)
        ss_tot = np.sum((rt_means - np.mean(rt_means))**2)
        r2 = 1 - ss_res / ss_tot if ss_tot > 0 else 0
        aic, bic = compute_aic_bic(n, 3, ss_res)
        results['power_law'] = {
            'params': {'S': popt[0], 'alpha': popt[1], 'baseline': popt[2]},
            'R2': r2, 'AIC': aic, 'BIC': bic, 'predicted': pred, 'k': 3
        }
    except RuntimeError:
        results['power_law'] = {'error': 'Fit failed'}

    # 2) Inverse square (alpha=2 fixed): RT = baseline - S/(r+r0)^2
    try:
        popt, _ = curve_fit(inverse_square, eccs, rt_means,
                            p0=[50, np.max(rt_means)],
                            bounds=([0, 0], [1e5, 2000]),
                            maxfev=10000)
        pred = inverse_square(eccs, *popt)
        ss_res = np.sum((rt_means - pred)**2)
        ss_tot = np.sum((rt_means - np.mean(rt_means))**2)
        r2 = 1 - ss_res / ss_tot if ss_tot > 0 else 0
        aic, bic = compute_aic_bic(n, 2, ss_res)
        results['inverse_square'] = {
            'params': {'S': popt[0], 'baseline': popt[1]},
            'R2': r2, 'AIC': aic, 'BIC': bic, 'predicted': pred, 'k': 2
        }
    except RuntimeError:
        results['inverse_square'] = {'error': 'Fit failed'}

    # 3) Gaussian: RT = baseline - S * exp(-r²/2σ²)
    try:
        popt, _ = curve_fit(gaussian, eccs, rt_means,
                            p0=[50, 5, np.max(rt_means)],
                            bounds=([0, 0.1, 0], [1e5, 100, 2000]),
                            maxfev=10000)
        pred = gaussian(eccs, *popt)
        ss_res = np.sum((rt_means - pred)**2)
        ss_tot = np.sum((rt_means - np.mean(rt_means))**2)
        r2 = 1 - ss_res / ss_tot if ss_tot > 0 else 0
        aic, bic = compute_aic_bic(n, 3, ss_res)
        results['gaussian'] = {
            'params': {'S': popt[0], 'sigma': popt[1], 'baseline': popt[2]},
            'R2': r2, 'AIC': aic, 'BIC': bic, 'predicted': pred, 'k': 3
        }
    except RuntimeError:
        results['gaussian'] = {'error': 'Fit failed'}

    # 4) Exponential: RT = baseline - S * exp(-λr)
    try:
        popt, _ = curve_fit(exponential, eccs, rt_means,
                            p0=[50, 0.3, np.max(rt_means)],
                            bounds=([0, 0.001, 0], [1e5, 10, 2000]),
                            maxfev=10000)
        pred = exponential(eccs, *popt)
        ss_res = np.sum((rt_means - pred)**2)
        ss_tot = np.sum((rt_means - np.mean(rt_means))**2)
        r2 = 1 - ss_res / ss_tot if ss_tot > 0 else 0
        aic, bic = compute_aic_bic(n, 3, ss_res)
        results['exponential'] = {
            'params': {'S': popt[0], 'lambda': popt[1], 'baseline': popt[2]},
            'R2': r2, 'AIC': aic, 'BIC': bic, 'predicted': pred, 'k': 3
        }
    except RuntimeError:
        results['exponential'] = {'error': 'Fit failed'}

    return results


def print_model_comparison(fit_results):
    """Print model comparison table."""
    print(f"\n  {'Model':<20} {'k':>3} {'R²':>8} {'AIC':>10} {'BIC':>10} {'Key params'}")
    print(f"  {'─'*70}")

    for name, res in fit_results.items():
        if 'error' in res:
            print(f"  {name:<20} {'—':>3} {'FAILED':>8}")
            continue
        params_str = ', '.join(f"{k}={v:.3f}" for k, v in res['params'].items())
        print(f"  {name:<20} {res['k']:>3} {res['R2']:>8.4f} "
              f"{res['AIC']:>10.1f} {res['BIC']:>10.1f} {params_str}")

    valid = {k: v for k, v in fit_results.items() if 'error' not in v}
    if valid:
        best_aic = min(valid, key=lambda k: valid[k]['AIC'])
        best_bic = min(valid, key=lambda k: valid[k]['BIC'])
        print(f"\n  Best by AIC: {best_aic}")
        print(f"  Best by BIC: {best_bic}")

        if 'power_law' in valid:
            alpha = valid['power_law']['params']['alpha']
            print(f"\n  Fitted alpha = {alpha:.3f}")
            if 1.5 <= alpha <= 2.5:
                print(f"  → Consistent with MFA inverse-square prediction (α ≈ 2)")
            elif alpha < 1.5:
                print(f"  → Broader than inverse-square; suggests extended field")
            else:
                print(f"  → Steeper than inverse-square; suggests focal field")


# ── Validity effect ───────────────────────────────────────────

def analyze_validity_effect(resp):
    """Compute and test validity effect (invalid - valid RT)."""
    valid_rt = resp[resp['validity'] == 'valid']['rt'].mean()
    invalid_rt = resp[resp['validity'] == 'invalid']['rt'].mean()
    effect = invalid_rt - valid_rt

    valid_acc = resp[resp['validity'] == 'valid']['correct'].mean()
    invalid_acc = resp[resp['validity'] == 'invalid']['correct'].mean()

    print(f"\n  Validity effect:")
    print(f"    Valid RT:    {valid_rt:.1f} ms  (acc: {valid_acc:.1%})")
    print(f"    Invalid RT:  {invalid_rt:.1f} ms  (acc: {invalid_acc:.1%})")
    print(f"    Effect:      {effect:.1f} ms")

    return effect


# ── Plotting ──────────────────────────────────────────────────

def plot_gradient(eccs, rt_means, fit_results, participant_id, output_dir):
    """Plot RT gradient with model fits."""
    if not HAS_PLOT:
        return

    fig, ax = plt.subplots(1, 1, figsize=(8, 5))

    ax.plot(eccs, rt_means, 'ko-', markersize=8, linewidth=2,
            label='Observed', zorder=5)

    colors = {'power_law': '#e74c3c', 'inverse_square': '#e67e22',
              'gaussian': '#3498db', 'exponential': '#2ecc71'}
    linestyles = {'power_law': '--', 'inverse_square': ':',
                  'gaussian': '-.', 'exponential': '--'}

    r_smooth = np.linspace(eccs.min(), eccs.max(), 200)

    for name, res in fit_results.items():
        if 'error' in res:
            continue
        p = res['params']
        if name == 'power_law':
            y = power_law(r_smooth, p['S'], p['alpha'], p['baseline'])
        elif name == 'inverse_square':
            y = inverse_square(r_smooth, p['S'], p['baseline'])
        elif name == 'gaussian':
            y = gaussian(r_smooth, p['S'], p['sigma'], p['baseline'])
        elif name == 'exponential':
            y = exponential(r_smooth, p['S'], p['lambda'], p['baseline'])
        else:
            continue
        label = f"{name} (R²={res['R2']:.3f})"
        ax.plot(r_smooth, y, color=colors.get(name, '#888'),
                linestyle=linestyles.get(name, '--'), linewidth=1.5,
                label=label)

    ax.set_xlabel('Eccentricity (°)', fontsize=12)
    ax.set_ylabel('RT (ms)', fontsize=12)
    ax.set_title(f'Exp 1: Attentional Gradient — {participant_id}', fontsize=13)
    ax.legend(fontsize=9)
    ax.grid(True, alpha=0.3)

    outfile = os.path.join(output_dir, f'exp1_gradient_{participant_id}.png')
    fig.tight_layout()
    fig.savefig(outfile, dpi=150)
    plt.close(fig)
    print(f"  Plot saved: {outfile}")


# ── Main ──────────────────────────────────────────────────────

def main():
    if len(sys.argv) < 2:
        print("Usage: python analyze_exp1.py <path_to_csv_or_folder>")
        print("  Accepts a single CSV, a glob pattern, or a directory.")
        sys.exit(1)

    path = sys.argv[1]
    print("=" * 60)
    print("EXPERIMENT 1 — ATTENTIONAL GRADIENT ANALYSIS")
    print("=" * 60)

    df = load_exp1_data(path)
    participants = df['participant_id'].unique() if 'participant_id' in df.columns else ['unknown']
    print(f"\nLoaded {len(df)} rows, {len(participants)} participant(s)")

    output_dir = os.path.join(os.path.dirname(path) if os.path.isfile(path) else path, 'results')
    os.makedirs(output_dir, exist_ok=True)

    all_results = []

    for pid in participants:
        print(f"\n{'─' * 50}")
        print(f"Participant: {pid}")
        print(f"{'─' * 50}")

        if 'participant_id' in df.columns:
            pdf = df[df['participant_id'] == pid]
        else:
            pdf = df

        catch_ok = analyze_catch_trials(pdf)
        resp = preprocess(pdf)

        if len(resp) < 50:
            print(f"  ⚠ Too few trials ({len(resp)}), skipping.")
            continue

        gradient_data, eccs, rt_means = analyze_gradient(resp)
        validity_effect = analyze_validity_effect(resp)
        fit_results = fit_models(eccs, rt_means)
        print_model_comparison(fit_results)
        plot_gradient(eccs, rt_means, fit_results, str(pid), output_dir)

        all_results.append({
            'participant_id': pid,
            'n_trials': len(resp),
            'catch_ok': catch_ok,
            'validity_effect_ms': validity_effect,
            'overall_accuracy': resp['correct'].mean(),
            'mean_rt': resp['rt'].mean(),
            'fit_results': {k: {kk: vv for kk, vv in v.items()
                                if kk != 'predicted'}
                           for k, v in fit_results.items()}
        })

    if len(all_results) > 1:
        print(f"\n{'=' * 60}")
        print("GROUP SUMMARY")
        print(f"{'=' * 60}")
        accs = [r['overall_accuracy'] for r in all_results]
        rts = [r['mean_rt'] for r in all_results]
        effects = [r['validity_effect_ms'] for r in all_results]
        print(f"  N = {len(all_results)}")
        print(f"  Mean accuracy: {np.mean(accs):.1%} ± {np.std(accs):.1%}")
        print(f"  Mean RT: {np.mean(rts):.1f} ± {np.std(rts):.1f} ms")
        print(f"  Validity effect: {np.mean(effects):.1f} ± {np.std(effects):.1f} ms")

        alphas = []
        for r in all_results:
            fr = r['fit_results']
            if 'power_law' in fr and 'error' not in fr['power_law']:
                alphas.append(fr['power_law']['params']['alpha'])
        if alphas:
            print(f"  Fitted alpha: {np.mean(alphas):.3f} ± {np.std(alphas):.3f}")
            print(f"  (MFA predicts α ≈ 2.0)")

    summary_file = os.path.join(output_dir, 'exp1_summary.json')
    with open(summary_file, 'w') as f:
        json.dump(all_results, f, indent=2, default=str)
    print(f"\nSummary saved: {summary_file}")


if __name__ == '__main__':
    main()
