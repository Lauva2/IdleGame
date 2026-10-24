"""Audit protected actual ordinary Android snapshots, never active fixtures."""
import hashlib,json,math
from pathlib import Path
root=Path(__file__).resolve().parents[1]
qa=root/'Docs/Verification/3d-d21-forest05'
def read(name):
    raw=(qa/name).read_bytes();head,revision,digest,payload=raw.split(b'\n',3)
    assert head==b'FORESTSHOP1' and hashlib.sha256(payload).hexdigest().upper().encode()==digest
    return json.loads(payload),hashlib.sha256(raw).hexdigest().upper()
def same(a,b):
    if isinstance(a,dict):return a.keys()==b.keys() and all(same(a[k],b[k]) for k in a)
    if isinstance(a,list):return len(a)==len(b) and all(same(x,y) for x,y in zip(a,b))
    if isinstance(a,(float,int)) and not isinstance(a,bool):return math.isclose(a,b,rel_tol=0,abs_tol=1e-10)
    return a==b
before,bhash=read('normal-held.save');after,ahash=read('normal-restorable.save')
paused,_=read('ordinary-paused-fishing.save');bg,_=read('ordinary-background-report.save')
f=after['fishing'];hist=f['recent'];checks={}
checks['same_original_chapter_and_forest_rewards']=before['stories']==after['stories']==5 and before['reputation']==after['reputation']==172 and before['forest']['requests']==after['forest']['requests']==3
checks['all_coin_growth_from_original_sales_only']=after['coins']==before['coins']+after['sales']-before['sales']
checks['original_facility_counts_and_settings_kept']=all(len(before[k])==len(after[k]) for k in ('plots','machines','shelves')) and all(before[k]==after[k] for k in ('music','sound','simpleAnimation','warehouse','decoration'))
checks['ordinary_ten_fish_from_zero']=before['fishing']['totalCaught']==0 and f['totalCaught']==len(hist)==10
checks['unique_contiguous_catch_ids']=sorted(x['sequence'] for x in hist)==list(range(1,11))
checks['all_species_maxima_and_quality_counts_match_actual_individuals']=True
for species,e in enumerate(f['entries']):
    fishes=[x for x in hist if x['species']==species];qc=[sum(x['quality']==q for x in fishes) for q in range(4)]
    checks['all_species_maxima_and_quality_counts_match_actual_individuals'] &= e['count']==len(fishes) and e['qualityCounts']==qc and e['longestMm']==max([x['lengthMm'] for x in fishes],default=0) and e['heaviestGrams']==max([x['weightGrams'] for x in fishes],default=0) and e['bestQuality']==max([x['quality'] for x in fishes],default=-1) and e['firstSequence']==min([x['sequence'] for x in fishes],default=0)
checks['background_preserves_exact_paused_fish_state']=same(paused['fishing'],bg['fishing']) and bg['fishing']['totalCaught']==8
checks['old_eight_catches_not_rewritten_by_resume']=same(paused['fishing']['recent'],[x for x in hist if x['sequence']<=8])
checks['grass_bait_used_in_actual_new_catch']=any(x['bait']==1 for x in hist) and hist[0]['sequence']==10
checks['final_cast_paused_and_pending_record_kept']=not f['running'] and f['phase']==1 and f['candidate']['sequence']==11
checks['no_automatic_forest_gathering']=after['forest']['gathered']==before['forest']['gathered']
result=dict(scope='Actual normal UI and wall time, before any prepared fixture. First8 catches; paused35.061089-second background; receipt resume; new grass bait. No fish or resources injected. Does not establish natural probabilities/all12 unlock or formal stress/sound acceptance.',beforeSha256=bhash,afterSha256=ahash,beforeCoins=before['coins'],afterCoins=after['coins'],salesDelta=after['sales']-before['sales'],caught=f['totalCaught'],discovered=sum(e['count']>0 for e in f['entries']),qualities=[sum(e['qualityCounts'][q] for e in f['entries']) for q in range(4)],checks=checks)
(qa/'ordinary-fishing-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print(json.dumps(result,ensure_ascii=False,indent=2));assert all(checks.values())
