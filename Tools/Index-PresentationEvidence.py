"""Freeze native originals and explicitly requested PTS samples without decoding.
Usage: python Tools/Index-PresentationEvidence.py <qa-folder> <unique-tag>
Indexes and freezes are immutable; supplements use a new tag.
"""
import hashlib,json,re,sys
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];q=(ROOT/sys.argv[1]).resolve();tag=sys.argv[2]
assert q.is_relative_to(ROOT/'Docs/Verification') and re.fullmatch('[A-Za-z0-9_-]+',tag)
sys.path.insert(0,str(ROOT/'Tools/PythonModules'));import imageio_ffmpeg
def sha(path):
 h=hashlib.sha256()
 with path.open('rb') as f:
  for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
 return h.hexdigest().upper()
def write(path,v):
 assert not path.exists(),'Preserve frozen index'
 path.write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
original=[]
for p in sorted((q/'Evidence').rglob('*')):
 if p.suffix.lower() not in ('.png','.mp4'):continue
 row=dict(path=p.relative_to(q).as_posix(),sha256=sha(p),bytes=p.stat().st_size)
 if p.suffix=='.png':
  b=p.read_bytes();assert b[:8]==b'\x89PNG\r\n\x1a\n';row.update(type='native-screenshot',width=int.from_bytes(b[16:20],'big'),height=int.from_bytes(b[20:24],'big'))
 else:
  reader=imageio_ffmpeg.read_frames(str(p));meta=next(reader);reader.close();row.update(type='original-video',width=meta['source_size'][0],height=meta['source_size'][1],duration=meta['duration'],nominalFPS=meta['fps'])
 original.append(row)
samples=[]
for index in sorted((q/'Samples').glob('*/sample-index.json')):
 record=json.loads(index.read_text(encoding='utf8'));video=q/record['sourceVideo'];assert sha(video)==record['sourceSha256']
 for row in record['frames']:
  file=q/row['path'];assert sha(file)==row['sha256'];samples.append(dict(**row,sourceVideo=record['sourceVideo'],sourceSha256=record['sourceSha256']))
indices=[]
for name,rows in [('original-media-index',original),('sample-media-index',samples)]:
 p=q/(name+'-'+tag+'.json');write(p,dict(utc=datetime.now(timezone.utc).isoformat(),candidate=q.name,rows=rows));indices.append(dict(path=p.name,sha256=sha(p),rows=len(rows)))
build=json.loads((q/'build-info.json').read_text(encoding='utf8'));assert sha(ROOT/build['apk'])==build['apkSha256']
record=dict(candidate=q.name,utc=datetime.now(timezone.utc).isoformat(),apkSha256=build['apkSha256'],sourceManifestSha256=sha(q/'source-files.sha256'),indices=indices,originalImages=sum(r['type']=='native-screenshot' for r in original),originalVideos=sum(r['type']=='original-video' for r in original),sampledFrames=len(samples),originalBytes=sum(r['bytes'] for r in original),sampleBytes=sum((q/r['path']).stat().st_size for r in samples),scope='Original media and necessary source-PTS samples retained locally on E. No full-video decode requirement or assertion all frames visually checked. Sampling is not a substitute for independent review or whole-product acceptance.')
write(q/('evidence-freeze-'+tag+'.json'),record);print(json.dumps(record,ensure_ascii=False,indent=2))
