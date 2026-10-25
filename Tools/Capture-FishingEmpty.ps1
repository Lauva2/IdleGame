param([Parameter(Mandatory=$true)][string]$Folder)
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
$serial=$env:FOREST_ADB_SERIAL;if(!$serial){throw 'Explicit ADB serial required'}
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
if(Test-Path "$Folder/Evidence/FishingEmpty"){throw 'Preserve empty-gallery original evidence'}
& Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant fishing-empty
$steps=@(@{tap=@(900,1850)},@{tap=@(900,300)},@{shot='00-all-undiscovered'},@{tap=@(535,515)},@{shot='01-discovered-empty'},@{tap=@(900,515)},@{shot='02-recent-empty'},@{tap=@(180,515)},@{tap=@(600,775)},@{shot='03-undiscovered-species-top'},@{drag_hold=@(1000,1740,1000,1300,400,700)},@{shot='04-undiscovered-species-bottom'},@{tap=@(850,1810)},@{shot='05-first-empty-automatic-wait'},@{back=$true},@{shot='06-restored-world-markers'})
@{directory="$Folder/Evidence/FishingEmpty";steps=$steps}|ConvertTo-Json -Depth 7|Set-Content "$Folder/Plans/fishing-empty-native.json"
& $python Tools/Forest-DeviceReview.py "$Folder/Plans/fishing-empty-native.json"
if($LASTEXITCODE -ne 0){throw 'Actual empty gallery plan failed'}
