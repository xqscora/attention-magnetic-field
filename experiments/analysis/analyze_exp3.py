"""
Experiment 3 — OE × Distractor Interaction Analysis
====================================================

Analyzes jsPsych output from exp3_oe.html.
Tests MFA Prediction 3: Dimension-specific attentional capture linked
to individual overexcitability (OE) profiles.

Key analyses:
  1. RT by distractor type (sensory, semantic, emotional, movement, neutral)
  2. OEQII subscale scoring (Psychomotor, Sensual, Intellectual,
     Imaginational, Emotional)
  3. OE subscale × distractor-type RT correlations
  4. Dimension-specific predictions: Sensual OE → sensory capture,
     Emotional OE → emotional capture, etc.

Usage:
    python analyze_exp3.py data/exp3_oe_*.csv
    python analyze_exp3.py data/

Cora Zeng, 2026
"""

import sys
import os
import glob
import json
import numpy as np
import pandas as pd
from scipy.stats import pearsonr, spearmanr
import warnings
warnings.filterwarnings('ignore')

try:
    import matplotlib
    matplotlib.use('Agg')
    import matplotlib.pyplot as plt
    HAS_PLOT = True
except ImportError:
    HAS_PLOT = False


# ── OE subscale mapping ──────────────────────────────────────

OE_SUBSCALES = {
    'psychomotor':   list(range(1, 11)),   # items 1-10
    'sensual':       list(range(11, 21)),   # items 11-20
    'intellectual':  list(range(21, 31)),   # items 21-30
    'imaginational': list(range(31, 41)),   # items 31-40
    'emotional':     list(range(41, 51)),   # items 41-50
}

# MFA predicted mappings: which OE subscale predicts which distractor capture
OE_DISTRACTOR_MAP = {
    'sensual':       'sensory',
    'intellectual':  'semantic',
    'emotional':     'emotional',
    'psychomotor':   'movement',
}


# ── Data loading ──────────────────────────────────────────────

def load_exp3_data(path):
    if os.path.isdir(path):
        files = sorted(glob.glob(os.path.join(path, 'exp3_oe_*.csv')))
    elif '*' in path:
        files = sorted(glob.glob(path))
    else:
        files = [path]

    if not files:
        raise FileNotFoundError(f"No Exp3 files found at: {path}")

    frames = []
    for f in files:
        df = pd.read_csv(f)
        df['source_file'] = os.path.basename(f)
        frames.append(df)
    return pd.concat(frames, ignore_index=True)


def preprocess_behavioral(df):
    resp = df[(df['phase'] == 'response') &
              (df['task'] == 'oe_distractor') &
              (df['is_practice'] == False) &
              (df['is_catch'] == False)].copy()

    resp['rt'] = pd.to_numeric(resp['rt'], errors='coerce')
    resp = resp.dropna(subset=['rt'])

    n_before = len(resp)
    resp = resp[(resp['rt'] >= 150) & (resp['rt'] <= 2000)]
    resp = resp[resp['timed_out'] == False]
    n_after = len(resp)

    print(f"  Behavioral trials: {n_before} → {n_after} after cleaning")
    return resp


def analyze_catch_trials(df):
    catch = df[(df['is_catch'] == True) & (df['phase'] == 'response')]
    if len(catch) == 0:
        return True
    acc = catch['correct'].mean()
    print(f"  Catch accuracy: {acc*100:.1f}% ({int(acc*len(catch))}/{len(catch)})")
    return acc >= 0.75


# ── OEQII scoring ────────────────────────────────────────────

def score_oeqii(df):
    """Extract and score OEQII responses from the data."""
    oe_rows = df[df['phase'] == 'oeqii']
    if len(oe_rows) == 0:
        print("  ⚠ No OEQII data found!")
        return None

    all_responses = {}
    for _, row in oe_rows.iterrows():
        if 'oeqii_parsed' in row and pd.notna(row['oeqii_parsed']):
            try:
                parsed = json.loads(row['oeqii_parsed'])
                for key, val in parsed.items():
                    if not key.endswith('_subscale'):
                        all_responses[key] = val
            except (json.JSONDecodeError, TypeError):
                pass

    if not all_responses:
        print("  ⚠ Could not parse OEQII responses!")
        return None

    scores = {}
    for subscale, items in OE_SUBSCALES.items():
        item_scores = []
        for item_num in items:
            key = f'oeqii_{item_num}'
            if key in all_responses:
                item_scores.append(all_responses[key])
        if item_scores:
            scores[subscale] = {
                'mean': np.mean(item_scores),
                'sum': np.sum(item_scores),
                'n_items': len(item_scores),
                'items': item_scores
            }

    return scores


def print_oe_profile(scores):
    """Print OE profile summary."""
    if not scores:
        return

    print(f"\n  OEQII Profile (0-4 scale):")
    print(f"  {'Subscale':<16} {'Mean':>6} {'Sum':>5} {'Items':>6}")
    print(f"  {'─'*40}")

    for sub in ['psychomotor', 'sensual', 'intellectual', 'imaginational', 'emotional']:
        if sub in scores:
            s = scores[sub]
            bar = '█' * int(s['mean'] * 5)
            print(f"  {sub:<16} {s['mean']:>6.2f} {s['sum']:>5.0f} {s['n_items']:>5}  {bar}")


# ── Distractor-type analysis ─────────────────────────────────

def analyze_distractor_types(resp):
    """Compute RT and accuracy by distractor type."""
    correct_only = resp[resp['correct'] == True]

    results = correct_only.groupby('distractor_type').agg(
        mean_rt=('rt', 'mean'),
        sd_rt=('rt', 'std'),
        n=('rt', 'count')
    ).reset_index()
    results['se_rt'] = results['sd_rt'] / np.sqrt(results['n'])

    acc_by_type = resp.groupby('distractor_type')['correct'].mean().reset_index()
    acc_by_type.columns = ['distractor_type', 'accuracy']
    results = results.merge(acc_by_type, on='distractor_type')

    neutral_rt = results[results['distractor_type'] == 'neutral']['mean_rt'].values
    if len(neutral_rt) > 0:
        results['capture_effect'] = results['mean_rt'] - neutral_rt[0]
    else:
        results['capture_effect'] = 0

    print(f"\n  Performance by distractor type (correct trials):")
    print(f"  {'Type':<12} {'RT':>8} {'SD':>7} {'Acc':>7} {'Capture':>9} {'N':>5}")
    print(f"  {'─'*52}")
    for _, row in results.iterrows():
        print(f"  {row['distractor_type']:<12} {row['mean_rt']:>8.1f} "
              f"{row['sd_rt']:>7.1f} {row['accuracy']:>6.1%} "
              f"{row['capture_effect']:>+8.1f} {int(row['n']):>5}")

    return results


# ── OE × Distractor correlation ──────────────────────────────

def correlate_oe_distractor(participants_data):
    """
    Across participants: correlate each OE subscale score with
    the corresponding distractor-type capture effect.
    """
    if len(participants_data) < 5:
        print(f"\n  ⚠ Need ≥5 participants for correlations (have {len(participants_data)})")
        return None

    print(f"\n  OE × Distractor Capture Correlations (N={len(participants_data)}):")
    print(f"  {'OE Subscale':<16} {'Distractor':<12} {'r':>7} {'p':>8} {'Predicted?':>11}")
    print(f"  {'─'*58}")

    results = {}

    for oe_sub, dist_type in OE_DISTRACTOR_MAP.items():
        oe_scores = []
        capture_effects = []

        for pd_item in participants_data:
            if pd_item['oe_scores'] and oe_sub in pd_item['oe_scores']:
                oe_val = pd_item['oe_scores'][oe_sub]['mean']
            else:
                continue

            dist_data = pd_item['distractor_results']
            capture = dist_data[dist_data['distractor_type'] == dist_type]['capture_effect'].values
            if len(capture) > 0:
                oe_scores.append(oe_val)
                capture_effects.append(capture[0])

        if len(oe_scores) >= 5:
            r, p = pearsonr(oe_scores, capture_effects)
            r_s, p_s = spearmanr(oe_scores, capture_effects)
            sig = '***' if p < .001 else '**' if p < .01 else '*' if p < .05 else '†' if p < .10 else ''
            predicted = 'YES' if r > 0 else 'no'
            print(f"  {oe_sub:<16} {dist_type:<12} {r:>7.3f} {p:>7.4f}{sig} {predicted:>10}")

            results[f'{oe_sub}_x_{dist_type}'] = {
                'r_pearson': r, 'p_pearson': p,
                'r_spearman': r_s, 'p_spearman': p_s,
                'n': len(oe_scores),
                'predicted_direction': r > 0
            }

    # Non-predicted correlations (cross-domain)
    print(f"\n  Cross-domain correlations (not predicted by MFA):")
    all_subs = list(OE_SUBSCALES.keys())
    all_types = ['sensory', 'semantic', 'emotional', 'movement']

    for oe_sub in all_subs:
        for dist_type in all_types:
            if oe_sub in OE_DISTRACTOR_MAP and OE_DISTRACTOR_MAP[oe_sub] == dist_type:
                continue

            oe_scores = []
            capture_effects = []

            for pd_item in participants_data:
                if pd_item['oe_scores'] and oe_sub in pd_item['oe_scores']:
                    oe_val = pd_item['oe_scores'][oe_sub]['mean']
                else:
                    continue
                dist_data = pd_item['distractor_results']
                capture = dist_data[dist_data['distractor_type'] == dist_type]['capture_effect'].values
                if len(capture) > 0:
                    oe_scores.append(oe_val)
                    capture_effects.append(capture[0])

            if len(oe_scores) >= 5:
                r, p = pearsonr(oe_scores, capture_effects)
                sig = '*' if p < .05 else ''
                if abs(r) > 0.3 or p < 0.1:
                    print(f"  {oe_sub:<16} {dist_type:<12} r={r:.3f} p={p:.4f}{sig}")

    return results


# ── Plotting ──────────────────────────────────────────────────

def plot_distractor_types(dist_results, participant_id, output_dir):
    if not HAS_PLOT:
        return

    fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(12, 5))

    types = dist_results['distractor_type'].values
    rts = dist_results['mean_rt'].values
    ses = dist_results['se_rt'].values
    captures = dist_results['capture_effect'].values

    colors = {'sensory': '#e74c3c', 'semantic': '#3498db', 'emotional': '#f1c40f',
              'movement': '#2ecc71', 'neutral': '#95a5a6'}
    bar_colors = [colors.get(t, '#888') for t in types]

    ax1.bar(types, rts, yerr=ses, color=bar_colors, alpha=0.8, capsize=4)
    ax1.set_ylabel('RT (ms)', fontsize=11)
    ax1.set_title('Mean RT by Distractor Type', fontsize=12)
    ax1.tick_params(axis='x', rotation=30)
    ax1.grid(True, alpha=0.3, axis='y')

    ax2.bar(types, captures, color=bar_colors, alpha=0.8)
    ax2.axhline(y=0, color='#333', linewidth=1)
    ax2.set_ylabel('Capture Effect (ms vs neutral)', fontsize=11)
    ax2.set_title('Attentional Capture by Type', fontsize=12)
    ax2.tick_params(axis='x', rotation=30)
    ax2.grid(True, alpha=0.3, axis='y')

    fig.suptitle(f'Exp 3: OE × Distractor — {participant_id}', fontsize=13)
    fig.tight_layout()

    outfile = os.path.join(output_dir, f'exp3_oe_{participant_id}.png')
    fig.savefig(outfile, dpi=150)
    plt.close(fig)
    print(f"  Plot saved: {outfile}")


def plot_oe_correlations(participants_data, output_dir):
    """Plot OE × capture scatter plots for predicted mappings."""
    if not HAS_PLOT or len(participants_data) < 5:
        return

    fig, axes = plt.subplots(2, 2, figsize=(10, 8))
    ax_list = axes.flatten()

    for idx, (oe_sub, dist_type) in enumerate(OE_DISTRACTOR_MAP.items()):
        ax = ax_list[idx]

        oe_scores = []
        capture_effects = []
        for pd_item in participants_data:
            if pd_item['oe_scores'] and oe_sub in pd_item['oe_scores']:
                oe_val = pd_item['oe_scores'][oe_sub]['mean']
            else:
                continue
            dist_data = pd_item['distractor_results']
            capture = dist_data[dist_data['distractor_type'] == dist_type]['capture_effect'].values
            if len(capture) > 0:
                oe_scores.append(oe_val)
                capture_effects.append(capture[0])

        if len(oe_scores) >= 3:
            ax.scatter(oe_scores, capture_effects, s=50, alpha=0.7,
                       color='#3498db', edgecolors='#2c3e50')
            r, p = pearsonr(oe_scores, capture_effects)
            if len(oe_scores) >= 3:
                z = np.polyfit(oe_scores, capture_effects, 1)
                x_line = np.linspace(min(oe_scores), max(oe_scores), 100)
                ax.plot(x_line, np.polyval(z, x_line), 'r--', alpha=0.7)

            ax.set_xlabel(f'{oe_sub.capitalize()} OE', fontsize=10)
            ax.set_ylabel(f'{dist_type} capture (ms)', fontsize=10)
            ax.set_title(f'r = {r:.3f}, p = {p:.3f}', fontsize=10)
            ax.grid(True, alpha=0.3)

    fig.suptitle('MFA Prediction 3: OE × Distractor Capture', fontsize=13)
    fig.tight_layout()

    outfile = os.path.join(output_dir, 'exp3_oe_correlations.png')
    fig.savefig(outfile, dpi=150)
    plt.close(fig)
    print(f"  Correlation plot saved: {outfile}")


# ── Main ──────────────────────────────────────────────────────

def main():
    if len(sys.argv) < 2:
        print("Usage: python analyze_exp3.py <path_to_csv_or_folder>")
        sys.exit(1)

    path = sys.argv[1]
    print("=" * 60)
    print("EXPERIMENT 3 — OE × DISTRACTOR INTERACTION ANALYSIS")
    print("=" * 60)

    df = load_exp3_data(path)
    participants = df['participant_id'].unique() if 'participant_id' in df.columns else ['unknown']
    print(f"\nLoaded {len(df)} rows, {len(participants)} participant(s)")

    output_dir = os.path.join(os.path.dirname(path) if os.path.isfile(path) else path, 'results')
    os.makedirs(output_dir, exist_ok=True)

    all_participants = []

    for pid in participants:
        print(f"\n{'─' * 50}")
        print(f"Participant: {pid}")
        print(f"{'─' * 50}")

        pdf = df[df['participant_id'] == pid] if 'participant_id' in df.columns else df

        catch_ok = analyze_catch_trials(pdf)
        oe_scores = score_oeqii(pdf)
        print_oe_profile(oe_scores)

        resp = preprocess_behavioral(pdf)
        if len(resp) < 50:
            print(f"  ⚠ Too few trials ({len(resp)}), skipping.")
            continue

        dist_results = analyze_distractor_types(resp)
        plot_distractor_types(dist_results, str(pid), output_dir)

        all_participants.append({
            'participant_id': pid,
            'n_trials': len(resp),
            'catch_ok': catch_ok,
            'oe_scores': oe_scores,
            'distractor_results': dist_results,
            'overall_accuracy': resp['correct'].mean(),
            'mean_rt': resp['rt'].mean(),
        })

    if len(all_participants) > 1:
        print(f"\n{'=' * 60}")
        print("GROUP ANALYSIS — OE × DISTRACTOR CORRELATIONS")
        print(f"{'=' * 60}")
        corr_results = correlate_oe_distractor(all_participants)
        plot_oe_correlations(all_participants, output_dir)

        print(f"\n{'=' * 60}")
        print("GROUP SUMMARY")
        print(f"{'=' * 60}")
        accs = [r['overall_accuracy'] for r in all_participants]
        rts = [r['mean_rt'] for r in all_participants]
        print(f"  N = {len(all_participants)}")
        print(f"  Mean accuracy: {np.mean(accs):.1%} ± {np.std(accs):.1%}")
        print(f"  Mean RT: {np.mean(rts):.1f} ± {np.std(rts):.1f} ms")

        oe_means = {sub: [] for sub in OE_SUBSCALES}
        for p in all_participants:
            if p['oe_scores']:
                for sub in OE_SUBSCALES:
                    if sub in p['oe_scores']:
                        oe_means[sub].append(p['oe_scores'][sub]['mean'])
        print(f"\n  Group OE profile:")
        for sub in ['psychomotor', 'sensual', 'intellectual', 'imaginational', 'emotional']:
            vals = oe_means[sub]
            if vals:
                print(f"    {sub:<16}: M={np.mean(vals):.2f}, SD={np.std(vals):.2f}")

    summary_data = []
    for p in all_participants:
        entry = {
            'participant_id': p['participant_id'],
            'n_trials': p['n_trials'],
            'catch_ok': p['catch_ok'],
            'overall_accuracy': p['overall_accuracy'],
            'mean_rt': p['mean_rt'],
            'oe_scores': {k: {'mean': v['mean'], 'sum': v['sum']}
                         for k, v in p['oe_scores'].items()} if p['oe_scores'] else None,
            'distractor_rt': p['distractor_results'][['distractor_type', 'mean_rt', 'capture_effect']].to_dict('records')
        }
        summary_data.append(entry)

    summary_file = os.path.join(output_dir, 'exp3_summary.json')
    with open(summary_file, 'w') as f:
        json.dump(summary_data, f, indent=2, default=str)
    print(f"\nSummary saved: {summary_file}")


if __name__ == '__main__':
    main()
