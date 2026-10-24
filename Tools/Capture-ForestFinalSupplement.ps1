param([string]$Folder='Docs/Verification/3d-d21-forest05')
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
$serial=if($env:FOREST_ADB_SERIAL){$env:FOREST_ADB_SERIAL}else{'127.0.0.1:16384'}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant forest-grove
$remote='/sdcard/'+(Split-Path -Leaf $Folder)+'-resin-supplement.mp4';[DateTime]::UtcNow.ToString('o')|Set-Content "$Folder/resin-supplement-start.txt"
$record=Start-Process -FilePath $adb -ArgumentList '-P','5038','-s',$serial,'shell','screenrecord','--size','1080x1920','--bit-rate','4000000','--time-limit','25',$remote -WindowStyle Hidden -PassThru
$steps=@(@{tap=@(150,410)},@{tap=@(300,1140)},@{shot='30-known-resin-before-actual-gather'},@{tap=@(850,1605)},@{shot='31-actual-resin-receipt'},@{wait=2},@{tap=@(300,1140)},@{shot='32-resin-cooldown-and-inventory-after'})
@{directory="$Folder/Evidence/Forest";steps=$steps}|ConvertTo-Json -Depth 6|Set-Content "$Folder/Plans/forest-resin-supplement.json"
& $python Tools/Forest-DeviceReview.py "$Folder/Plans/forest-resin-supplement.json"
if($LASTEXITCODE -ne 0){throw 'Resin actual supplement failed'}
$record.WaitForExit();& $adb -P 5038 -s $serial pull $remote "$Folder/Evidence/resin-supplement.mp4"
& $adb -P 5038 -s $serial pull /sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save "$Folder/prepared-after-resin-supplement.save"
& Tools/Capture-ForestShopping.ps1 -Folder $Folder -Mode Reduced
& Tools/Capture-ForestReviewDetails.ps1 -Folder $Folder -Mode Credits
[DateTime]::UtcNow.ToString('o')|Set-Content "$Folder/final-supplement-completed.txt"
