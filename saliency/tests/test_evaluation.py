"""Regression checks for local evaluation, independent of benchmark datasets."""

import contextlib
import io
import json
from pathlib import Path
import sys
import tempfile
import unittest

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import mfa_saliency as model


def pairwise_auc(saliency, fixations):
    """The original evaluator, retained as a numerical reference."""
    values = saliency.flatten()
    mask = fixations.flatten() > 0
    if mask.sum() == 0 or mask.sum() == len(mask):
        return 0.5
    positive, negative = values[mask], values[~mask]
    if len(negative) > 10000:
        negative = np.random.RandomState(42).choice(negative, 10000, replace=False)
    return np.mean([
        np.mean(value > negative) + 0.5 * np.mean(value == negative)
        for value in positive
    ])


class AUCRegressionTests(unittest.TestCase):
    def test_random_ties_and_sampled_negatives_match_pairwise_reference(self):
        rng = np.random.RandomState(7)
        for shape in [(13, 17), (101, 105)]:
            for tied in [False, True]:
                with self.subTest(shape=shape, tied=tied):
                    values = rng.randint(0, 8, size=shape).astype(float) if tied else rng.rand(*shape)
                    fixations = np.zeros(shape)
                    fixations.flat[rng.choice(values.size, 100, replace=False)] = 1
                    self.assertAlmostEqual(model.compute_auc(values, fixations),
                                           pairwise_auc(values, fixations), places=14)

    def test_known_orderings_and_ties(self):
        fixations = np.array([[0, 0, 1, 1]])
        self.assertEqual(model.compute_auc(np.array([[0., 1., 2., 3.]]), fixations), 1.)
        self.assertEqual(model.compute_auc(np.array([[3., 2., 1., 0.]]), fixations), 0.)
        self.assertEqual(model.compute_auc(np.ones((1, 4)), fixations), .5)

    def test_degenerate_and_nonfinite_inputs_retain_previous_behavior(self):
        values = np.array([[np.nan, np.inf, -np.inf, 1., 2.]])
        for fixations in [np.zeros((1, 5)), np.ones((1, 5)), np.array([[1, 0, 1, 0, 0]])]:
            self.assertEqual(model.compute_auc(values, fixations), pairwise_auc(values, fixations))


class DatasetRegressionTests(unittest.TestCase):
    def test_empty_input_reports_problem_without_creating_output(self):
        with tempfile.TemporaryDirectory() as folder:
            output = Path(folder) / 'output'
            with self.assertRaisesRegex(ValueError, 'No supported images'):
                model.process_dataset(folder, str(output))
            self.assertFalse(output.exists())

    def test_nonpositive_limit_is_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            for limit in [0, -1]:
                with self.assertRaisesRegex(ValueError, 'max_images must be positive'):
                    model.process_dataset(folder, folder, max_images=limit)

    def test_blur_is_recorded_and_zero_is_not_reported_as_auto(self):
        # Use fresh images and paired fixation files; no downloaded data needed.
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            stimuli, fixations, output = [root / name for name in ['stimuli', 'fixations', 'output']]
            stimuli.mkdir()
            fixations.mkdir()
            rng = np.random.RandomState(8)
            for i in range(2):
                Image.fromarray(rng.randint(0, 256, (24, 32, 3), dtype=np.uint8)).save(stimuli / f'image{i}.png')
                points = np.zeros((24, 32), dtype=np.uint8)
                points[8 + i, 10 + i] = 255
                Image.fromarray(points).save(fixations / f'image{i}_fixPts.jpg')
            stream = io.StringIO()
            with contextlib.redirect_stdout(stream):
                rows = model.process_dataset(str(stimuli), str(output), str(fixations), output_blur_sigma=0)
            self.assertEqual(len(rows), 2)
            self.assertIn('Output blur:  0', stream.getvalue())
            saved = json.loads((output / 'evaluation_results.json').read_text())
            self.assertEqual(saved['n_images'], 2)
            self.assertEqual(saved['config']['output_blur_sigma'], 0)
            self.assertEqual(len(list(output.glob('*_mfa.png'))), 2)


if __name__ == '__main__':
    unittest.main()
