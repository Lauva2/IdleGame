param([Parameter(Mandatory=$true)][string]$Candidate,[Parameter(Mandatory=$true)][string]$Audit)
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
$workspaceRoot=(Get-Location).Path
if($Candidate -notmatch '^[a-z0-9-]+$'){throw 'Invalid candidate'}
if(Get-Process Unity,bee_backend,AndroidPlayerBuildProgram -ErrorAction SilentlyContinue){throw 'Build workers must be stopped'}
$mapping=Get-Content -LiteralPath 'Docs/Maintenance/2026-10-28-bee-cache-map/result.json' -Raw|ConvertFrom-Json
$logical=Join-Path $workspaceRoot 'Library/Bee'
$physical='C:\Users\Lauva\.codex\visualizations\2026\10\22\01a16331-da0c-7212-86b6-d3ab1a2524e9\Scratch\IdleGame\d27\Bee'
$link=Get-Item -LiteralPath $logical -Force
if($mapping.logical -ne $logical -or $mapping.physical -ne $physical -or $link.LinkType -ne 'Junction' -or [IO.Path]::GetFullPath(@($link.Target)[0]) -ne $physical){throw 'Recorded cache mapping differs'}
$cursor=Get-Item -LiteralPath $physical
while($cursor){if($cursor.Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Physical linked ancestor'};$cursor=$cursor.Parent}
$auditPath=[IO.Path]::GetFullPath((Join-Path $workspaceRoot $Audit))
if(!$auditPath.StartsWith((Join-Path $workspaceRoot 'Docs/Maintenance')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or (Test-Path $auditPath)){throw 'Use fresh project maintenance audit'}
$build=Get-Content -LiteralPath "Docs/Verification/$Candidate/build-info.json" -Raw|ConvertFrom-Json
$retained=@(@{path=(Join-Path $workspaceRoot $build.apk);sha256=$build.apkSha256},@{path=(Join-Path $workspaceRoot $build.sourceArchive);sha256=$build.sourceArchiveSha256},@{path=(Join-Path $workspaceRoot $build.sourceManifest);sha256=$build.sourceManifestSha256})
# Unique native debug artifacts remain in their objcopy directories.
$symbols=@(Get-ChildItem -LiteralPath (Join-Path $physical 'artifacts') -Directory -Filter 'objcopy_*'|ForEach-Object {Get-ChildItem -LiteralPath $_.FullName -File -Recurse}|ForEach-Object {@{path=$_.FullName;bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}})
$targets=@()
foreach($relative in @('Android/Prj','artifacts/Android')){
 $target=[IO.Path]::GetFullPath((Join-Path $physical $relative))
 if(!$target.StartsWith($physical+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or $target -eq $physical){throw 'Target escaped exact mapped cache'}
 if(!(Test-Path -LiteralPath $target)){continue}
 $cursor=Get-Item -LiteralPath $target
 while($cursor.FullName -ne $physical){if($cursor.Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Target has linked ancestor'};$cursor=$cursor.Parent}
 $items=@(Get-ChildItem -LiteralPath $target -Force -Recurse)
 if(@($items|Where-Object {$_.Attributes -band [IO.FileAttributes]::ReparsePoint}).Count){throw 'Linked target contents'}
 $tracked=@(& git -c "safe.directory=$($workspaceRoot.Replace('\','/'))" ls-files -- "Library/Bee/$relative")
 if($LASTEXITCODE -ne 0 -or $tracked.Count){throw 'Generated target contains tracked files'}
 $files=@($items|Where-Object {!$_.PSIsContainer})
 $targets+=@{relative=$relative;absolute=$target;files=$files.Count;bytes=($files|Measure-Object Length -Sum).Sum}
}
foreach($file in $retained+$symbols){if((Get-FileHash -LiteralPath $file.path -Algorithm SHA256).Hash -ne $file.sha256){throw 'Protected artifact differs'}}
New-Item -ItemType Directory -Path $auditPath|Out-Null
$record=@{utc=[DateTime]::UtcNow.ToString('o');candidate=$Candidate;mapping=$mapping;targets=$targets;retained=$retained;nativeDebugArtifacts=$symbols;freeBefore=@{C=(Get-PSDrive C).Free;E=(Get-PSDrive E).Free};scope='Only the fixed untracked Android export and compilation cache inside the exact recorded task scratch Bee directory. Physical ancestors and contents verified without links. Native debug artifacts, logical junction, source assets, saves, delivered APK and original media retained.'}
$record|ConvertTo-Json -Depth 8|Set-Content -LiteralPath "$auditPath/plan.json" -Encoding UTF8
foreach($target in $targets){
 if((Resolve-Path -LiteralPath $target.absolute).Path -ne $target.absolute){throw 'Target identity changed'}
 Remove-Item -LiteralPath $target.absolute -Recurse -Force
}
foreach($file in $retained+$symbols){if((Get-FileHash -LiteralPath $file.path -Algorithm SHA256).Hash -ne $file.sha256){throw 'Protected artifact changed'}}
$result=@{utc=[DateTime]::UtcNow.ToString('o');removedDirectories=$targets.Count;removedFiles=($targets|Measure-Object files -Sum).Sum;logicalBytes=($targets|Measure-Object bytes -Sum).Sum;retainedNativeFiles=$symbols.Count;allProtectedHashesMatch=$true;freeAfter=@{C=(Get-PSDrive C).Free;E=(Get-PSDrive E).Free}}
$result|ConvertTo-Json -Depth 4|Set-Content -LiteralPath "$auditPath/result.json" -Encoding UTF8
$result|ConvertTo-Json -Depth 4
