param([Parameter(Mandatory=$true)][string]$Folder)
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
$serial=$env:FOREST_ADB_SERIAL;if(!$serial){throw 'Explicit ADB serial required'}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$candidate=Split-Path -Leaf $Folder
if(Test-Path "$Folder/Evidence/FishingSpots-r02"){throw 'Preserve original supplement'}
function Plan($name,$steps){
 @{directory="$Folder/Evidence/FishingSpots-r02";steps=$steps}|ConvertTo-Json -Depth 7|Set-Content "$Folder/Plans/$name.json"
 & $python Tools/Forest-DeviceReview.py "$Folder/Plans/$name.json"
 if($LASTEXITCODE -ne 0){throw 'Actual corrected spot plan failed'}
}
& Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant fishing-filled
$remote="/sdcard/$candidate-fishing-motion-spots-r02.mp4"
[DateTime]::UtcNow.ToString('o')|Set-Content "$Folder/fishing-motion-spots-r02-start.txt"
$record=Start-Process -FilePath $adb -ArgumentList '-P','5038','-s',$serial,'shell','screenrecord','--size','1080x1920','--bit-rate','4000000','--time-limit','65',$remote -WindowStyle Hidden -PassThru
Plan 'deep-water-corrected' @(@{tap=@(150,410)},@{tap=@(540,300)},@{shot='40-deep-water-map-actual-entry'},@{tap=@(320,1380)},@{tap=@(540,1530)},@{shot='41-deep-water-grass-wait'},@{wait=14},@{shot='42-deep-water-actual-catch'},@{tap=@(590,1630)},@{shot='43-deep-water-individual-location-and-bait'})
& $adb -P 5038 -s $serial pull /sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save "$Folder/prepared-deep-water-after.save"
Plan 'shallows-corrected' @(@{back=$true},@{tap=@(180,300)},@{shot='44-shallows-map'},@{tap=@(860,880)},@{tap=@(180,1530)},@{shot='45-shallows-grain-wait'},@{wait=14},@{shot='46-shallows-actual-catch'},@{tap=@(590,1630)},@{shot='47-shallows-individual-location-and-bait'},@{back=$true},@{shot='48-restored-forest-markers'})
& $adb -P 5038 -s $serial pull /sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save "$Folder/prepared-shallows-after.save"
$record.WaitForExit();if($record.ExitCode -ne 0){throw 'Corrected spot recorder failed'}
& $adb -P 5038 -s $serial pull $remote "$Folder/Evidence/fishing-motion-spots-r02.mp4"
