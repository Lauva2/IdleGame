"""Read-only signal measurements of the unedited MuMu long recording, not listening QA."""
import hashlib
import json
import math
from pathlib import Path
import subprocess
import sys
import numpy as np

recording = Path(sys.argv[1])
ffmpeg = Path(__file__).parent / "PythonModules/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe"
decoded = subprocess.run([str(ffmpeg), "-v", "error", "-i", str(recording),
                          "-f", "f32le", "-ac", "2", "-ar", "48000", "pipe:1"],
                         capture_output=True, check=True).stdout
samples = np.frombuffer(decoded, dtype="<f4").reshape(-1, 2)
assert np.all(np.isfinite(samples))
duration = len(samples)/48000
assert duration >= 130, "At least two nominal 64-second music cycles"
peak = float(np.abs(samples).max())
clipped = int(np.count_nonzero(np.abs(samples) >= 1))
rows = []
for start in range(0, int(duration), 5):
    window = samples[start*48000:min((start+5)*48000, len(samples))]
    rms = float(np.sqrt(np.mean(window.astype(np.float64)**2)))
    rows.append({"startSeconds": start, "endSeconds": min(start+5, duration),
                 "rmsDBFS": 20*math.log10(rms) if rms else None,
                 "peak": float(np.abs(window).max())})
print(json.dumps({"recordingSHA256": hashlib.sha256(recording.read_bytes()).hexdigest().upper(),
                  "decodedSeconds": duration, "finiteSamples": True, "peak": peak,
                  "peakDBFS": 20*math.log10(peak), "samplesAtOrBeyondFullScale": clipped,
                  "noDigitalClippingInDecodedRecording": clipped == 0,
                  "listeningQualityVerified": False,
                  "limits": "Recorded samples only. Does not prove pleasant mix, inaudible loop seam, absence of perceived stacking or every cue trigger.",
                  "fiveSecondWindows": rows}, ensure_ascii=False, indent=2))
