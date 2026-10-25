"""Read-only audit of labelled prepared prior records and real APK catches.

Usage: python Tools/Audit-FishingNotices.py <QA-folder> [capture-prefix]
This verifies inputs and catches; the independent reviewer judges visible UI.
"""
import hashlib,json,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1]
qa=(root/sys.argv[1]).resolve();assert qa.is_relative_to(root/'Docs/Verification')
prefix=sys.argv[2] if len(sys.argv)>2 else 'notice'
rows=[]
for kind in ('new','length','weight','both','ordinary'):
    scope=json.loads((qa/f'Prepared/fishing-notice-{kind}-scope.json').read_text(encoding='utf-8-sig'))
    expected=scope['expected'];prior=expected['prior']
    raw=(qa/f'{prefix}-{kind}-after.save').read_bytes()
    _,_,digest,payload=raw.split(b'\n',3)
    assert hashlib.sha256(payload).hexdigest().upper().encode()==digest.strip()
    state=json.loads(payload);f=state['fishing'];actual=f['recent'][0]
    fresh=prior is None
    length=fresh or actual['lengthMm']>prior['lengthMm']
    weight=fresh or actual['weightGrams']>prior['weightGrams']
    derived='new' if fresh else 'both' if length and weight else 'length' if length else 'weight' if weight else 'ordinary'
    e=f['entries'][actual['species']]
    checks=dict(actual_catch_equals_prepared_seed=actual==expected['expectedCatch'],exactly_one_real_new_catch=f['totalCaught']==(1 if fresh else 2),paused_on_opening_individual=not f['running'],expected_notice_from_independent_prior=derived==kind,record_length_correct=e['longestMm']==max(actual['lengthMm'],0 if fresh else prior['lengthMm']),record_weight_correct=e['heaviestGrams']==max(actual['weightGrams'],0 if fresh else prior['weightGrams']),quality_count_preserved=e['qualityCounts'][0]==(1 if fresh else 2))
    rows.append(dict(kind=kind,preparedSeed=expected['seed'],prior=prior,actual=actual,sha256=hashlib.sha256(raw).hexdigest().upper(),checks=checks))
result=dict(scope='Explicit artificial prior records/RNG seed, followed by one actual automatic APK catch. Excluded from ordinary progress and natural probability evidence. Visible notice text must also be inspected in untouched native screenshots/video.',capturePrefix=prefix,samples=rows,allPassed=all(all(r['checks'].values()) for r in rows))
(qa/'prepared-notice-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print(json.dumps(result,ensure_ascii=False,indent=2));assert result['allPassed']
