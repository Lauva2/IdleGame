"""Export only requested native source frames, with actual PTS and SHA indexes.

No crop, resize, painting, probability simulation or full-video expansion.
Usage: python Tools/Sample-PresentationEvidence.py <qa-folder> <request-json>
Request: [{"video":"Startup/cold.mp4","start":0,"end":8,"fps":4,"id":"cold"}]
"""
import hashlib,json,re,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
qa=(ROOT/sys.argv[1]).resolve();assert qa.is_relative_to(ROOT/'Docs/Verification')
request=(ROOT/sys.argv[2]).resolve();specs=json.loads(request.read_text(encoding='utf-8-sig'))
sys.path.insert(0,str(ROOT/'Tools/PythonModules'));import imageio_ffmpeg
ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()
def sha(path):
 h=hashlib.sha256()
 with path.open('rb') as f:
  for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
 return h.hexdigest().upper()
def write(path,value):path.write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
for spec in specs:
 video=(qa/'Evidence'/spec['video']).resolve();assert video.is_relative_to(qa/'Evidence') and video.suffix=='.mp4' and video.is_file()
 start,end,fps=spec['start'],spec['end'],spec['fps'];assert 0<=start<end and 0<fps<=10 and (end-start)*fps<=900
 name=spec['id'];assert re.fullmatch('[A-Za-z0-9_-]+',name)
 out=qa/'Samples'/name;assert not out.exists(),'Preserve previous sample/export audit'
 out.mkdir(parents=True);original=sha(video)
 expr=f"select='if(between(t,{start},{end}),if(isnan(prev_selected_t),1,gte(t-prev_selected_t,{1/fps:.9f})),0)',showinfo"
 args=[ffmpeg,'-hide_banner','-nostdin','-n','-threads','2','-noautorotate','-i',str(video),'-vf',expr,'-vsync','0','-threads','2','-pix_fmt','rgb24','-compression_level','6',str(out/'frame-%04d.png')]
 result=subprocess.run(args,stdout=subprocess.PIPE,stderr=subprocess.PIPE)
 (out/'ffmpeg.txt').write_bytes(result.stderr);assert result.returncode==0,result.stderr[-2000:]
 times=[float(t) for t in re.findall(rb'Parsed_showinfo[^\n]*\bn:\s*\d+[^\n]*\bpts_time:([0-9.]+)',result.stderr)]
 files=sorted(out.glob('frame-*.png'));assert len(files)==len(times)>0,(len(files),len(times))
 rows=[]
 for file,t in zip(files,times):
  data=file.read_bytes();assert data[:8]==b'\x89PNG\r\n\x1a\n';w=int.from_bytes(data[16:20],'big');h=int.from_bytes(data[20:24],'big')
  rows.append(dict(path=file.relative_to(qa).as_posix(),sourcePTSSeconds=t,sha256=sha(file),width=w,height=h))
 assert original==sha(video)
 record=dict(sourceVideo=video.relative_to(qa).as_posix(),sourceSha256=original,selection=spec,operation='ffmpeg select at original source PTS; native encoded size/orientation, RGB24 lossless PNG; no resizing/cropping/compositing',frames=rows)
 write(out/'sample-index.json',record)
 print(json.dumps(dict(id=name,frames=len(rows),bytes=sum(p.stat().st_size for p in files)),ensure_ascii=False),flush=True)
