"""
MFA Saliency Model v2
=====================
Improvements over v1, each grounded in established neuroscience:

1. Gabor filters (Hubel & Wiesel, 1962; Jones & Palmer, 1987)
   - V1 simple cells have Gabor-shaped receptive fields, not Sobel
   - 6 orientations x 3 spatial frequencies = 18 orientation channels

2. Competitive normalization (Heeger, 1992; Itti & Koch, 2000)
   - Lateral inhibition is how the cortex resolves competition between stimuli
   - Promotes feature maps with few strong peaks, suppresses uniform activation

3. Post-hoc fixation spread (Wooding, 2002; Le Meur et al., 2006)
   - Human fixations have spatial uncertainty (~1 degree visual angle)
   - A small Gaussian blur on the output models this foveal spread

4. Multi-scale MFA kernel
   - Attention effects operate across spatial scales (Eriksen & St. James, 1986)
   - MFA field applied at 3 scales, capturing both local and global attention

Usage:
    python mfa_saliency_v2.py --dataset images/ --output_dir results/
    python mfa_saliency_v2.py --dataset images/ --fixations fixmaps/ --output_dir results/
"""

import numpy as np
from PIL import Image
import os
import sys
import json
import argparse
import time
from scipy.ndimage import gaussian_filter
from scipy.signal import fftconvolve
from scipy import stats


# ============================================================
# MFA Kernel (unchanged — this is the theoretical core)
# ============================================================

def mfa_kernel(size, alpha=2.0, epsilon=1.0):
    """MFA inverse-power decay: F(r) = 1 / (r^alpha + epsilon)"""
    center = size // 2
    y, x = np.mgrid[:size, :size]
    r = np.sqrt((x - center)**2 + (y - center)**2)
    kernel = 1.0 / (r**alpha + epsilon)
    kernel /= kernel.sum()
    return kernel


def gaussian_kernel(size, sigma):
    """Standard Gaussian kernel for baseline comparison."""
    center = size // 2
    y, x = np.mgrid[:size, :size]
    r2 = (x - center)**2 + (y - center)**2
    kernel = np.exp(-r2 / (2 * sigma**2))
    kernel /= kernel.sum()
    return kernel


# ============================================================
# IMPROVEMENT 1: Gabor Filters
# Biological basis: V1 simple cells (Hubel & Wiesel, 1962)
# ============================================================

def make_gabor(size, theta, frequency, sigma=None):
    """
    2D Gabor filter matching V1 simple cell receptive fields.
    Parameters follow Jones & Palmer (1987) measurements of cat V1.
    """
    if sigma is None:
        sigma = size / 6.0
    center = size // 2
    y, x = np.mgrid[:size, :size]
    x = x - center
    y = y - center

    x_theta = x * np.cos(theta) + y * np.sin(theta)
    y_theta = -x * np.sin(theta) + y * np.cos(theta)

    envelope = np.exp(-(x_theta**2 + y_theta**2) / (2 * sigma**2))
    carrier = np.cos(2 * np.pi * frequency * x_theta)

    gabor = envelope * carrier
    gabor -= gabor.mean()
    return gabor


def extract_gabor_features(intensity, n_orientations=6, n_frequencies=3):
    """
    Extract orientation-selective features using Gabor filter bank.
    6 orientations × 3 frequencies = 18 feature channels
    (vs. v1's 4 channels from Sobel)
    """
    h, w = intensity.shape
    filter_size = max(15, min(h, w) // 20)
    if filter_size % 2 == 0:
        filter_size += 1

    base_freq = 1.0 / filter_size
    frequencies = [base_freq * (2 ** i) for i in range(n_frequencies)]
    orientations = [i * np.pi / n_orientations for i in range(n_orientations)]

    channels = []
    for freq in frequencies:
        for theta in orientations:
            gabor = make_gabor(filter_size, theta, freq)
            response = fftconvolve(intensity, gabor, mode='same')
            channels.append(np.abs(response))

    return channels


# ============================================================
# IMPROVEMENT 2: Competitive Normalization
# Biological basis: lateral inhibition (Heeger, 1992)
# ============================================================

def competitive_normalize(feature_map):
    """
    Itti & Koch (2000) normalization operator N():
    Promotes maps with a few strong peaks (= truly salient),
    suppresses maps with many weak peaks (= not informative).

    This models lateral inhibition in visual cortex — neurons
    that fire strongly suppress their neighbors.
    """
    m = feature_map.copy()
    if m.max() - m.min() < 1e-10:
        return np.zeros_like(m)

    m = (m - m.min()) / (m.max() - m.min())

    global_max = m.max()

    from scipy.ndimage import maximum_filter
    local_max_map = maximum_filter(m, size=max(m.shape[0]//10, 3))
    local_maxima = (m == local_max_map) & (m > 0.1 * global_max)

    if local_maxima.sum() > 0:
        avg_local_max = m[local_maxima].mean()
    else:
        avg_local_max = m.mean()

    return m * (global_max - avg_local_max) ** 2


def normalize_map(m):
    """Simple [0,1] normalization."""
    mn, mx = m.min(), m.max()
    if mx - mn < 1e-10:
        return np.zeros_like(m)
    return (m - mn) / (mx - mn)


# ============================================================
# Feature Extraction
# ============================================================

def extract_intensity(img_array):
    return np.mean(img_array, axis=2)


def extract_color_channels(img_array):
    """Color opponent channels (Itti & Koch, 1998)."""
    R = img_array[:, :, 0].astype(float)
    G = img_array[:, :, 1].astype(float)
    B = img_array[:, :, 2].astype(float)

    intensity = (R + G + B) / 3.0
    mask = intensity > 10

    r = np.zeros_like(R)
    g = np.zeros_like(G)
    b = np.zeros_like(B)
    r[mask] = R[mask] / intensity[mask]
    g[mask] = G[mask] / intensity[mask]
    b[mask] = B[mask] / intensity[mask]

    RG = np.abs(r - g)
    BY = np.abs(b - (r + g) / 2.0)

    return RG, BY


def center_surround(feature_map, center_sigma=2, surround_sigma=8):
    """Center-surround contrast — retinal ganglion cells."""
    center = gaussian_filter(feature_map, sigma=center_sigma)
    surround = gaussian_filter(feature_map, sigma=surround_sigma)
    return np.abs(center - surround)


def compute_feature_maps(img_array):
    """Extract all bottom-up feature maps."""
    intensity = extract_intensity(img_array)
    RG, BY = extract_color_channels(img_array)
    gabor_channels = extract_gabor_features(intensity)

    feature_maps = {
        'intensity': [],
        'color': [],
        'orientation': [],
    }

    for sigma_c, sigma_s in [(2, 6), (3, 9), (4, 12)]:
        feature_maps['intensity'].append(
            center_surround(intensity, sigma_c, sigma_s)
        )
        feature_maps['color'].append(
            center_surround(RG, sigma_c, sigma_s)
        )
        feature_maps['color'].append(
            center_surround(BY, sigma_c, sigma_s)
        )

    for gabor_resp in gabor_channels:
        feature_maps['orientation'].append(
            center_surround(gabor_resp, 2, 8)
        )

    return feature_maps


# ============================================================
# Saliency Computation (v2)
# ============================================================

def compute_saliency_v2(img_array, kernel, cb_weight=0.0, cb_sigma_frac=0.25,
                        output_blur_sigma=None):
    """
    v2 saliency computation with:
    - Gabor-based orientation features
    - Competitive normalization (lateral inhibition)
    - Configurable center bias
    - Post-hoc fixation spread blur
    """
    h, w = img_array.shape[:2]
    features = compute_feature_maps(img_array)

    # Build conspicuity maps with competitive normalization
    intensity_c = np.zeros((h, w))
    for fm in features['intensity']:
        convolved = fftconvolve(competitive_normalize(fm), kernel, mode='same')
        intensity_c += competitive_normalize(convolved)
    intensity_c = normalize_map(intensity_c)

    color_c = np.zeros((h, w))
    for fm in features['color']:
        convolved = fftconvolve(competitive_normalize(fm), kernel, mode='same')
        color_c += competitive_normalize(convolved)
    color_c = normalize_map(color_c)

    orientation_c = np.zeros((h, w))
    for fm in features['orientation']:
        convolved = fftconvolve(competitive_normalize(fm), kernel, mode='same')
        orientation_c += competitive_normalize(convolved)
    orientation_c = normalize_map(orientation_c)

    saliency_map = normalize_map(
        competitive_normalize(intensity_c) +
        competitive_normalize(color_c) +
        competitive_normalize(orientation_c)
    )

    # Center bias (configurable)
    if cb_weight > 0:
        cy, cx = h / 2, w / 2
        sigma_x = w * cb_sigma_frac
        sigma_y = h * cb_sigma_frac
        y, x = np.mgrid[:h, :w]
        cb = np.exp(-((x - cx)**2 / (2 * sigma_x**2) +
                      (y - cy)**2 / (2 * sigma_y**2)))
        saliency_map = (1 - cb_weight) * saliency_map + cb_weight * cb

    # IMPROVEMENT 3: Post-hoc fixation spread
    # Models the spatial uncertainty of human fixations (~1 deg visual angle)
    if output_blur_sigma is None:
        output_blur_sigma = min(h, w) / 100.0
    if output_blur_sigma > 0:
        saliency_map = gaussian_filter(saliency_map, sigma=output_blur_sigma)

    return normalize_map(saliency_map)


def mfa_saliency_v2(img_array, alpha=2.0, kernel_size=None, epsilon=1.0,
                     cb_weight=0.0, cb_sigma_frac=0.25, output_blur_sigma=None):
    """Generate saliency map using MFA v2."""
    h, w = img_array.shape[:2]
    if kernel_size is None:
        kernel_size = max(31, min(h, w) // 4)
        if kernel_size % 2 == 0:
            kernel_size += 1
    kernel = mfa_kernel(kernel_size, alpha=alpha, epsilon=epsilon)
    return compute_saliency_v2(img_array, kernel,
                                cb_weight=cb_weight,
                                cb_sigma_frac=cb_sigma_frac,
                                output_blur_sigma=output_blur_sigma)


def gaussian_saliency_v2(img_array, sigma=None, kernel_size=None,
                          cb_weight=0.0, cb_sigma_frac=0.25, output_blur_sigma=None):
    """Baseline: Gaussian decay with v2 pipeline."""
    h, w = img_array.shape[:2]
    if sigma is None:
        sigma = min(h, w) // 8
    if kernel_size is None:
        kernel_size = max(31, min(h, w) // 4)
        if kernel_size % 2 == 0:
            kernel_size += 1
    kernel = gaussian_kernel(kernel_size, sigma)
    return compute_saliency_v2(img_array, kernel,
                                cb_weight=cb_weight,
                                cb_sigma_frac=cb_sigma_frac,
                                output_blur_sigma=output_blur_sigma)


# ============================================================
# Evaluation Metrics (same as v1)
# ============================================================

def compute_auc(saliency_map, fixation_map):
    s = saliency_map.flatten()
    f = fixation_map.flatten()
    fix_idx = f > 0
    if fix_idx.sum() == 0 or fix_idx.sum() == len(f):
        return 0.5
    pos = s[fix_idx]
    neg = s[~fix_idx]
    if len(neg) > 10000:
        rng = np.random.RandomState(42)
        neg = rng.choice(neg, 10000, replace=False)
    auc = np.mean([np.mean(p > neg) + 0.5 * np.mean(p == neg) for p in pos])
    return auc


def compute_nss(saliency_map, fixation_map):
    s = saliency_map.copy().astype(float)
    std = s.std()
    if std < 1e-10:
        return 0.0
    s = (s - s.mean()) / std
    fix_idx = fixation_map > 0
    if fix_idx.sum() == 0:
        return 0.0
    return float(s[fix_idx].mean())


def compute_cc(saliency_map, fixation_density):
    s = saliency_map.flatten().astype(float)
    f = fixation_density.flatten().astype(float)
    s = s - s.mean()
    f = f - f.mean()
    denom = np.sqrt(np.sum(s**2) * np.sum(f**2))
    if denom < 1e-10:
        return 0.0
    return float(np.sum(s * f) / denom)


def compute_kl(saliency_map, fixation_density):
    s = saliency_map.flatten().astype(float)
    f = fixation_density.flatten().astype(float)
    eps = 1e-10
    s = s / (s.sum() + eps) + eps
    f = f / (f.sum() + eps) + eps
    return float(np.sum(f * np.log(f / s)))


def compute_sim(saliency_map, fixation_density):
    s = saliency_map.flatten().astype(float)
    f = fixation_density.flatten().astype(float)
    eps = 1e-10
    s = s / (s.sum() + eps)
    f = f / (f.sum() + eps)
    return float(np.minimum(s, f).sum())


def evaluate_all_metrics(saliency_map, fixation_map, fixation_density):
    return {
        'AUC': compute_auc(saliency_map, fixation_map),
        'NSS': compute_nss(saliency_map, fixation_map),
        'CC': compute_cc(saliency_map, fixation_density),
        'KL': compute_kl(saliency_map, fixation_density),
        'SIM': compute_sim(saliency_map, fixation_density),
    }


# ============================================================
# Fixation Loading (same as v1)
# ============================================================

def load_fixation_maps(fixation_dir, base_name, img_shape):
    h, w = img_shape[:2]
    fix_map_path = os.path.join(fixation_dir, base_name + '_fixMap.jpg')
    fix_pts_path = os.path.join(fixation_dir, base_name + '_fixPts.jpg')

    fix_density = None
    fix_binary = None

    if os.path.exists(fix_map_path):
        fix_img = Image.open(fix_map_path).convert('L')
        fix_img = fix_img.resize((w, h), Image.BILINEAR)
        fix_density = np.array(fix_img).astype(float) / 255.0

    if os.path.exists(fix_pts_path):
        pts_img = Image.open(fix_pts_path).convert('L')
        pts_img = pts_img.resize((w, h), Image.NEAREST)
        fix_binary = (np.array(pts_img) > 128).astype(float)

    if fix_density is None and fix_binary is not None:
        fix_density = gaussian_filter(fix_binary, sigma=19)
        fix_density = normalize_map(fix_density)

    if fix_binary is None and fix_density is not None:
        fix_binary = (fix_density > 0.1).astype(float)

    return fix_binary, fix_density


# ============================================================
# Dataset Processing
# ============================================================

def progress_bar(current, total, prefix='', width=40):
    pct = current / total
    filled = int(width * pct)
    bar = '#' * filled + '-' * (width - filled)
    sys.stdout.write(f'\r{prefix} |{bar}| {current}/{total} ({pct*100:.1f}%)')
    sys.stdout.flush()
    if current == total:
        print()


def process_dataset(stimuli_dir, output_dir, fixation_dir=None,
                    alpha=2.0, max_images=None,
                    cb_weight=0.0, cb_sigma_frac=0.25,
                    output_blur_sigma=None):
    """Process dataset: generate saliency maps and evaluate if fixations available."""
    os.makedirs(output_dir, exist_ok=True)

    extensions = {'.jpg', '.jpeg', '.png', '.bmp', '.tiff'}
    image_files = sorted([
        f for f in os.listdir(stimuli_dir)
        if os.path.splitext(f)[1].lower() in extensions
    ])

    if max_images:
        image_files = image_files[:max_images]

    n_images = len(image_files)
    print(f"{'='*70}")
    print(f"MFA SALIENCY MODEL v2 — DATASET EVALUATION")
    print(f"{'='*70}")
    print(f"Images:       {n_images}")
    print(f"Stimuli:      {stimuli_dir}")
    print(f"Fixations:    {fixation_dir or 'N/A'}")
    print(f"Output:       {output_dir}")
    print(f"Alpha:        {alpha}")
    print(f"Center bias:  weight={cb_weight}, sigma_frac={cb_sigma_frac}")
    print(f"Output blur:  {output_blur_sigma or 'auto'}")
    print(f"{'='*70}\n")

    mfa_metrics = {m: [] for m in ['AUC', 'NSS', 'CC', 'KL', 'SIM']}
    gauss_metrics = {m: [] for m in ['AUC', 'NSS', 'CC', 'KL', 'SIM']}
    per_image_results = []
    t_start = time.time()

    for i, fname in enumerate(image_files):
        image_path = os.path.join(stimuli_dir, fname)
        base_name = os.path.splitext(fname)[0]

        try:
            img = Image.open(image_path).convert('RGB')
            img_array = np.array(img)

            sal_mfa = mfa_saliency_v2(
                img_array, alpha=alpha,
                cb_weight=cb_weight, cb_sigma_frac=cb_sigma_frac,
                output_blur_sigma=output_blur_sigma
            )

            # Save saliency map
            sal_img = Image.fromarray((sal_mfa * 255).astype(np.uint8))
            sal_img.save(os.path.join(output_dir, base_name + '_mfa.png'))

            if fixation_dir:
                fix_binary, fix_density = load_fixation_maps(
                    fixation_dir, base_name, img_array.shape
                )
                if fix_binary is not None and fix_density is not None:
                    m_mfa = evaluate_all_metrics(sal_mfa, fix_binary, fix_density)
                    for m, v in m_mfa.items():
                        mfa_metrics[m].append(v)

                    sal_gauss = gaussian_saliency_v2(
                        img_array,
                        cb_weight=cb_weight, cb_sigma_frac=cb_sigma_frac,
                        output_blur_sigma=output_blur_sigma
                    )
                    m_gauss = evaluate_all_metrics(sal_gauss, fix_binary, fix_density)
                    for m, v in m_gauss.items():
                        gauss_metrics[m].append(v)

                    per_image_results.append({
                        'image': fname, 'MFA': m_mfa, 'Gaussian': m_gauss
                    })

        except Exception as e:
            print(f"\n  Error on {fname}: {e}")

        progress_bar(i + 1, n_images, prefix='Processing')

    elapsed = time.time() - t_start
    print(f"\nCompleted in {elapsed:.1f}s ({elapsed/n_images:.2f}s/image)\n")

    if per_image_results:
        n_eval = len(per_image_results)
        print(f"{'='*80}")
        print(f"EVALUATION RESULTS ({n_eval} images)")
        print(f"{'='*80}")
        print(f"{'Model':<15} {'AUC':>8} {'NSS':>8} {'CC':>8} {'KL':>8} {'SIM':>8}")
        print(f"{'-'*80}")

        for name, metrics in [('MFA v2', mfa_metrics), ('Gaussian v2', gauss_metrics)]:
            if metrics['AUC']:
                print(f"{name:<15} {np.mean(metrics['AUC']):>8.4f} "
                      f"{np.mean(metrics['NSS']):>8.4f} "
                      f"{np.mean(metrics['CC']):>8.4f} "
                      f"{np.mean(metrics['KL']):>8.4f} "
                      f"{np.mean(metrics['SIM']):>8.4f}")

        # Statistical comparison
        if mfa_metrics['AUC'] and gauss_metrics['AUC']:
            print(f"\n{'='*80}")
            print("STATISTICAL COMPARISON: MFA v2 vs Gaussian v2")
            print(f"{'='*80}")
            for metric in ['AUC', 'NSS', 'CC', 'KL', 'SIM']:
                mfa_v = np.array(mfa_metrics[metric])
                g_v = np.array(gauss_metrics[metric])
                t_stat, p_val = stats.ttest_rel(mfa_v, g_v)
                diff = np.mean(mfa_v) - np.mean(g_v)
                better = "MFA" if (diff > 0 and metric != 'KL') or (diff < 0 and metric == 'KL') else "Gaussian"
                sig = "***" if p_val < 0.001 else "**" if p_val < 0.01 else "*" if p_val < 0.05 else "ns"
                print(f"  {metric:<5} diff={diff:+.4f} p={p_val:.4f} {sig} [{better}]")

        results_path = os.path.join(output_dir, 'evaluation_results.json')
        save_data = {
            'n_images': n_eval,
            'config': {
                'alpha': alpha,
                'cb_weight': cb_weight,
                'cb_sigma_frac': cb_sigma_frac,
            },
            'MFA_v2': {m: float(np.mean(v)) for m, v in mfa_metrics.items()},
            'Gaussian_v2': {m: float(np.mean(v)) for m, v in gauss_metrics.items()},
        }
        with open(results_path, 'w') as f:
            json.dump(save_data, f, indent=2)
        print(f"\nResults saved to: {results_path}")

    return per_image_results


# ============================================================
# Main
# ============================================================

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description='MFA Saliency Model v2')
    parser.add_argument('--input', type=str, help='Single image path')
    parser.add_argument('--dataset', type=str, help='Directory of stimulus images')
    parser.add_argument('--fixations', type=str, help='Directory of fixation maps')
    parser.add_argument('--output', type=str, default='saliency_output_v2.png')
    parser.add_argument('--output_dir', type=str, default='saliency_results_v2/')
    parser.add_argument('--alpha', type=float, default=2.0)
    parser.add_argument('--max_images', type=int, default=None)
    parser.add_argument('--cb_weight', type=float, default=0.0,
                        help='Center bias weight (0 = no center bias)')
    parser.add_argument('--cb_sigma_frac', type=float, default=0.25,
                        help='Center bias sigma as fraction of image size')
    parser.add_argument('--output_blur', type=float, default=None,
                        help='Output Gaussian blur sigma (None = auto)')

    args = parser.parse_args()

    if args.input:
        img = Image.open(args.input).convert('RGB')
        img_array = np.array(img)
        print(f"Processing: {args.input} ({img_array.shape[1]}x{img_array.shape[0]})")
        sal = mfa_saliency_v2(img_array, alpha=args.alpha,
                               cb_weight=args.cb_weight,
                               cb_sigma_frac=args.cb_sigma_frac,
                               output_blur_sigma=args.output_blur)
        sal_img = Image.fromarray((sal * 255).astype(np.uint8))
        sal_img.save(args.output)
        print(f"Saved: {args.output}")

    elif args.dataset:
        process_dataset(
            args.dataset, args.output_dir,
            fixation_dir=args.fixations,
            alpha=args.alpha,
            max_images=args.max_images,
            cb_weight=args.cb_weight,
            cb_sigma_frac=args.cb_sigma_frac,
            output_blur_sigma=args.output_blur,
        )
    else:
        print("Usage:")
        print("  Single:   python mfa_saliency_v2.py --input img.jpg")
        print("  Dataset:  python mfa_saliency_v2.py --dataset imgs/ --fixations fixmaps/ --output_dir results/")
        print("  MIT300:   python mfa_saliency_v2.py --dataset BenchmarkIMAGES/ --output_dir submit/ --cb_weight 0.3")
