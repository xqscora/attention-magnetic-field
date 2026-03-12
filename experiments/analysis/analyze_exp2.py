"""
Experiment 2 — Load-Distractor Function Analysis
=================================================

Analyzes jsPsych output from exp2_load.html.
Tests MFA Prediction 2: Sigmoid (continuous) distractor interference
decay with perceptual load, not a binary step function.

Key analyses:
  1. Compatibility effect (incongruent - congruent RT) as f(load)
  2. Sigmoid vs step-function model comparison (AIC/BIC)
  3. Distance × load interaction (near vs far distractors)

Usage:
    python analyze_exp2.py data/exp2_load_*.csv
    python analyze_exp2.py data/

Cora Zeng, 2026
"""

import sys
import os
import glob
import json
import numpy as np
import pandas as pd
from scipy.optimize import curve_fit
from scipy.stats import pearsonr
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

def load_exp2_data(path):
    if os.path.isdir(path):
        files = sorted(glob.glob(os.path.join(path, 'exp2_load_*.csv')))
    elif '*' in path:
        files = sorted(glob.glob(path))
    else:
        files = [path]

    if not files:
        raise FileNotFoundError(f"No Exp2 files found at: {path}")

    frames = []
    for f in files:
        df = pd.read_csv(f)
        df['source_file'] = os.path.basename(f)
        frames.append(df)
    return pd.concat(frames, ignore_index=True)


def preprocess(df):
    resp = df[(df['phase'] == 'response') &
              (df['task'] == 'load') &
              (df['is_practice'] == False) &
              (df['is_catch'] == False)].copy()

    resp['rt'] = pd.to_numeric(resp['rt'], errors='coerce')
    resp = resp.dropna(subset=['rt'])

    n_before = len(resp)
    resp = resp[(resp['rt'] >= 150) & (resp['rt'] <= 2500)]
    resp = resp[resp['timed_out'] == False]
    n_after = len(resp)

    print(f"  Trials: {n_before} → {n_after} after RT cleaning "
          f"({n_before - n_after} removed)")

    return resp


def analyze_catch_trials(df):
    catch = df[(df['is_catch'] == True) & (df['phase'] == 'response')]
    if len(catch) == 0:
        print("  No catch trials found.")
        return True
    acc = catch['correct'].mean()
    print(f"  Catch trial accuracy: {acc*100:.1f}% ({int(acc*len(catch))}/{len(catch)})")
    return acc >= 0.75


# ── Core analysis ─────────────────────────────────────────────

def compute_compatibility_effect(resp):
    """Compute compatibility effect (incongruent - congruent RT) by load and distance."""
    correct_only = resp[resp['correct'] == True]

    pivot = correct_only.groupby(['load', 'congruency', 'distractor_distance']).agg(
        mean_rt=('rt', 'mean'),
        n=('rt', 'count')
    ).reset_index()

    results = {}

    for dist in ['near', 'far']:
        dist_data = pivot[pivot['distractor_distance'] == dist]
        loads = sorted(dist_data['load'].unique())
        ce_by_load = []

        for load in loads:
            load_data = dist_data[dist_data['load'] == load]
            inc_rt = load_data[load_data['congruency'] == 'incongruent']['mean_rt'].values
            con_rt = load_data[load_data['congruency'] == 'congruent']['mean_rt'].values

            if len(inc_rt) > 0 and len(con_rt) > 0:
                ce = inc_rt[0] - con_rt[0]
                ce_by_load.append({'load': load, 'ce': ce})

        results[dist] = pd.DataFrame(ce_by_load)

    return results


def print_load_table(resp):
    """Print RT × load × congruency × distance table."""
    correct_only = resp[resp['correct'] == True]

    print(f"\n  Mean RT (correct trials) by Load × Congruency × Distance:")
    print(f"  {'Load':>5} {'Cong':>8} {'Dist':>5} {'RT':>8} {'Acc':>7} {'N':>5}")
    print(f"  {'─'*45}")

    for load in sorted(resp['load'].unique()):
        for cong in ['congruent', 'neutral', 'incongruent']:
            for dist in ['near', 'far']:
                sub = resp[(resp['load'] == load) &
                           (resp['congruency'] == cong) &
                           (resp['distractor_distance'] == dist)]
                if len(sub) > 0:
                    rt = sub[sub['correct'] == True]['rt'].mean()
                    acc = sub['correct'].mean()
                    print(f"  {load:>5} {cong:>8} {dist:>5} {rt:>8.1f} {acc:>6.1%} {len(sub):>5}")


# ── Model fitting ─────────────────────────────────────────────

def sigmoid(x, L, k, x0):
    """Sigmoid: CE = L / (1 + exp(k*(x - x0)))"""
    return L / (1 + np.exp(k * (x - x0)))

def power_decay(x, a, b):
    """Power-law decay: CE = a / x^b"""
    return a / (x**b + 0.01)

def step_func(x, a, threshold):
    """Step function: CE = a if x < threshold, else 0"""
    return np.where(x < threshold, a, 0.0)

def linear_decay(x, a, b):
    """Linear: CE = a - b*x"""
    return np.maximum(a - b * x, 0)

def exponential_decay(x, a, lam):
    """Exponential: CE = a * exp(-lam * x)"""
    return a * np.exp(-lam * x)


def compute_aic_bic(n, k, ss_res):
    if ss_res <= 0 or n <= k:
        return np.inf, np.inf
    log_lik = -n/2 * np.log(2 * np.pi * ss_res / n) - n/2
    aic = 2*k - 2*log_lik
    bic = k*np.log(n) - 2*log_lik
    return aic, bic


def fit_load_models(loads, ce_values):
    """Fit sigmoid, step, power, exponential, and linear to CE × load."""
    n = len(loads)
    results = {}

    # Sigmoid (MFA prediction)
    try:
        popt, _ = curve_fit(sigmoid, loads, ce_values,
                            p0=[max(ce_values), 1.0, 4.0],
                            bounds=([0, 0.01, 0], [200, 10, 10]),
                            maxfev=10000)
        pred = sigmoid(loads, *popt)
        ss_res = np.sum((ce_values - pred)**2)
        ss_tot = np.sum((ce_values - np.mean(ce_values))**2)
        r2 = 1 - ss_res / ss_tot if ss_tot > 0 else 0
        aic, bic = compute_aic_bic(n, 3, ss_res)
        results['sigmoid'] = {
            'params': {'L': popt[0], 'k': popt[1], 'x0': popt[2]},
            'R2': r2, 'AIC': aic, 'BIC': bic, 'predicted': pred, 'k': 3
        }
    except RuntimeError:
        results['sigmoid'] = {'error': 'Fit failed'}

    # Step function (Lavie binary prediction)
    best_r2 = -np.inf
    best_thresh = 3.0
    for thresh in np.arange(1.5, 8.5, 0.1):
        below = ce_values[loads < thresh]
        a = np.mean(below) if len(below) > 0 else ce_values.max()
        pred = step_func(loads, a, thresh)
        ss_res = np.sum((ce_values - pred)**2)
        ss_tot = np.sum((ce_values - np.mean(ce_values))**2)
        r2 = 1 - ss_res / ss_tot if ss_tot > 0 else 0
        if r2 > best_r2:
            best_r2 = r2
            best_thresh = thresh
            best_a = a

    pred = step_func(loads, best_a, best_thresh)
    ss_res = np.sum((ce_values - pred)**2)
    aic, bic = compute_aic_bic(n, 2, ss_res)
    results['step'] = {
        'params': {'a': best_a, 'threshold': best_thresh},
        'R2': best_r2, 'AIC': aic, 'BIC': bic, 'predicted': pred, 'k': 2
    }

    # Power-law decay
    try:
        popt, _ = curve_fit(power_decay, loads, ce_values,
                            p0=[max(ce_values), 1.0],
                            bounds=([0, 0.01], [500, 10]),
                            maxfev=10000)
        pred = power_decay(loads, *popt)
        ss_res = np.sum((ce_values - pred)**2)
        ss_tot = np.sum((ce_values - np.mean(ce_values))**2)
        r2 = 1 - ss_res / ss_tot if ss_tot > 0 else 0
        aic, bic = compute_aic_bic(n, 2, ss_res)
        results['power_decay'] = {
            'params': {'a': popt[0], 'b': popt[1]},
            'R2': r2, 'AIC': aic, 'BIC': bic, 'predicted': pred, 'k': 2
        }
    except RuntimeError:
        results['power_decay'] = {'error': 'Fit failed'}

    # Exponential decay
    try:
        popt, _ = curve_fit(exponential_decay, loads, ce_values,
                            p0=[max(ce_values), 0.5],
                            bounds=([0, 0.001], [500, 10]),
                            maxfev=10000)
        pred = exponential_decay(loads, *popt)
        ss_res = np.sum((ce_values - pred)**2)
        ss_tot = np.sum((ce_values - np.mean(ce_values))**2)
        r2 = 1 - ss_res / ss_tot if ss_tot > 0 else 0
        aic, bic = compute_aic_bic(n, 2, ss_res)
        results['exponential'] = {
            'params': {'a': popt[0], 'lambda': popt[1]},
            'R2': r2, 'AIC': aic, 'BIC': bic, 'predicted': pred, 'k': 2
        }
    except RuntimeError:
        results['exponential'] = {'error': 'Fit failed'}

    # Linear decay
    try:
        popt, _ = curve_fit(linear_decay, loads, ce_values,
                            p0=[max(ce_values), 10],
                            maxfev=10000)
        pred = linear_decay(loads, *popt)
        ss_res = np.sum((ce_values - pred)**2)
        ss_tot = np.sum((ce_values - np.mean(ce_values))**2)
        r2 = 1 - ss_res / ss_tot if ss_tot > 0 else 0
        aic, bic = compute_aic_bic(n, 2, ss_res)
        results['linear'] = {
            'params': {'a': popt[0], 'b': popt[1]},
            'R2': r2, 'AIC': aic, 'BIC': bic, 'predicted': pred, 'k': 2
        }
    except RuntimeError:
        results['linear'] = {'error': 'Fit failed'}

    return results


def print_model_comparison(fit_results, label=""):
    print(f"\n  Model comparison{' — ' + label if label else ''}:")
    print(f"  {'Model':<18} {'k':>3} {'R²':>8} {'AIC':>10} {'BIC':>10}")
    print(f"  {'─'*55}")

    for name, res in fit_results.items():
        if 'error' in res:
            print(f"  {name:<18} {'—':>3} {'FAILED':>8}")
            continue
        print(f"  {name:<18} {res['k']:>3} {res['R2']:>8.4f} "
              f"{res['AIC']:>10.1f} {res['BIC']:>10.1f}")

    valid = {k: v for k, v in fit_results.items() if 'error' not in v}
    if valid:
        best_aic = min(valid, key=lambda k: valid[k]['AIC'])
        best_bic = min(valid, key=lambda k: valid[k]['BIC'])
        print(f"\n  Best by AIC: {best_aic}")
        print(f"  Best by BIC: {best_bic}")

        if best_aic == 'sigmoid' or best_bic == 'sigmoid':
            print(f"  → Supports MFA: continuous (sigmoid) transition, not binary step")
        elif best_aic == 'step' or best_bic == 'step':
            print(f"  → Supports Lavie: binary step function")
        else:
            print(f"  → Continuous decay model wins, consistent with MFA framework")


# ── Plotting ──────────────────────────────────────────────────

def plot_load_function(ce_data, fit_results, participant_id, output_dir):
    if not HAS_PLOT:
        return

    fig, axes = plt.subplots(1, 2, figsize=(12, 5))

    for idx, dist in enumerate(['near', 'far']):
        ax = axes[idx]
        df = ce_data[dist]
        if len(df) == 0:
            continue

        loads = df['load'].values.astype(float)
        ces = df['ce'].values

        ax.bar(loads, ces, width=0.6, color='#3498db' if dist == 'near' else '#e74c3c',
               alpha=0.7, label='Observed CE')

        if dist in fit_results:
            x_smooth = np.linspace(loads.min(), loads.max(), 200)
            for name, res in fit_results[dist].items():
                if 'error' in res:
                    continue
                p = res['params']
                if name == 'sigmoid':
                    y = sigmoid(x_smooth, p['L'], p['k'], p['x0'])
                    ax.plot(x_smooth, y, 'r--', linewidth=2,
                            label=f'Sigmoid (R²={res["R2"]:.3f})')
                elif name == 'step':
                    y = step_func(x_smooth, p['a'], p['threshold'])
                    ax.plot(x_smooth, y, 'g:', linewidth=2,
                            label=f'Step (R²={res["R2"]:.3f})')

        ax.set_xlabel('Perceptual Load (set size)', fontsize=11)
        ax.set_ylabel('Compatibility Effect (ms)', fontsize=11)
        ax.set_title(f'{dist.capitalize()} distractor', fontsize=12)
        ax.legend(fontsize=9)
        ax.grid(True, alpha=0.3)
        ax.axhline(y=0, color='#888', linewidth=0.5)

    fig.suptitle(f'Exp 2: Load–Distractor Function — {participant_id}', fontsize=13)
    fig.tight_layout()

    outfile = os.path.join(output_dir, f'exp2_load_{participant_id}.png')
    fig.savefig(outfile, dpi=150)
    plt.close(fig)
    print(f"  Plot saved: {outfile}")


# ── Main ──────────────────────────────────────────────────────

def main():
    if len(sys.argv) < 2:
        print("Usage: python analyze_exp2.py <path_to_csv_or_folder>")
        sys.exit(1)

    path = sys.argv[1]
    print("=" * 60)
    print("EXPERIMENT 2 — LOAD-DISTRACTOR FUNCTION ANALYSIS")
    print("=" * 60)

    df = load_exp2_data(path)
    participants = df['participant_id'].unique() if 'participant_id' in df.columns else ['unknown']
    print(f"\nLoaded {len(df)} rows, {len(participants)} participant(s)")

    output_dir = os.path.join(os.path.dirname(path) if os.path.isfile(path) else path, 'results')
    os.makedirs(output_dir, exist_ok=True)

    all_results = []

    for pid in participants:
        print(f"\n{'─' * 50}")
        print(f"Participant: {pid}")
        print(f"{'─' * 50}")

        pdf = df[df['participant_id'] == pid] if 'participant_id' in df.columns else df

        catch_ok = analyze_catch_trials(pdf)
        resp = preprocess(pdf)

        if len(resp) < 100:
            print(f"  ⚠ Too few trials ({len(resp)}), skipping.")
            continue

        print_load_table(resp)
        ce_data = compute_compatibility_effect(resp)

        fit_all = {}
        for dist in ['near', 'far']:
            df_ce = ce_data[dist]
            if len(df_ce) < 4:
                continue
            loads = df_ce['load'].values.astype(float)
            ces = df_ce['ce'].values

            print(f"\n  Compatibility effect — {dist} distractor:")
            for _, row in df_ce.iterrows():
                print(f"    Load {int(row['load'])}: CE = {row['ce']:.1f} ms")

            fit_results = fit_load_models(loads, ces)
            print_model_comparison(fit_results, dist)
            fit_all[dist] = fit_results

        plot_load_function(ce_data, fit_all, str(pid), output_dir)

        all_results.append({
            'participant_id': pid,
            'n_trials': len(resp),
            'catch_ok': catch_ok,
            'overall_accuracy': resp['correct'].mean(),
            'mean_rt': resp['rt'].mean(),
            'ce_data': {k: v.to_dict('records') for k, v in ce_data.items()},
            'fit_results': {dist: {name: {kk: vv for kk, vv in res.items() if kk != 'predicted'}
                                   for name, res in fits.items()}
                           for dist, fits in fit_all.items()}
        })

    if len(all_results) > 1:
        print(f"\n{'=' * 60}")
        print("GROUP SUMMARY")
        print(f"{'=' * 60}")
        accs = [r['overall_accuracy'] for r in all_results]
        rts = [r['mean_rt'] for r in all_results]
        print(f"  N = {len(all_results)}")
        print(f"  Mean accuracy: {np.mean(accs):.1%} ± {np.std(accs):.1%}")
        print(f"  Mean RT: {np.mean(rts):.1f} ± {np.std(rts):.1f} ms")

    summary_file = os.path.join(output_dir, 'exp2_summary.json')
    with open(summary_file, 'w') as f:
        json.dump(all_results, f, indent=2, default=str)
    print(f"\nSummary saved: {summary_file}")


if __name__ == '__main__':
    main()
