"""Prune only untracked, reproducible PNG samples after verifying source movies.

Keeps tracked necessary frames, every native screenshot, MP4, save and index.
Uses a new audit directory for each run; never rewrites frozen evidence indexes.
"""
import argparse, hashlib, json, re, shutil, subprocess
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
def sha(p):
    h = hashlib.sha256()
    with p.open('rb') as f:
        for b in iter(lambda: f.read(1024 * 1024), b''): h.update(b)
    return h.hexdigest().upper()
def write(p, data):
    p.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf8')
def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--audit', required=True)
    ap.add_argument('--index-tag', default='r01')
    ap.add_argument('--execute', action='store_true')
    ap.add_argument('candidates', nargs='+')
    a = ap.parse_args()
    assert re.fullmatch(r'[A-Za-z0-9_-]+', a.index_tag)
    audit = (ROOT / a.audit).resolve()
    assert audit.is_relative_to(ROOT / 'Docs/Maintenance') and not audit.exists()
    tracked = set(subprocess.check_output(['git', '-c', 'safe.directory='+ROOT.as_posix(), 'ls-files', '-z'], cwd=ROOT).decode('utf8').split('\0'))
    movies, rows, indexes = {}, [], {}
    for candidate in a.candidates:
        assert re.fullmatch(r'3d-d\d+-[a-z0-9-]+', candidate)
        q = ROOT / 'Docs/Verification' / candidate
        index = q / f'sample-media-index-{a.index_tag}.json'
        indexes[index.relative_to(ROOT).as_posix()] = sha(index)
        for row in json.loads(index.read_text(encoding='utf-8-sig'))['rows']:
            p = q / row['path']
            assert p.resolve() == p.absolute() and p.is_relative_to(q / 'Samples') and p.suffix == '.png'
            if not p.exists() or p.relative_to(ROOT).as_posix() in tracked: continue
            movie = (q / row['sourceVideo']).resolve()
            assert movie.is_relative_to(q) and movie.suffix == '.mp4' and movie.is_file()
            if movie not in movies:
                actual = sha(movie)
                assert actual == row['sourceSha256'], 'Original identity mismatch'
                movies[movie] = actual
            assert movies[movie] == row['sourceSha256'] and sha(p) == row['sha256']
            rows.append(dict(path=p.relative_to(ROOT).as_posix(), bytes=p.stat().st_size,
                             sha256=row['sha256'], sourceVideo=movie.relative_to(ROOT).as_posix(),
                             sourceSha256=row['sourceSha256'], sourcePTSSeconds=row['sourcePTSSeconds']))
    assert len({r['path'] for r in rows}) == len(rows)
    audit.mkdir(parents=True)
    before = shutil.disk_usage(ROOT).free
    write(audit / 'plan.json', dict(utc=datetime.now(timezone.utc).isoformat(), authorization='User explicitly allowed decoded-frame deletion while retaining original movies and time/SHA indexes on 2026-10-25; standing AGENTS.md policy.',
          scope='Only listed untracked reproducible PNGs under candidate Samples. Historical paths in frozen indexes cease to exist; re-export from source PTS as needed.', files=rows, indexes=indexes,
          movies=[dict(path=p.relative_to(ROOT).as_posix(), sha256=h) for p,h in movies.items()]))
    if a.execute:
        for row in rows:
            p = ROOT / row['path']
            assert p.resolve() == p.absolute() and p.is_relative_to(ROOT / 'Docs/Verification')
            assert sha(p) == row['sha256']
            p.unlink()
        assert all(sha(p) == h for p,h in movies.items())
        assert all(sha(ROOT/p) == h for p,h in indexes.items())
    result=dict(executed=a.execute, files=len(rows), logicalBytes=sum(r['bytes'] for r in rows),
                originalMoviesVerifiedBeforeAndAfter=len(movies), indexesUnchanged=True,
                freeBefore=before, freeAfter=shutil.disk_usage(ROOT).free)
    write(audit / 'result.json', result)
    print(json.dumps(result, indent=2))
if __name__ == '__main__': main()
