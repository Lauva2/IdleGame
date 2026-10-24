param([string]$Folder='Docs/Verification/3d-d19-experience01')
$ErrorActionPreference='Stop'
$serial=if($env:FOREST_ADB_SERIAL){$env:FOREST_ADB_SERIAL}else{'127.0.0.1:16385'}
Set-Location (Split-Path -Parent $PSScriptRoot)
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant late
$steps=@(
 @{tap=@(130,300)},@{shot='10-overview'},@{tap=@(885,1612)},@{shot='11-map-zoom'},@{swipe=@(550,1000,650,1080,700)},@{shot='12-map-drag'},@{tap=@(998,1612)},
 @{tap=@(390,300)},@{shot='13-twelve-garden'},@{tap=@(260,820)},@{shot='14-plot-configuration'},@{back=$true},
 @{tap=@(660,300)},@{tap=@(355,980)},@{shot='15-cauldron-recipes'},@{swipe=@(850,1720,850,1070,650)},@{swipe=@(850,1720,850,1070,650)},@{shot='16-cauldron-bottom'},@{back=$true},
 @{tap=@(700,980)},@{shot='17-workbench-recipes'},@{swipe=@(850,1720,850,1070,650)},@{swipe=@(850,1720,850,1070,650)},@{shot='18-workbench-bottom'},@{back=$true},
 @{tap=@(930,300)},@{tap=@(320,950)},@{shot='19-shelf-products'},@{swipe=@(850,1720,850,1070,650)},@{swipe=@(850,1720,850,1070,650)},@{swipe=@(850,1720,850,1070,650)},@{shot='20-shelf-bottom'},@{back=$true},
 @{tap=@(540,1850)},@{shot='21-order-available'},@{tap=@(350,770)},@{shot='22-order-accepted'},
 @{tap=@(900,1850)},@{shot='23-products-top'},@{swipe=@(700,1620,700,620,700)},@{shot='24-products-bottom'},
 @{tap=@(900,1850)},@{tap=@(790,410)},@{shot='25-residents-top'},@{tap=@(350,725)},@{shot='26-resident-story'},@{swipe=@(850,1720,850,1070,650)},@{shot='27-resident-story-bottom'},@{back=$true},
 @{tap=@(800,725)},@{shot='28-memorial'},@{back=$true},@{swipe=@(700,1630,700,650,700)},@{shot='29-residents-bottom'},
 @{tap=@(180,1850)},@{tap=@(660,300)},@{tap=@(200,1690)},@{shot='30-workshop-facilities'},@{tap=@(130,300)},@{tap=@(200,1690)},@{shot='31-courtyard-facilities'}
)
New-Item -ItemType Directory -Force "$Folder/Plans"|Out-Null
@{directory=($Folder+'/Evidence/Interfaces');steps=$steps}|ConvertTo-Json -Depth 7|Set-Content "$Folder/Plans/interfaces.json"
[DateTime]::UtcNow.ToString('o')|Set-Content "$Folder/interfaces-start.txt"
$remote='/sdcard/d19-interfaces.mp4'
$record=Start-Process -FilePath $adb -ArgumentList @('-P','5038','-s',$serial,'shell','screenrecord','--size','1080x1920','--bit-rate','4000000','--time-limit','120',$remote) -PassThru -WindowStyle Hidden
& $python Tools/Forest-DeviceReview.py "$Folder/Plans/interfaces.json"
if($LASTEXITCODE -ne 0){throw 'Interface actions failed'}
$record.WaitForExit()
& $adb -P 5038 -s $serial pull $remote "$Folder/Evidence/interfaces.mp4"
if($LASTEXITCODE -ne 0){throw 'Interface video pull failed'}
