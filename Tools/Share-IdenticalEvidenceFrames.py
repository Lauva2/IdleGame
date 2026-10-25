"""Share byte-identical completed local frames on NTFS; retain every path/hash."""
import collections,csv,hashlib,json,os,shutil,sys
from datetime import datetime,timezone
from pathlib import Path
root=Path(__file__).resolve().parents[1];q=(root/sys.argv[1]).resolve()
assert q.is_relative_to(root/'Docs/Verification')
journal=q/'identical-frame-storage-sharing.json';assert not journal.exists(),'Preserve prior journal'
storage=json.loads((q/'local-frame-storage.json').read_text(encoding='utf-8-sig'))
allowed=[(root/'Builds/Analysis').resolve(),Path('C:/Users/Lauva/.codex/visualizations/2026/10/22/01a16331-da0c-7212-86b6-d3ab1a2524e9').resolve()]
bases=[Path(p).resolve() for p in storage['absoluteFrameRoots']]
assert all(any(p.is_relative_to(a) and p!=a for a in allowed) for p in bases)
def digest(p):
    h=hashlib.sha256()
    with p.open('rb') as f:
        for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
    return h.hexdigest().upper()
groups=collections.defaultdict(list)
for row in csv.DictReader((q/'frame-index.csv').open(encoding='utf-8-sig')):
    p=Path(row['path']).resolve()
    assert any(p.is_relative_to(a) for a in bases) and p.suffix in ('.png','.webp') and p.name.startswith('frame-')
    groups[(p.drive.casefold(),row['sha256'])].append((p,int(row['bytes'])))
plans=[]
for (_,sha),items in groups.items():
    if len(items)<2:continue
    canonical=items[0][0]
    for p,size in items:
        assert p.stat().st_size==size and digest(p)==sha
    for p,size in items[1:]:
        if p.stat().st_ino!=canonical.stat().st_ino:
            temporary=p.with_name(p.name+'.sharetmp');assert not temporary.exists()
            plans.append((canonical,p,temporary,sha,size))
before={p.drive:shutil.disk_usage(p).free for p in bases};records=[]
for canonical,p,temporary,sha,size in plans:
    os.link(canonical,temporary)
    assert digest(temporary)==sha
    os.replace(temporary,p)
    assert p.stat().st_size==size and digest(p)==sha and p.stat().st_ino==canonical.stat().st_ino
    records.append(dict(path=str(p),canonical=str(canonical),bytes=size,sha256=sha))
after={p.drive:shutil.disk_usage(p).free for p in bases}
result=dict(utc=datetime.now(timezone.utc).isoformat(),scope='Only completed sampled frames with verified identical byte hashes. Source PNG/MP4, APK, reports and every frame path remain unchanged. Atomic replacement with a same-volume hard link; no resizing/re-encoding or visual editing.',files=len(records),logicalDuplicateBytes=sum(r['bytes'] for r in records),freeBytesBefore=before,freeBytesAfter=after,records=records)
journal.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps({k:v for k,v in result.items() if k!='records'},ensure_ascii=False))
