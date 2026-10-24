"""Compare labelled prepared seeds with real final-APK automatic landings."""
import hashlib,json
from pathlib import Path
root=Path(__file__).resolve().parents[1];qa=root/'Docs/Verification/3d-d21-forest05';rows=[]
for quality in range(4):
    scope=json.loads((qa/f'Prepared/fishing-seed-q{quality}-scope.json').read_text(encoding='utf8'))
    raw=(qa/f'quality-q{quality}-after.save').read_bytes();_,_,digest,payload=raw.split(b'\n',3)
    assert hashlib.sha256(payload).hexdigest().upper().encode()==digest
    s=json.loads(payload);f=s['fishing'];actual=f['recent'][0];expected=scope['expected']['expected']
    ok=f['totalCaught']==1 and len(f['recent'])==1 and actual==expected and actual['quality']==quality and not f['running'] and f['entries'][actual['species']]['qualityCounts'][quality]==1
    rows.append(dict(quality=quality,preparedSeed=scope['expected']['seed'],actual=actual,sha256=hashlib.sha256(raw).hexdigest().upper(),passed=ok))
result=dict(scope='Prepared independent RNG seed only. Each first catch waited/bit/landed automatically through actual APK UI, then opened individual details and paused. Not ordinary progression or natural probability evidence.',samples=rows)
(qa/'prepared-quality-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf8');print(json.dumps(result,ensure_ascii=False,indent=2));assert all(r['passed'] for r in rows)
