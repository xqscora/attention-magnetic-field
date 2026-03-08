# Attention as a Magnetic Field

**The Magnetic Field Model of Attention (MFA)** — a unified framework in which attentional intensity decays as F = S/r^α, deriving α = 2 from first principles across three independent disciplines.

[![DOI](https://zenodo.org/badge/DOI/10.5281/zenodo.18900059.svg)](https://doi.org/10.5281/zenodo.18900059)

## Overview

Five decades of attention research have produced five major but isolated theories: the spotlight model, gradient model, perceptual load theory, resource theory, and spreading activation. The MFA unifies all five within a single field equation, deriving the exponent α = 2 independently from:

1. **Geometric flux conservation** (physics)
2. **Quadratic wiring-cost optimization** (information theory / neuroscience)
3. **Amari-type neural field equations** (computational biology)

Six field equations formalize attentional gradients, capture, energy conservation, superposition, task-switch inertia (Lenz's Law), and priming (induced magnetization). Five parameters span the ADHD-to-hyperfocus continuum.

## Key Results

- **Model comparison**: Reanalysis of published gradient data favors power-law over Gaussian decay (ΔAIC = 6.6; Handy et al., 1996)
- **Cortical-transform model**: Combining 1/r² with logarithmic retino-cortical mapping yields R² = 0.99 fit to behavioral data
- **Load reanalysis**: Continuous resource allocation outperforms binary step function (ΔAIC > 10; Lavie & Cox, 1997)
- **Eight falsifiable predictions** proposed for prospective testing
- **Cross-disciplinary bridge**: Connects physics, neuroscience, clinical science (ADHD), gifted education (Dabrowski's OE), and AI (Transformer attention)

## Paper

The full manuscript is available as a preprint:

> Zeng, C. (2026). Attention as a Magnetic Field. *Zenodo*. https://doi.org/10.5281/zenodo.18900059

## Repository Structure

```
├── analysis/       # Python scripts for data reanalysis
├── figures/         # Main figures and supplementary simulation screenshots
└── simulation/      # Unity simulation source (Assets + ProjectSettings)
```

## Analysis Scripts

| Script | Description |
|---|---|
| `attention_gradient_data.py` | Spatial gradient data extraction and curve fitting |
| `lavie_load_analysis.py` | Lavie & Cox (1997) load effect reanalysis |
| `aic_bic_analysis.py` | AIC/BIC model comparison across datasets |
| `multi_dataset_analysis.py` | Multi-dataset meta-analytic comparison |

### Requirements

```
numpy
scipy
matplotlib
```

## Simulation

An interactive Unity simulation instantiates all six MFA equations across four scenes:

1. **Comparison** — Power-law vs Gaussian decay side-by-side
2. **Attention Life** — Auto-evolving demo cycling through focus, distraction, task switching, ADHD, hyperfocus, and flow
3. **Multi-Task** — Interactive superposition with energy conservation
4. **Curvature** — Geometric interpretation: attention as curvature of connectivity space

Built with Unity 2022 LTS.

## Citation

If you use this code or framework, please cite:

```bibtex
@article{zeng2026attention,
  title={Attention as a Magnetic Field},
  author={Zeng, Cora},
  year={2026},
  doi={10.5281/zenodo.18900059},
  publisher={Zenodo}
}
```

## License

This work is licensed under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/).

## Contact

Cora Zeng — xqscora@gmail.com | ORCID: [0009-0006-4150-5568](https://orcid.org/0009-0006-4150-5568)
