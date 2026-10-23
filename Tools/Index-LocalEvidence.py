"""Index locally retained QA media without changing or deleting any evidence."""
import csv
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parent.parent
evidence = root / "Docs/Verification"
extensions = {".png", ".jpg", ".jpeg", ".gif", ".webp", ".mp4", ".flac", ".wav", ".opus", ".f32", ".raw", ".zip"}
files = sorted(p for p in evidence.rglob("*") if p.is_file() and p.suffix.lower() in extensions)
total = 0
with (evidence / "local-media-index.csv").open("w", encoding="utf-8", newline="") as out:
    writer = csv.writer(out)
    writer.writerow(["relativePath", "bytes", "sha256"])
    for index, path in enumerate(files, 1):
        digest = hashlib.sha256()
        size = 0
        with path.open("rb") as stream:
            for chunk in iter(lambda: stream.read(1024*1024), b""):
                digest.update(chunk)
                size += len(chunk)
        writer.writerow([path.relative_to(root).as_posix(), size, digest.hexdigest().upper()])
        total += size
        if index % 5000 == 0:
            print(f"Indexed {index}/{len(files)} files", flush=True)
summary = {"capturedUTC": datetime.now(timezone.utc).isoformat(), "files": len(files), "bytes": total,
           "index": "Docs/Verification/local-media-index.csv", "mediaRetainedLocally": True,
           "note": "Repository contains selected numbered final screenshots and audio. Bulk raw media/decoded frames stay at the indexed local paths; an index is not a replacement for viewing actual evidence."}
(evidence / "local-media-summary.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
print(json.dumps(summary, ensure_ascii=False, indent=2))
