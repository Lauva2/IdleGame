param([string]$Folder='Docs/Verification/3d-d20-shopping02')
$ErrorActionPreference='Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
$serial=if($env:FOREST_ADB_SERIAL){$env:FOREST_ADB_SERIAL}else{'127.0.0.1:16384'}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$candidate=Split-Path -Leaf $Folder
if($candidate -notmatch '^[a-zA-Z0-9-]+$'){throw 'Unsafe candidate name'}
& Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant initial
$steps=@(@{shot='00-welcome'},@{tap=@(850,1775)},@{shot='01-key-accepted'},@{tap=@(850,410)},@{wait=2},@{shot='02-harvested-to-workshop'},@{tap=@(850,410)},@{shot='03-first-recipe-choice'},@{tap=@(850,1250)},@{shot='04-tea-configured'},@{tap=@(850,410)},@{shot='05-first-shelf-choice'},@{tap=@(850,1115)},@{shot='06-awaiting-real-sale'},@{wait=15},@{wait=15},@{shot='07-real-income'},@{tap=@(540,1850)},@{shot='08-order-entry'})
New-Item -ItemType Directory -Force "$Folder/Plans"|Out-Null
@{directory=($Folder+'/Evidence/Initial');steps=$steps}|ConvertTo-Json -Depth 7|Set-Content "$Folder/Plans/initial-flow.json"
[DateTime]::UtcNow.ToString('o')|Set-Content "$Folder/initial-flow-start.txt"
$remote='/sdcard/'+$candidate+'-initial-flow.mp4'
$record=Start-Process -FilePath $adb -ArgumentList @('-P','5038','-s',$serial,'shell','screenrecord','--size','1080x1920','--bit-rate','4000000','--time-limit','80',$remote) -PassThru -WindowStyle Hidden
& $python Tools/Forest-DeviceReview.py "$Folder/Plans/initial-flow.json"
if($LASTEXITCODE -ne 0){throw 'Actual initial input plan failed'}
$record.WaitForExit()
if($record.ExitCode -ne 0){throw 'Initial screenrecord failed'}
& $adb -P 5038 -s $serial pull $remote "$Folder/Evidence/initial-flow.mp4"
if($LASTEXITCODE -ne 0){throw 'Initial video pull failed'}
# HOME is deliberately sent only after screenrecord has ended, unlike d19.
[DateTime]::UtcNow.ToString('o')|Set-Content "$Folder/initial-home-after-recording.txt"
& $adb -P 5038 -s $serial shell input keyevent KEYCODE_HOME
Start-Sleep -Seconds 2
& $adb -P 5038 -s $serial pull /sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save "$Folder/initial-completed.save"
if($LASTEXITCODE -ne 0){throw 'Initial completed save pull failed'}
