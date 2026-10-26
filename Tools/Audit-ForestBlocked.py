"""Check actual blocked20s snapshots without pretending to replay all clocks."""
import hashlib,json,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1];q=(root/sys.argv[1]).resolve()
assert q.is_relative_to(root/'Docs/Verification')
def read(name):
    raw=(q/name).read_bytes();_,_,digest,payload=raw.split(b'\n',3)
    assert hashlib.sha256(payload).hexdigest().upper().encode()==digest
    return json.loads(payload)
rows=[]
for case in ('blocked-full','blocked-input','blocked-reserve'):
    a=read(case+'-before.save');b=read(case+'-after.save')
    deltas={k:b[k]-a[k] for k in ('harvests','crafts','sales')}
    checks=dict(counters_unchanged=all(v==0 for v in deltas.values()),inventory_unchanged=all(a[k]==b[k] for k in ('crops','items','sold')),configuration_kept=all([(v['config'],v['level']) for v in a[k]]==[(v['config'],v['level']) for v in b[k]] for k in ('plots','machines','shelves')),orders_and_fish_and_moments_kept=all(a[k]==b[k] for k in ('orders','fishing','moments')))
    rows.append(dict(case=case,deltas=deltas,checks=checks))
result=dict(scope='Three actual20-second blocked source clips. Counter/inventory/configuration/order/fish/moment comparison only, not a full interval replay.',cases=rows,allPassed=all(all(r['checks'].values()) for r in rows))
out=q/'negative-counters-audit.json';assert not out.exists(),'Preserve earlier audit'
out.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf8');print(json.dumps(result,ensure_ascii=False,indent=2));assert result['allPassed']
