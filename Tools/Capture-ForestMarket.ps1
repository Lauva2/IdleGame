param([string]$Folder='Docs/Verification/3d-d21-forest05')
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
$serial=if($env:FOREST_ADB_SERIAL){$env:FOREST_ADB_SERIAL}else{'127.0.0.1:16384'}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$candidate=Split-Path -Leaf $Folder
[DateTime]::UtcNow.ToString('o')|Set-Content "$Folder/market-activation-utc.txt"
& Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant forest-visits
$steps=@(@{tap=@(660,300)},@{tap=@(355,980)},@{shot='01-new-tea-recipe'},@{swipe=@(850,1740,850,1040,700)},@{shot='02-tea-recipe-bottom'},@{back=$true},@{tap=@(700,980)},@{shot='03-new-workbench-recipes'},@{swipe=@(850,1740,850,1040,700)},@{swipe=@(850,1740,850,1040,700)},@{shot='04-spray-and-balm-recipes'},@{back=$true},@{tap=@(900,1850)},@{shot='05-nine-products-top'})
for($i=0;$i -lt 3;$i++){$steps+=@{swipe=@(1000,1640,1000,640,750)}}
$steps+=@(@{shot='06-all-new-products-bottom'},@{tap=@(180,1850)},@{tap=@(930,300)},@{shot='07-new-products-shop-default'})
New-Item -ItemType Directory -Force -Path "$Folder/Plans/Market"|Out-Null
@{directory="$Folder/Evidence/Market";steps=$steps}|ConvertTo-Json -Depth 6|Set-Content "$Folder/Plans/Market/recipes-and-shop.json"
& $python Tools/Forest-DeviceReview.py "$Folder/Plans/Market/recipes-and-shop.json"
if($LASTEXITCODE -ne 0){throw 'Market real UI plan failed'}
& $adb -P 5038 -s $serial shell pidof com.forestatelier.curioshop|Set-Content "$Folder/market-start-pid.txt"
$marketProcessId=(Get-Content "$Folder/market-start-pid.txt" -Raw).Trim()
& $adb -P 5038 -s $serial shell cat "/proc/$marketProcessId/stat"|Set-Content "$Folder/market-process-stat-start.txt"
& $adb -P 5038 -s $serial pull /sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save "$Folder/market-before.save"
for($i=1;$i -le 2;$i++){
    $stem="shop-closeup-forest-$i";$remote="/sdcard/$candidate-$stem.mp4"
    [DateTime]::UtcNow.ToString('o')|Set-Content "$Folder/$stem-start.txt"
    & $adb -P 5038 -s $serial shell screenrecord --size 1080x1920 --bit-rate 4000000 --time-limit 160 $remote
    if($LASTEXITCODE -ne 0){throw 'Market recorder failed'}
    & $adb -P 5038 -s $serial pull $remote "$Folder/Evidence/$stem.mp4"
    if($LASTEXITCODE -ne 0){throw 'Market video pull failed'}
}
[DateTime]::UtcNow.ToString('o')|Set-Content "$Folder/market-end.txt"
& $adb -P 5038 -s $serial shell pidof com.forestatelier.curioshop|Set-Content "$Folder/market-end-pid.txt"
& $adb -P 5038 -s $serial pull /sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save "$Folder/market-after.save"
& $adb -P 5038 -s $serial pull /sdcard/Android/data/com.forestatelier.curioshop/files/performance-v2.csv "$Folder/performance-market.csv"
& $adb -P 5038 -s $serial shell dumpsys meminfo com.forestatelier.curioshop|Set-Content "$Folder/meminfo-market.txt"
& $adb -P 5038 -s $serial shell cat "/proc/$marketProcessId/stat"|Set-Content "$Folder/market-process-stat-end.txt"
& $adb -P 5038 -s $serial logcat "--pid=$marketProcessId" -d -s Unity AndroidRuntime|Set-Content "$Folder/logcat-market.txt"
