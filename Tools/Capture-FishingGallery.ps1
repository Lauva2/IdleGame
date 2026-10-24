param([string]$Folder='Docs/Verification/3d-d21-forest05',[string]$RunTag='r02')
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
$serial=if($env:FOREST_ADB_SERIAL){$env:FOREST_ADB_SERIAL}else{'127.0.0.1:16384'}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$candidate=Split-Path -Leaf $Folder
if($RunTag -notmatch '^[a-zA-Z0-9-]+$'){throw 'Unsafe gallery tag'}
& Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant fishing-filled
if($LASTEXITCODE -ne 0){throw 'Gallery prepared fixture failed'}
$steps=@(@{tap=@(900,1850)},@{tap=@(900,300)},@{shot='00-twelve-fish-list-top'})
for($i=0;$i -lt 12;$i++){
    $steps+=@{tap=@(900,1850)}
    $distance=if($i -lt 9){$i*272}else{3000}
    while($distance -gt 0){$amount=[Math]::Min(1000,$distance);$steps+=@{drag_hold=@(1000,1640,1000,(1640-$amount),300,900)};$distance-=$amount}
    $y=if($i -lt 9){775}else{1100+($i-9)*272}
    $name='species-'+$i.ToString('D2')
    $steps+=@(@{tap=@(600,$y)},@{shot=($name+'-top')},@{swipe=@(850,1740,850,1340,800)},@{shot=($name+'-middle')},@{swipe=@(850,1740,850,1040,800)},@{shot=($name+'-bottom')},@{back=$true})
}
$steps+=@(@{tap=@(900,1850)},@{tap=@(535,515)},@{shot='13-discovered-filter'},@{tap=@(900,515)},@{shot='14-recent-first'})
for($i=0;$i -lt 10;$i++){$steps+=@{swipe=@(1070,1640,1070,640,750)}}
$steps+=@(@{shot='15-recent-thirtieth-and-retention-note'},@{tap=@(180,1850)},@{tap=@(150,410)},@{shot='16-grove-default'})
New-Item -ItemType Directory -Force -Path "$Folder/Plans/Gallery"|Out-Null
@{directory="$Folder/Evidence/Gallery-$RunTag";steps=$steps}|ConvertTo-Json -Depth 6|Set-Content "$Folder/Plans/Gallery/gallery-$RunTag.json"
$remote="/sdcard/$candidate-fishing-gallery-$RunTag.mp4";[DateTime]::UtcNow.ToString('o')|Set-Content "$Folder/fishing-gallery-$RunTag-start.txt"
$record=Start-Process -FilePath $adb -ArgumentList '-P','5038','-s',$serial,'shell','screenrecord','--size','1080x1920','--bit-rate','4000000','--time-limit','180',$remote -WindowStyle Hidden -PassThru
& $python Tools/Forest-DeviceReview.py "$Folder/Plans/Gallery/gallery-$RunTag.json"
if($LASTEXITCODE -ne 0){throw 'Gallery actual UI input failed'}
$record.WaitForExit();if($record.ExitCode -ne 0){throw 'Gallery recorder failed'}
& $adb -P 5038 -s $serial pull $remote "$Folder/Evidence/fishing-gallery-$RunTag.mp4"
