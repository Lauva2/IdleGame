"""Read-only full saved-state replay from each applied fixture SHA to its HOME save.

Includes startup foreground ticks before the movie, not a claim of video duration.
Actual navigation/occlusion must still be visually reviewed in native media.
"""
import hashlib,json,re,subprocess,sys
from datetime import datetime
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
q=(ROOT/sys.argv[1]).resolve();assert q.is_relative_to(ROOT/'Docs/Verification')
tag=sys.argv[2];assert re.fullmatch(r'[a-zA-Z0-9-]+',tag)
mono='C:/Program Files/Unity/Hub/Editor/2022.3.8f1c1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
def read(p):
    raw=p.read_bytes();head,_,digest,payload=raw.split(b'\n',3)
    assert head.strip()==b'FORESTSHOP1' and hashlib.sha256(payload).hexdigest().upper().encode()==digest.strip()
    return json.loads(payload)
def utc(t):return datetime.fromisoformat(t.replace('Z','+00:00'))
activations=[json.loads(s) for s in (q/'prepared-input-identity.jsonl').read_text(encoding='utf-8-sig').splitlines() if s.strip()]
sources={sha(p):p for p in (q/'Prepared').rglob('*.save')}
out=q/('LifeReplay-'+tag);assert not out.exists(),'Preserve earlier replay audits';out.mkdir()
audit=q/('life-full-state-audit-'+tag+'.json');assert not audit.exists()
rows=[]
for after in sorted(q.glob('life-*-after.save')):
    name=after.stem[5:-6];group,profile=name.split('-',1)
    actions=q/'Evidence/Life'/group/profile/'actions.jsonl'
    first=json.loads(actions.read_text(encoding='utf8').splitlines()[0])['utc']
    variant='late' if group=='Courtyard' else 'moment-finished'
    record=max((r for r in activations if r['variant']==variant and utc(r['utc'])<=utc(first)),key=lambda r:utc(r['utc']))
    source=sources[record['sha256']]
    steps=[] if group=='Courtyard' else [dict(kind='visit',id=i,arg=0,expected=True) for i in [0,0,1,2,2]]+[dict(kind='choose',id=2,arg=1,expected=True)]
    plan=out/(name+'-actions.json');plan.write_text(json.dumps(steps,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
    result=subprocess.run([mono,str(ROOT/'Tools/TestOutput/CompareMomentActions.exe'),str(source),str(after),str(plan),'--from-fixture'],stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
    output=result.stdout.decode('utf-8-sig').lstrip('\ufeff')
    (out/(name+'-replay.txt')).write_text(output,encoding='utf8')
    state=read(after)
    rows.append(dict(group=group,profile=profile,appliedFixture=source.relative_to(q).as_posix(),fixtureSha256=record['sha256'],after=after.name,afterSha256=sha(after),passed=result.returncode==0,coins=state['coins'],fish=state['fishing']['totalCaught'],moments=state['moments'],recordedActions=len(steps)))
result=dict(scope=__doc__.strip(),startupMethod='OfflineSettlement.Resume using actual startup report.away, then remaining foreground elapsed; all report fields reproduced, not copied from final.',rows=rows,allPassed=len(rows)==8 and all(r['passed'] for r in rows))
audit.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print(json.dumps(dict(cases=len(rows),allPassed=result['allPassed'],failures=[r['after'] for r in rows if not r['passed']]),ensure_ascii=False,indent=2))
assert result['allPassed']
