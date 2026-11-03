# Official benchmark results

These aggregate results were returned by the MIT/Tubingen benchmark
administrator. MIT300 and CAT2000 are different test datasets and should not be
compared as successive measurements of the same test set.

| Dataset | Submitted entry | AUC up | shuffled AUC up | NSS up | CC up | KL down | SIM up |
|---|---|---:|---:|---:|---:|---:|---:|
| MIT300 | MFA (original) | 0.79257 | 0.61391 | 1.13355 | 0.44071 | 1.04403 | 0.43762 |
| MIT300 | MFA v2 | 0.80646 | 0.57871 | 1.25499 | 0.49557 | 0.94256 | 0.46895 |
| CAT2000 | MFA | 0.84555 | 0.54580 | 1.54283 | 0.63400 | 0.71585 | 0.53248 |

## Provenance and naming

- The original MIT300 scores were returned on March 19, 2026 and are listed on
  the [official MIT300 leaderboard](https://saliency.tuebingen.ai/results.html).
- The MIT300 v2 scores were returned on November 3, 2026, for the cleaned
  prediction ZIP supplied on May 12. Keeping both MIT300 entries, named `MFA`
  and `MFA v2`, was requested on November 3; publication of v2 is not yet
  confirmed in this record.
- The CAT2000 scores were returned separately on November 3, 2026 for the
  archive shared on May 12. This submission was named `MFA` and described
  Gabor features, competitive normalization, and center bias. Its exact
  configuration has not been independently reproduced here; do not equate it
  with the original MIT300 implementation simply because the entry names match.
  CAT2000 leaderboard publication is not yet confirmed.

## Interpretation

MFA v2 improves five reported MIT300 metrics, while the original has higher
shuffled AUC. Aggregate scores alone do not establish statistical significance,
the contribution of the inverse-square kernel, or the cause of the sAUC change.
No single overall rank is inferred by averaging these different metrics.

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
