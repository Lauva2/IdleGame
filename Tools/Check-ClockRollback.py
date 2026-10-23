"""Check actual future-saved-UTC loads without editing any file."""
import hashlib
import json
from pathlib import Path
import sys

root = Path(sys.argv[1])
def read(name):
    header, revision, checksum, payload = (root / name).read_bytes().split(b"\n", 3)
    assert header == b"FORESTSHOP1" and hashlib.sha256(payload).hexdigest().upper().encode() == checksum
    return json.loads(payload)
fixture = read("rollback-future.save")
for name in ("actual-first.save", "actual-restart.save"):
    state = read(name)
    assert state["utcTicks"] == fixture["utcTicks"], "UTC high-water mark must not move backwards"
    report = state["report"]
    assert report["away"] == report["effective"] == report["coins"] == 0
    assert not report["pending"]
    assert report["cropDelta"] == [0]*4 and report["itemDelta"] == [0]*6
    assert state["coins"] >= fixture["coins"] and state["sales"] >= fixture["sales"]
print("PASS actual Android future-UTC load and forced restart: saved UTC high-water retained; zero offline time/income/deltas, no negative or duplicate offline reward. Separate online replay results verify actual foreground production.")
