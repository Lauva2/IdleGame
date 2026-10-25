param([Parameter(Mandatory=$true)][string]$Folder)
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
$serial=$env:FOREST_ADB_SERIAL;if(!$serial){throw 'Explicit ADB serial required'}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$candidate=Split-Path -Leaf $Folder
if(Test-Path "$Folder/Evidence/FishingSpots"){throw 'Preserve original spot suite'}
function Plan($name,$steps){
 @{directory="$Folder/Evidence/FishingSpots";steps=$steps}|ConvertTo-Json -Depth 7|Set-Content "$Folder/Plans/$name.json"
 & $python Tools/Forest-DeviceReview.py "$Folder/Plans/$name.json"
 if($LASTEXITCODE -ne 0){throw 'Actual fishing spot plan failed'}
}
& Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant fishing-filled
$remote="/sdcard/$candidate-fishing-motion-spots.mp4"
[DateTime]::UtcNow.ToString('o')|Set-Content "$Folder/fishing-motion-spots-start.txt"
$record=Start-Process -FilePath $adb -ArgumentList '-P','5038','-s',$serial,'shell','screenrecord','--size','1080x1920','--bit-rate','4000000','--time-limit','105',$remote -WindowStyle Hidden -PassThru
Plan 'three-spots' @(@{tap=@(150,410)},@{shot='20-pavilion-map'},@{tap=@(780,1250)},@{shot='21-pavilion-pool-wait'},@{tap=@(930,1530)},@{wait=14},@{shot='22-actual-berry-bait-catch'},@{tap=@(686,1802)},@{shot='23-pool-zoom-out'},@{swipe=@(550,700,650,800,700)},@{shot='24-pool-real-pan'},@{tap=@(966,1802)},@{shot='25-pool-recenter'},@{back=$true},@{tap=@(540,300)},@{shot='26-creek-map'},@{tap=@(320,1380)},@{tap=@(540,1530)},@{shot='27-deep-water-grass-bait-wait'},@{wait=14},@{shot='28-deep-water-actual-catch'},@{tap=@(826,1802)},@{shot='29-deep-water-zoom-in'},@{back=$true},@{tap=@(180,300)},@{shot='30-shallows-map'},@{tap=@(860,880)},@{shot='31-shallows-cast-or-wait'},@{wait=14},@{shot='32-shallows-actual-catch'},@{back=$true},@{shot='33-restored-world-markers'})
$record.WaitForExit();if($record.ExitCode -ne 0){throw 'Actual three-spot recorder failed'}
& $adb -P 5038 -s $serial pull $remote "$Folder/Evidence/fishing-motion-spots.mp4"
& Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant fishing-simple
$remote="/sdcard/$candidate-fishing-motion-simple.mp4"
[DateTime]::UtcNow.ToString('o')|Set-Content "$Folder/fishing-motion-simple-start.txt"
$record=Start-Process -FilePath $adb -ArgumentList '-P','5038','-s',$serial,'shell','screenrecord','--size','1080x1920','--bit-rate','4000000','--time-limit','32',$remote -WindowStyle Hidden -PassThru
Plan 'fishing-simple' @(@{tap=@(150,410)},@{tap=@(780,1250)},@{shot='34-simple-wait'},@{wait=14},@{shot='35-simple-actual-catch'},@{back=$true})
$record.WaitForExit();if($record.ExitCode -ne 0){throw 'Simple recorder failed'}
& $adb -P 5038 -s $serial pull $remote "$Folder/Evidence/fishing-motion-simple.mp4"
