"""Losslessly share identical immutable decoded PNG files using NTFS hardlinks.

Original screenshots/videos are excluded. Every retained path, byte count and
SHA remains valid. Index equality is checked against actual bytes before each
atomic replacement; no unique file content is deleted.
"""
import collections,csv,hashlib,json,os,re,sys,time
from pathlib import Path
root=Path(__file__).resolve().parents[1]
allowed=Path('C:/Users/Lauva/.codex/visualizations/2026/10/22/01a16331-da0c-7212-86b6-d3ab1a2524e9').resolve()
journal=(root/sys.argv[1]).resolve()
assert journal.is_relative_to(root/'Builds/Analysis') and not journal.exists()
def digest(p):
 h=hashlib.sha256()
 with p.open('rb') as stream:
  for block in iter(lambda:stream.read(1024*1024),b''):h.update(block)
 return h.hexdigest().upper()
groups=collections.defaultdict(set)
for index in (root/'Docs/Verification').glob('3d-*/frame-index.csv'):
 for r in csv.DictReader(index.open(encoding='utf-8-sig')):
  p=Path(r['path'])
  if p.is_absolute() and p.drive.upper()=='C:' and re.fullmatch(r'frame-\d+\.png',p.name):
   p=p.resolve()
   if p.is_relative_to(allowed) and p.is_file():groups[(r['sha256'],int(r['bytes']))].add(p)
records=[];errors=[];unchanged=0;count=0;shared_bytes=0
for (expected,size),paths in groups.items():
 if len(paths)<2:continue
 canonical=sorted(paths)[0]
 assert canonical.stat().st_size==size and digest(canonical)==expected
 for target in sorted(paths)[1:]:
  assert target.is_relative_to(allowed) and target.stat().st_size==size
  if os.path.samefile(canonical,target):unchanged+=1;continue
  assert digest(target)==expected
  if canonical.stat().st_nlink>=1000:
   canonical=target;continue
  temporary=target.with_name(target.name+'.hardlink-'+str(os.getpid()))
  assert not temporary.exists() and temporary.resolve().is_relative_to(allowed)
  try:
   os.link(canonical,temporary)
   assert os.path.samefile(canonical,temporary) and temporary.stat().st_size==size
   os.replace(temporary,target)
   assert os.path.samefile(canonical,target) and digest(target)==expected
   records.append(dict(path=str(target),canonical=str(canonical),sha256=expected,bytes=size))
   count+=1;shared_bytes+=size
   if count%500==0:print(json.dumps(dict(pathsLinked=count,logicalDuplicateBytesShared=shared_bytes)),flush=True)
  except OSError as e:
   errors.append(dict(path=str(target),error=str(e)))
  finally:
   if temporary.exists():temporary.unlink()
result=dict(scope='Only completed indexed decoded PNGs. All original paths, file bytes and SHA preserved; identical content stored once with NTFS hardlinks. No original screenshots/videos, project assets, reports, or unique data removed.',utc=time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime()),pathsLinked=count,alreadyShared=unchanged,logicalDuplicateBytesShared=shared_bytes,errors=errors,records=records)
journal.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print(json.dumps({k:v for k,v in result.items() if k!='records'},ensure_ascii=False),flush=True)
