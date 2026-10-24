"""Audit real before/after ordinary saves, without writing game progression.
This checks conservation across the documented normal second-wave actions;
it does not replace original videos, independent art review, or natural
new-game chapter acceptance. No fixtures are generated or activated here.
"""
import hashlib, json, sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]
folder=(root/sys.argv[1]).resolve(); assert folder.is_relative_to(root/'Docs/Verification')
def read(path):
    data=path.read_bytes(); parts=data.split(b'\n',3)
    assert parts[0].rstrip(b'\r')==b'FORESTSHOP1'
    assert hashlib.sha256(parts[3]).hexdigest().upper().encode()==parts[2].rstrip(b'\r')
    return json.loads(parts[3]),hashlib.sha256(data).hexdigest().upper()
b,bsha=read(folder/'normal-held.save')
a,asha=read(folder/'normal-restorable.save')
checks={}
def check(name,ok):
    checks[name]=bool(ok)
    assert ok,name
check('ordinary_new_resources_initially_zero',b['crops'][4:]==[0,0,0] and b['items'][6:]==[0,0,0] and b['forest']['discovered']==0)
check('strict_expanded_shape',a['version']==2 and len(a['crops'])==7 and len(a['items'])==9 and len(a['sold'])==9)
check('chapter_kept_and_new_requests_separate',a['stories']==b['stories']==5 and a['chapterComplete'] and a['forest']['requests']==3)
check('original_facility_counts_kept',len(a['plots'])==len(b['plots']) and len(a['machines'])==len(b['machines']) and len(a['shelves'])==len(b['shelves']))
check('all_discoveries_and_paths_visited',a['forest']['discovered']==7 and a['forest']['visited']==7)
check('all_four_repairs_complete',all(a['forest'][name] for name in ('greenhouse','bridge','vines','pavilion')))
check('four_nursery_slots',len(a['forest']['beds'])==4)
check('three_new_goods_actually_sold',all(n>0 for n in a['sold'][6:]))
check('resin_regathered_normally',a['forest']['gathered'][2]>=6)
rewards=sum([150,220,300][b['forest']['requests']:a['forest']['requests']])
costs=sum(cost for name,cost in zip(('greenhouse','bridge','vines','pavilion'),(800,500,0,1200)) if a['forest'][name] and not b['forest'][name])
expected=b['coins']+a['sales']-b['sales']+rewards-costs
check('exact_coin_conservation',a['coins']==expected)
check('new_request_reputation_only',a['reputation']-b['reputation']==6*(a['forest']['requests']-b['forest']['requests']))
result=dict(scope='Actual ordinary Android progression. Same existing player, normal UI and wall time; no prepared saves activated before this final checkpoint. Forest entry began on forest01, no discoveries there; forest02 continues all new-resource actions.',beforeSha256=bsha,afterSha256=asha,beforeCoins=b['coins'],afterCoins=a['coins'],salesDelta=a['sales']-b['sales'],forestRequestRewards=rewards,forestRepairCosts=costs,computedCoins=expected,newSold=a['sold'][6:],gathered=a['forest']['gathered'],nurseryConfigs=[x['config'] for x in a['forest']['beds']],checks=checks,allPassed=True,wholeFirstReleaseComplete=False,limitations='Not a natural new-game full original chapter or12bed purchase proof; human sound and formal long/abnormal gates remain deferred.')
(folder/'ordinary-expansion-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print(json.dumps(result,ensure_ascii=False,indent=2))
