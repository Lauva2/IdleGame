"""Prepare licensed CC0 background music from the verified lossless source.

Never downloads a file or synthesizes a replacement melody. The source is
Kistol's Hot Springs Town, published at the URL recorded below.
"""
import hashlib
import json
from pathlib import Path
import subprocess
import sys

root = Path(__file__).resolve().parent.parent
source = Path(sys.argv[1]).resolve()
target = Path(sys.argv[2]).resolve()
target.relative_to(root)
expected = "EFE6844EBBACEF3B6D22C7D0208AE20B1EBA666555316BDD323B4C5315F46EDD"
assert hashlib.sha256(source.read_bytes()).hexdigest().upper() == expected, "Wrong source audio"
ffmpeg = root / "Tools/PythonModules/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe"
target.parent.mkdir(parents=True, exist_ok=True)
measure = subprocess.run([str(ffmpeg), "-hide_banner", "-i", str(source), "-af",
                          "loudnorm=I=-20:TP=-2:LRA=7:print_format=json", "-f", "null", "-"],
                         check=True, capture_output=True).stderr.decode("utf-8", "replace")
level = json.loads(measure[measure.rfind("{"):measure.rfind("}")+1])
gain = -20-float(level["input_i"])
# Keep the complete 160-second musical form and tempo. Short boundary fades
# suppress the original end-to-start sample discontinuity without overlapping
# or dropping a bar. Perceived looping quality still needs human listening.
filters = f"volume={gain:.6f}dB,afade=t=in:st=0:d=0.02,afade=t=out:st=159.98:d=0.02"
subprocess.run([str(ffmpeg), "-v", "error", "-y", "-i", str(source), "-af", filters,
                "-ar", "44100", "-ac", "2", "-c:a", "pcm_s16le", "-map_metadata", "-1", str(target)], check=True)
print(json.dumps({"title": "Hot Springs Town", "creator": "Kistol", "license": "CC0-1.0",
                  "sourcePage": "https://opengameart.org/content/hot-springs-town",
                  "licenseURL": "https://creativecommons.org/publicdomain/zero/1.0/",
                  "sourceSHA256": expected, "sourceIntegratedLUFS": float(level["input_i"]),
                  "gainDB": gain, "filters": filters, "output": target.relative_to(root).as_posix(),
                  "outputSHA256": hashlib.sha256(target.read_bytes()).hexdigest().upper(),
                  "durationSeconds": 160, "sampleRate": 44100, "channels": 2,
                  "humanListeningApproved": False}, ensure_ascii=False, indent=2))
