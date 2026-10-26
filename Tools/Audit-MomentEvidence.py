"""Read-only checks of actual APK snapshots from explicitly prepared moments.
No fixture is counted as ordinary progress, no media is modified.
"""
import hashlib,json,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1];q=(root/sys.argv[1]).resolve()
assert q.is_relative_to(root/'Docs/Verification')
def read(name):
    raw=(q/name).read_bytes();head,_,digest,payload=raw.split(b'\n',3)
    assert head==b'FORESTSHOP1' and hashlib.sha256(payload).hexdigest().upper().encode()==digest
    return json.loads(payload)
checks={}
correction=q/'moment-capture-corrections.json'
fitting=json.loads(correction.read_text(encoding='utf-8-sig'))['useForActualInput'] if correction.is_file() else {}
for variant in ('start','simple'):
    s=read(f'moment-flow-{variant}-after.save')
    checks[f'{variant}_all_three_deliberate_actions_and_swaps']=s['moments']==dict(stages=[3,3,3],choices=[1,1,1])
    checks[f'{variant}_correct_animation_mode']=s['simpleAnimation']==(variant=='simple')
for profile in ('Default','Long','Narrow','SafeTall'):
    stem=fitting.get(profile,profile)
    s=read(f'moment-fit-{stem}-after.save')
    checks[f'{profile}_actual_choices_saved']=s['moments']==dict(stages=[3,3,3],choices=[1,1,1])
    images=list((q/'Evidence/MomentFit'/stem).glob('*.png'))
    dimensions=(720,1600) if profile=='Narrow' else (1080,2340) if profile=='Long' else (1080,1920)
    checks[f'{profile}_native_framebuffer_sizes']=len(images)==16 and all((int.from_bytes(p.read_bytes()[16:20],'big'),int.from_bytes(p.read_bytes()[20:24],'big'))==dimensions for p in images)
for variant in ('locked','partial','finished'):
    fixture=read(f'Prepared/moment-{variant}.save');after=read(f'moment-state-{variant}-after.save')
    checks[f'journal_{variant}_does_not_advance_or_change_choices']=fixture['moments']==after['moments']
before=read('moment-reload-before.save');loaded=read('moment-reload-loaded.save');after=read('moment-reload-after.save')
checks['closing_and_actual_restart_keeps_pending_steps']=before['moments']==loaded['moments']==dict(stages=[2,2,2],choices=[-1,-1,-1])
checks['only_the_later_explicit_star_choice_finishes']=after['moments']==dict(stages=[2,2,3],choices=[-1,-1,1])
checks['pending_reload_does_not_give_fish']=before['fishing']['totalCaught']==loaded['fishing']['totalCaught']==after['fishing']['totalCaught']
checks['ordinary_all_three_events_still_protected']=read('normal-restorable.save')['moments']==dict(stages=[3,3,3],choices=[0,1,0])
result=dict(candidate=q.name,fittingSources=fitting,scope='Actual installed APK snapshots after separately labelled prepared UI captures. Native sizes and saved choices are checked, not visual quality or natural progression. Incorrect-coordinate originals remain with an explicit correction record; the ordinary save has separate full-state action/online/offline replays and is never replaced by these prepared states.',checks=checks,allPassed=all(checks.values()))
out=q/'moment-prepared-audit.json';assert not out.exists(),'Preserve earlier audit'
out.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print(json.dumps(result,ensure_ascii=False,indent=2));assert result['allPassed']
