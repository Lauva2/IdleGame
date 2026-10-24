"""Read-only compare frozen APK inputs with actual Git index/commit blobs."""
import hashlib,json,subprocess,sys
from pathlib import Path
root=Path(__file__).resolve().parents[1];qa=(root/sys.argv[1]).resolve();assert qa.is_relative_to(root/'Docs/Verification')
ref=sys.argv[2] if len(sys.argv)>2 else ':'
rows=[]
for line in (qa/'source-files.sha256').read_text(encoding='utf8').splitlines():
    digest,name=line.split('  ',1);assert name.startswith(('Assets/','Packages/','ProjectSettings/'))
    spec=':'+name if ref==':' else ref+':'+name
    data=subprocess.check_output(['git','-c','safe.directory='+str(root).replace('\\','/'),'show',spec],cwd=root)
    actual=hashlib.sha256(data).hexdigest().upper();rows.append(dict(file=name,expected=digest,gitBlobSha256=actual,passed=actual==digest))
result=dict(candidate=qa.name,gitTree=ref,files=len(rows),allPassed=all(r['passed'] for r in rows),mismatches=[r for r in rows if not r['passed']],meaning='Git blob bytes equal exact frozen APK source inputs; no EOL normalization substituted different source.')
if '--no-write' not in sys.argv:(qa/'git-stage-source-identity.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print(json.dumps(result,ensure_ascii=False,indent=2));assert result['allPassed']
