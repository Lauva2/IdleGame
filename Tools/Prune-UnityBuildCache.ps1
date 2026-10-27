param([Parameter(Mandatory=$true)][ValidatePattern('^[a-zA-Z0-9-]+$')][string]$Candidate,[Parameter(Mandatory=$true)][string]$Audit,[ValidateSet('AndroidIntermediates','AndroidExport')][string]$CacheGroup='AndroidIntermediates')
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
$workspaceRoot=(Get-Location).Path
$auditPath=[IO.Path]::GetFullPath((Join-Path $workspaceRoot $Audit))
$auditRoot=(Join-Path $workspaceRoot 'Docs/Maintenance')+[IO.Path]::DirectorySeparatorChar
if(!$auditPath.StartsWith($auditRoot,[StringComparison]::OrdinalIgnoreCase) -or (Test-Path -LiteralPath $auditPath)){throw 'Use a fresh maintenance audit directory'}
if(Get-Process Unity,bee_backend,AndroidPlayerBuildProgram -ErrorAction SilentlyContinue){throw 'Stop active Unity builds before pruning generated intermediates'}
$qa=Join-Path $workspaceRoot "Docs/Verification/$Candidate"
$build=Get-Content -LiteralPath "$qa/build-info.json" -Raw|ConvertFrom-Json
$preserved=@(@{path=$build.apk;sha256=$build.apkSha256},@{path=$build.sourceArchive;sha256=$build.sourceArchiveSha256},@{path=$build.sourceManifest;sha256=$build.sourceManifestSha256})
foreach($file in $preserved){if((Get-FileHash -LiteralPath (Join-Path $workspaceRoot $file.path) -Algorithm SHA256).Hash -ne $file.sha256){throw 'A frozen retained artifact differs'}}
$targets=@()
$relativeTargets=if($CacheGroup -eq 'AndroidExport'){@('Library/Bee/Android/Prj')}else{@('Library/Bee/Android/Prj/IL2CPP/Gradle/launcher/build','Library/Bee/Android/Prj/IL2CPP/Gradle/unityLibrary/build','Library/Bee/artifacts/Android')}
foreach($relative in $relativeTargets){
 $target=[IO.Path]::GetFullPath((Join-Path $workspaceRoot $relative))
 $guard=(Join-Path $workspaceRoot 'Library/Bee')+[IO.Path]::DirectorySeparatorChar
 if(!$target.StartsWith($guard,[StringComparison]::OrdinalIgnoreCase) -or $target -eq $guard.TrimEnd([IO.Path]::DirectorySeparatorChar)){throw 'Invalid generated-cache target'}
 if(!(Test-Path -LiteralPath $target)){continue}
 $resolved=(Resolve-Path -LiteralPath $target).Path
 if($resolved -ne $target){throw 'Target must resolve exactly inside the workspace'}
 $chain=Get-Item -LiteralPath $target
 while($chain.FullName -ne $workspaceRoot){if($chain.Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Refuse a reparse-point ancestor'};$chain=$chain.Parent}
 $items=@(Get-ChildItem -LiteralPath $target -Recurse -Force)
 if(@($items|Where-Object {$_.Attributes -band [IO.FileAttributes]::ReparsePoint}).Count){throw 'Refuse a cache tree containing links'}
 $tracked=@(& git -c "safe.directory=$($workspaceRoot.Replace('\','/'))" ls-files -- $relative)
 if($LASTEXITCODE -ne 0 -or $tracked.Count){throw 'Cache target contains versioned files'}
 $files=@($items|Where-Object {!$_.PSIsContainer})
 $targets+=@{relative=$relative;absolute=$target;files=$files.Count;bytes=($files|Measure-Object Length -Sum).Sum}
}
New-Item -ItemType Directory -Path $auditPath|Out-Null
$record=@{utc=[DateTime]::UtcNow.ToString('o');candidate=$Candidate;cacheGroup=$CacheGroup;scope='Only the listed fixed, untracked Unity-generated Android intermediate/export cache directories. Unity is stopped, all absolute targets and ancestors verified, links refused. Frozen APK/source ZIP/manifests, Assets, saves, original QA media and native debug symbols retained. Regenerated on a future build.';authorization='Standing user request to remove unused project files; reversible generated build cache cleanup during authorized local work.';targets=$targets;retained=$preserved;freeBefore=(Get-PSDrive E).Free}
$record|ConvertTo-Json -Depth 6|Set-Content -LiteralPath "$auditPath/plan.json" -Encoding utf8
foreach($target in $targets){
 if((Resolve-Path -LiteralPath $target.absolute).Path -ne $target.absolute){throw 'Cache path changed before removal'}
 Remove-Item -LiteralPath $target.absolute -Recurse -Force
}
foreach($file in $preserved){if((Get-FileHash -LiteralPath (Join-Path $workspaceRoot $file.path) -Algorithm SHA256).Hash -ne $file.sha256){throw 'Retained artifact changed'}}
$result=@{utc=[DateTime]::UtcNow.ToString('o');removedDirectories=$targets.Count;logicalBytes=($targets|Measure-Object bytes -Sum).Sum;freeBefore=$record.freeBefore;freeAfter=(Get-PSDrive E).Free;allFrozenArtifactsUnchanged=$true}
$result|ConvertTo-Json|Set-Content -LiteralPath "$auditPath/result.json" -Encoding utf8
$result|ConvertTo-Json
