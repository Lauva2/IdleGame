param([Parameter(Mandatory=$true)][string]$Folder)
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
$serial=$env:FOREST_ADB_SERIAL;if(!$serial){throw 'Explicit ADB serial required'}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
if(Test-Path "$Folder/Evidence/production-window.mp4"){throw 'Preserve original window evidence'}
& Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant journey-window
if($LASTEXITCODE -ne 0){throw 'Window fixture failed'}
$plan="$Folder/Plans/window.json"
@{directory="$Folder/Evidence/Window";steps=@(@{tap=@(660,300)},@{shot='01-before-single-completion'})}|ConvertTo-Json -Depth 6|Set-Content $plan
& $python Tools/Forest-DeviceReview.py $plan
if($LASTEXITCODE -ne 0){throw 'Window real UI setup failed'}
& $adb -P 5038 -s $serial pull /sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save "$Folder/window-before.save"
[DateTime]::UtcNow.ToString('o')|Set-Content "$Folder/production-window-start.txt"
$remote='/sdcard/'+(Split-Path -Leaf $Folder)+'-production-window.mp4'
& $adb -P 5038 -s $serial shell screenrecord --size 1080x1920 --bit-rate 4000000 --time-limit 40 $remote
if($LASTEXITCODE -ne 0){throw 'Window recorder failed'}
& $adb -P 5038 -s $serial pull $remote "$Folder/Evidence/production-window.mp4"
& $adb -P 5038 -s $serial pull /sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save "$Folder/window-after.save"
@{directory="$Folder/Evidence/Window";steps=@(@{shot='02-after-quiet-window'})}|ConvertTo-Json -Depth 6|Set-Content "$Folder/Plans/window-after.json"
& $python Tools/Forest-DeviceReview.py "$Folder/Plans/window-after.json"
& 'C:/Program Files/Unity/Hub/Editor/2022.3.8f1c1/Editor/Data/MonoBleedingEdge/bin/mono.exe' Tools/TestOutput/CompareOnline.exe "$Folder/window-before.save" "$Folder/window-after.save" | Tee-Object -FilePath "$Folder/window-online-replay.txt"
if($LASTEXITCODE -ne 0){throw 'Window complete state replay failed'}
