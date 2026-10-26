param([Parameter(Mandatory=$true)][string]$Folder)
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
$serial=$env:FOREST_ADB_SERIAL;if(!$serial){throw 'Explicit ADB serial required'}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
if(Test-Path "$Folder/Evidence/shop-closeup-zoom-clearance.mp4"){throw 'Keep original zoom clearance evidence'}
& Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant visits
if($LASTEXITCODE -ne 0){throw 'Zoom sale fixture activation failed'}
$p="$Folder/Plans/zoom-sale.json"
@{directory="$Folder/Evidence/ZoomSales";steps=@(@{tap=@(930,300)},@{tap=@(888,1572)},@{tap=@(888,1572)},@{tap=@(888,1572)},@{shot='01-closest-shop-camera'})}|ConvertTo-Json -Depth 7|Set-Content $p
& $python Tools/Forest-DeviceReview.py $p
if($LASTEXITCODE -ne 0){throw 'Actual zoom setup failed'}
$remote='/sdcard/'+(Split-Path -Leaf $Folder)+'-shop-zoom.mp4'
[DateTime]::UtcNow.ToString('o')|Set-Content "$Folder/zoom-sale-start.txt"
& $adb -P 5038 -s $serial shell screenrecord --size 1080x1920 --bit-rate 4000000 --time-limit 40 $remote
if($LASTEXITCODE -ne 0){throw 'Actual zoom recorder failed'}
& $adb -P 5038 -s $serial pull $remote "$Folder/Evidence/shop-closeup-zoom-clearance.mp4"
@{directory="$Folder/Evidence/ZoomSales";steps=@(@{shot='02-closest-after-sales'},@{swipe=@(550,1000,300,1500,900)},@{shot='03-panned-source'},@{wait=12},@{shot='04-panned-after-sales'})}|ConvertTo-Json -Depth 7|Set-Content "$Folder/Plans/zoom-sale-after.json"
& $python Tools/Forest-DeviceReview.py "$Folder/Plans/zoom-sale-after.json"
if($LASTEXITCODE -ne 0){throw 'Actual pan plan failed'}
$panRemote='/sdcard/'+(Split-Path -Leaf $Folder)+'-shop-pan.mp4'
& $adb -P 5038 -s $serial shell screenrecord --size 1080x1920 --bit-rate 4000000 --time-limit 20 $panRemote
if($LASTEXITCODE -ne 0){throw 'Actual pan recorder failed'}
& $adb -P 5038 -s $serial pull $panRemote "$Folder/Evidence/shop-closeup-pan-clearance.mp4"
@{directory="$Folder/Evidence/ZoomSales";steps=@(@{swipe=@(550,1000,1000,1450,900)},@{swipe=@(550,1000,1000,1450,900)},@{swipe=@(550,1000,1000,1450,900)},@{swipe=@(550,1000,1000,1450,900)},@{wait=3},@{shot='05-real-pan-far-away-from-shelves'})}|ConvertTo-Json -Depth 7|Set-Content "$Folder/Plans/zoom-sale-offscreen.json"
& $python Tools/Forest-DeviceReview.py "$Folder/Plans/zoom-sale-offscreen.json"
if($LASTEXITCODE -ne 0){throw 'Actual offscreen source plan failed'}
& $adb -P 5038 -s $serial pull /sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save "$Folder/offscreen-sales-before.save"
$offRemote='/sdcard/'+(Split-Path -Leaf $Folder)+'-shop-offscreen.mp4'
& $adb -P 5038 -s $serial shell screenrecord --size 1080x1920 --bit-rate 4000000 --time-limit 35 $offRemote
if($LASTEXITCODE -ne 0){throw 'Actual offscreen source recorder failed'}
& $adb -P 5038 -s $serial pull $offRemote "$Folder/Evidence/shop-closeup-offscreen-source.mp4"
& $adb -P 5038 -s $serial pull /sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save "$Folder/offscreen-sales-after.save"
