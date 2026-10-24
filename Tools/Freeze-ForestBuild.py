"""Freeze exact Assets/Packages/ProjectSettings bytes of a completed APK.
Artifacts stay in the project build directory (which may be a verified local
junction); there is no network operation or modification of source inputs.
"""
import hashlib, json, re, subprocess, sys, zipfile
from datetime import datetime, timezone
from pathlib import Path
root=Path(__file__).resolve().parents[1]
candidate=sys.argv[1]
assert candidate and all(c.isalnum() or c=='-' for c in candidate)
qa=root/'Docs/Verification'/candidate
apk=root/'Builds/Android'/candidate/('ForestShop-'+candidate+'.apk')
assert apk.is_file() and qa.is_dir()
def sha(path):
    h=hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda:stream.read(1024*1024),b''): h.update(block)
    return h.hexdigest().upper()
manifest=qa/'source-files.sha256'
assert not manifest.exists(), 'Frozen source identity must never be silently overwritten'
inputs=sorted(p for top in ('Assets','Packages','ProjectSettings') for p in (root/top).rglob('*') if p.is_file())
rows=[(p.relative_to(root).as_posix(),sha(p)) for p in inputs]
manifest.write_text(''.join(digest+'  '+name+'\n' for name,digest in rows),encoding='utf8')
archive=apk.parent/'build-source-inputs.zip'
assert not archive.exists()
with zipfile.ZipFile(archive,'w',compression=zipfile.ZIP_DEFLATED,compresslevel=6) as zipped:
    for name,_ in rows: zipped.write(root/name,name)
with zipfile.ZipFile(archive) as zipped:
    assert len(zipped.namelist())==len(rows)
    for name,digest in rows: assert hashlib.sha256(zipped.read(name)).hexdigest().upper()==digest
head=subprocess.check_output(['git','-c','safe.directory='+str(root).replace('\\','/'),'rev-parse','HEAD'],cwd=root,text=True).strip()
settings=(root/'ProjectSettings/ProjectSettings.asset').read_text(encoding='utf8')
version=re.search(r'^  bundleVersion: (.+)$',settings,re.M).group(1).strip()
code=int(re.search(r'^  AndroidBundleVersionCode: (\d+)$',settings,re.M).group(1))
record=dict(candidate=candidate,utc=datetime.now(timezone.utc).isoformat(),sourceBaseCommit=head,sourceInputFiles=len(rows),sourceManifest=str(manifest.relative_to(root)),sourceManifestSha256=sha(manifest),apk=str(apk.relative_to(root)),apkBytes=apk.stat().st_size,apkSha256=sha(apk),sourceArchive=str(archive.relative_to(root)),sourceArchiveBytes=archive.stat().st_size,sourceArchiveSha256=sha(archive),sourceArchiveVerified=True,build='Unity2022.3.62f2 IL2CPP Release ARM64+ARMv7 non-development GLES3 API23-32',version=version,versionCode=code,artAcceptance='Pending independent evaluation of this exact APK. Editor and earlier candidate scores do not transfer.')
(qa/'build-info.json').write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print(json.dumps(record,ensure_ascii=False,indent=2))
