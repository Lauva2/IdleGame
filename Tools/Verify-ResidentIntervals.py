"""Replay every saved field across actual greeting/ambient capture intervals.
Read-only greetings have no Core action. Forest navigation uses existing visit
actions; the verifier reconstructs startup resume and production from the
exact labelled fixture, not from fields copied out of the final save.
"""
import hashlib,json,subprocess,sys
from datetime import datetime
from pathlib import Path
root=Path(__file__).resolve().parents[1]
q=(root/sys.argv[1]).resolve();assert q.is_relative_to(root/'Docs/Verification')
tag=sys.argv[2];assert tag.isalnum()
out=q/('ResidentReplay-'+tag);assert not out.exists();out.mkdir()
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
activations=[json.loads(s) for s in (q/'prepared-input-identity.jsonl').read_text(encoding='utf-8-sig').splitlines() if s.strip()]
sources={sha(p):p for p in (q/'Prepared').rglob('*.save')}
utc=lambda t:datetime.fromisoformat(t.replace('Z','+00:00'))
mono='C:/Program Files/Unity/Hub/Editor/2022.3.8f1c1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
rows=[]
for after in sorted(q.glob('resident-*-after.save')):
    name=after.stem[9:-6]
    if name.startswith('fit-'): variant='simple';path=name
    elif name=='ambient':variant='moment-finished';path=name
    elif name=='rays':variant='simple';path=name
    elif name.startswith('greeting-'):variant=name;path=name
    else:continue
    actions=q/'Evidence/Residents'/path/'actions.jsonl'
    first=json.loads(actions.read_text(encoding='utf8').splitlines()[0])['utc']
    applied=max((r for r in activations if r['variant']==variant and utc(r['utc'])<=utc(first)),key=lambda r:utc(r['utc']))
    source=sources[applied['sha256']]
    steps=json.loads((q/'resident-ambient-actions.json').read_text(encoding='utf-8-sig')) if name=='ambient' else []
    plan=out/(name+'-actions.json');plan.write_text(json.dumps(steps)+'\n',encoding='utf8')
    result=subprocess.run([mono,str(root/'Tools/TestOutput/CompareMomentActions.exe'),str(source),str(after),str(plan),'--from-fixture'],stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
    output=result.stdout.decode('utf-8-sig').lstrip('\ufeff');(out/(name+'-replay.txt')).write_text(output,encoding='utf8')
    rows.append(dict(name=name,variant=variant,source=source.relative_to(q).as_posix(),sourceSha256=applied['sha256'],after=after.name,afterSha256=sha(after),recordedCoreActions=len(steps),passed=result.returncode==0))
report=dict(scope=__doc__,rows=rows,allPassed=bool(rows) and all(r['passed'] for r in rows))
(q/('resident-full-state-audit-'+tag+'.json')).write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print(json.dumps(dict(cases=len(rows),allPassed=report['allPassed'],failures=[r['name'] for r in rows if not r['passed']]),ensure_ascii=False,indent=2));assert report['allPassed']
