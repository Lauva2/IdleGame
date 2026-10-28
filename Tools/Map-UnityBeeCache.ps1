param([ValidatePattern('^[a-z0-9-]+$')][string]$Task='d27',[Parameter(Mandatory=$true)][string]$Audit)
$ErrorActionPreference='Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
$workspaceRoot=(Get-Location).Path
$source=[IO.Path]::GetFullPath((Join-Path $workspaceRoot 'Library/Bee'))
$allowedRoot='C:\Users\Lauva\.codex\visualizations\2026\10\22\01a16331-da0c-7212-86b6-d3ab1a2524e9'
$destination=[IO.Path]::GetFullPath((Join-Path $allowedRoot "Scratch/IdleGame/$Task/Bee"))
$auditPath=[IO.Path]::GetFullPath((Join-Path $workspaceRoot $Audit))
function QuietBuild {
 if(Get-Process Unity,bee_backend,AndroidPlayerBuildProgram -ErrorAction SilentlyContinue){throw 'Stop Unity and build workers before changing a generated cache mapping'}
}
function NoLinksToRoot([string]$Path,[string]$Root){
 $item=Get-Item -LiteralPath $Path -Force
 while($true){
  if($item.Attributes -band [IO.FileAttributes]::ReparsePoint){throw ('Refuse linked ancestor: '+$item.FullName)}
  if($item.FullName -eq $Root){break}
  if(!$item.Parent){throw 'Root is not an ancestor'}
  $item=$item.Parent
 }
}
QuietBuild
if(!$source.StartsWith((Join-Path $workspaceRoot 'Library')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Unexpected generated source path'}
if(!$destination.StartsWith($allowedRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or $destination -eq $allowedRoot){throw 'Destination escaped authorized scratch directory'}
if(!$auditPath.StartsWith((Join-Path $workspaceRoot 'Docs/Maintenance')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or (Test-Path -LiteralPath $auditPath)){throw 'Use a fresh project maintenance audit'}
if(!(Test-Path -LiteralPath $source) -or (Test-Path -LiteralPath $destination)){throw 'Source must exist and destination must be new'}
NoLinksToRoot $source $workspaceRoot
NoLinksToRoot $allowedRoot ([IO.Path]::GetPathRoot($allowedRoot))
$items=@(Get-ChildItem -LiteralPath $source -Force -Recurse)
if(@($items|Where-Object {$_.Attributes -band [IO.FileAttributes]::ReparsePoint}).Count){throw 'Refuse linked cache contents'}
$tracked=@(& git -c "safe.directory=$($workspaceRoot.Replace('\','/'))" ls-files -- Library/Bee)
if($LASTEXITCODE -ne 0 -or $tracked.Count){throw 'Cache contains versioned files'}
$files=@($items|Where-Object {!$_.PSIsContainer}|ForEach-Object {@{path=$_.FullName.Substring($source.Length+1);bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}})
if((Get-PSDrive C).Free -lt (($files|Measure-Object bytes -Sum).Sum+10GB)){throw 'Keep at least10GB headroom on C'}
New-Item -ItemType Directory -Path $auditPath|Out-Null
$record=@{utc=[DateTime]::UtcNow.ToString('o');scope='Only an untracked Unity-generated Bee cache is copied into task-authorized C scratch and byte-verified before replacing the old E directory with a junction. Source assets, saves, APKs and original evidence are not moved.';logical=$source;physical=$destination;files=$files;freeBefore=@{C=(Get-PSDrive C).Free;E=(Get-PSDrive E).Free};cleanup='After build, stop Unity/workers, verify this exact mapping and remove only regenerable build intermediates inside its recorded physical cache. Never recursively delete the entire project Library or a computed unchecked target.'}
$record|ConvertTo-Json -Depth 7|Set-Content -LiteralPath "$auditPath/plan.json" -Encoding UTF8
New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force|Out-Null
Copy-Item -LiteralPath $source -Destination $destination -Recurse
$copied=@(Get-ChildItem -LiteralPath $destination -File -Recurse -Force)
if($copied.Count -ne $files.Count){throw 'Copied cache file count differs; original retained'}
foreach($file in $files){if((Get-FileHash -LiteralPath (Join-Path $destination $file.path) -Algorithm SHA256).Hash -ne $file.sha256){throw 'Copied bytes differ; original retained'}}
QuietBuild
NoLinksToRoot $source $workspaceRoot
if((Resolve-Path -LiteralPath $source).Path -ne $source){throw 'Canonical source changed before removal'}
if((Resolve-Path -LiteralPath $destination).Path -ne $destination){throw 'Canonical destination changed'}
NoLinksToRoot $destination $allowedRoot
foreach($file in $files){if((Get-FileHash -LiteralPath (Join-Path $source $file.path) -Algorithm SHA256).Hash -ne $file.sha256){throw 'Original cache changed during copy; retained'}}
# Checked literal source is inside this project's Library/Bee; no other tree.
Remove-Item -LiteralPath $source -Recurse -Force
New-Item -ItemType Junction -Path $source -Target $destination|Out-Null
$link=Get-Item -LiteralPath $source -Force
if($link.LinkType -ne 'Junction' -or [IO.Path]::GetFullPath(@($link.Target)[0]) -ne $destination){throw 'Cache junction identity differs'}
foreach($file in $files){if((Get-FileHash -LiteralPath (Join-Path $source $file.path) -Algorithm SHA256).Hash -ne $file.sha256){throw 'Logical mapped bytes differ'}}
$result=@{utc=[DateTime]::UtcNow.ToString('o');logical=$source;physical=$destination;copiedFiles=$files.Count;copiedBytes=($files|Measure-Object bytes -Sum).Sum;allSourceAndCopiedHashesMatch=$true;freeAfter=@{C=(Get-PSDrive C).Free;E=(Get-PSDrive E).Free}}
$result|ConvertTo-Json -Depth 4|Set-Content -LiteralPath "$auditPath/result.json" -Encoding UTF8
$result|ConvertTo-Json -Depth 4
