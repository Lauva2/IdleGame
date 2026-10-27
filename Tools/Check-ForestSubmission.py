"""Compare frozen metadata and necessary PNGs with exact staged/commit blobs."""
import argparse, hashlib, json, re, subprocess
from pathlib import Path

root = Path(__file__).resolve().parents[1]
ap = argparse.ArgumentParser(description=__doc__)
ap.add_argument('folder')
ap.add_argument('--ref', default=':')
ap.add_argument('--index-tag', default='r01')
ap.add_argument('--no-write', action='store_true')
a = ap.parse_args()
q = (root / a.folder).resolve()
assert q.is_relative_to(root / 'Docs/Verification')
assert re.fullmatch('[A-Za-z0-9_-]+', a.index_tag)
index_path = q / ('submission-file-index-'+a.index_tag+'.json')
frozen = json.loads(index_path.read_text(encoding='utf8'))
images = []
for path in sorted(q.glob('versioned-media-selection*.json')):
    images.extend(json.loads(path.read_text(encoding='utf8'))['rows'])
rows = [dict(path='Docs/Verification/'+q.name+'/'+r['path'], sha256=r['sha256'])
        for r in frozen['files']]
rows.extend(dict(path=r['gitPath'], sha256=r['sha256']) for r in images)
rows.append(dict(path=index_path.relative_to(root).as_posix(),
                 sha256=hashlib.sha256(index_path.read_bytes()).hexdigest().upper()))
assert len(rows) == len({r['path'] for r in rows})
specs = []
for r in rows:
    assert '\n' not in r['path'] and '\r' not in r['path']
    local = (root/r['path']).resolve()
    assert local.is_relative_to(q)
    assert hashlib.sha256(local.read_bytes()).hexdigest().upper() == r['sha256']
    specs.append((':' if a.ref == ':' else a.ref+':')+r['path'])
result = subprocess.run(['git', '-c', 'safe.directory='+root.as_posix(), 'cat-file', '--batch'],
                        cwd=root, input=('\n'.join(specs)+'\n').encode('utf8'),
                        stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=True)
pos = 0
mismatches = []
for r in rows:
    end = result.stdout.index(b'\n', pos)
    header = result.stdout[pos:end].split()
    assert len(header) == 3 and header[1] == b'blob', (r['path'], header)
    size = int(header[2])
    begin = end+1
    actual = hashlib.sha256(result.stdout[begin:begin+size]).hexdigest().upper()
    if actual != r['sha256']:
        mismatches.append(dict(path=r['path'], expected=r['sha256'], actual=actual))
    pos = begin+size
    assert result.stdout[pos:pos+1] == b'\n'
    pos += 1
assert pos == len(result.stdout)
report = dict(candidate=q.name, gitTree=a.ref, frozenMetadata=len(frozen['files']),
              necessaryImages=len(images), submissionIndexAlsoVerified=True,
              totalBlobs=len(rows), allPassed=not mismatches, mismatches=mismatches,
              scope='Current frozen QA metadata, necessary PNGs and the submission index equal exact Git blob bytes. APK source inputs have a separate225-file check; no EOL conversion substituted native saves, logs, reports or image pixels.')
if not a.no_write:
    suffix = '' if a.index_tag == 'r01' else '-'+a.index_tag
    out = q/('git-stage-submission-identity'+suffix+'.json')
    assert not out.exists(), 'Preserve prior verification'
    out.write_text(json.dumps(report, ensure_ascii=False, indent=2)+'\n', encoding='utf8')
print(json.dumps(report, ensure_ascii=False, indent=2))
assert report['allPassed']
