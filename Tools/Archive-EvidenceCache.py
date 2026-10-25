"""Losslessly archive local decoded-frame caches, verify every byte, then remove expanded copies.

Original videos, native screenshots, tracked files, APKs and reports are never cleanup targets.
Use --execute --drive C/E to archive; --restore ID restores the original directory and hashes.
"""
import argparse,hashlib,json,os,re,shutil,subprocess,sys,time,tarfile
from datetime import datetime,timezone
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
SIDE=Path('C:/Users/Lauva/.codex/visualizations/2026/10/22/01a16331-da0c-7212-86b6-d3ab1a2524e9')
AUDIT=ROOT/'Docs/Maintenance/2026-10-25-storage-cleanup'
SEVEN=ROOT/'Tools/SevenZip/7z.exe'
TAR=Path('C:/Windows/System32/tar.exe')
SEARCH=[ROOT/'Builds/Analysis',ROOT/'Docs/Verification',SIDE]
ALLOWED=[ROOT.resolve(),SIDE.resolve()]
FLAGS=0x08000000 if os.name=='nt' else 0
FRAME=re.compile(r'(?:frame[-_]?\d{3,}|\d{4,8})\.(?:png|webp|jpe?g)$',re.I)

def sha(path):
    h=hashlib.sha256()
    with path.open('rb') as f:
        for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
    return h.hexdigest().upper()

def write_json(path,value):
    path.parent.mkdir(parents=True,exist_ok=True)
    temp=path.with_name(path.name+'.tmp')
    temp.write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    os.replace(temp,path)

def read_json(path):return json.loads(path.read_text(encoding='utf-8-sig'))
def link(path):return bool(getattr(path.lstat(),'st_file_attributes',0)&0x400)

def checked(path):
    lexical=Path(os.path.abspath(path));resolved=path.resolve()
    if lexical!=resolved or not any(resolved!=r and resolved.is_relative_to(r) for r in ALLOWED):
        raise RuntimeError('Target is not an exact ordinary directory inside authorized roots: '+str(path))
    if link(path):raise RuntimeError('Never recursively remove a reparse point: '+str(path))
    return resolved

def git_tracked():
    raw=subprocess.check_output(['git','-c','safe.directory='+ROOT.as_posix(),'ls-files','-z'],cwd=ROOT)
    return {str((ROOT/p.decode('utf-8')).absolute()).casefold() for p in raw.split(b'\0') if p}

def candidates(tracked):
    jobs=[];seen=set()
    for base in SEARCH:
        todo=[base]
        while todo:
            d=todo.pop()
            if not d.is_dir() or link(d):continue
            if 'evidence-cache-archives' in str(d).casefold():continue
            try:entries=list(d.iterdir())
            except OSError:continue
            children=[p for p in entries if p.is_dir() and not link(p)]
            todo.extend(children)
            if children or any(link(p) or not p.is_file() for p in entries):continue
            images=[p for p in entries if p.suffix.lower() in ('.png','.webp','.jpg','.jpeg')]
            if len(images)<10 or not all(FRAME.fullmatch(p.name) for p in images):continue
            if any(p.suffix.lower() not in ('.png','.webp','.jpg','.jpeg','.json','.csv','.txt','.jsonl') for p in entries):continue
            if any(str(p.absolute()).casefold() in tracked for p in entries):continue
            source=checked(d)
            if source in seen:continue
            seen.add(source);jobs.append(dict(source=str(source),bytes=sum(p.stat().st_size for p in entries),files=len(entries),frames=len(images)))
    return sorted(jobs,key=lambda r:r['bytes'])

def run(args,**kwargs):
    return subprocess.run([str(SEVEN),*map(str,args)],creationflags=FLAGS,check=True,**kwargs)

def listing(archive):
    if str(archive).endswith('.tar.zst'):
        text=subprocess.check_output([str(TAR),'-tf',str(archive)],creationflags=FLAGS).decode('utf-8')
        return [(name.replace('\\','/'),None) for name in text.splitlines() if name and not name.endswith('/')]
    text=run(['l','-slt','-sccUTF-8',archive],stdout=subprocess.PIPE,stderr=subprocess.PIPE).stdout.decode('utf-8',errors='strict')
    records=[]
    for block in re.split(r'\r?\n\s*\r?\n',text):
        record=dict(line.split(' = ',1) for line in block.splitlines() if ' = ' in line)
        if 'Path' in record and 'Size' in record and not record.get('Attributes','').startswith('D') and record.get('Folder')!='+':
            records.append((record['Path'].replace('\\','/'),int(record['Size'])))
    return records

def verify_archive(archive,expected,log):
    records=listing(archive)
    if len(records)!=len(expected) or {name for name,_ in records}!=set(expected):
        raise RuntimeError('Archive entry names differ; original directory retained')
    with log.open('wb') as errors:
        command=[str(TAR),'-xOf',str(archive)] if str(archive).endswith('.tar.zst') else [str(SEVEN),'x','-so','-y','-bso0','-bsp0',str(archive)]
        process=subprocess.Popen(command,stdout=subprocess.PIPE,stderr=errors,creationflags=FLAGS)
        try:
            for name,size in records:
                if size is None:size=expected[name]['bytes']
                if size!=expected[name]['bytes']:raise RuntimeError('Archived length mismatch: '+name)
                remaining=size;h=hashlib.sha256()
                while remaining:
                    b=process.stdout.read(min(1024*1024,remaining))
                    if not b:raise RuntimeError('Incomplete archive extraction stream')
                    h.update(b);remaining-=len(b)
                if h.hexdigest().upper()!=expected[name]['sha256']:raise RuntimeError('Archived byte SHA mismatch: '+name)
            if process.stdout.read(1):raise RuntimeError('Unexpected archive data after all entries')
            if process.wait()!=0:raise RuntimeError('Archive CRC/decompression verification failed')
        except BaseException:
            process.kill();process.wait();raise

def source_matches(source,files):
    checked(source)
    actual=list(source.iterdir())
    if {p.name for p in actual}!={r['name'] for r in files}:raise RuntimeError('Source membership changed')
    for row in files:
        p=source/row['name']
        if link(p) or not p.is_file() or p.stat().st_size!=row['bytes'] or sha(p)!=row['sha256']:
            raise RuntimeError('Source changed during archival: '+str(p))

def archive_job(job,drive,catalog,tracked):
    source=checked(Path(job['source']))
    archive_base=(SIDE/'evidence-cache-archives') if drive=='C' else ROOT/'Builds/Archives/evidence-cache-archives'
    archive_base.mkdir(parents=True,exist_ok=True)
    ident=hashlib.sha256(str(source).casefold().encode()).hexdigest()[:16]
    old=next((r for r in catalog['archives'] if r['id']==ident),None)
    archive=Path(old['archive']) if old else archive_base/(ident+'.tar.zst')
    manifest_file=archive_base/(ident+'.manifest.json')
    temp=archive_base/(ident+'.partial.tar.zst');log=archive_base/(ident+'.verify.log')
    start=time.monotonic();before=shutil.disk_usage(source).free
    if archive.exists():
        if old is None or sha(archive)!=old['archiveSha256'] or sha(manifest_file)!=old['manifestSha256']:
            raise RuntimeError('Existing archive identity unknown; keep original')
        manifest=read_json(manifest_file);source_matches(source,manifest['files'])
    else:
        if temp.exists() or manifest_file.exists():raise RuntimeError('Keep prior incomplete archive output: '+ident)
        files=[dict(name=p.name,bytes=p.stat().st_size,sha256=sha(p)) for p in sorted(source.iterdir())]
        manifest=dict(id=ident,sourceDirectory=str(source),createdUtc=datetime.now(timezone.utc).isoformat(),scope='Exact original file bytes, including original completion/partial metadata. No claim that previously incomplete evidence became complete.',files=files)
        write_json(manifest_file,manifest)
        with (archive_base/(ident+'.archive.log')).open('wb') as output:
            process=subprocess.Popen([str(TAR),'--zstd','--options','zstd:compression-level=3','-cf',str(temp),'@-'],stdin=subprocess.PIPE,stdout=output,stderr=output,creationflags=FLAGS)
            try:
                # Explicit real-file TAR records preserve byte hashes for NTFS
                # hard links as well; no symbolic-link traversal or image editing.
                with tarfile.open(fileobj=process.stdin,mode='w|',dereference=True,format=tarfile.PAX_FORMAT) as stream:
                    for row in files:stream.add(source/row['name'],arcname=source.name+'/'+row['name'],recursive=False)
                    stream.add(manifest_file,arcname=manifest_file.name,recursive=False)
                process.stdin.close()
                if process.wait()!=0:raise RuntimeError('Native lossless Zstandard archive creation failed')
            except BaseException:
                process.kill();process.wait();raise
    expected={source.name+'/'+r['name']:r for r in manifest['files']}
    expected[manifest_file.name]=dict(bytes=manifest_file.stat().st_size,sha256=sha(manifest_file))
    verify_archive(archive if archive.exists() else temp,expected,log)
    source_matches(source,manifest['files'])
    if any(str((source/r['name']).absolute()).casefold() in tracked for r in manifest['files']):raise RuntimeError('Never remove a tracked file')
    if not archive.exists():os.replace(temp,archive)
    row=dict(id=ident,sourceDirectory=str(source),archive=str(archive),archiveBytes=archive.stat().st_size,archiveSha256=sha(archive),manifest=str(manifest_file),manifestSha256=sha(manifest_file),files=len(manifest['files']),originalBytes=sum(r['bytes'] for r in manifest['files']),allArchivedFileSha256Verified=True,expandedCopyRemoved=False)
    catalog['archives']=[r for r in catalog['archives'] if r['id']!=ident]+[row]
    write_json(AUDIT/('archives-'+drive+'.json'),catalog)
    # The full resolved path, source membership and every byte were checked
    # immediately above. No recursive removal is allowed before that proof.
    checked(source);shutil.rmtree(source)
    row['expandedCopyRemoved']=True;row['seconds']=round(time.monotonic()-start,2)
    row['volumeFreeIncreaseBytes']=shutil.disk_usage(archive_base).free-before
    write_json(AUDIT/('archives-'+drive+'.json'),catalog)
    print(json.dumps(dict(id=ident,drive=drive,originalMB=round(row['originalBytes']/1e6,1),archiveMB=round(row['archiveBytes']/1e6,1),freedMB=round(row['volumeFreeIncreaseBytes']/1e6,1),seconds=row['seconds']),ensure_ascii=False),flush=True)

def restore(ident):
    for drive in ['C','E']:
        path=AUDIT/('archives-'+drive+'.json')
        if not path.exists():continue
        catalog=read_json(path);row=next((r for r in catalog['archives'] if r['id']==ident),None)
        if row is None:continue
        archive=Path(row['archive']);mf=Path(row['manifest']);assert sha(archive)==row['archiveSha256'] and sha(mf)==row['manifestSha256']
        manifest=read_json(mf);source=Path(row['sourceDirectory'])
        if source.exists():raise RuntimeError('Refuse to overwrite an existing original directory')
        parent=source.parent.resolve();assert any(parent.is_relative_to(r) for r in ALLOWED)
        if shutil.disk_usage(parent).free<row['originalBytes']+64*1024*1024:raise RuntimeError('Insufficient free space for exact restore')
        if str(archive).endswith('.tar.zst'):
            subprocess.run([str(TAR),'-xf',str(archive),'-C',str(parent),source.name+'/*'],creationflags=FLAGS,check=True)
        else:run(['x','-y','-bso0','-bsp0',archive,'-o'+str(parent),source.name+'/*'])
        source_matches(source,manifest['files'])
        row['expandedCopyRemoved']=False;row['lastRestoreUtc']=datetime.now(timezone.utc).isoformat();row['allRestoredFileSha256Verified']=True
        write_json(path,catalog);print('RESTORED and SHA verified '+str(source));return
    raise RuntimeError('Archive ID not in local catalogs')

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--execute',action='store_true');p.add_argument('--drive',choices=['C','E']);p.add_argument('--max-jobs',type=int);p.add_argument('--min-bytes',type=int,default=0);p.add_argument('--restore');a=p.parse_args()
    AUDIT.mkdir(parents=True,exist_ok=True)
    if a.restore:restore(a.restore);return
    tracked=git_tracked();jobs=candidates(tracked)
    if a.drive:jobs=[j for j in jobs if Path(j['source']).drive.upper()==a.drive+':']
    jobs=[j for j in jobs if j['bytes']>=a.min_bytes]
    plan=dict(utc=datetime.now(timezone.utc).isoformat(),jobs=len(jobs),bytes=sum(j['bytes'] for j in jobs),directories=jobs)
    write_json(ROOT/'Builds/Archives'/('plan-'+str(a.drive)+'.json'),plan)
    print(json.dumps(dict(drive=a.drive,jobs=len(jobs),bytes=plan['bytes'],execute=a.execute)),flush=True)
    if not a.execute:return
    if not a.drive:raise ValueError('Explicit drive required for mutation')
    path=AUDIT/('archives-'+a.drive+'.json')
    catalog=read_json(path) if path.exists() else dict(scope='Lossless local retention of decoded frames; originals, native screenshots, APKs, reports and tracked files untouched. Expanded copies removed only after full streaming byte-SHA verification.',archives=[])
    count=0
    for job in jobs:
        reserve=64*1024*1024
        if shutil.disk_usage(Path(job['source'])).free<job['bytes']+reserve:
            print('DEFER insufficient temporary space '+job['source'],flush=True);continue
        archive_job(job,a.drive,catalog,tracked);count+=1
        if a.max_jobs and count>=a.max_jobs:break
    print(json.dumps(dict(completed=count,remainingLiveFrameDirectories=len([j for j in jobs if Path(j['source']).exists()]))),flush=True)

if __name__=='__main__':main()
