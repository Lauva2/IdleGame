"""One labelled isolated input for the 0.2.8 quick navigation check.
No natural progression claim. Caller protects and restores both ordinary saves.
"""
import hashlib,json,sys,time
from pathlib import Path
root=Path(__file__).resolve().parents[1]
q=(root/sys.argv[1]).resolve();assert q.is_relative_to(root/'Docs/Verification')
raw=(q/'normal-held.save').read_bytes();head,version,digest,payload=raw.split(b'\n',3)
assert head.strip()==b'FORESTSHOP1' and hashlib.sha256(payload).hexdigest().upper().encode()==digest.strip()
s=json.loads(payload)
assert s['version']==4 and s['stories']==5 and len(s['machines'])==2 and len(s['plots'])>=2
s['report']['pending']=False;s['dialogue']=-1;s['forest']['dialogue']=-1
s['chapterCelebrated']=True;s['forest']['celebrationSeen']=True
s['fishing']['running']=False;s['simpleAnimation']=True
order_id=s['nextOrderId'];s['nextOrderId']+=1
s['orders']=[dict(id=order_id,resident=1,item=1,count=8,story=False,forestStory=False,active=True,reserve=True,cancelled=False)]
s['items'][1]=7;s['crops'][1]=0
s['plots'][1].update(config=1,workItem=1,remaining=29,pending=-2)
for p in s['plots']:
    if p['config']==1 or p['workItem']==1:p.update(config=1,workItem=1,remaining=29,pending=-2)
s['machines'][1].update(config=5,workItem=-1,remaining=-1,pending=-2)
s['shelves'][0].update(config=1,workItem=-1,remaining=2,pending=-2)
s['utcTicks']=time.time_ns()//100+621355968000000000
data=json.dumps(s,ensure_ascii=False,separators=(',',':')).encode('utf8')
wrapped=b'FORESTSHOP1\n1\n'+hashlib.sha256(data).hexdigest().upper().encode()+b'\n'+data
out=q/'Prepared';out.mkdir(exist_ok=True);p=out/'supply-smoke.save';assert not p.exists();p.write_bytes(wrapped)
record=dict(prepared=True,sourceOrdinarySha256=hashlib.sha256(raw).hexdigest().upper(),fixtureSha256=hashlib.sha256(wrapped).hexdigest().upper(),scope=__doc__,changed='Isolated active reserved mushroom-bottle order7/8, missing mushroom/growth29s, non-bottle machine recipe, bottle shelf, simplified presentation and fresh fixture clock. No prepared values restored as ordinary progress.')
(out/'supply-smoke-scope.json').write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print('PREPARED supply-smoke; not ordinary progress')
