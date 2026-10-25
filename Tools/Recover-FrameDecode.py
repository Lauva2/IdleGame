"""Resume an interrupted PNG sampling pass without losing valid prefix frames.

Every valid existing prefix frame is compared, pixel-for-pixel by SHA256 RGB,
to a fresh decode of the original movie. Invalid tail files are preserved in
local diagnostics. New frames append with the original FPS/order, no seek.
"""
import csv,hashlib,json,os,re,shutil,subprocess,sys,time
from pathlib import Path
from PIL import Image
root=Path(__file__).resolve().parents[1];qa=(root/sys.argv[1]).resolve()
assert qa.is_relative_to(root/'Docs/Verification')
video=qa/sys.argv[2];assert Path(os.path.abspath(video)).is_relative_to(qa) and video.suffix=='.mp4'
directory=Path(sys.argv[3]).resolve();fps=int(sys.argv[4]);assert fps in (2,10)
allowed=Path('C:/Users/Lauva/.codex/visualizations/2026/10/22/01a16331-da0c-7212-86b6-d3ab1a2524e9').resolve()
assert directory.is_relative_to(allowed) or directory.is_relative_to(root/'Builds/Analysis')
assert directory.is_dir() and not (directory/'decode-complete.json').exists()
diag=root/'Builds/Analysis/d22-market2-recovery';assert not diag.exists();diag.mkdir()
def digest(p):
 h=hashlib.sha256()
 with p.open('rb') as s:
  for b in iter(lambda:s.read(1024*1024),b''):h.update(b)
 return h.hexdigest().upper()
source_sha=digest(video);files=sorted(directory.glob('frame-*.png'));rows=[];bad=[];invalid=False
assert [int(re.fullmatch(r'frame-(\d+)\.png',p.name)[1]) for p in files]==list(range(1,len(files)+1))
for p in files:
 try:
  with Image.open(p) as im:im.verify()
 except (OSError,SyntaxError):invalid=True
 if invalid:
  entry=dict(file=p.name,bytes=p.stat().st_size,sha256=digest(p));bad.append(entry)
  shutil.move(str(p),str(diag/p.name))
 else:rows.append(dict(file=p.name,bytes=p.stat().st_size,sha256=digest(p)))
count=len(rows);assert count>0
sys.path.insert(0,str(root/'Tools/PythonModules'));import imageio_ffmpeg
ff=imageio_ffmpeg.get_ffmpeg_exe()
hashes=subprocess.check_output([ff,'-hide_banner','-loglevel','error','-nostdin','-threads','2','-i',str(video),'-vf','fps='+str(fps),'-frames:v',str(count),'-pix_fmt','rgb24','-c:v','rawvideo','-threads','1','-f','framehash','-hash','sha256','-'],text=True)
expected=[x.split(',')[-1].strip().upper() for x in hashes.splitlines() if x and not x.startswith('#')]
assert len(expected)==count
for row,wanted in zip(rows,expected):
 with Image.open(directory/row['file']) as im:
  rgb=im.convert('RGB');row['rgbSha256']=hashlib.sha256(rgb.tobytes()).hexdigest().upper()
 assert row['rgbSha256']==wanted,'Existing prefix pixel mismatch: '+row['file']
print(json.dumps(dict(validPrefixFrames=count,allPrefixPixelsMatchOriginalVideo=True,invalidTailPreserved=len(bad))),flush=True)
prefix=diag/'retained-prefix.json';prefix.write_text(json.dumps(rows,indent=2)+'\n',encoding='utf8')
# Select after FPS conversion and pass timestamps through: no new seek/keyframe
# alignment, CFR duplication or overwriting of already verified prefix frames.
subprocess.run([ff,'-hide_banner','-loglevel','error','-nostdin','-n','-threads','2','-noautorotate','-i',str(video),'-vf','fps='+str(fps)+',select=gte(n\\,'+str(count)+')','-fps_mode','passthrough','-threads','2','-pix_fmt','rgb24','-compression_level','6','-start_number',str(count+1),str(directory/'frame-%05d.png')],check=True)
all_files=sorted(directory.glob('frame-*.png'))
assert [int(p.stem.split('-')[1]) for p in all_files]==list(range(1,len(all_files)+1)) and len(all_files)>count
assert digest(video)==source_sha
for p in all_files[count:]:
 with Image.open(p) as im:im.verify()
marker=dict(sourceVideo=str(video),sourceSha256=source_sha,sampleFPS=fps,frameExtension='png',frames=len(all_files),operation='Original valid prefix retained; every prefix RGB hash matched original full decode, then remaining FPS samples appended without seeking; native dimensions, no pixel editing.',recovered=True)
(directory/'decode-complete.json').write_text(json.dumps(marker,indent=2)+'\n',encoding='utf8')
result=dict(utc=time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime()),scope='Original source movie retained. Interrupted disk-full decode never accepted as complete. Valid prefix retained and RGB-verified against original movie; invalid tail preserved, remaining frames appended.',sourceVideo=str(video),sourceSha256=source_sha,directory=str(directory),validPrefixFrames=count,finalFrames=len(all_files),allPrefixPixelsMatchOriginalVideo=True,prefixManifest=str(prefix),prefixManifestSha256=digest(prefix),invalidTail=bad)
(qa/'decode-recovery-market2.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf8');print(json.dumps(result,ensure_ascii=False),flush=True)
