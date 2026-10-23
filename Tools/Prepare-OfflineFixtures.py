"""Make explicitly isolated clock fixtures; never use them as ordinary progress.

Only saved UTC is changed. Inventory, money, progression and facility state are
copied from the supplied real save. The original is never overwritten.
"""
import copy
import datetime as dt
import hashlib
import json
from pathlib import Path
import sys

source, destination = map(Path, sys.argv[1:3])
selected = sys.argv[3] if len(sys.argv) > 3 else None
raw = source.read_bytes()
header, revision, checksum, payload = raw.split(b"\n", 3)
assert header == b"FORESTSHOP1"
assert hashlib.sha256(payload).hexdigest().upper().encode() == checksum
state = json.loads(payload)
assert not state["report"]["pending"], "close the ordinary report before backing up"
destination.mkdir(parents=True, exist_ok=False)
now = dt.datetime.now(dt.timezone.utc)
origin = dt.datetime(1, 1, 1, tzinfo=dt.timezone.utc)
delta = now - origin
ticks = (delta.days * 86400 + delta.seconds) * 10_000_000 + delta.microseconds * 10
records = []
cases = (("one-minute", 60), ("one-hour", 3600), ("eight-hours", 28800), ("over-eight-hours", 32400))
if selected == "eight-hours-under-cap":
    cases = ((selected, 28770),)
elif selected == "rollback-future":
    cases = ((selected, -3600),)
elif selected == "chapter-two-hours":
    cases = ((selected, 7200),)
elif selected:
    cases = tuple(case for case in cases if case[0] == selected)
    assert cases, "unknown selected fixture"
for index, (name, seconds) in enumerate(cases):
    fixture = copy.deepcopy(state)
    fixture["utcTicks"] = ticks - seconds * 10_000_000
    data = json.dumps(fixture, ensure_ascii=False, separators=(",", ":")).encode("utf-8")
    wrapped = b"\n".join((b"FORESTSHOP1", str(500_000 + index).encode(), hashlib.sha256(data).hexdigest().upper().encode(), data))
    (destination / (name + ".save")).write_bytes(wrapped)
    records.append({"name": name, "preparedAwaySecondsAtGeneration": seconds, "sha256": hashlib.sha256(wrapped).hexdigest().upper()})
(destination / "fixture-method.json").write_text(json.dumps({"isolatedClockFixtures": True, "source": str(source), "sourceSHA256": hashlib.sha256(raw).hexdigest().upper(), "generatedUTC": now.isoformat(), "changedPayloadFields": ["utcTicks"], "fixtures": records}, ensure_ascii=False, indent=2), encoding="utf-8")
print("Prepared " + str(len(records)) + " isolated saved-UTC fixtures. No ordinary progress or source file changed.")
