"""Labelled isolated second-wave art states. Never ordinary progression.
The caller must preserve both live saves before activation and restore the
newest legitimate pair after review. Validator runs before any device push.
"""
import json, hashlib, sys, time
from pathlib import Path
root=Path(__file__).resolve().parents[1]
folder=(root/sys.argv[1]).resolve(); assert folder.is_relative_to(root/'Docs/Verification')
kind=sys.argv[2]
assert kind in ('forest-entry','forest-edge','forest-creek','forest-grove','forest-growth','forest-full','forest-cooling','forest-ending','forest-report','forest-simple','forest-pavilion','forest-visits','forest-vines','forest-dialogue0','forest-dialogue1','forest-dialogue2')
for name in ('normal-held.save','normal-held.save.bak'): assert (folder/name).is_file(), 'Both ordinary saves must be protected'
wrapper=(folder/'normal-held.save').read_bytes().split(b'\n',3)
assert hashlib.sha256(wrapper[3]).hexdigest().upper().encode()==wrapper[2]
s=json.loads(wrapper[3])
f=lambda c=-1,w=-1,r=-1:dict(level=1,config=c,pending=-2,workItem=w,remaining=r)
forest=dict(started=True,greenhouse=True,bridge=True,vines=True,pavilion=False,celebrationSeen=False,discovered=7,visited=7,location=0,requests=2,dialogue=-1,beds=[f(4,4,25),f(4,4,0),f(5,5,45),f(5,5,0)],available=[3]*3,gathered=[6,3,3],regrow=[0]*3)
s.update(version=2,coins=25000,reputation=180,sales=60000,harvests=140,crafts=160,sold=[20]*9,crops=[30]*7,items=[24]*9,friendship=[60]*5,warehouse=4,plots=[f(i%4,i%4,(i%4)*4) for i in range(12)],machines=[f(6,6,20),f(7,7,25)],shelves=[f(6,r=8),f(7,r=8),f(8,r=8),f(5,r=8)],orders=[],stories=5,tutorial=4,welcomeRead=True,sideRoom=True,chapterComplete=True,chapterCelebrated=True,dialogue=-1,decoration=[True]*4,simpleAnimation=False,utcTicks=time.time_ns()//100+621355968000000000,refreshSeconds=0,nextOrderId=301,random=9527,playedSeconds=0,forest=forest,report=dict(pending=False,away=0,effective=0,coins=0,cropDelta=[0]*7,itemDelta=[0]*9,hint=''))
if kind in ('forest-entry','forest-edge'):
    forest.update(started=kind=='forest-edge',greenhouse=False,bridge=False,vines=False,discovered=0,visited=1 if kind=='forest-edge' else 0,requests=0,beds=[],gathered=[0]*3)
    s.update(crops=[30]*4+[0]*3,items=[24]*6+[0]*3,sold=[20]*6+[0]*3,machines=[f(4,4,12),f(5,5,25)],shelves=[f(0,r=8),f(1,r=8),f(4,r=8),f(5,r=8)])
elif kind=='forest-creek': forest['location']=1
elif kind in ('forest-grove','forest-pavilion'): forest['location']=2
elif kind=='forest-ending': forest.update(pavilion=True,requests=3,celebrationSeen=True,location=2)
elif kind=='forest-full':
    s['crops']=[160]*7
    forest['beds']=[f(4,4,0),f(4,4,0),f(5,5,0),f(5,5,0)]
    s['machines']=[f(),f()]
elif kind=='forest-cooling': forest.update(available=[0]*3,regrow=[90,120,180])
elif kind=='forest-report': s['report']=dict(pending=True,away=36000,effective=28800,coins=8256,cropDelta=[-2,4,8,0,8,12,0],itemDelta=[5,7,0,0,1,3,6,4,2],hint='温室继续按当前配置培育。\n森林恢复了采集余量，未自动采集。\n部分原料库存已满，生产等待。')
elif kind=='forest-simple': s['simpleAnimation']=True
elif kind=='forest-visits': s.update(crops=[60]*7,items=[60]*9,machines=[f(6,6,20),f(8,8,25)])
elif kind=='forest-vines':
    forest.update(vines=False,discovered=3,visited=3,location=1,gathered=[6,3,0])
    s['shelves'][2]['config']=2
elif kind.startswith('forest-dialogue'):
    story=int(kind[-1]); forest.update(dialogue=story,requests=max(2,story+1))
if kind=='forest-pavilion': forest['requests']=3
payload=json.dumps(s,ensure_ascii=False,separators=(',',':')).encode('utf8')
out=folder/'Prepared'; out.mkdir(exist_ok=True)
(out/(kind+'.save')).write_bytes(b'FORESTSHOP1\n1\n'+hashlib.sha256(payload).hexdigest().upper().encode()+b'\n'+payload)
(out/'FOREST-SCOPE.txt').write_text('PREPARED SECOND-WAVE ART STATES. Inventories, twelve beds, unlocked species/paths, requests and late repair states are explicitly prepared. These are not ordinary progression or actual offline earnings. Real UI, production, transitions and gestures must be observed in the installed APK. Restore newest documented normal-restorable main and backup after testing.\n',encoding='utf8')
print(out/(kind+'.save'))
