"""Audit actual ordinary catch histories across retained APK candidates.

Prepared seeds/entries are never inputs. Aggregate checks retain earlier fish
even after the live recent list has correctly reached its 30-entry limit.
"""
import hashlib,json,math,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]
qa=(root/sys.argv[1]).resolve();prior=(root/sys.argv[2]).resolve()
assert qa.is_relative_to(root/'Docs/Verification') and prior.is_relative_to(root/'Docs/Verification')

def read(folder,name):
    raw=(folder/name).read_bytes();head,revision,digest,payload=raw.split(b'\n',3)
    assert head.strip()==b'FORESTSHOP1' and hashlib.sha256(payload).hexdigest().upper()==digest.decode().strip()
    return json.loads(payload),hashlib.sha256(raw).hexdigest().upper()

def same(a,b):
    if isinstance(a,dict):return a.keys()==b.keys() and all(same(a[k],b[k]) for k in a)
    if isinstance(a,list):return len(a)==len(b) and all(same(x,y) for x,y in zip(a,b))
    if isinstance(a,(float,int)) and not isinstance(a,bool):return math.isclose(a,b,rel_tol=0,abs_tol=1e-10)
    return a==b

before,bhash=read(qa,'normal-held.save');after,ahash=read(qa,'normal-restorable.save')
paused,_=read(qa,'ordinary-continuous-end.save');background,_=read(qa,'ordinary-background-report.save')
history={};consistent=True
sources=[(prior,'normal-held.save'),(prior,'ordinary-continuous-end.save'),(qa,'normal-held.save'),(qa,'normal-restorable.save')]
for folder,name in sources:
    state,_=read(folder,name)
    for fish in state['fishing']['recent']:
        sequence=fish['sequence']
        if sequence in history:consistent &= history[sequence]==fish
        history[sequence]=fish
f=after['fishing'];checks={}
checks['ordinary_catches_advance_from_latest_previous_candidate']=f['totalCaught']>before['fishing']['totalCaught']==23
checks['all_actual_ids_unique_contiguous_and_unchanged']=consistent and sorted(history)==list(range(1,f['totalCaught']+1))
checks['actual_recent_limit_retains_latest_thirty']=len(f['recent'])==min(30,f['totalCaught']) and [x['sequence'] for x in f['recent']]==list(range(f['totalCaught'],max(0,f['totalCaught']-30),-1))
checks['all_species_aggregates_match_actual_history']=True
for species,e in enumerate(f['entries']):
    fishes=[x for x in history.values() if x['species']==species]
    checks['all_species_aggregates_match_actual_history'] &= e['count']==len(fishes) and e['qualityCounts']==[sum(x['quality']==q for x in fishes) for q in range(4)] and e['longestMm']==max((x['lengthMm'] for x in fishes),default=0) and e['heaviestGrams']==max((x['weightGrams'] for x in fishes),default=0) and e['firstSequence']==min((x['sequence'] for x in fishes),default=0) and e['bestQuality']==max((x['quality'] for x in fishes),default=-1)
pending=before['fishing']['candidate']
checks['pending_fish_survives_install_and_view_resume']=pending['sequence'] in history and pending==history[pending['sequence']]
checks['background_keeps_paused_fish_exact']=same(paused['fishing'],background['fishing'])
checks['money_only_changes_with_original_sales']=after['coins']-before['coins']==after['sales']-before['sales']
checks['original_chapter_and_forest_progress_kept']=before['stories']==after['stories']==5 and before['reputation']==after['reputation']==172 and before['forest']['requests']==after['forest']['requests']==3
checks['ordinary_settings_and_facility_counts_kept']=all(before[k]==after[k] for k in ('music','sound','simpleAnimation','warehouse','decoration')) and all(len(before[k])==len(after[k]) for k in ('plots','machines','shelves'))
checks['fishing_does_not_auto_gather']=before['forest']['gathered']==after['forest']['gathered']
checks['final_ordinary_cast_paused']=not f['running'] and (f['phase'] not in (1,2) or f['candidate']['sequence']==f['totalCaught']+1)
result=dict(scope='Only actual ordinary snapshots, before any prepared fixture. Earlier candidate catches retain their original identity. Full reconstructed history verifies aggregates beyond30 recent entries; no natural probability or full-release acceptance claim.',beforeSha256=bhash,afterSha256=ahash,incomingFish=before['fishing']['totalCaught'],fish=f['totalCaught'],newFishInThisCandidate=f['totalCaught']-before['fishing']['totalCaught'],discovered=sum(e['count']>0 for e in f['entries']),coins=after['coins'],checks=checks)
(qa/'ordinary-fishing-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print(json.dumps(result,ensure_ascii=False,indent=2));assert all(checks.values())
