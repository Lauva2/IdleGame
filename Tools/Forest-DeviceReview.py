"""Explicit ADB review plans. Captures original framebuffer, never edits images.
Usage: python Tools/Forest-DeviceReview.py plan.json
Plan keys: directory, steps. Actions: tap [x,y], swipe [x,y,x2,y2,ms],
drag_hold [x,y,x2,y2,moveMs,holdMs] uses actual Android motion events and
holds before release to avoid scroll inertia; wait seconds (<=15), shot name,
back true. Every command is timestamped.
Prepared progression is stored separately and must be labelled by its caller.
"""
import subprocess, json, sys, time, re, datetime, os
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
ADB=ROOT/'Tools/AndroidSdk/platform-tools/adb.exe'
BASE=[str(ADB),'-P','5038','-s',os.environ.get('FOREST_ADB_SERIAL','127.0.0.1:16385')]
plan=json.loads(Path(sys.argv[1]).read_text(encoding='utf-8-sig'))
logical=Path(os.path.abspath(ROOT/plan['directory']))
review_root=ROOT/'Docs/Verification'
assert logical.is_relative_to(review_root), 'Logical capture path must stay in project verification'
dest=logical.resolve()
if not dest.is_relative_to(review_root):
    # Large original media may live behind an explicitly recorded NTFS junction
    # in the other user-authorized writable workspace. Do not weaken the guard
    # to allow arbitrary external screenshot destinations.
    relative=logical.relative_to(review_root)
    assert len(relative.parts)>=2 and relative.parts[1]=='Evidence'
    evidence=review_root/relative.parts[0]/'Evidence'
    record=json.loads((evidence.parent/'local-media-storage.json').read_text(encoding='utf-8-sig'))
    allowed=Path('C:/Users/Lauva/.codex/visualizations/2026/10/22/01a16331-da0c-7212-86b6-d3ab1a2524e9').resolve()
    physical=evidence.resolve()
    assert physical.is_relative_to(allowed) and dest.is_relative_to(physical)
    assert Path(record['logicalPath'])==evidence and Path(record['physicalPath']).resolve()==physical
dest.mkdir(parents=True,exist_ok=True)
log=dest/'actions.jsonl'
def run(*args):
    return subprocess.check_output(BASE+list(map(str,args)))
for step in plan['steps']:
    begin=datetime.datetime.now(datetime.timezone.utc).isoformat()
    if 'tap' in step: run('shell','input','tap',*step['tap']); time.sleep(.4)
    elif 'swipe' in step: run('shell','input','swipe',*step['swipe']); time.sleep(.4)
    elif 'drag_hold' in step:
        x,y,x2,y2,duration,hold=step['drag_hold']
        assert all(isinstance(v,int) and v>=0 for v in (x,y,x2,y2)) and 0<duration<=5000 and 0<=hold<=2000
        run('shell','input','touchscreen','motionevent','DOWN',x,y)
        try:
            # Cross the drag threshold with a tiny move before larger segments;
            # Unity initializes its drag origin when that threshold is crossed.
            dx,dy=x2-x,y2-y;length=max(abs(dx),abs(dy),1)
            run('shell','input','touchscreen','motionevent','MOVE',round(x+dx*min(10,length)/length),round(y+dy*min(10,length)/length));time.sleep(.12)
            for i in range(1,7):
                run('shell','input','touchscreen','motionevent','MOVE',round(x+(x2-x)*i/6),round(y+(y2-y)*i/6)); time.sleep(duration/6000)
            time.sleep(hold/1000)
        finally: run('shell','input','touchscreen','motionevent','UP',x2,y2)
        time.sleep(.15)
    elif 'wait' in step: assert 0<=step['wait']<=15; time.sleep(step['wait'])
    elif 'back' in step: run('shell','input','keyevent','KEYCODE_BACK'); time.sleep(.4)
    elif 'shot' in step:
        assert re.fullmatch(r'[a-zA-Z0-9_-]+',step['shot'])
        focus=run('shell','dumpsys','window').decode(errors='replace')
        assert re.search(r'mCurrentFocus=.*com\.forestatelier\.curioshop',focus), 'Game is not focused'
        (dest/(step['shot']+'.png')).write_bytes(run('exec-out','screencap','-p'))
    else: raise ValueError(step)
    with log.open('a',encoding='utf8') as f: f.write(json.dumps({'utc':begin,'step':step},ensure_ascii=False)+'\n')
    print(step,flush=True)
