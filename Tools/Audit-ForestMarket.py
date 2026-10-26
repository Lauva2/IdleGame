"""Summarize a bounded same-process actual market capture, preserving all CSV rows."""
import csv,datetime as dt,hashlib,json,re,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1];qa=(root/sys.argv[1]).resolve()
assert qa.is_relative_to(root/'Docs/Verification')
def stamp(path):return dt.datetime.fromisoformat(path.read_text(encoding='utf-8-sig').strip().replace('Z','+00:00'))
def read(name):
 raw=(qa/name).read_bytes();_,_,digest,payload=raw.split(b'\n',3)
 assert hashlib.sha256(payload).hexdigest().upper().encode()==digest.strip()
 return json.loads(payload)
start=stamp(qa/'market-activation-utc.txt');end=stamp(qa/'market-end.txt')
p=(qa/'market-start-pid.txt').read_text(encoding='utf-8-sig').strip()
assert p==(qa/'market-end-pid.txt').read_text(encoding='utf-8-sig').strip()
rows=[]
for x in csv.reader((qa/'performance-market.csv').read_text(encoding='utf-8-sig').splitlines()):
 if not x:continue
 moment=dt.datetime.fromisoformat(x[0].replace('Z','+00:00'))
 row=dict(utc=x[0],frames=int(x[1]),seconds=float(x[2]),averageFPS=float(x[3]),p95Milliseconds=float(x[4]),unityAllocatedBytes=int(x[5]),maximumMilliseconds=float(x[6]),belongsToCapturedProcessInterval=start<=moment<=end)
 rows.append(row)
current=[r for r in rows if r['belongsToCapturedProcessInterval']]
before=read('market-before.save');after=read('market-after.save')
memory=(qa/'meminfo-market.txt').read_text(encoding='utf-8-sig');pss=re.search(r'TOTAL PSS:\s*(\d+)',memory)
log=(qa/'logcat-market.txt').read_text(encoding='utf-8-sig')
errors=re.findall(r'^.*(?:FATAL EXCEPTION|Fatal signal|ANR in |Save failed|NullReferenceException|OutOfMemoryException).*$',log,re.M)
segmented='--segmented-startup' in sys.argv
scope='Actual same-PID MuMu market recordings, all historical CSV rows retained. '+('The300s collector starts after booting clears, excluding initialization/loading but still including initial pages/menus; separate uncut startup media/logs retain those loading costs. ' if segmented else 'First300s sample includes startup/initial UI; ')+'No stable-only or formal30min/physical-phone claim. Single PSS snapshot is not a memory trend.'
r=dict(scope=scope,startupLoadingExcluded=segmented,pid=int(p),actualOnlineSeconds=after['playedSeconds']-before['playedSeconds'],coinDelta=after['coins']-before['coins'],salesDelta=after['sales']-before['sales'],newProductsSold=[after['sold'][i]-before['sold'][i] for i in range(6,9)],allCsvRows=rows,currentRows=current,pssKilobytes=int(pss[1]) if pss else None,fatalOrSaveErrorMatches=errors,hasFiveMinuteSample=any(x['seconds']>=299 for x in current))
(qa/'market-audit.json').write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n',encoding='utf8');print(json.dumps({k:v for k,v in r.items() if k!='allCsvRows'},ensure_ascii=False,indent=2))
assert r['hasFiveMinuteSample'] and r['coinDelta']==r['salesDelta'] and all(n>0 for n in r['newProductsSold']) and not errors
