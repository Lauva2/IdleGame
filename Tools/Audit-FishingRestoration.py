"""Read-only final normal-save and checkpoint restoration audit."""
import hashlib,json,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1];q=(root/sys.argv[1]).resolve();assert q.is_relative_to(root/'Docs/Verification')
def read(name):
 raw=(q/name).read_bytes();header,_,sha,payload=raw.split(b'\n',3)
 assert header.strip()==b'FORESTSHOP1' and hashlib.sha256(payload).hexdigest().upper().encode()==sha.strip()
 return json.loads(payload),hashlib.sha256(raw).hexdigest().upper()
start,_=read('normal-restorable.save');before,bsha=read('normal-before-region-fit.save');report,_=read('region-fit-restore-report.save');final,fsha=read('ordinary-after-region-fit-final.save')
identity=json.loads((q/'ordinary-region-checkpoint-identity.json').read_text(encoding='utf-8-sig'))
device=(q/'region-fit-restored-device-sha256.txt').read_text(encoding='utf-8-sig').splitlines()
hashes=[line.split()[0].upper() for line in device if line.strip()]
checks=dict(latest_primary_and_backup_restored_exactly=hashes==[f['sha256'] for f in identity['files']],latest_primary_matches_checkpoint=bsha==identity['files'][0]['sha256'],all_fish_records_unchanged=start['fishing']==before['fishing']==report['fishing']==final['fishing'],normal_catch_total_kept=final['fishing']['totalCaught']==39,normal_species_kept=sum(e['count']>0 for e in final['fishing']['entries'])==6,no_pending_report_after_claim=not final['report']['pending'],fishing_paused=not final['fishing']['running'],chapter_and_forest_progress_kept=all(final[k]==start[k] for k in ['stories','reputation','friendship','sideRoom','chapterComplete','chapterCelebrated']) and all(final['forest'][k]==start['forest'][k] for k in ['requests','discovered','visited','gathered','greenhouse','bridge','vines','pavilion']),ordinary_configuration_kept=all(final[k]==before[k] for k in ['warehouse','decoration','music','sound','simpleAnimation']) and all([(f['level'],f['config']) for f in final[k]]==[(f['level'],f['config']) for f in before[k]] for k in ['plots','machines','shelves']),coins_only_follow_original_sales=final['coins']-start['coins']==final['sales']-start['sales'],latest_checkpoint_not_rolled_back=final['coins']>=before['coins']>=966230)
r=dict(scope='Latest ordinary pair protected after prior restorations and real foreground production; all fixture assets excluded. Full offline state replay is separately recorded; this audit checks final identity, fish and progression preservation.',latestCheckpointSha256=bsha,finalSaveSha256=fsha,coins=final['coins'],fish=39,discovered=6,plots=len(final['plots']),shelves=len(final['shelves']),latestAwaySeconds=report['report']['away'],latestOfflineCoins=report['report']['coins'],checks=checks,allPassed=all(checks.values()))
(q/'ordinary-final-audit.json').write_text(json.dumps(r,ensure_ascii=False,indent=2)+'\n',encoding='utf8');print(json.dumps(r,ensure_ascii=False,indent=2));assert r['allPassed']
