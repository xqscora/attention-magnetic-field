# MFA Saliency Model

Computational saliency model based on the **Magnetic Field of Attention (MFA)** theory. Predicts where humans look in images using an inverse-square decay kernel (1/r²), analogous to magnetic field strength.

## How It Works

The model extracts low-level visual features (intensity, color, orientation) and applies the MFA inverse-power kernel instead of the standard Gaussian kernel used by most saliency models.

This is a handcrafted, non-deep-learning fixation-prediction model, not an object
recognition model. It has explicit design assumptions, including feature filters,
spatial scales, normalization, and an optional center prior. Center-prior
parameters were selected using MIT1003 fixation data; "no deep-network training"
does not mean "no data-dependent tuning" or "no inductive bias."

### Key components

| Component | Implementation | Biological basis |
|-----------|---------------|-----------------|
| Orientation features | Gabor filter bank (6 orientations × 3 frequencies) | V1 simple cells (Hubel & Wiesel, 1962) |
| Center-surround contrast | Difference of Gaussians at 3 scales | Retinal ganglion cells |
| Feature competition | Competitive normalization (Itti & Koch, 2000) | Lateral inhibition in visual cortex |
| Attention kernel | **MFA: 1/(r² + ε)** | MFA theory — inverse-square decay |
| Fixation spread | Post-hoc Gaussian blur | Foveal spatial uncertainty (~1° visual angle) |

## Usage

### Single image

```bash
python mfa_saliency.py --input photo.jpg --output saliency_map.png
```

### Dataset evaluation (with ground-truth fixation maps)

```bash
python mfa_saliency.py --dataset ALLSTIMULI/ --fixations ALLFIXATIONMAPS/ --output_dir results/ --cb_weight 0.4 --cb_sigma_frac 0.1667
```

### Generate predictions for MIT300 benchmark

```bash
python mfa_saliency.py --dataset BenchmarkIMAGES/ --output_dir submit/ --cb_weight 0.4 --cb_sigma_frac 0.1667 --output_blur 19
```

### Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `--alpha` | 2.0 | Decay exponent (MFA predicts α=2) |
| `--cb_weight` | 0.0 | Center bias weight (0–1) |
| `--cb_sigma_frac` | 0.25 | Center bias Gaussian sigma as fraction of image size |
| `--output_blur` | auto | Output Gaussian blur sigma (models fixation spread) |
| `--max_images` | all | Limit number of images to process |

## MIT/Tübingen Saliency Benchmark

This model has official entries on the
[MIT/Tübingen Saliency Benchmark](https://saliency.tuebingen.ai/): the original
MFA and MFA v2 on MIT300, and MFA v2 on CAT2000.

See [benchmark results and version notes](BENCHMARK_RESULTS.md) for the original
MFA and MFA v2 MIT300 results, and the separately evaluated CAT2000 submission.
The script in this directory implements the v2 pipeline; it should not be used
to reproduce the original MFA entry without its original implementation.

The MIT300 command above records the historical v2 run settings. The submitted
MIT300 archive (`MFA_v2_clean.zip`, SHA-256
`df6414eb3c3008bad555742773bcfe8326071ac5d0a5b7762f93247c627ad050`) contains
300 maps and matches the saved submission folder byte-for-byte and pixel-for-
pixel. Regenerating all 300 maps from this checkout with NumPy 1.26.4, SciPy
1.13.1, and Pillow 10.4.0 produced matching shapes and at most a one-level
8-bit pixel difference; this is close reproduction, not bit-exact reproduction.

The CAT2000 archive (`CAT2000_MFA_submit.zip`, SHA-256
`3ac0eb54944f49f832af152fa95d6f82cf8fd78cd68db76e8ef4a2def98bfb8f`) contains
2,000 maps and likewise matches its saved submission folder exactly. A rerun
through the documented max-640-pixel generation path reproduced 1,717 maps
pixel-for-pixel; 283 differed, with all output shapes matching. Spot checks of
those differences found examples reproduced exactly by full-resolution
inference, alongside examples reproduced by the 640-pixel path. The submitted
CAT2000 archive therefore appears to contain maps from more than one inference
scale. The exact per-image generation path for all 2,000 maps has not been
reconstructed, so the official CAT2000 score should be attributed to the
submitted archive, not claimed as a clean rerun of one uniform configuration.

Local MIT1003 results and official hidden-test scores are distinct.

### Local evaluation caveats

The local AUC routine samples up to 10,000 nonfixated pixels with seed 42 and
counts ties as half a win. It is not a replacement for the official benchmark
evaluator, and this script does not compute shuffled AUC. Supply both fixation
points (`*_fixPts.jpg`) and fixation-density maps (`*_fixMap.jpg`) for local
evaluation. If only density maps are present, the legacy loader thresholds them
to produce proxy points; AUC and NSS from that fallback are not measurements
at the actual recorded fixation locations.

`evaluation_results.json` records the center-prior settings and output blur.
`output_blur_sigma: null` means the existing automatic rule, `min(height, width)
/ 100`; zero disables the output blur. The 2026-11 maintenance update accelerates
local AUC computation and improves diagnostics without changing saliency-map
generation or retuning against the hidden test scores.

### Regression checks

From the repository root, after installing the dependencies:

```bash
python -m unittest discover -s saliency/tests -v
```

## Dependencies

```bash
pip install -r requirements.txt
```

## License

The source code uses the [PolyForm Noncommercial License 1.0.0](../LICENSE).
It permits use, modification, and redistribution only for purposes allowed by
the license and does not grant permission for commercial-purpose use. The
license does not require downstream modified versions to publish source code
or use the same license, so it is not GPL-style copyleft. See the root README
for the repository-wide scope and the license text for permitted-purpose
details.

## References

- Hubel, D.H. & Wiesel, T.N. (1962). Receptive fields, binocular interaction and functional architecture in the cat's visual cortex. *Journal of Physiology*.
- Itti, L., Koch, C., & Niebur, E. (1998). A model of saliency-based visual attention for rapid scene analysis. *IEEE TPAMI*.
- Itti, L. & Koch, C. (2000). A saliency-based search mechanism for overt and covert shifts of visual attention. *Vision Research*.
- Heeger, D.J. (1992). Normalization of cell responses in cat striate cortex. *Visual Neuroscience*.
