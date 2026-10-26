"""Freeze non-media candidate data and verify the build inputs before review.
Immutable metadata supplements can be frozen using another unique tag.
"""
import hashlib,json,re,sys,zipfile
from datetime import datetime,timezone
from pathlib import Path
root=Path(__file__).resolve().parents[1];q=(root/sys.argv[1]).resolve();tag=sys.argv[2]
assert q.is_relative_to(root/'Docs/Verification') and re.fullmatch('[A-Za-z0-9_-]+',tag)
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
 return h.hexdigest().upper()
output=q/f'submission-file-index-{tag}.json';assert not output.exists(),'Preserve submission identity'
build=json.loads((q/'build-info.json').read_text(encoding='utf8'))
assert sha(root/build['apk'])==build['apkSha256']
assert sha(root/build['sourceArchive'])==build['sourceArchiveSha256']
assert sha(q/'source-files.sha256')==build['sourceManifestSha256']
inputs=[]
with zipfile.ZipFile(root/build['sourceArchive']) as z:
 for line in (q/'source-files.sha256').read_text(encoding='utf8').splitlines():
  expected,name=line.split('  ',1)
  assert name.startswith(('Assets/','Packages/','ProjectSettings/'))
  assert sha(root/name)==expected and hashlib.sha256(z.read(name)).hexdigest().upper()==expected,name
  inputs.append(name)
assert len(inputs)==build['sourceInputFiles']
rows=[]
for p in sorted(q.rglob('*')):
 if not p.is_file() or p.suffix.lower() in ('.png','.mp4','.webp','.jpg','.jpeg'):continue
 rows.append(dict(path=p.relative_to(q).as_posix(),bytes=p.stat().st_size,sha256=sha(p)))
record=dict(utc=datetime.now(timezone.utc).isoformat(),candidate=q.name,apkSha256=build['apkSha256'],sourceInputsVerified=len(inputs),sourceManifestSha256=build['sourceManifestSha256'],sourceArchiveSha256=build['sourceArchiveSha256'],files=rows,scope='Metadata, raw logs, fixture/ordinary saves, plans, audits and guides present at submission. Original media and source-PTS frames are frozen separately. Later independent report/status documents are separate additions, not silent edits to this index.')
output.write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print(json.dumps(dict(candidate=q.name,inputs=len(inputs),metadataFiles=len(rows),index=output.name,sha256=sha(output)),ensure_ascii=False,indent=2))
