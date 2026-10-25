"""Complete one native clip at the standard indexer's sampling rule before full indexing."""
import hashlib,importlib.util,json,subprocess,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]
qa=(root/sys.argv[1]).resolve();assert qa.is_relative_to(root/'Docs/Verification')
video=(qa/sys.argv[2]).resolve();assert video.is_relative_to(qa/'Evidence') and video.suffix=='.mp4'
frames=(root/sys.argv[3]).resolve();assert frames.is_relative_to(root/'Builds/Analysis')
fps=10 if video.stem.casefold().startswith(('shop-closeup','initial-flow','fishing-motion')) else 2
out=frames/video.relative_to(qa/'Evidence').parent/(video.stem+'-frames-'+str(fps)+'fps')
assert not out.exists(),'Preserve partial or completed frames'
out.mkdir(parents=True)
sys.path.insert(0,str(root/'Tools/PythonModules'));import imageio_ffmpeg
spec=importlib.util.spec_from_file_location('forest_index',root/'Tools/Index-GameEvidence.py');module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
expected=module.sha256(video)
subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-nostdin','-n','-threads','2','-noautorotate','-i',str(video),'-vf','fps='+str(fps),'-threads','2','-pix_fmt','rgb24','-compression_level','6',str(out/'frame-%05d.png')],check=True)
assert module.sha256(video)==expected
items=list(out.glob('frame-*.png'));assert items
for p in items:assert module.png_size(p)==(1080,1920)
identity=dict(sourceVideo=str(video),sourceSha256=expected,sampleFPS=fps,operation='ffmpeg FPS sampling, native encoded resolution/orientation, lossless decoded RGB pixels',frameExtension='png',frames=len(items))
module.write_json(out/'decode-complete.json',identity)
print(json.dumps(dict(video=str(video),directory=str(out),frames=len(items),sourceSha256=expected)))
