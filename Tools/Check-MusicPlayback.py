"""Match actual device music to the licensed source; does not judge musical taste."""
import json
from pathlib import Path
import subprocess
import sys
import numpy as np

ffmpeg = Path(__file__).parent / "PythonModules/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe"
def decode(path, start=None, duration=None):
    cmd = [str(ffmpeg), "-v", "error"]
    if start is not None: cmd += ["-ss", str(start)]
    if duration is not None: cmd += ["-t", str(duration)]
    cmd += ["-i", str(path), "-f", "f32le", "-ar", "8000", "-ac", "1", "pipe:1"]
    return np.frombuffer(subprocess.run(cmd, check=True, capture_output=True).stdout, dtype="<f4").astype(np.float64)

reference = decode(sys.argv[1])
reference_seconds = len(reference)/8000
results = []
starts = tuple(float(value) for value in sys.argv[3].split(",")) if len(sys.argv) > 3 else (3, 170, 335)
for start in starts:
    window = decode(sys.argv[2], start, 10)
    assert len(window) >= 9.9*8000, "Recording must cover the final window"
    window -= window.mean()
    n = len(window)
    size = 1 << (len(reference)+n-2).bit_length()
    conv = np.fft.irfft(np.fft.rfft(reference, size)*np.fft.rfft(window[::-1], size), size)
    sums = np.concatenate(([0.0], np.cumsum(reference)))
    squares = np.concatenate(([0.0], np.cumsum(reference**2)))
    energy = squares[n:]-squares[:-n]-(sums[n:]-sums[:-n])**2/n
    corr = conv[n-1:len(reference)]/np.sqrt(np.maximum(energy, 1e-20)*np.dot(window, window))
    index = int(np.argmax(corr))
    result = {"recordingStartSeconds": start, "sourceOffsetSeconds": index/8000, "correlation": float(corr[index])}
    assert result["correlation"] >= 0.65, result
    results.append(result)
for result in results[1:]:
    advance = result["sourceOffsetSeconds"]-results[0]["sourceOffsetSeconds"]
    expected = result["recordingStartSeconds"]-results[0]["recordingStartSeconds"]
    error = (advance-expected+reference_seconds/2)%reference_seconds-reference_seconds/2
    result["phaseErrorSeconds"] = error
    assert abs(error) < 0.15, "Unexpected music restart, timing shift or wrong loop length"
print(json.dumps({"pass": True, "sourceSeconds": reference_seconds, "matches": results,
                  "meaning": "Device output matches the new licensed music at the specified windows with consistent phase; only windows spanning at least two source periods establish a multi-period check.",
                  "humanListeningApproved": False}, ensure_ascii=False, indent=2))
