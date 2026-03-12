/**
 * MFA Experiment Suite — Shared Utilities
 * Cora Zeng, 2026
 * 
 * Common functions for visual angle calibration, stimulus drawing,
 * trial generation, and data handling across all three experiments.
 */

const MFA = {

  // ── Calibration state ──────────────────────────────────────────
  pixelsPerCm: null,
  viewingDistanceCm: 60,
  pixelsPerDegree: null,

  CREDIT_CARD_WIDTH_CM: 8.56,

  calcPixelsPerDegree(pxPerCm, distCm) {
    return distCm * Math.tan(Math.PI / 180) * pxPerCm;
  },

  initCalibration(pxPerCm, distCm) {
    this.pixelsPerCm = pxPerCm;
    this.viewingDistanceCm = distCm || 60;
    this.pixelsPerDegree = this.calcPixelsPerDegree(pxPerCm, this.viewingDistanceCm);
  },

  degToPx(deg) {
    if (!this.pixelsPerDegree) {
      this.pixelsPerDegree = this.calcPixelsPerDegree(37.8, 60);
    }
    return deg * this.pixelsPerDegree;
  },

  // ── Canvas drawing primitives ──────────────────────────────────

  drawFixation(ctx, cx, cy, size, color, lw) {
    size = size || 20; color = color || '#fff'; lw = lw || 2;
    ctx.strokeStyle = color;
    ctx.lineWidth = lw;
    ctx.beginPath();
    ctx.moveTo(cx - size / 2, cy);
    ctx.lineTo(cx + size / 2, cy);
    ctx.moveTo(cx, cy - size / 2);
    ctx.lineTo(cx, cy + size / 2);
    ctx.stroke();
  },

  drawLetter(ctx, letter, x, y, size, color) {
    size = size || 28; color = color || '#fff';
    ctx.font = 'bold ' + size + "px 'Courier New', monospace";
    ctx.fillStyle = color;
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';
    ctx.fillText(letter, x, y);
  },

  drawCue(ctx, x, y, size, color, lw) {
    size = size || 40; color = color || '#ffdd00'; lw = lw || 3;
    ctx.strokeStyle = color;
    ctx.lineWidth = lw;
    ctx.strokeRect(x - size / 2, y - size / 2, size, size);
  },

  clearCanvas(ctx, w, h, bg) {
    ctx.fillStyle = bg || '#333';
    ctx.fillRect(0, 0, w, h);
  },

  // ── Randomization helpers ──────────────────────────────────────

  shuffle(arr) {
    const a = arr.slice();
    for (let i = a.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      var tmp = a[i]; a[i] = a[j]; a[j] = tmp;
    }
    return a;
  },

  randInt(min, max) {
    return Math.floor(Math.random() * (max - min + 1)) + min;
  },

  randChoice(arr) {
    return arr[Math.floor(Math.random() * arr.length)];
  },

  // Full factorial with repetitions, then shuffled
  factorial(factors, reps) {
    reps = reps || 1;
    var trials = [{}];
    var names = Object.keys(factors);
    for (var n = 0; n < names.length; n++) {
      var name = names[n];
      var levels = factors[name];
      var next = [];
      for (var t = 0; t < trials.length; t++) {
        for (var l = 0; l < levels.length; l++) {
          var obj = {};
          for (var k in trials[t]) obj[k] = trials[t][k];
          obj[name] = levels[l];
          next.push(obj);
        }
      }
      trials = next;
    }
    var all = [];
    for (var r = 0; r < reps; r++) {
      for (var i = 0; i < trials.length; i++) {
        var copy = {};
        for (var k in trials[i]) copy[k] = trials[i][k];
        all.push(copy);
      }
    }
    return this.shuffle(all);
  },

  // ── Data export ────────────────────────────────────────────────

  downloadCSV(jsPsych, filename) {
    var csv = jsPsych.data.get().csv();
    var blob = new Blob([csv], { type: 'text/csv' });
    var url = URL.createObjectURL(blob);
    var a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  },

  // ── Screen size check ─────────────────────────────────────────

  getScreenInfo() {
    return {
      windowWidth: window.innerWidth,
      windowHeight: window.innerHeight,
      screenWidth: screen.width,
      screenHeight: screen.height,
      devicePixelRatio: window.devicePixelRatio || 1
    };
  },

  // ── Calibration trial builder ──────────────────────────────────
  // Returns a jsPsych timeline node for credit-card resize calibration.

  buildCalibrationTrial(jsPsych) {
    var self = this;
    return {
      type: jsPsychHtmlKeyboardResponse,
      stimulus: function () {
        return '<div style="text-align:center; color:#fff; max-width:700px; margin:auto;">' +
          '<h2>Screen Calibration</h2>' +
          '<p>Please hold a <strong>standard credit/debit card</strong> against your screen.</p>' +
          '<p>Use the <strong>left/right arrow keys</strong> to resize the blue rectangle ' +
          'until it exactly matches the width of your card.</p>' +
          '<p>Press <strong>Enter</strong> when done.</p>' +
          '<div id="cal-box" style="width:300px; height:' +
          Math.round(300 * 5.398 / 8.56) + 'px; ' +
          'border:3px solid #4af; margin:30px auto; background:rgba(68,170,255,0.15);"></div>' +
          '<p id="cal-info" style="font-size:14px; color:#aaa;">Current width: 300 px</p>' +
          '</div>';
      },
      choices: ['Enter'],
      response_ends_trial: true,
      on_load: function () {
        var box = document.getElementById('cal-box');
        var info = document.getElementById('cal-info');
        var w = 300;
        var handler = function (e) {
          if (e.key === 'ArrowRight') { w += 2; }
          else if (e.key === 'ArrowLeft') { w = Math.max(50, w - 2); }
          else { return; }
          box.style.width = w + 'px';
          box.style.height = Math.round(w * 5.398 / 8.56) + 'px';
          info.textContent = 'Current width: ' + w + ' px';
        };
        document.addEventListener('keydown', handler);
        jsPsych.getDisplayElement()._calHandler = handler;
      },
      on_finish: function (data) {
        var handler = jsPsych.getDisplayElement()._calHandler;
        if (handler) document.removeEventListener('keydown', handler);
        var box = document.getElementById('cal-box');
        var widthPx = box ? parseInt(box.style.width) : 300;
        var pxPerCm = widthPx / self.CREDIT_CARD_WIDTH_CM;
        self.initCalibration(pxPerCm, 60);
        data.calibration_px_per_cm = pxPerCm;
        data.calibration_px_per_deg = self.pixelsPerDegree;
        data.calibration_card_width_px = widthPx;
      }
    };
  },

  // ── URL parameter parsing (for Prolific / JATOS / Qualtrics) ────

  getUrlParam(name) {
    try {
      var url = new URL(window.location.href);
      return url.searchParams.get(name);
    } catch (e) { return null; }
  },

  // ── Participant ID trial ──────────────────────────────────────

  buildParticipantIdTrial(jsPsych) {
    var self = this;
    var urlId = self.getUrlParam('PROLIFIC_PID') ||
                self.getUrlParam('participant_id') ||
                self.getUrlParam('workerId') ||
                self.getUrlParam('id');

    if (urlId) {
      return {
        type: jsPsychCallFunction,
        func: function () {
          jsPsych.data.addProperties({ participant_id: urlId, id_source: 'url' });
        }
      };
    }

    return {
      type: jsPsychSurveyText,
      preamble: '<h3 style="color:#fff;">Participant Information</h3>',
      questions: [{
        prompt: '<span style="color:#fff;">Enter your Participant ID:</span>',
        placeholder: 'e.g., P001',
        required: true,
        name: 'participant_id'
      }],
      button_label: 'Continue',
      data: { phase: 'participant_id' },
      on_finish: function (data) {
        var pid = data.response.participant_id || ('P_' + Date.now());
        jsPsych.data.addProperties({ participant_id: pid, id_source: 'manual' });
      }
    };
  },

  // ── Data saving (multi-platform) ──────────────────────────────

  saveData(jsPsych, filename) {
    var csv = jsPsych.data.get().csv();
    if (typeof jatos !== 'undefined') {
      jatos.submitResultData(csv)
        .then(function () { jatos.endStudy(); })
        .catch(function () { MFA.downloadCSV(jsPsych, filename); });
      return;
    }
    this.downloadCSV(jsPsych, filename);
  },

  // ── Catch trial insertion ─────────────────────────────────────

  insertCatchTrials(trials, interval, letters) {
    interval = interval || 40;
    letters = letters || ['T', 'L'];
    var result = [];
    for (var i = 0; i < trials.length; i++) {
      result.push(trials[i]);
      if ((i + 1) % interval === 0 && i < trials.length - 1) {
        result.push({
          is_catch: true,
          target_letter: MFA.randChoice(letters)
        });
      }
    }
    return result;
  },

  // ── Common instruction styles ──────────────────────────────────

  instructionStyle: 'style="text-align:center; color:#fff; max-width:750px; ' +
    'margin:auto; font-size:18px; line-height:1.7;"',

  keyReminder: '<p style="color:#aaa; font-size:14px; margin-top:30px;">' +
    'Press <strong>F</strong> for T &nbsp;|&nbsp; Press <strong>J</strong> for L</p>'
};
