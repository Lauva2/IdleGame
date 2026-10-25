"""User-authorized cleanup of untracked decoded frames with retained original MP4s and indexes."""
import argparse,csv,hashlib,importlib.util,json,os,re,shutil
from datetime import datetime,timezone
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
AUDIT=ROOT/'Docs/Maintenance/2026-10-25-storage-cleanup'
spec=importlib.util.spec_from_file_location('archive_cache',ROOT/'Tools/Archive-EvidenceCache.py')
cache=importlib.util.module_from_spec(spec);spec.loader.exec_module(cache)
def sha(p):return cache.sha(p)
def resolve_row(qa,text):
    p=Path(text)
    return (p if p.is_absolute() else qa/p).resolve()
def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--execute',action='store_true');args=parser.parse_args()
    if (AUDIT/'frame-cleanup-result.json').exists():
        raise RuntimeError('This dated cleanup is complete; preserve its audit and use a new audit directory for another run')
    approval=cache.read_json(AUDIT/'frame-retention-approval.json')
    if not approval.get('confirmed') or approval.get('source')!='user':raise RuntimeError('Explicit user retention-policy change required')
    tracked=cache.git_tracked();mapping={};movie_hashes={};indexes={}
    for qa in (ROOT/'Docs/Verification').iterdir():
        if not qa.is_dir():continue
        media=qa/'media-index.csv'
        if media.is_file():
            with media.open(encoding='utf-8-sig',newline='') as f:
                for r in csv.DictReader(f):
                    if not r.get('path') or not re.fullmatch(r'[A-Fa-f0-9]{64}',r.get('sha256','')):continue
                    p=resolve_row(qa,r['path'])
                    if p.suffix.lower()=='.mp4' and p.is_file():movie_hashes.setdefault(str(p).casefold(),set()).add(r['sha256'].upper())
        index=qa/'frame-index.csv'
        if not index.is_file():continue
        indexes[str(index)]=sha(index)
        with index.open(encoding='utf-8-sig',newline='') as f:
            for r in csv.DictReader(f):
                if not r.get('path') or not r.get('sourceVideo'):continue
                frame=resolve_row(qa,r['path']);text=Path(r['sourceVideo'])
                choices=[text] if text.is_absolute() else [qa/text,qa/'Evidence'/text]
                movie=next((p.resolve() for p in choices if p.is_file() and p.suffix.lower()=='.mp4'),None)
                if movie is None:continue
                entry=mapping.setdefault(str(frame.parent).casefold(),dict(movies=set(),files={},indexes=set()))
                entry['movies'].add(str(movie));entry['files'][frame.name]=r;entry['indexes'].add(str(index))
    jobs=[];held=[]
    for job in cache.candidates(tracked):
        d=Path(job['source']);entry=mapping.get(str(d).casefold())
        images=[p for p in d.iterdir() if p.suffix.lower() in ('.png','.webp','.jpg','.jpeg')]
        if entry is None or any(p.name not in entry['files'] for p in images):held.append(dict(**job,reason='No complete frame-to-original mapping'));continue
        if any(str(Path(m)).casefold() not in movie_hashes for m in entry['movies']):held.append(dict(**job,reason='Original movie SHA not indexed'));continue
        jobs.append(dict(**job,movies=sorted(entry['movies']),frameIndexes=sorted(entry['indexes'])))
    originals={m for j in jobs for m in j['movies']};proof=[];bad=set()
    for i,m in enumerate(sorted(originals),1):
        actual=sha(Path(m));expected=movie_hashes[m.casefold()];passed=expected=={actual}
        proof.append(dict(path=m,bytes=Path(m).stat().st_size,sha256=actual,indexMatched=passed))
        if not passed:bad.add(m)
        if i%20==0:print('VERIFIED original movies '+str(i)+'/'+str(len(originals)),flush=True)
    safe=[]
    for j in jobs:
        if any(m in bad for m in j['movies']):held.append(dict(**j,reason='Original movie identity mismatch'))
        else:safe.append(j)
    plan=dict(utc=datetime.now(timezone.utc).isoformat(),scope='Only decoded-frame caches, with explicit user approval and verified existing original movie SHA. Original MP4, native screenshots, source, APKs, saves, reports and existing indexes retained.',directories=safe,held=held,originalMovies=proof,frameIndexHashes=indexes)
    cache.write_json(AUDIT/'frame-cleanup-plan.json',plan)
    print(json.dumps(dict(eligibleDirectories=len(safe),logicalBytes=sum(j['bytes'] for j in safe),originalMovies=len(proof),held=len(held),execute=args.execute)),flush=True)
    if not args.execute:return
    before={d:shutil.disk_usage(d+':/').free for d in ['C','E']};removed=[]
    for j in safe:
        d=cache.checked(Path(j['source']));items=list(d.iterdir());entry=mapping[str(d).casefold()]
        if len(items)!=j['files'] or any(cache.link(p) or not p.is_file() or str(p.absolute()).casefold() in tracked for p in items):raise RuntimeError('Target changed; refuse removal')
        for p in items:
            if p.suffix.lower() in ('.png','.webp','.jpg','.jpeg'):
                r=entry['files'][p.name]
                if r.get('bytes') and p.stat().st_size!=int(r['bytes']):raise RuntimeError('Frame changed after indexing')
            else:
                # Preserve small original decode/timing metadata beside the
                # cleanup record. Image cache bytes alone are disposable.
                target=AUDIT/'frame-metadata'/hashlib.sha256(str(d).encode()).hexdigest()[:16]/p.name
                target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(p,target)
                if sha(target)!=sha(p):raise RuntimeError('Metadata copy mismatch')
        cache.checked(d);shutil.rmtree(d);removed.append(j)
        if len(removed)%10==0:print('REMOVED frame directories '+str(len(removed))+'/'+str(len(safe)),flush=True)
    for m in proof:
        if not Path(m['path']).is_file() or sha(Path(m['path']))!=m['sha256']:raise RuntimeError('Original movie changed')
    for path,digest in indexes.items():
        if sha(Path(path))!=digest:raise RuntimeError('Original index changed')
    result=dict(utc=datetime.now(timezone.utc).isoformat(),scope=plan['scope'],removedDirectories=len(removed),removedFrameCacheFiles=sum(j['files'] for j in removed),logicalFileBytes=sum(j['bytes'] for j in removed),freeBefore=before,freeAfter={d:shutil.disk_usage(d+':/').free for d in ['C','E']},originalMoviesVerifiedBeforeAndAfter=len(proof),allOriginalMovieHashesUnchanged=True,allOriginalIndexesUnchanged=True,heldDirectories=len(held),note='Freed volume bytes differ from logical file sizes due to pre-existing hard links and concurrent OS writes. Historical reports/indexes remain frozen; sampled-frame paths are no longer live and can be re-exported from retained sources.')
    cache.write_json(AUDIT/'frame-cleanup-result.json',result);print(json.dumps(result,indent=2),flush=True)
if __name__=='__main__':main()
