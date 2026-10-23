"""Read-only checks of actual Android recordings and saved switch states.

Decoded sample measurements establish output routing, never listening quality.
Windows must be chosen from the accompanying original capture timeline.
"""
import hashlib
import json
import math
from pathlib import Path
import subprocess
import sys
import numpy as np

root = Path(sys.argv[1])
ffmpeg = Path(__file__).parent / "PythonModules/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe"
recording = root / "independent-switches-original.flac"
expectations = {"both-on": (True, True, 4, 10), "music-only": (True, False, 15, 20),
                "both-off": (False, False, 25, 30), "sound-only": (False, True, 36, 41)}
results = []
for name, (music, sound, start, end) in expectations.items():
    raw = (root / (name + ".save")).read_bytes()
    header, revision, checksum, payload = raw.split(b"\n", 3)
    assert header == b"FORESTSHOP1"
    assert hashlib.sha256(payload).hexdigest().upper().encode() == checksum
    state = json.loads(payload)
    assert (state["music"], state["sound"]) == (music, sound), name
    decoded = subprocess.run([str(ffmpeg), "-v", "error", "-ss", str(start), "-t", str(end-start),
                              "-i", str(recording), "-f", "f32le", "-ac", "2", "-ar", "48000", "pipe:1"],
                             check=True, capture_output=True).stdout
    samples = np.frombuffer(decoded, dtype="<f4")
    assert len(samples) >= (end-start)*48000*2 - 4096, name
    assert np.all(np.isfinite(samples)), name
    rms = float(np.sqrt(np.mean(samples.astype(np.float64)**2)))
    peak = float(np.max(np.abs(samples)))
    nonzero = int(np.count_nonzero(samples))
    assert (nonzero == 0) if name == "both-off" else (nonzero > 0), name
    results.append({"name": name, "music": music, "sound": sound, "windowSeconds": [start, end],
                    "samples": len(samples), "nonzeroSamples": nonzero, "rms": rms, "peak": peak,
                    "rmsDBFS": 20*math.log10(rms) if rms else None,
                    "peakDBFS": 20*math.log10(peak) if peak else None})
print(json.dumps({"pass": True, "recordingSHA256": hashlib.sha256(recording.read_bytes()).hexdigest().upper(),
                  "listeningQualityVerified": False, "results": results}, ensure_ascii=False, indent=2))
