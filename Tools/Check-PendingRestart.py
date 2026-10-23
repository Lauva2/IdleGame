"""Read-only check of actual saves while an offline report remains pending."""
import json
import math
import sys
from pathlib import Path

def read(path):
    return json.loads(Path(path).read_text(encoding="utf-8-sig").splitlines()[-1])

def same(a, b, key="state"):
    if isinstance(a, dict):
        assert a.keys() == b.keys(), key
        for name in a:
            same(a[name], b[name], key + "." + name)
    elif isinstance(a, list):
        assert len(a) == len(b), key
        for i, (left, right) in enumerate(zip(a, b)):
            same(left, right, key + "[" + str(i) + "]")
    elif isinstance(a, float) or isinstance(b, float):
        # Unity JSON can round the last decimal during a decode/encode cycle.
        assert math.isclose(a, b, rel_tol=0, abs_tol=1e-9), (key, a, b)
    else:
        assert a == b, (key, a, b)

before, after = map(read, sys.argv[1:3])
assert after["report"]["pending"]
assert after.pop("utcTicks") >= before.pop("utcTicks")
same(before, after)
print("PASS loaded pending report: counters/inventory/reward unchanged; facility/time floats within 1e-9 second; saved UTC advanced; coins=" + str(after["coins"]) + ", income=" + str(after["report"]["coins"]))
