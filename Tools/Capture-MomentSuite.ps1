param(
 [Parameter(Mandatory=$true)][string]$Folder,
 [ValidateSet('Flow','Fit','States','Reload','All')][string]$Group='All'
)
$ErrorActionPreference='Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
if(!$env:FOREST_ADB_SERIAL){throw 'Explicit device required'}
# Run only after other device capture suites finish: each group owns its UI.
foreach($part in @('Flow','Fit','States','Reload')){
 if($Group -ne 'All' -and $Group -ne $part){continue}
 $log="$Folder/capture-moment-$($part.ToLowerInvariant()).log"
 if(Test-Path -LiteralPath $log){throw ('Preserve previous capture log: '+$log)}
 try{
  & Tools/Capture-MomentReview.ps1 -Folder $Folder -Group $part *> $log
  if($LASTEXITCODE -ne 0){throw ('Native capture failed: '+$part)}
  Write-Output ('COMPLETE moments '+$part)
 }catch{
  Get-Content -LiteralPath $log -Tail 20
  throw
 }
}
if($Group -eq 'All'){
 $python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
 & $python Tools/Audit-MomentEvidence.py $Folder
 if($LASTEXITCODE -ne 0){throw 'Prepared moment evidence audit failed'}
}
