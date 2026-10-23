"""Keep every performance row; classify known lifecycle intervals explicitly."""
import csv
import datetime as dt
import json
from pathlib import Path
import re
import sys

root = Path(sys.argv[1])
life = root / "Lifecycle"
stat = (life / "process-stat-final.txt").read_text(encoding="utf-8-sig").strip()
pid = stat.split(" ", 1)[0]
assert pid == (life / "pid.txt").read_text(encoding="utf-8-sig").strip()
assert pid == (life / "pid-final.txt").read_text(encoding="utf-8-sig").strip()
fields = stat[stat.rfind(")")+2:].split()
hz = float((life / "clock-ticks-per-second.txt").read_text(encoding="utf-8-sig"))
start = int(fields[19])/hz  # /proc stat field 22; suffix starts at field 3
uptime = float((life / "device-uptime-final.txt").read_text(encoding="utf-8-sig").split()[0])
elapsed = uptime-start
assert elapsed >= 1800
rows = []
for row in csv.reader((root / "performance-30min.csv").read_text(encoding="utf-8-sig").splitlines()):
    stamp, frames, seconds, fps, p95, memory, maximum = row
    rows.append(dict(utc=stamp, frames=int(frames), sampledSeconds=float(seconds), averageFPS=float(fps),
                     p95Milliseconds=float(p95), unityAllocatedBytes=int(memory), maximumMilliseconds=float(maximum)))
assert len(rows) == 6, "Review interval classification if new rows were collected"
conditions = ["startup and simplified-animation test", "simplified-to-full animation transition",
              "actual Android HOME/resume, 91.056486 seconds in background",
              "full animation; ordinary production, panels, recording, upgrades",
              "full animation; ordinary production, rotation, timezone, capacity recording",
              "full animation; ordinary production, capacity recording, local evidence decoding"]
for row, condition in zip(rows, conditions):
    row["condition"] = condition
    row["steadyThresholdsMet"] = row["averageFPS"] >= 29 and row["p95Milliseconds"] <= 40
pss = {}
for name in ("memory-0627.txt", "memory-0631.txt", "memory-final.txt"):
    value = re.search(r"TOTAL PSS:\s*(\d+)", (life / name).read_text(encoding="utf-8-sig"))
    assert value, name
    pss[name] = int(value[1])
log = (life / "logcat-current-process.txt").read_text(encoding="utf-8-sig")
matches = re.findall(r"^.*(?:FATAL EXCEPTION|Fatal signal|ANR in |Save failed|NullReferenceException|OutOfMemoryException).*$", log, re.M)
print(json.dumps(dict(device="MuMu Android12 emulator, not a physical representative device", pid=int(pid),
                     sameProcessElapsedSeconds=elapsed, physicalDeviceAcceptance=False,
                     allSixRowsPreserved=True, rows=rows, pssKilobytes=pss, fatalOrSaveErrorLogMatches=matches),
                 ensure_ascii=False, indent=2))
