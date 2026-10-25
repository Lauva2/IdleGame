param([Parameter(Mandatory=$true)][string]$Folder)
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
if(!$env:FOREST_ADB_SERIAL){throw 'Explicit ADB serial required'}
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
function Plan($name,$steps){$p="$Folder/Plans/legacy-$name.json";@{directory="$Folder/Evidence/Legacy";steps=$steps}|ConvertTo-Json -Depth 7|Set-Content $p;& $python Tools/Forest-DeviceReview.py $p;if($LASTEXITCODE -ne 0){throw 'Legacy UI plan failed'}}
& Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant states
Plan 'states' @(@{tap=@(930,300)},@{shot='01-empty-reserved-shelf-symbols'},@{tap=@(200,1690)},@{shot='02-empty-reserved-written-status'},@{tap=@(540,1850)},@{shot='03-shortage-order'})
& Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant unrestored
Plan 'before' @(@{shot='04-annex-before-overview'},@{tap=@(930,300)},@{shot='05-basic-sign-pot'},@{tap=@(660,300)},@{shot='06-basic-curtain-rug'})
& Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant chapter
Plan 'after' @(@{shot='07-prepared-chapter'},@{back=$true},@{tap=@(930,300)},@{shot='08-reward-sign-pot'},@{tap=@(660,300)},@{shot='09-reward-curtain-rug'})
