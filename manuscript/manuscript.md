# Attention as a Magnetic Field

**Cora Zeng**

*Independent Researcher, Chengdu, China*

*Corresponding author: Cora Zeng (xqscora@gmail.com)*

---

## Abstract

Five decades of attention research have produced five major but isolated theories. I propose the Magnetic Field Model of Attention (MFA), a unified framework in which attentional intensity decays as F = S/r^α. The exponent α = 2 is derived independently from geometric flux conservation, neural wiring-cost optimization, and Amari-type neural field equations. Six field equations formalize attentional gradients, capture, energy conservation, superposition, task-switch inertia, and priming. Five parameters span the ADHD-to-hyperfocus continuum. Reanalysis of published gradient data using AIC model selection favors power-law over Gaussian decay (ΔAIC = 6.6); reanalysis of load data confirms continuous over binary resource allocation (ΔAIC > 10 against step function). Eight falsifiable predictions are proposed.

---

## Main Text

The pattern is visible in any classroom: an individual diagnosed with ADHD drifts through a math lesson yet spends three unbroken hours building in Minecraft. Five theoretical traditions address different facets of this puzzle: the spotlight model (1), the gradient model (2), perceptual load theory (3), resource theory (4), and spreading activation (5). Yet none explains both hyperfocus and distractibility in the same individual, and no mathematical framework connects them.

This fragmentation has tangible consequences. Without a unifying framework, clinicians cannot predict when a child diagnosed with ADHD will hyperfocus or drift; educators lack quantitative guidance for designing attention-sustaining instruction; and researchers across neuroscience, physics, and information theory cannot connect their findings to attentional phenomena. The observation that attention radiates from a focal point, intense nearby and diminishing with distance across multiple dimensions simultaneously, invites a field-theoretic approach. In physics, any conserved quantity radiating from a point source through three-dimensional space must obey an inverse-square law; in neuroscience, neural connectivity costs rise quadratically with wiring distance (31, 32). The convergence of these independent constraints on the same functional form suggests that 1/r² decay may be a structural inevitability rather than an empirical coincidence.

Prior field-based approaches have been limited. Neural field theory (6) models cortical dynamics but does not address behavioral phenomena like mind-wandering. Zanca et al.'s (7) gravitational model predicts visual scanpaths, but gravity has no magnetization (so it cannot represent priming), no electromagnetic induction (so it cannot formalize task-switch costs), and no permanent/electromagnet distinction (so it cannot separate automatic from controlled processing). Magnetic field dynamics address all three limitations.

### Core Model

In the MFA, a task acts as a magnet; cognitive resources correspond to iron filings; filing density at a given distance reflects processing intensity. The magnetic analogy is chosen over gravitational or electric alternatives for principled reasons: only magnetic fields simultaneously support (i) inverse-square decay, (ii) permanent versus temporary magnetization, mapping onto automatic versus controlled processing (9), (iii) induction effects, formalizing task-switch costs as Lenz's Law (8), and (iv) superposition with cancellation, predicting attentional dead zones between competing foci. These four properties map onto four phenomena that existing theories address only in isolation. Permanent magnets represent automatized skills requiring minimal energy; electromagnets represent controlled attention. Residual magnetism corresponds to priming (Table S1).

Six equations define the model:

**(1) Attentional gradient.** F(r) = S/r^α. The exponent α = 2 is derived from three independent first principles (full derivation in Supplementary Note S-α). The most intuitive: if a finite attentional flux Φ spreads isotropically over a sphere of radius r, conservation requires F(r)·4πr² = Φ, yielding F = Φ/(4πr²) = S/r². This geometric argument is reinforced by information-theoretic optimality under quadratic neural wiring costs (31, 32) and by the steady-state near-field limit of Amari-type neural field equations (6, 33). The qualitative predictions hold for any α > 0; the value α = 2 is an empirically testable claim about the effective dimensionality of psychological space.

**(2) Capture criterion.** A distractor captures attention when its salience F_p exceeds the field at distance r: F_p > S/r², yielding a critical radius r_crit = √(S/F_p).

**(3) Energy conservation.** Total attentional energy E_total is constant at a given arousal level. Strengthening one focus weakens others.

**(4) Superposition.** Multiple foci produce F_total = Σ S_i/r_i². Two equal foci create a detection minimum at the midpoint.

**(5) Lenz's Law.** F_inertia = −k · I_engaged · (dS/dt). The system resists change in proportion to engagement depth and switch speed (8).

**(6) Induced magnetization.** Activated resources acquire their own field: F_threshold = F_0 · (1 − M(r)). Temporary magnetization corresponds to priming; permanent magnetization to automatization (9).

Psychological distance is multidimensional: R² = Σ w_i · r_i² over spatial, sensory, semantic, personal-relevance, and emotional dimensions (see Individual Differences).

### Five Theories as Special Cases

Each classical tradition emerges as a limiting case of the MFA equations (Fig. 3):

*(i) Spotlight.* When S is large, F(r) ≫ F_thresh for all r < r_crit, creating a zone of approximately uniform processing: the attentional spotlight (1, 11). The spotlight's apparent sharp boundary arises from the steep 1/r² drop-off: at twice the critical radius, field strength falls to one-quarter, producing an effectively binary attended/unattended distinction.

*(ii) Gradient.* LaBerge (2) proposed that processing efficiency decreases monotonically from the focus without specifying the decay function. MFA identifies it as 1/r² and derives it from first principles, filling the mathematical gap that the gradient tradition left open.

*(iii) Perceptual load.* Lavie's (3) central prediction, that high load eliminates distractor processing, follows from Equation 2: when S is large, r_crit = √(S/F_p) shrinks, excluding peripheral distractors. MFA predicts continuous shrinkage rather than binary switching, consistent with recent evidence (27).

*(iv) Resource theory.* Kahneman's (4) limited-capacity model posits a fixed pool divided among tasks but leaves the distribution law unspecified. MFA formalizes this as Equation 3 (energy conservation) combined with Equation 4 (superposition): E_total = Σ S_i, with each focus drawing from the same finite supply and distributing resources according to 1/r².

*(v) Spreading activation.* Collins and Loftus's (5) spreading activation is Equation 6 operating across semantic distance: activation propagating through a network is equivalent to field-induced magnetization, with decay governed by the same inverse-square law.

Unlike the gravitational model (7), which requires externally imposed inhibition of return, MFA derives IOR-like behavior through energy depletion and magnetization saturation (Table S1).

### Converging Evidence

**Table 1. Independent findings consistent with MFA predictions across six domains**

| Domain | Study | Key Finding | MFA Interpretation |
|---|---|---|---|
| Gradient | Downing (1988) | Sensitivity decreases monotonically from cued location | 1/r² field decay |
| Gradient | Mangun & Hillyard (1988) | d' and ERP amplitudes decline with eccentricity | Neural and behavioral gradients co-vary |
| Load | Lavie (1995) | High load eliminates distractor processing | Strong S captures all filings |
| Load | Benoni & Tsal (2012) | Load-distractor relationship is "more complex than initially proposed" | Multi-parameter interaction (S, r, F_thresh) |
| ADHD | Kofler et al. (2013), N = 319 | RT variability (not mean RT) is ADHD's most reliable marker | High σ produces oscillating S(t) |
| ADHD | Epstein et al. (2011) | Methylphenidate reduces RT variability without changing mean RT | MPH reduces σ, not S_0 |
| Cocktail party | Conway et al. (2001) | 29% detect own name in unattended channel | r_personal ~ 0 yields small R |
| Cocktail party | Harris & Pashler (2004) | Emotional words do not capture like one's own name | Personal relevance and emotional valence are separate dimensions |
| Flow | Weber et al. (2023) | Pupil dilation tracks flow linearly | Stable high S drives tonic LC-NE |
| Flow | Katahira et al. (2018) | Flow: increased frontal θ, moderate α | High S (θ) + adequate E_total (α) |
| Surround | Muller et al. (2005); Hopf et al. (2006) | Center-surround attentional profile | Competing field lines produce suppressive ring |

These findings predate the MFA. Post-hoc consistency does not constitute confirmation. However, the breadth of convergence across six domains, spanning behavioral, electrophysiological, and neuroimaging methods, provides a foundation for the prospective tests proposed below.

Two findings deserve emphasis. First, the MFA independently predicts that *variability*, not mean level, should be the core ADHD parameter. Kofler et al.'s (14) meta-analysis of 319 studies confirmed exactly this, and Epstein et al. (16) showed that methylphenidate reduces RT variability while barely changing mean RT, consistent with the drug stabilizing the field (σ decreases) rather than strengthening it. Second, the cocktail-party dissociation (10, 17) supports multidimensional distance: personal relevance and emotional valence occupy separate dimensions of R², producing different capture patterns that single-dimension salience models cannot explain.

The cross-domain pattern merits emphasis: MFA does not merely fit individual findings post hoc but predicts qualitative relationships among them. The model links Kofler et al.'s (14) RT variability finding to Epstein et al.'s (16) pharmacological data through a single mechanism (σ modulation), and connects Conway et al.'s (10) cocktail-party capture to Harris and Pashler's (17) dissociation through multidimensional distance. These connections would be unexpected under separate theories but emerge naturally from a unified field framework.

No prior study has explicitly fitted power-law against Gaussian and exponential decay functions to attentional gradient data. The reanalysis below provides a first such test.

### Model Comparison: Reanalysis of Published Data

To test whether attentional gradients follow power-law, Gaussian, or exponential decay, I reanalyzed published data from three spatial gradient datasets and one load dataset. Data were extracted from published tables; Lavie and Cox (41) values were estimated from published figures, following standard meta-analytic procedure. All models have equal complexity (three free parameters for gradient data, two for load data). Model selection uses AIC (42) alongside R².

**Spatial gradients.** Handy, Kingstone, and Mangun (34) Experiment 3 (suprathreshold detection, N = 8, 6 eccentricities): power law provides the best fit (R² = 0.99, AIC = 28.3) over exponential (R² = 0.98, AIC = 30.7) and Gaussian (R² = 0.96, AIC = 34.9), with ΔAIC = 6.6 between power law and Gaussian. The power-law form captures continued RT increase at large separations where Gaussian models predict premature asymptote. For Handy et al. Experiment 1 (threshold detection, N = 16), all three models yield comparable fits (R² = 0.77-0.79, ΔAIC < 2), reflecting non-monotonic RT at large eccentricities. Shepherd and Muller (40) (N = 9, 4 separations) show all R² > 0.90 but insufficient points to discriminate.

**Load effect.** Lavie and Cox (41) Experiment 2 (N = 12, set sizes 1, 2, 4, 6): MFA predicts distractor interference decreases continuously with load rather than switching off at a threshold. All continuous models outperform the binary step function (ΔAIC > 10, indicating essentially no support for binary switching per Burnham and Anderson (42)). The power-law exponent is approximately 1.0, consistent with interference inversely proportional to engaged field strength.

**Exponent discrepancy.** Across spatial datasets, the fitted exponent (α = 0.14-0.22) falls well below the theoretical α = 2. This does not indicate near-linear decay. It arises because the experiments measure distance in visual eccentricity (degrees), not in cortical distance (millimeters), and the mapping between them is highly nonlinear. The cortical magnification factor compresses foveal space: one degree near fixation spans far more cortical tissue than one degree in the periphery, following M = M_0/(1 + e/e_2) with e_2 ~ 3 degrees (39). Such divergence is computationally expected: transforming a strict 1/r² cortical field through the nonlinear retino-cortical magnification mapping predictably generates flatter visual-eccentricity gradients. The theoretical α = 2 is a cortical-space prediction; testing it requires measuring gradients in cortical coordinates via fMRI retinotopic mapping (Prediction 1). Threshold detection produces steeper gradients than suprathreshold detection, consistent with MFA: higher task demands (larger effective S) produce steeper fields.

### Individual Differences: The Overexcitability Bridge

Five parameters capture individual variation (Table S2): field strength S, total energy E_total, stability σ (ADHD = high σ; hyperfocus = low σ), automatization N_perm, and peripheral threshold F_thresh.

No existing model formally maps Dabrowski's Overexcitability (OE) profiles (23) onto quantitative attentional parameters. In the MFA, each OE type maps onto a specific distance-dimension weight: Sensual OE elevates w_sensory, Emotional OE elevates w_emotional, and Intellectual OE elevates w_semantic (18). This resolves what I term the Gifted Attention Paradox (19): a gifted child with high Intellectual OE has both high S for intellectually engaging tasks (producing hyperfocus) and high w_semantic (producing capture by interesting distractors). The apparent contradiction dissolves into two independent parameters.

### Falsifiable Predictions

Eight predictions adjudicate MFA against alternatives:

1. **Inverse-square decay.** AIC/BIC will favor 1/r² over Gaussian in a Posner paradigm with 8+ eccentricities, measured in cortical coordinates.
2. **Continuous load saturation.** Distractor interference follows a continuous curve across 8 load levels, not a binary switch.
3. **OE-distractor interaction.** Sensual OE predicts greater capture by sensory distractors; Intellectual OE by semantic distractors.
4. **Multiplicative switch costs.** Task-switch costs reflect engagement depth times switch speed.
5. **Cascading priming.** Three-stage priming shows distance-dependent facilitation with distinct soft-iron (rapid decay) and hard-steel (persistent) components.
6. **Dual-task null point.** A detection minimum appears at the midpoint between two foci, shifting predictably with strength ratio.
7. **ADHD periodicity.** Spectral analysis of CPT data reveals periodic RT troughs from σ oscillation.
8. **Energy conservation.** Central-task incentives reduce peripheral detection in inverse proportion, sum approximately constant.

Detailed designs and power analyses appear in Supplementary Note 2.

### Neural Substrates

The MFA operates at Marr's computational level (43). Its parameters map onto neural substrates: S corresponds to phasic LC-NE activity and dorsal attention network engagement (20, 30); σ reflects LC tonic-to-phasic balance; E_total maps onto metabolic resources; N_perm onto basal ganglia habit circuits; F_thresh onto the ventral attention and salience networks (21).

The locus coeruleus-norepinephrine (LC-NE) system provides a particularly direct substrate for S and σ. Aston-Jones and Cohen (20) demonstrated that the LC operates in two modes: a tonic mode with low baseline firing and high phasic responses (high S, low σ, corresponding to focused attention) and a phasic mode with elevated baseline and reduced phasic responses (low S, high σ, corresponding to distractible scanning). Methylphenidate shifts the LC toward tonic mode, directly explaining why stimulants reduce RT variability (decreasing σ) without substantially altering mean performance (S_0). The metabolic constraint underlying E_total is grounded in the brain's glucose budget: the brain consumes approximately 20% of total metabolic energy despite comprising 2% of body mass, and attentional engagement measurably increases regional glucose uptake (30), imposing a hard physical ceiling on total deployable field strength.

Endogenous electromagnetic fields add a literal dimension to the analogy: weak electric fields entrain neural oscillations (22), and ephaptic coupling modulates spike timing (24), suggesting neural populations serve as both targets ("iron") and sources ("magnetized iron") of the attentional field.

### Geometric Interpretation

At a deeper level, the MFA field equations may reflect the curvature of neural connectivity space, analogous to how Newtonian force laws can emerge as the weak-field limit of geometric theories. When a task engages neural resources, it modifies local connectivity (via synaptic gain, oscillatory coherence, or ephaptic coupling), effectively curving the connectivity manifold. Resources follow geodesics through this curved space, concentrating near the focus because the geometry channels them. In the weak-curvature limit, geodesic flow reduces to F = S/r² (37). The decay exponent α = d_eff - 1 depends on the effective dimensionality of the connectivity space; empirical measurements of brain network dimensionality yield d_eff in the range 2.5-3.5 (38), consistent with α near 2. This geometric perspective is a proposed theoretical extension that awaits formal mathematical proof (see Supplementary Text for full development).

### Discussion

The MFA reconfigures ADHD as high σ combined with low F_thresh, not as a deficit (15). This reframing carries ethical weight: if ADHD reflects a parameter setting rather than a broken mechanism, one that may confer advantages in environments requiring broad environmental monitoring, then purely deficit-based models risk misrepresenting the underlying neurobiology. The dimensional perspective aligns with NIMH's Research Domain Criteria (RDoC) initiative, which advocates studying psychopathology along continuous dimensions rather than discrete diagnostic categories. ADHD and hyperfocus are two states of one system: when σ is large, S(t) oscillates widely, producing hyperfocus at wave peaks and distraction at troughs. Stimulants reduce σ and increase E_total. Flow states correspond to high S, low σ, sufficient E_total, and minimal peripheral competition, matching pupil-dilation (25) and EEG data (26). For education, the MFA prescribes increasing S through relevance and challenge, reducing F_peripheral through environmental control, and building N_perm through teaching to automaticity.

**Limitations.** First, cognitive resources may have modality-specific components; MFA currently treats them as a single pool. Second, volitional control remains unspecified. Third, power-law decay does not win all datasets: Gaussian fits marginally better for threshold RT (ΔAIC = 0.5), and exponential outperforms power law for load data (ΔAIC = 6.2). MFA's strongest empirical support comes from the suprathreshold gradient (ΔAIC = 6.6 favoring power law). A definitive test across 8+ eccentricities in cortical coordinates is needed (Prediction 1). Fourth, the fitted spatial exponent (0.22) departs from the theoretical 2; this likely reflects the nonlinear cortical magnification transform but requires direct verification via fMRI retinotopic mapping. Fifth, all current evidence is post-hoc; the eight prospective tests are necessary for genuine validation. Sixth, attentional deployment may be directionally asymmetric (1), while MFA assumes radial symmetry.

The MFA unifies five attention theories within one field equation, formalizes task-switch costs and priming as emergent properties, and spans the ADHD-hyperfocus spectrum with five parameters. Multidimensional distance bridges attention science and Overexcitability theory. Reanalysis of published gradient and load data provides preliminary quantitative support, and eight prospective tests can adjudicate MFA against alternatives. A dedicated experiment measuring attentional gradients in cortical coordinates would provide the definitive test of the inverse-square prediction.

---

## References and Notes

1. M. I. Posner, Orienting of attention. *Q. J. Exp. Psychol.* **32**, 3-25 (1980).
2. D. LaBerge, Spatial extent of attention. *J. Exp. Psychol. Hum. Percept. Perform.* **9**, 371-379 (1983).
3. N. Lavie, Perceptual load and selective attention. *J. Exp. Psychol. Hum. Percept. Perform.* **21**, 451-468 (1995).
4. D. Kahneman, *Attention and Effort* (Prentice-Hall, 1973).
5. A. M. Collins, E. F. Loftus, Spreading-activation theory. *Psychol. Rev.* **82**, 407-428 (1975).
6. P. A. Robinson, T. Babaie-Janvier, Neural field theory of corticothalamic attention. *Front. Neurosci.* **13**, 1240 (2019).
7. D. Zanca, M. Gori, S. Melacci, A. Rufa, Gravitational models explain shifts on human visual attention. *Sci. Rep.* **10**, 16335 (2020).
8. S. Monsell, Task switching. *Trends Cogn. Sci.* **7**, 134-140 (2003).
9. W. Schneider, R. M. Shiffrin, Controlled and automatic processing. *Psychol. Rev.* **84**, 1-66 (1977).
10. A. R. A. Conway, N. Cowan, M. F. Bunting, Cocktail party phenomenon revisited. *Psychon. Bull. Rev.* **8**, 331-335 (2001).
11. C. W. Eriksen, J. D. St. James, Visual attention within and around the field of focal attention. *Percept. Psychophys.* **40**, 225-240 (1986).
12. C. J. Downing, Expectancy and visual-spatial attention. *J. Exp. Psychol. Hum. Percept. Perform.* **14**, 188-202 (1988).
13. G. R. Mangun, S. A. Hillyard, Spatial gradients of visual attention. *Electroencephalogr. Clin. Neurophysiol.* **70**, 417-428 (1988).
14. M. J. Kofler *et al.*, Reaction time variability in ADHD: A meta-analytic review of 319 studies. *Clin. Psychol. Rev.* **33**, 795-811 (2013).
15. F. X. Castellanos, R. Tannock, Neuroscience of ADHD. *Nat. Rev. Neurosci.* **3**, 617-628 (2002).
16. J. N. Epstein *et al.*, Evidence for higher reaction time variability for children with ADHD. *Neuropsychology* **25**, 427-441 (2011).
17. C. R. Harris, H. Pashler, Attention and the processing of emotional words and names. *Psychol. Sci.* **15**, 171-178 (2004).
18. A. N. Rinn, M. J. Reynolds, Overexcitabilities and ADHD in the gifted. *Roeper Rev.* **34**, 38-45 (2012).
19. J. T. Webb *et al.*, *Misdiagnosis and Dual Diagnoses of Gifted Children and Adults* (Great Potential Press, 2005).
20. G. Aston-Jones, J. D. Cohen, An integrative theory of locus coeruleus-norepinephrine function. *Annu. Rev. Neurosci.* **28**, 403-450 (2005).
21. V. Menon, L. Q. Uddin, Saliency, switching, attention and control. *Brain Struct. Funct.* **214**, 655-667 (2010).
22. F. Frohlich, D. A. McCormick, Endogenous electric fields guide neocortical network activity. *Neuron* **67**, 129-143 (2010).
23. K. Dabrowski, *Positive Disintegration* (Little, Brown, 1964).
24. C. A. Anastassiou, R. Perin, H. Markram, C. Koch, Ephaptic coupling of cortical neurons. *Nat. Neurosci.* **14**, 217-223 (2011).
25. R. Weber *et al.*, Pupil dilation and flow. *Sci. Rep.* **13**, 2693 (2023).
26. K. Katahira *et al.*, EEG correlates of flow state. *Front. Psychol.* **9**, 300 (2018).
27. H. Benoni, Y. Tsal, Controlling for dilution while manipulating load. *Psychon. Bull. Rev.* **19**, 631-638 (2012).
28. N. G. Muller *et al.*, The attentional field has a Mexican hat distribution. *Vis. Res.* **45**, 1129-1137 (2005).
29. J.-M. Hopf *et al.*, Spatial suppression surrounding the focus of attention. *PNAS* **103**, 1053-1058 (2006).
30. M. Corbetta, G. L. Shulman, Control of goal-directed and stimulus-driven attention. *Nat. Rev. Neurosci.* **3**, 201-215 (2002).
31. D. B. Chklovskii, T. Schikorski, C. F. Stevens, Wiring optimization in cortical circuits. *Neuron* **34**, 341-347 (2002).
32. E. Bullmore, O. Sporns, The economy of brain network organization. *Nat. Rev. Neurosci.* **13**, 336-349 (2012).
33. S. Amari, Dynamics of pattern formation in lateral-inhibition type neural fields. *Biol. Cybern.* **27**, 77-87 (1977).
34. T. C. Handy, A. Kingstone, G. R. Mangun, Spatial distribution of visual attention. *Percept. Psychophys.* **58**, 613-627 (1996).
35. R. N. Shepard, Toward a universal law of generalization. *Science* **237**, 1317-1323 (1987).
36. C. R. Sims, Efficient coding explains the universal law of generalization. *Science* **360**, 652-656 (2018).
37. C. W. Misner, K. S. Thorne, J. A. Wheeler, *Gravitation* (W. H. Freeman, 1973).
38. L. K. Gallos, H. A. Makse, M. Sigman, A small world of weak ties provides optimal global integration of self-similar modules in functional brain networks. *PNAS* **109**, 2825-2830 (2012).
39. S. A. Engel, G. H. Glover, B. A. Wandell, Retinotopic organization in human visual cortex and the spatial precision of functional MRI. *Cereb. Cortex* **7**, 181-192 (1997).
40. M. Shepherd, J. M. Muller, Movement versus focusing of visual attention. *Percept. Psychophys.* **46**, 146-154 (1989).
41. N. Lavie, S. Cox, On the efficiency of visual selective attention: Efficient visual search leads to inefficient distractor rejection. *Psychol. Sci.* **8**, 395-398 (1997).
42. K. P. Burnham, D. R. Anderson, *Model Selection and Multimodel Inference* (Springer, 2nd ed., 2002).
43. D. Marr, *Vision: A Computational Investigation into the Human Representation and Processing of Visual Information* (W. H. Freeman, 1982).

---

## Acknowledgments

The author thanks A. Graesser for encouragement and discussion. This research received no external funding. An interactive simulation (Unity engine, C#) instantiating all six equations is available as Supplementary Software (Figs. S1-S4); it verifies computational feasibility and parameter interactions but does not constitute empirical evidence. The curvature visualization adapts geodesic rendering from T. Hutton's open-source project (github.com/timhutton/GravityIsNotAForce). AI-based language tools were used solely for grammar checking and language polishing; all scientific content is the author's own work.

## Conflict of Interest

The author declares no conflicts of interest.

---

## Figure Legends

**Fig. 1.** The MFA mapping. Left: a physical magnet among iron filings; density decays with distance. Right: the psychological analogue, where a task generates a field capturing cognitive resources with intensity F = S/r².

**Fig. 2.** Model comparison. **(A)** Theoretical decay functions normalized to equal near-field intensity; shaded region marks the heavy tail predicted by power law but not Gaussian. **(B)** Reanalysis of Handy et al. (34) Experiment 3 RT data (N = 8, 6 eccentricities): power law R² = 0.99, AIC = 28.3; Gaussian R² = 0.96, AIC = 34.9 (ΔAIC = 6.6). Handy Experiment 1 (N = 16): models indistinguishable (ΔAIC < 2). **(C)** Load effect: Lavie and Cox (41) compatibility effect vs. set size. Continuous models outperform step function (ΔAIC > 10).

**Fig. 3.** Five theories as MFA special cases. Each classical model emerges when specific parameters or conditions dominate.

**Fig. 4.** Cross-disciplinary convergence. The 1/r² law is derived independently from physics (flux conservation), mathematics (wiring-cost optimality), biology (neural field theory), and psychology (five unified theories, 11 converging findings).

**Fig. 5.** The σ continuum from ADHD (high σ, unstable S(t)) through typical attention to hyperfocus (low σ). Shaded regions mark lapses below F_thresh.

**Fig. 6.** Superposition and null point. Two equal foci create a detection minimum at the midpoint; unequal foci shift the minimum toward the weaker focus.

**Fig. S1-S4.** Supplementary simulation figures (see Supplementary Software).
