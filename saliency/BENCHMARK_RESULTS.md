# Official benchmark results

These aggregate results were returned by the MIT/Tubingen benchmark
administrator. MIT300 and CAT2000 are different test datasets and should not be
compared as successive measurements of the same test set.

| Dataset | Submitted entry | AUC up | shuffled AUC up | NSS up | CC up | KL down | SIM up |
|---|---|---:|---:|---:|---:|---:|---:|
| MIT300 | MFA (original) | 0.79257 | 0.61391 | 1.13355 | 0.44071 | 1.04403 | 0.43762 |
| MIT300 | MFA v2 | 0.80646 | 0.57871 | 1.25499 | 0.49557 | 0.94256 | 0.46895 |
| CAT2000 | MFA v2 | 0.84555 | 0.54580 | 1.54283 | 0.63400 | 0.71585 | 0.53248 |

## Provenance and naming

- The original MIT300 scores were returned on March 19, 2026 and are listed on
  the [official MIT300 leaderboard](https://saliency.tuebingen.ai/results.html).
- The MIT300 v2 scores were returned on November 3, 2026, for the cleaned
  prediction ZIP supplied on May 12. Both entries are now published on the
  [official MIT300 leaderboard](https://saliency.tuebingen.ai/results.html),
  named `MFA` and `MFA v2`.
- The CAT2000 scores were returned separately on November 3, 2026 for the
  archive supplied on May 12. The entry is published as `MFA v2` on the
  [official CAT2000 leaderboard](https://saliency.tuebingen.ai/results_CAT2000.html).
  The submitted archive contains 2,000 maps and has SHA-256
  `3ac0eb54944f49f832af152fa95d6f82cf8fd78cd68db76e8ef4a2def98bfb8f`.
- The submitted CAT2000 archive matches its saved local submission folder
  exactly. A reproduction using the current code and a uniform max-640-pixel
  path matched 1,717/2,000 maps pixel-for-pixel; spot checks show that some
  remaining maps match full-resolution inference instead. This indicates mixed
  inference scales in the archived map set; the per-image history of all maps
  is not known. The published score is valid for the submitted maps, while the
  exact historical generation path is not fully reproduced.
- The submitted MIT300 v2 archive (`MFA_v2_clean.zip`) has SHA-256
  `df6414eb3c3008bad555742773bcfe8326071ac5d0a5b7762f93247c627ad050` and
  contains 300 maps. The saved submission folder matches it exactly. A rerun
  from the current checkout matched all shapes and differed by at most one
  8-bit pixel level, so reproduction is close but not bit-exact.

## Interpretation

MFA v2 improves AUC, NSS, CC, KL divergence, and similarity on MIT300, while
the original has higher shuffled AUC. The CAT2000 entry is also MFA v2. These
aggregate scores alone do not establish statistical significance, the
contribution of the inverse-square kernel, or the cause of the MIT300 sAUC
change. No single overall rank is inferred by averaging these different
metrics.

These are classic saliency-map submissions, not calibrated probabilistic
models with metric-specific readouts. The reported scores evaluate complete
pipelines, including their feature extraction and center priors. They do not
by themselves validate an inverse-square law of human attention.

Local experiments on MIT1003, including the historical 20-image run, are
development evaluations and are not official MIT300 or CAT2000 results.

## Evaluation reference

Kummerer, M., Wallis, T. S. A., and Bethge, M. (2018).
[Saliency Benchmarking Made Easy: Separating Models, Maps and Metrics](https://openaccess.thecvf.com/content_ECCV_2018/html/Matthias_Kummerer_Saliency_Benchmarking_Made_ECCV_2018_paper.html).
ECCV 2018.
