# MFA Saliency Model

Computational saliency model based on the **Magnetic Field of Attention (MFA)** theory. Predicts where humans look in images using an inverse-square decay kernel (1/r²), analogous to magnetic field strength.

## How It Works

The model extracts low-level visual features (intensity, color, orientation) and applies the MFA inverse-power kernel instead of the standard Gaussian kernel used by most saliency models.

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

This model has been evaluated on the [MIT/Tübingen Saliency Benchmark](https://saliency.tuebingen.ai/) (MIT300 dataset).

## Dependencies

```bash
pip install -r requirements.txt
```

## References

- Hubel, D.H. & Wiesel, T.N. (1962). Receptive fields, binocular interaction and functional architecture in the cat's visual cortex. *Journal of Physiology*.
- Itti, L., Koch, C., & Niebur, E. (1998). A model of saliency-based visual attention for rapid scene analysis. *IEEE TPAMI*.
- Itti, L. & Koch, C. (2000). A saliency-based search mechanism for overt and covert shifts of visual attention. *Vision Research*.
- Heeger, D.J. (1992). Normalization of cell responses in cat striate cortex. *Visual Neuroscience*.
