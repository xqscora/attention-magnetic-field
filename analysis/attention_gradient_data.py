"""
Attention Gradient Empirical Data for MFA Curve Fitting
=======================================================

Compiled from published experimental studies on spatial attention gradients.
Each dataset contains performance (RT, A', d', or ERP amplitude) measured
at multiple distances/eccentricities from the attended location.

For MFA model fitting: F_att(r) = S / r^alpha

Usage:
    python attention_gradient_data.py   # prints summary + runs demo fits
"""

import numpy as np
from scipy.optimize import curve_fit
from scipy.stats import pearsonr
import warnings

# ===========================================================================
# DATASET 1: Handy, Kingstone, & Mangun (1996)
# Source: Perception & Psychophysics, 58(4), 613-627
# "Spatial distribution of visual attention: Perceptual sensitivity
#  and response latency"
# ===========================================================================

# --- Experiment 1: Threshold-level luminance detection, endogenous cuing ---
# N = 16 subjects
# 6 locations in upper visual hemifield, 6.0° from fixation
# Located along an imaginary semicircle, 2.5° center-to-center spacing
# Task: forced two-choice target-present/absent decision

# A' values (nonparametric sensitivity measure)
# Rows = probe location (1-6), Columns = cue location (1-6)
HKM96_exp1_Aprime = np.array([
    [.845, .801, .796, .789, .790, .746],  # probe 1
    [.856, .857, .819, .751, .789, .771],  # probe 2
    [.772, .739, .813, .787, .799, .743],  # probe 3
    [.733, .786, .706, .802, .741, .744],  # probe 4
    [.814, .791, .797, .783, .845, .835],  # probe 5
    [.868, .865, .835, .877, .854, .886],  # probe 6
])

# RT values (ms) - accuracy-emphasis instructions
HKM96_exp1_RT = np.array([
    [493, 579, 554, 568, 558, 568],  # probe 1
    [556, 501, 575, 572, 552, 566],  # probe 2
    [596, 549, 510, 603, 572, 602],  # probe 3
    [585, 545, 519, 499, 556, 560],  # probe 4
    [593, 583, 571, 536, 496, 547],  # probe 5
    [561, 565, 560, 556, 508, 467],  # probe 6
])

# Neutral cue values for reference
HKM96_exp1_Aprime_neutral = np.array([.817, .828, .792, .798, .827, .853])
HKM96_exp1_RT_neutral = np.array([554, 557, 551, 560, 538, 523])

# --- Experiment 3: Suprathreshold RT, speed emphasis ---
# N = 8 subjects
# Same display as Exp 1, but suprathreshold targets, speed-emphasis
# Shows BROADER gradient (more typical of RT literature)

HKM96_exp3_RT = np.array([
    [243, 251, 305, 304, 312, 329],  # probe 1
    [261, 240, 268, 284, 311, 320],  # probe 2
    [296, 246, 235, 259, 307, 290],  # probe 3
    [314, 292, 282, 245, 247, 285],  # probe 4
    [306, 283, 289, 273, 244, 272],  # probe 5
    [325, 317, 327, 293, 269, 244],  # probe 6
])
HKM96_exp3_RT_neutral = np.array([289, 265, 262, 264, 286, 298])


def extract_gradient(matrix, spacing_deg=2.5):
    """
    Extract attention gradient from probe×cue matrix.

    Computes mean performance at each cue-probe distance.
    Distance = |probe_idx - cue_idx| × spacing_deg

    Returns:
        distances: array of distances in degrees
        means: mean performance at each distance
        sems: standard error of means at each distance
        counts: number of data points at each distance
    """
    n = matrix.shape[0]
    max_dist = n - 1
    distances = []
    means = []
    sems = []
    counts = []

    for d in range(max_dist + 1):
        values = []
        for i in range(n):
            for j in range(n):
                if abs(i - j) == d:
                    values.append(matrix[i, j])
        values = np.array(values)
        distances.append(d * spacing_deg)
        means.append(np.mean(values))
        sems.append(np.std(values, ddof=1) / np.sqrt(len(values)))
        counts.append(len(values))

    return np.array(distances), np.array(means), np.array(sems), np.array(counts)


# Pre-compute gradients
HKM96_exp1_Aprime_gradient = extract_gradient(HKM96_exp1_Aprime)
HKM96_exp1_RT_gradient = extract_gradient(HKM96_exp1_RT)
HKM96_exp3_RT_gradient = extract_gradient(HKM96_exp3_RT)


# ===========================================================================
# DATASET 2: Baruch & Goldfarb (2020) — Mexican Hat Profile
# Source: Frontiers in Psychology, 11, 854
# "Mexican Hat Modulation of Visual Acuity Following an Exogenous Cue"
# ===========================================================================
# N = 15 per experiment (2 experiments, 30 total)
# 12 placeholders on imaginary circle, 4.5° from fixation
# 7 cue-target distances in degrees of visual angle:
BG2020_distances_deg = np.array([0.0, 2.3, 4.5, 6.4, 7.8, 8.7, 9.0])

# Measure: IES = RT/accuracy (lower = better performance)
# Key finding: cubic (Mexican hat) trend at short SOAs, linear at long SOAs
# Short SOA (83, 133 ms): cubic trend F(1,14) = 5.9-7.17, p < .05
# Long SOA (216-466 ms): linear trend F(1,14) = 6.57-14.04, p < .05
#
# Combined short-SOA cubic regression: F(1,3) = 22.93, p < .05, R^2 = .958
#
# NOTE: Exact IES values not tabulated in paper; data available on request
# from corresponding author (Orit Baruch, University of Haifa).
# The qualitative pattern (from Figures 2-7):
#   - At short SOAs: enhancement at d=0, SUPPRESSION at d≈2.3-4.5°,
#     then recovery at d≈6.4-9.0° → Mexican hat
#   - At long SOAs: monotonic degradation with distance → classical gradient
#
# Approximate values digitized from Figure 4 (combined short SOAs):
# These are ROUGH estimates from the published figure; use with caution
BG2020_IES_short_SOA_approx = np.array([680, 740, 720, 700, 710, 715, 710])
BG2020_IES_long_SOA_approx = np.array([660, 680, 690, 700, 710, 720, 730])


# ===========================================================================
# DATASET 3: Downing (1988) — Sensitivity Gradient
# Source: J. Exp. Psychol. HPP, 14(2), 188-202
# "Expectancy and visual-spatial attention: Effects on perceptual quality"
# ===========================================================================
# N = not specified in abstract (multiple experiments)
# 12-location circular display, endogenous (arrow) cuing
# 4 tasks: luminance detection, brightness discrimination,
#          orientation discrimination, form discrimination
# Measure: d' (signal detection sensitivity)
#
# Key quantitative findings (from Handy et al. 1996 review, p. 614):
#   "Downing found decreases in d' with increasing target distance
#    from the attended location. However, in all four task conditions
#    the d' distributions showed a large initial drop, with relatively
#    little change in d' beyond about 3° from the attended location."
#
# Pattern: FOCAL gradient — steep drop within ~3°, then plateau
# Steeper gradient for orientation & form tasks than luminance & brightness
#
# Approximate d' pattern (reconstructed from verbal description):
Downing88_distances_approx_deg = np.array([0, 1.5, 3.0, 4.5, 6.0, 7.5, 9.0])
Downing88_dprime_luminance_approx = np.array([2.5, 1.8, 1.5, 1.4, 1.4, 1.3, 1.3])
Downing88_dprime_form_approx = np.array([2.5, 1.5, 1.2, 1.1, 1.1, 1.0, 1.0])


# ===========================================================================
# DATASET 4: Mangun & Hillyard (1988) — Behavioral + ERP Gradient
# Source: Electroencephalography & Clinical Neurophysiology, 70(5), 417-428
# "Spatial gradients of visual attention: behavioral and
#  electrophysiological evidence"
# ===========================================================================
# N = not specified in abstract
# 3 stimulus locations: left VF, right VF, midline (above fixation)
# Primary task: detect shorter target bars at attended location
# Secondary task: respond to targets at unattended locations
#
# Measures:
#   1. d' (target detectability) — decreased with distance from attended
#   2. P135 ERP amplitude (μV) — decreased with distance
#   3. N190 ERP amplitude (μV) — decreased with distance
#
# Key finding: "d' scores decreased progressively as attention was
# directed to locations increasingly distant from a given stimulus location"
# Same pattern for P135 and N190 amplitudes.
#
# 3 locations means only 3 distance levels (0, 1, 2 steps).
# Exact numerical values require access to original paper.
# Qualitative pattern: monotonic decrease with distance.


# ===========================================================================
# DATASET 5: LaBerge (1983) — V-Shape RT Pattern
# Source: J. Exp. Psychol. HPP, 9(3), 371-379
# "Spatial extent of attention to letters and words"
# ===========================================================================
# N = 135 undergraduates total (across experiments)
# 5 letter positions within 5-letter strings
# Probe: digit "7" in one of 5 positions
# Visual angle: each letter ~0.5°, total string ~2.5° (estimated)
#
# Position coding: 1=leftmost, 3=center, 5=rightmost
# Distance from center in letter positions:
LaBerge83_positions = np.array([1, 2, 3, 4, 5])
LaBerge83_dist_from_center = np.array([2, 1, 0, 1, 2])  # letter positions
LaBerge83_dist_from_center_deg = LaBerge83_dist_from_center * 0.5  # approx °

# Pattern (from paper description and LaBerge et al. 1991 review):
#   Letter-categorization task: V-shaped RT (narrow ~1 letter focus)
#   Word-categorization task: flat RT (broad ~5 letter focus)
#
# Approximate RT values from V-shape description:
LaBerge83_RT_letter_task_approx = np.array([520, 490, 460, 490, 520])  # V-shape
LaBerge83_RT_word_task_approx = np.array([480, 480, 475, 480, 480])    # flat


# ===========================================================================
# DATASET 6: Müller & Kleinschmidt (2004) — Neural Center-Surround
# Source: NeuroReport, 15(6), 2004
# "The attentional spotlight's penumbra: center-surround modulation
#  in striate cortex"
# ===========================================================================
# fMRI study: BOLD signal in striate cortex
# Pattern: enhanced activity at attended center, SUPPRESSED at near
#          surround, normal at far locations.
# Center-surround profile in V1.
# Exact BOLD % signal change values require access to paper.
# Key qualitative result: suppression at near distances, not at far.


# ===========================================================================
# DERIVED QUANTITIES FOR MFA FITTING
# ===========================================================================

def get_best_gradient_datasets():
    """
    Return the datasets most suitable for MFA curve fitting,
    already processed into (distance, performance) format.

    Returns dict of datasets, each containing:
        - distances_deg: distance from attended location (degrees)
        - performance: performance measure (higher = better, except RT)
        - measure: what was measured
        - N: sample size
        - source: citation
        - notes: additional info
    """
    datasets = {}

    # Dataset A: HKM96 Exp1 A' (focal gradient)
    d, m, se, n = HKM96_exp1_Aprime_gradient
    datasets['HKM96_exp1_Aprime'] = {
        'distances_deg': d,
        'performance': m,
        'sem': se,
        'n_datapoints': n,
        'measure': "A' (nonparametric sensitivity)",
        'direction': 'higher_is_better',
        'N': 16,
        'source': 'Handy, Kingstone, & Mangun (1996) Exp 1',
        'journal': 'Perception & Psychophysics, 58(4), 613-627',
        'notes': 'Threshold detection, endogenous cue, accuracy emphasis. Focal gradient within 2.5°.'
    }

    # Dataset B: HKM96 Exp1 RT (focal gradient)
    d, m, se, n = HKM96_exp1_RT_gradient
    datasets['HKM96_exp1_RT'] = {
        'distances_deg': d,
        'performance': m,
        'sem': se,
        'n_datapoints': n,
        'measure': 'RT (ms)',
        'direction': 'lower_is_better',
        'N': 16,
        'source': 'Handy, Kingstone, & Mangun (1996) Exp 1',
        'journal': 'Perception & Psychophysics, 58(4), 613-627',
        'notes': 'Same paradigm as A\'. Focal gradient, matches A\' pattern.'
    }

    # Dataset C: HKM96 Exp3 RT (broad gradient) — BEST FOR MFA
    d, m, se, n = HKM96_exp3_RT_gradient
    datasets['HKM96_exp3_RT'] = {
        'distances_deg': d,
        'performance': m,
        'sem': se,
        'n_datapoints': n,
        'measure': 'RT (ms)',
        'direction': 'lower_is_better',
        'N': 8,
        'source': 'Handy, Kingstone, & Mangun (1996) Exp 3',
        'journal': 'Perception & Psychophysics, 58(4), 613-627',
        'notes': 'Suprathreshold detection, speed emphasis. BROAD gradient extending to 7.5°+. Most typical of RT gradient literature.'
    }

    # Dataset D: Baruch & Goldfarb (2020) Mexican Hat
    datasets['BG2020_MexicanHat'] = {
        'distances_deg': BG2020_distances_deg,
        'performance': BG2020_IES_short_SOA_approx,
        'sem': None,
        'n_datapoints': np.full(7, 15),
        'measure': 'IES (RT/accuracy, lower=better)',
        'direction': 'lower_is_better',
        'N': 15,
        'source': 'Baruch & Goldfarb (2020)',
        'journal': 'Frontiers in Psychology, 11, 854',
        'notes': 'APPROXIMATE values from figure. Short SOA (83-133 ms). Mexican hat profile: enhancement at center, suppression at 2-4°, recovery at 6-9°.'
    }

    return datasets


# ===========================================================================
# MFA MODEL FUNCTIONS FOR CURVE FITTING
# ===========================================================================

def mfa_inverse_square(r, S, baseline, r0=0.5):
    """
    MFA basic field equation: F_att(r) = S / (r + r0)^2
    For RT fitting: RT = baseline - S/(r+r0)^2
    r0 prevents singularity at r=0
    """
    return baseline - S / (r + r0) ** 2


def mfa_inverse_power(r, S, alpha, baseline, r0=0.5):
    """
    MFA generalized: F_att(r) = S / (r + r0)^alpha
    For RT fitting: RT = baseline - S/(r+r0)^alpha
    """
    return baseline - S / (r + r0) ** alpha


def mfa_for_accuracy(r, S, baseline, r0=0.5):
    """
    For accuracy measures (higher = better):
    performance = baseline_low + S / (r + r0)^2
    """
    return baseline + S / (r + r0) ** 2


def mfa_mexican_hat(r, S1, S2, sigma, baseline, r0=0.5):
    """
    MFA with center-surround (Mexican hat):
    F(r) = S1/(r+r0)^2 - S2*exp(-(r-mu)^2/(2*sigma^2))
    Center excitation (1/r^2) minus Gaussian surround suppression
    """
    center = S1 / (r + r0) ** 2
    surround = S2 * np.exp(-r ** 2 / (2 * sigma ** 2))
    return baseline + center - surround


def fit_mfa_to_rt_gradient(distances, rt_means, alpha_fixed=2.0):
    """
    Fit MFA 1/r^alpha model to RT gradient data.

    For RT data: RT(r) = RT_baseline - S/(r+r0)^alpha
    where RT_baseline is the asymptotic RT at large distances.

    Returns: fitted parameters, R^2, predicted values
    """
    r0 = 0.5

    if alpha_fixed is not None:
        def model(r, S, baseline):
            return baseline - S / (r + r0) ** alpha_fixed

        p0 = [50.0, np.max(rt_means)]
        bounds = ([0, 0], [1e6, 1e4])
    else:
        def model(r, S, alpha, baseline):
            return baseline - S / (r + r0) ** alpha

        p0 = [50.0, 2.0, np.max(rt_means)]
        bounds = ([0, 0.1, 0], [1e6, 10, 1e4])

    try:
        popt, pcov = curve_fit(model, distances, rt_means, p0=p0, bounds=bounds)
        predicted = model(distances, *popt)
        ss_res = np.sum((rt_means - predicted) ** 2)
        ss_tot = np.sum((rt_means - np.mean(rt_means)) ** 2)
        r_squared = 1 - ss_res / ss_tot

        return {
            'params': popt,
            'param_names': ['S', 'baseline'] if alpha_fixed else ['S', 'alpha', 'baseline'],
            'R_squared': r_squared,
            'predicted': predicted,
            'alpha': alpha_fixed if alpha_fixed else popt[1],
        }
    except RuntimeError as e:
        return {'error': str(e)}


def fit_mfa_to_accuracy_gradient(distances, accuracy_means, alpha_fixed=2.0):
    """
    Fit MFA 1/r^alpha model to accuracy/sensitivity gradient.

    For accuracy: perf(r) = perf_baseline + S/(r+r0)^alpha
    """
    r0 = 0.5

    if alpha_fixed is not None:
        def model(r, S, baseline):
            return baseline + S / (r + r0) ** alpha_fixed

        p0 = [0.05, np.min(accuracy_means)]
        bounds = ([0, 0], [10, 1])
    else:
        def model(r, S, alpha, baseline):
            return baseline + S / (r + r0) ** alpha

        p0 = [0.05, 2.0, np.min(accuracy_means)]
        bounds = ([0, 0.1, 0], [10, 10, 1])

    try:
        popt, pcov = curve_fit(model, distances, accuracy_means, p0=p0, bounds=bounds)
        predicted = model(distances, *popt)
        ss_res = np.sum((accuracy_means - predicted) ** 2)
        ss_tot = np.sum((accuracy_means - np.mean(accuracy_means)) ** 2)
        r_squared = 1 - ss_res / ss_tot

        return {
            'params': popt,
            'param_names': ['S', 'baseline'] if alpha_fixed else ['S', 'alpha', 'baseline'],
            'R_squared': r_squared,
            'predicted': predicted,
            'alpha': alpha_fixed if alpha_fixed else popt[1],
        }
    except RuntimeError as e:
        return {'error': str(e)}


# ===========================================================================
# MAIN: Summary & Demo Fits
# ===========================================================================

if __name__ == '__main__':
    print("=" * 70)
    print("ATTENTION GRADIENT DATA — SUMMARY FOR MFA CURVE FITTING")
    print("=" * 70)

    datasets = get_best_gradient_datasets()

    for name, ds in datasets.items():
        print(f"\n{'─' * 60}")
        print(f"Dataset: {name}")
        print(f"  Source: {ds['source']}")
        print(f"  Journal: {ds['journal']}")
        print(f"  Measure: {ds['measure']} ({ds['direction']})")
        print(f"  N subjects: {ds['N']}")
        print(f"  Distances (°): {ds['distances_deg']}")
        print(f"  Performance:   {np.round(ds['performance'], 3)}")
        if ds['sem'] is not None:
            print(f"  SEM:           {np.round(ds['sem'], 4)}")
        print(f"  Notes: {ds['notes']}")

    print(f"\n{'=' * 70}")
    print("MFA CURVE FITTING RESULTS")
    print("=" * 70)

    # Fit 1: HKM96 Exp3 RT (broad gradient) — inverse-square
    print("\n--- HKM96 Exp3 RT: F(r) = S/r^2 model ---")
    d3 = datasets['HKM96_exp3_RT']
    result = fit_mfa_to_rt_gradient(d3['distances_deg'], d3['performance'], alpha_fixed=2.0)
    if 'error' not in result:
        print(f"  S = {result['params'][0]:.2f}")
        print(f"  RT_baseline = {result['params'][1]:.1f} ms")
        print(f"  alpha = {result['alpha']}")
        print(f"  R^2 = {result['R_squared']:.4f}")
        print(f"  Predicted: {np.round(result['predicted'], 1)}")
        print(f"  Observed:  {np.round(d3['performance'], 1)}")

    # Fit 2: HKM96 Exp3 RT -- free alpha
    print("\n--- HKM96 Exp3 RT: F(r) = S/r^a model (free alpha) ---")
    result_free = fit_mfa_to_rt_gradient(d3['distances_deg'], d3['performance'], alpha_fixed=None)
    if 'error' not in result_free:
        print(f"  S = {result_free['params'][0]:.2f}")
        print(f"  alpha = {result_free['params'][1]:.3f}")
        print(f"  RT_baseline = {result_free['params'][2]:.1f} ms")
        print(f"  R^2 = {result_free['R_squared']:.4f}")

    # Fit 3: HKM96 Exp1 A' (accuracy) -- inverse-square
    print("\n--- HKM96 Exp1 A': F(r) = S/r^2 model ---")
    d1 = datasets['HKM96_exp1_Aprime']
    result_a = fit_mfa_to_accuracy_gradient(d1['distances_deg'], d1['performance'], alpha_fixed=2.0)
    if 'error' not in result_a:
        print(f"  S = {result_a['params'][0]:.4f}")
        print(f"  A'_baseline = {result_a['params'][1]:.4f}")
        print(f"  alpha = {result_a['alpha']}")
        print(f"  R^2 = {result_a['R_squared']:.4f}")
        print(f"  Predicted: {np.round(result_a['predicted'], 3)}")
        print(f"  Observed:  {np.round(d1['performance'], 3)}")

    # Fit 4: HKM96 Exp1 A' -- free alpha
    print("\n--- HKM96 Exp1 A': F(r) = S/r^alpha model (free alpha) ---")
    result_a2 = fit_mfa_to_accuracy_gradient(d1['distances_deg'], d1['performance'], alpha_fixed=None)
    if 'error' not in result_a2:
        print(f"  S = {result_a2['params'][0]:.4f}")
        print(f"  alpha = {result_a2['params'][1]:.3f}")
        print(f"  A'_baseline = {result_a2['params'][2]:.4f}")
        print(f"  R^2 = {result_a2['R_squared']:.4f}")

    # Fit 5: HKM96 Exp1 RT (focal gradient)
    print("\n--- HKM96 Exp1 RT (focal): F(r) = S/r^alpha (free alpha) ---")
    d1r = datasets['HKM96_exp1_RT']
    result_r1 = fit_mfa_to_rt_gradient(d1r['distances_deg'], d1r['performance'], alpha_fixed=None)
    if 'error' not in result_r1:
        print(f"  S = {result_r1['params'][0]:.2f}")
        print(f"  alpha = {result_r1['params'][1]:.3f}")
        print(f"  RT_baseline = {result_r1['params'][2]:.1f} ms")
        print(f"  R^2 = {result_r1['R_squared']:.4f}")

    print(f"\n{'=' * 70}")
    print("COMPARISON OF FITTED alpha VALUES")
    print("=" * 70)
    print("""
    MFA predicts alpha ≈ 2 (inverse-square law).

    If fitted alpha values cluster near 2, this supports the MFA
    framework's use of magnetic-field-like 1/r^2 decay.

    Values of alpha < 2 suggest broader gradients (more spread);
    alpha > 2 suggests steeper, more focal gradients.

    For focal (threshold) tasks: alpha may be > 2
    For broad (suprathreshold) tasks: alpha may be ≈ 1-2
    This is consistent with MFA's field strength S parameter:
    high S → narrow effective field, low S → broad field.
    """)

    print("=" * 70)
    print("ADDITIONAL DATA SOURCES TO OBTAIN")
    print("=" * 70)
    print("""
    The following papers contain relevant gradient data that would
    strengthen the MFA fitting analysis. Exact values require accessing
    the original papers (behind paywalls or available through libraries):

    1. Downing (1988) — d' at 12 locations around circular display
       Tables/figures with d' × distance for 4 task types
       J. Exp. Psychol. HPP, 14(2), 188-202

    2. Mangun & Hillyard (1988) — d' AND P135/N190 ERP amplitudes
       3 locations, progressive decrease with distance
       EEG & Clin. Neurophysiol., 70(5), 417-428

    3. LaBerge (1983) — RT at 5 probe positions
       V-shaped RT function for letter categorization
       J. Exp. Psychol. HPP, 9(3), 371-379

    4. Henderson & Macquistan (1993) — RT gradient out to 19.7°
       8 locations (Exp 1) and 4 locations (Exp 2-3)
       Perception & Psychophysics, 53, 221-230

    5. Cutzu & Tsotsos (2003) — accuracy × inter-target separation
       Evidence for suppressive annulus
       Vision Research, 43(2), 205-219

    6. Müller et al. (2005) — Mexican hat from distractor interference
       Vision Research, 45(9), 1129-1137

    7. Baruch & Goldfarb (2020) — exact IES values available on request
       from corresponding author (Orit Baruch, University of Haifa)
       Frontiers in Psychology, 11, 854

    8. Shulman, Wilson, & Sheehy (1985) — RT × distance from cue
       Perception & Psychophysics, 37, 59-65

    9. Downing & Pinker (1985) — RT at 10 locations
       In: Attention and Performance XI, pp. 171-188

    10. Eriksen & St. James (1986) — zoom lens RT data
        Perception & Psychophysics, 40, 225-240
    """)
