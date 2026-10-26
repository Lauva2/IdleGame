"""Audit real ordinary progress before fixtures; never edits a save/device."""
import hashlib,json,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1];q=(root/sys.argv[1]).resolve();assert q.is_relative_to(root/'Docs/Verification')
def read(name):
 raw=(q/name).read_bytes();head,_,digest,payload=raw.split(b'\n',3)
 assert head.strip()==b'FORESTSHOP1' and hashlib.sha256(payload).hexdigest().upper().encode()==digest.strip()
 return json.loads(payload),hashlib.sha256(raw).hexdigest().upper()
def fish_same(a,b):
 copy=dict(a)
 for k in ('remaining','duration'):
  if abs(a[k]-b[k])>1e-10:return False
  copy[k]=b[k]
 return copy==b
before,bsha=read('normal-held.save');after,asha=read('normal-restorable.save')
paused,_=read('ordinary-continuous-end.save');bg,_=read('ordinary-background-report.save')
f=after['fishing'];new=[v for v in f['recent'] if v['sequence']>before['fishing']['totalCaught']]
checks={
 'new_fish_are_real_recorded_foreground_catches':f['totalCaught']>before['fishing']['totalCaught'] and len(new)==f['totalCaught']-before['fishing']['totalCaught'],
 'bounded_recent_ids_are_unique_contiguous':sorted(v['sequence'] for v in f['recent'])==list(range(max(1,f['totalCaught']-29),f['totalCaught']+1)),
 'background_fish_clock_rng_records_unchanged':fish_same(paused['fishing'],bg['fishing']),
 'restorable_retains_paused_fish':fish_same(bg['fishing'],f) and not f['running'],
 'ordinary_settings_facilities_and_original_progress_kept':all(before[k]==after[k] for k in ('stories','reputation','friendship','sideRoom','chapterComplete','chapterCelebrated','warehouse','decoration','music','sound','simpleAnimation')) and all([(v['config'],v['level']) for v in before[k]]==[(v['config'],v['level']) for v in after[k]] for k in ('plots','machines','shelves')),
 'nursery_configuration_kept':[(v['config'],v['level']) for v in before['forest']['beds']]==[(v['config'],v['level']) for v in after['forest']['beds']],
 'forest_repairs_and_gathered_counts_kept':all(before['forest'][k]==after['forest'][k] for k in ('requests','discovered','visited','gathered','greenhouse','bridge','vines','pavilion')),
 'money_only_follows_original_sales':after['coins']-before['coins']==after['sales']-before['sales'],
 'no_pending_report_in_latest_checkpoint':not after['report']['pending'],
 'update_full_replay_passed':'PASS actual Android offline replay:' in (q/'update-offline-replay.txt').read_text(encoding='utf-8-sig'),
 'foreground_full_replay_passed':'PASS actual Android online replay:' in (q/'ordinary-online-replay.txt').read_text(encoding='utf-8-sig'),
 'background_full_replay_passed':'PASS actual Android offline replay:' in (q/'ordinary-background-replay.txt').read_text(encoding='utf-8-sig')}
stats=True
for i,e in enumerate(f['entries']):
 old=before['fishing']['entries'][i];caught=[c for c in new if c['species']==i]
 stats &= e['count']==old['count']+len(caught) and all(e['qualityCounts'][grade]==old['qualityCounts'][grade]+sum(c['quality']==grade for c in caught) for grade in range(4))
 stats &= e['longestMm']==max([old['longestMm']]+[c['lengthMm'] for c in caught]) and e['heaviestGrams']==max([old['heaviestGrams']]+[c['weightGrams'] for c in caught]) and e['bestQuality']==max([old['bestQuality']]+[c['quality'] for c in caught])
 stats &= e['firstSequence']==(old['firstSequence'] if old['count'] else min([c['sequence'] for c in caught],default=0))
checks['all_individual_parameters_quality_totals_and_records_match_new_catches']=stats
result=dict(beforeSha256=bsha,afterSha256=asha,beforeCoins=before['coins'],afterCoins=after['coins'],beforeFish=before['fishing']['totalCaught'],fish=f['totalCaught'],discovered=sum(e['count']>0 for e in f['entries']),salesDelta=after['sales']-before['sales'],checks=checks,allPassed=all(checks.values()),scope='Actual ordinary save, ordinary UI and wall time only, before all prepared evidence. The prior d24-experience01 actual cap income was already claimed and retained as this baseline. Clock-only tolerance1e-10 seconds follows the existing Core verifier; all other fish fields stay exact.')
(q/'ordinary-fishing-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf8');print(json.dumps(result,ensure_ascii=False,indent=2));assert result['allPassed']
