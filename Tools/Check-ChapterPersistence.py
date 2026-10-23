"""Verify actual UI chapter and decoration persistence; label clock fixtures explicitly."""
import hashlib
import json
from pathlib import Path
import sys
root = Path(sys.argv[1])
ordinary = len(sys.argv) > 2 and sys.argv[2] == "--ordinary"
def read(name):
    header, revision, checksum, payload = (root / name).read_bytes().split(b"\n", 3)
    assert header == b"FORESTSHOP1" and hashlib.sha256(payload).hexdigest().upper().encode() == checksum
    return json.loads(payload)
report, continued = read("01-real-chapter-before-interaction.save" if ordinary else "actual-report.save"), read("continued.save")
assert report["chapterComplete"] and not report["chapterCelebrated"]
assert report["report"]["pending"] == (not ordinary)
assert report["stories"] == 5 and report["reputation"] >= 150 and report["sales"] >= 60000 and report["sideRoom"]
assert all(n >= 1 for n in report["sold"])
assert continued["chapterComplete"] and continued["chapterCelebrated"] and not continued["report"]["pending"]
assert continued["decoration"] == [True]*4
if ordinary:
    raw = (root.parent / "NormalQA/ordinary-continuation-start.save").read_bytes().split(b"\n", 3)
    assert raw[0] == b"FORESTSHOP1" and hashlib.sha256(raw[3]).hexdigest().upper().encode() == raw[2]
    initial = json.loads(raw[3])
    assert not initial["chapterComplete"] and not initial["chapterCelebrated"]
    assert initial["sales"] < 60000 <= report["sales"]
    assert initial["stories"] == report["stories"] == 5
    assert initial["reputation"] == report["reputation"] == 154
    assert initial["sideRoom"] and all(n >= 1 for n in initial["sold"])
    elapsed = report["playedSeconds"] - initial["playedSeconds"]
    wall = (report["utcTicks"] - initial["utcTicks"]) / 10000000
    assert elapsed > 3600 and abs(elapsed-wall) < 2, "Actual saved UTC and played interval agree"
    assert report["coins"]-initial["coins"] == report["sales"]-initial["sales"]
    assert continued["coins"]-report["coins"] == continued["sales"]-report["sales"]
    print(f"Ordinary threshold crossing: played={elapsed:.9f}s, saved wall={wall:.9f}s, sales={initial['sales']}->{report['sales']}; no money beyond sales")
for prefix, selected in (("basic", False), ("rewards", True)):
    before = read(prefix + ("-before-exit.save" if prefix == "basic" else "-before-restart.save"))
    after = read(prefix + ("-after-restart.save"))
    assert before["decoration"] == after["decoration"] == [selected]*4
    assert before["chapterComplete"] and after["chapterComplete"]
    assert before["chapterCelebrated"] and after["chapterCelebrated"]
    assert before["stories"] == after["stories"] == 5
    assert before["reputation"] == after["reputation"] == report["reputation"]
    assert after["sales"] >= before["sales"] >= report["sales"]
    assert after["coins"]-before["coins"] == after["sales"]-before["sales"], "No extra chapter money on restart"
if ordinary:
    print("PASS ORDINARY chapter: real wall-clock progress crosses 60000 sales, celebration continues without reset, all four legally unlocked basic/reward selections persist across actual process restarts, no duplicate chapter money. Separate online replay verifies full inventory and facility timing.")
else:
    print("PASS ISOLATED saved-UTC chapter: normal rules unlock celebration, continue without reset, all four basic/reward selections persist across actual process restarts, no duplicate chapter money. NOT ordinary wall-clock chapter completion.")
