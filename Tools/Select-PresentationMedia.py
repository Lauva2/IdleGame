"""List necessary native/PTS PNGs for Git without changing any image bytes.
Usage: python Tools/Select-PresentationMedia.py <qa-folder> <request-json>
The request is a list of exact QA-relative PNG paths. Full originals stay local.
"""
import hashlib,json,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1];q=(root/sys.argv[1]).resolve()
assert q.is_relative_to(root/'Docs/Verification')
names=json.loads((root/sys.argv[2]).read_text(encoding='utf-8-sig'))
assert names and len(names)==len(set(names))
known={}
for index in sorted(q.glob('original-media-index-*.json')):
    for row in json.loads(index.read_text(encoding='utf8'))['rows']:known[row['path']]=row
for index in sorted(q.glob('sample-media-index-*.json')):
    for row in json.loads(index.read_text(encoding='utf8'))['rows']:known[row['path']]=row
rows=[]
for name in names:
    p=(q/name).resolve();assert p.is_relative_to(q) and p.suffix=='.png' and p.is_file()
    raw=p.read_bytes();digest=hashlib.sha256(raw).hexdigest().upper()
    assert raw[:8]==b'\x89PNG\r\n\x1a\n' and known[name]['sha256']==digest
    rows.append(dict(path=name,gitPath=p.relative_to(root).as_posix(),sha256=digest,bytes=len(raw),width=int.from_bytes(raw[16:20],'big'),height=int.from_bytes(raw[20:24],'big'),sourcePTSSeconds=known[name].get('sourcePTSSeconds'),sourceVideo=known[name].get('sourceVideo')))
out=q/'versioned-media-selection.json';assert not out.exists(),'Preserve selection identity'
out.write_text(json.dumps(dict(candidate=q.name,images=len(rows),bytes=sum(r['bytes'] for r in rows),scope='Necessary original native screenshots and lossless source-PTS frames selected for Git. All other original media remains local with immutable SHA/time indexes; no pixel rewriting.',rows=rows),ensure_ascii=False,indent=2)+'\n',encoding='utf8')
(q/'versioned-media-paths.txt').write_text(''.join(r['gitPath']+'\n' for r in rows),encoding='utf8')
print(json.dumps(dict(images=len(rows),bytes=sum(r['bytes'] for r in rows))))
