"""Create labelled, isolated art fixtures, never normal player progress.
Usage: python Tools/Prepare-ForestReview.py <verification-folder> <variant>
Caller must stop the app, preserve both ordinary saves, and restore after review.
"""
import json, hashlib, sys, copy, time
from pathlib import Path
root=Path(__file__).resolve().parents[1]
folder=(root/sys.argv[1]).resolve(); assert folder.is_relative_to(root/'Docs/Verification')
kind=sys.argv[2]; assert kind in ('late','visits','initial','unrestored','states','chapter','report','dialogue0','dialogue1','dialogue2','dialogue3','dialogue4','blocked-full','blocked-input','blocked-reserve','busy','simple','resume')
lines=(folder/'normal-held.save').read_bytes().split(b'\n',3)
assert hashlib.sha256(lines[3]).hexdigest().upper().encode()==lines[2]
s=json.loads(lines[3]); f=lambda c=-1,w=-1,r=-1:dict(level=1,config=c,pending=-2,workItem=w,remaining=r)
s.update(version=1,forest=None)  # Explicit legacy-shape fixture; real loader must migrate it.
s.update(coins=25000,reputation=150,sales=60000,harvests=120,crafts=120,sold=[20]*6,crops=[30]*4,items=[24]*6,friendship=[60]*5,warehouse=3,plots=[f(i%4,i%4,(i%4)*4) for i in range(12)],machines=[f(4,4,12),f(5,5,25)],shelves=[f(0,r=8),f(1,r=8),f(4,r=8),f(5,r=8)],orders=[dict(id=101,resident=2,item=5,count=7,story=False,active=False,reserve=True,cancelled=False),dict(id=102,resident=1,item=3,count=7,story=False,active=False,reserve=True,cancelled=False),dict(id=103,resident=4,item=0,count=8,story=False,active=False,reserve=True,cancelled=False)],stories=5,tutorial=4,welcomeRead=True,sideRoom=True,chapterComplete=True,chapterCelebrated=True,dialogue=-1,decoration=[True]*4,simpleAnimation=False,utcTicks=time.time_ns()//100+621355968000000000,refreshSeconds=0,nextOrderId=104,random=9527,playedSeconds=0,report=dict(pending=False,away=0,effective=0,coins=0,cropDelta=[0]*4,itemDelta=[0]*6,hint=''))
if kind=='visits':
    s['items']=[60]*6  # Below capacity80 including incoming products, stocked through five-body recording.
elif kind=='busy':
    s.update(plots=[f(0,0,0) for i in range(12)],machines=[f(0,0,15),f(1,1,20)])
elif kind=='simple': s['simpleAnimation']=True
elif kind=='resume':
    s.update(crops=[0]*4,items=[0]*6,plots=[f(i%2,i%2,20 if i%2==0 else 30) for i in range(12)],machines=[f(0),f(1)],shelves=[f(r=8) for i in range(4)])
elif kind=='blocked-full':
    s.update(crops=[80]*4,items=[80]*6,plots=[f(i%4,i%4,0) for i in range(12)],machines=[f(4),f(5)],shelves=[f(r=8) for i in range(4)])
elif kind in ('blocked-input','blocked-reserve'):
    s.update(crops=[0]*4,items=[0]*6,plots=[f(3,3,50) for i in range(12)],machines=[f(4),f(5)],shelves=[f(1,r=8) for i in range(4)])
    if kind=='blocked-reserve':
        s['items'][1]=7; s['orders'][0].update(item=1,count=7,active=True,reserve=True)
elif kind=='initial':
    s.update(coins=50,reputation=0,sales=0,harvests=0,crafts=0,sold=[0]*6,crops=[0]*4,items=[0]*6,friendship=[0]*5,warehouse=1,plots=[f(0,0,0)],machines=[f()],shelves=[f(r=8)],orders=[],stories=0,tutorial=0,welcomeRead=False,sideRoom=False,chapterComplete=False,chapterCelebrated=False,decoration=[False]*4,nextOrderId=1)
elif kind=='unrestored':
    s.update(sideRoom=False,chapterComplete=False,chapterCelebrated=False,decoration=[False]*4)
    s['plots']=s['plots'][:4]
elif kind=='states':
    s['shelves'][2]['config']=2; s['shelves'][3]['config']=3
    s['items'][0]=0; s['items'][1]=7; s['orders'][0].update(item=1,count=8,active=True)
elif kind=='chapter': s['chapterCelebrated']=False
elif kind=='report': s['report']=dict(pending=True,away=36000,effective=28800,coins=8256,cropDelta=[-2,4,8,0],itemDelta=[5,7,0,0,1,3],hint='委托商品已预留。\n部分原料库存已满，生产等待。')
elif kind.startswith('dialogue'): s['dialogue']=int(kind[-1])
payload=json.dumps(s,ensure_ascii=False,separators=(',',':')).encode('utf8')
wrapper=b'FORESTSHOP1\n1\n'+hashlib.sha256(payload).hexdigest().upper().encode()+b'\n'+payload
out=folder/'Prepared'; out.mkdir(exist_ok=True)
(out/(kind+'.save')).write_bytes(wrapper)
(out/'SCOPE.txt').write_text('PREPARED ART FIXTURES, NOT NORMAL PROGRESSION. Twelve beds, stocked shelves, relationships, orders, chapter, report and dialogue are explicitly prepared. normal-held.save(.bak) preserves the initial ordinary update baseline; restore the NEWEST ordinary main+backup pair documented in the candidate submission, e.g. normal-restorable.save(.bak), rather than rolling back later legitimate income. All screens and dynamics are from the actual non-development APK; fixtures do not prove ordinary progression or offline earnings.\n',encoding='utf8')
print(out/(kind+'.save'))
