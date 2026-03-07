# Attention as a Magnetic Field

**The Magnetic Field Model of Attention (MFA)** — a unified framework in which attentional intensity decays as F = S/r^α.

## Overview

Five decades of attention research have produced five major but isolated theories: the spotlight model, gradient model, perceptual load theory, resource theory, and spreading activation. The MFA unifies all five within a single field equation, deriving the exponent α = 2 independently from geometric flux conservation, neural wiring-cost optimization, and Amari-type neural field equations.

Six field equations formalize attentional gradients, capture, energy conservation, superposition, task-switch inertia (Lenz's Law), and priming (induced magnetization). Five parameters span the ADHD-to-hyperfocus continuum.

## Key Results

- **Model comparison**: Reanalysis of published gradient data favors power-law over Gaussian decay (ΔAIC = 6.6)
- **Load reanalysis**: Continuous resource allocation outperforms binary step function (ΔAIC > 10)
- **Eight falsifiable predictions** proposed for prospective testing
- **Cross-disciplinary derivation**: 1/r² emerges from physics, mathematics, and biology independently

## Repository Structure

```
├── manuscript/          # Source manuscript (Markdown)
├── analysis/            # Python scripts for data reanalysis
├── figures/             # Main figures and supplementary simulation screenshots
└── simulation/          # Unity simulation source (Assets + ProjectSettings)
```

## Analysis Scripts

| Script | Description |
|---|---|
| `attention_gradient_data.py` | Spatial gradient data extraction and curve fitting |
| `lavie_load_analysis.py` | Lavie & Cox (1997) load effect reanalysis |
| `aic_bic_analysis.py` | AIC/BIC model comparison across datasets |
| `multi_dataset_analysis.py` | Multi-dataset meta-analytic comparison |

## Simulation

An interactive Unity simulation instantiates all six MFA equations across four scenes:

1. **Comparison** — Power-law vs Gaussian decay side-by-side
2. **Attention Life** — Auto-evolving demo cycling through focus, distraction, task switching, ADHD, hyperfocus, and flow
3. **Multi-Task** — Interactive superposition with energy conservation
4. **Curvature** — Geometric interpretation: attention as curvature of connectivity space

## Citation

> Zeng, C. (2026). Attention as a Magnetic Field. *Preprint*.

## License

This work is licensed under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/).

## Contact

Cora Zeng — xqscora@gmail.com
