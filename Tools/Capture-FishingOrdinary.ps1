param([Parameter(Mandatory=$true)][string]$Folder)
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
$serial=if($env:FOREST_ADB_SERIAL){$env:FOREST_ADB_SERIAL}else{'127.0.0.1:16384'}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$candidate=Split-Path -Leaf $Folder
if(Test-Path -LiteralPath "$Folder/prepared-activation.txt"){throw 'Ordinary evidence must precede any fixture activation'}
if(Test-Path -LiteralPath "$Folder/ordinary-fishing-start.save"){throw 'Preserve first actual run; use explicit supplement names'}
New-Item -ItemType Directory -Force -Path "$Folder/Plans","$Folder/Evidence/Ordinary"|Out-Null
$steps=@(@{tap=@(850,1780)},@{shot='03-report-claimed-overview'},@{tap=@(150,410)},@{tap=@(860,880)},@{wait=1},@{shot='04-ordinary-stable-wait'})
@{directory="$Folder/Evidence/Ordinary";steps=$steps}|ConvertTo-Json -Depth 6|Set-Content "$Folder/Plans/ordinary-start.json"
& $python Tools/Forest-DeviceReview.py "$Folder/Plans/ordinary-start.json"
if($LASTEXITCODE -ne 0){throw 'Ordinary actual UI plan failed'}
& $adb -P 5038 -s $serial pull /sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save "$Folder/ordinary-fishing-start.save"
& $adb -P 5038 -s $serial shell pidof com.forestatelier.curioshop|Set-Content "$Folder/ordinary-start-pid.txt"
[DateTime]::UtcNow.ToString('o')|Set-Content "$Folder/ordinary-fishing-start.txt"
& $adb -P 5038 -s $serial shell screenrecord --size 1080x1920 --bit-rate 4000000 --time-limit 160 "/sdcard/$candidate-ordinary-fishing.mp4"
if($LASTEXITCODE -ne 0){throw 'Actual ordinary fishing recorder failed'}
& $adb -P 5038 -s $serial shell input keyevent KEYCODE_HOME
Start-Sleep -Seconds 1
foreach($suffix in @('','.bak')){
 & $adb -P 5038 -s $serial pull "/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save$suffix" "$Folder/ordinary-continuous-end.save$suffix"
 if($LASTEXITCODE -ne 0){throw 'Ordinary actual end snapshot failed'}
}
[DateTime]::UtcNow.ToString('o')|Set-Content "$Folder/ordinary-home-at.txt"
& $adb -P 5038 -s $serial pull "/sdcard/$candidate-ordinary-fishing.mp4" "$Folder/Evidence/fishing-motion-ordinary.mp4"
Start-Sleep -Seconds 35
& $adb -P 5038 -s $serial shell am start -W -n com.forestatelier.curioshop/com.unity3d.player.UnityPlayerActivity|Set-Content "$Folder/ordinary-background-resume.txt"
Start-Sleep -Seconds 5
& $adb -P 5038 -s $serial pull /sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save "$Folder/ordinary-background-report.save"
& $adb -P 5038 -s $serial shell pidof com.forestatelier.curioshop|Set-Content "$Folder/ordinary-background-pid.txt"
@{directory="$Folder/Evidence/Ordinary";steps=@(@{shot='05-background-report'},@{drag_hold=@(1000,1740,1000,1140,500,600)},@{shot='06-background-report-bottom'})}|ConvertTo-Json -Depth 6|Set-Content "$Folder/Plans/ordinary-background.json"
& $python Tools/Forest-DeviceReview.py "$Folder/Plans/ordinary-background.json"
if($LASTEXITCODE -ne 0){throw 'Actual background report screenshot failed'}
