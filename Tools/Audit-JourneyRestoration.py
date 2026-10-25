"""Read-only audit of the latest ordinary pair restored after d23 fixtures."""
import hashlib,json,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1];q=(root/sys.argv[1]).resolve()
assert q.is_relative_to(root/'Docs/Verification')
def read(name):
    raw=(q/name).read_bytes();head,_,digest,payload=raw.split(b'\n',3)
    assert head.strip()==b'FORESTSHOP1' and hashlib.sha256(payload).hexdigest().upper().encode()==digest.strip()
    return json.loads(payload),hashlib.sha256(raw).hexdigest().upper()
start,sha=read('normal-restorable.save');backup,bsha=read('normal-restorable.save.bak')
report,_=read('ordinary-final-report.save');final,fsha=read('ordinary-final.save')
identity=json.loads((q/'ordinary-restorable-identity.json').read_text(encoding='utf-8-sig'))
audit=json.loads((q/'ordinary-fishing-audit.json').read_text(encoding='utf-8-sig'))
device=[line.split()[0].upper() for line in (q/'ordinary-restored-device-sha256.txt').read_text(encoding='utf-8-sig').splitlines() if line.strip()]
def same_paused_fish(a,b):
    # Android/C# double serialization may round the final mantissa digit.
    # Match the existing online verifier's 1e-10-second clock tolerance; every
    # other fish field, candidate and record still requires exact equality.
    if abs(a['remaining']-b['remaining'])>1e-10:return False
    copy=dict(a);copy['remaining']=b['remaining'];return copy==b
checks=dict(
    latest_documented_primary_and_backup_restored_exactly=device==[sha,bsha]==[f['sha256'] for f in identity['files']],
    protected_primary_is_latest_audited_ordinary=sha==audit['afterSha256'],
    all_fish_records_and_paused_clock_unchanged=same_paused_fish(start['fishing'],report['fishing']) and same_paused_fish(report['fishing'],final['fishing']),
    normal_catches_and_species_kept=final['fishing']['totalCaught']==audit['fish'] and sum(e['count']>0 for e in final['fishing']['entries'])==audit['discovered'],
    no_pending_report_after_actual_claim=not final['report']['pending'],
    fishing_paused=not final['fishing']['running'],
    original_chapter_and_forest_progress_kept=all(final[k]==start[k] for k in ['stories','reputation','friendship','sideRoom','chapterComplete','chapterCelebrated']) and all(final['forest'][k]==start['forest'][k] for k in ['requests','discovered','visited','gathered','greenhouse','bridge','vines','pavilion']),
    ordinary_configuration_kept=all(final[k]==start[k] for k in ['warehouse','decoration','music','sound','simpleAnimation']) and all([(f['level'],f['config']) for f in final[k]]==[(f['level'],f['config']) for f in start[k]] for k in ['plots','machines','shelves']),
    money_only_follows_original_sales=final['coins']-start['coins']==final['sales']-start['sales'],
    protected_progress_not_rolled_back=final['coins']>=start['coins'] and final['sales']>=start['sales'],
    full_offline_replay_passed=(q/'ordinary-final-offline-replay.txt').read_text(encoding='utf-8-sig').startswith('PASS actual Android offline replay:'))
result=dict(scope='Latest actual ordinary primary/backup, after all isolated fixtures; no prepared resources or fish are restored. Snapshot money continues to grow during normal foreground operation.',checkpointSha256=sha,backupSha256=bsha,finalSaveSha256=fsha,coins=final['coins'],sales=final['sales'],fish=final['fishing']['totalCaught'],discovered=sum(e['count']>0 for e in final['fishing']['entries']),plots=len(final['plots']),shelves=len(final['shelves']),awaySeconds=report['report']['away'],offlineCoins=report['report']['coins'],pausedClockRoundtripErrorSeconds=abs(start['fishing']['remaining']-final['fishing']['remaining']),pausedClockToleranceSeconds=1e-10,checks=checks,allPassed=all(checks.values()))
(q/'ordinary-final-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(json.dumps(result,ensure_ascii=False,indent=2));assert result['allPassed']
