param([string]$Folder='Docs/Verification/3d-d21-forest05')
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
$serial=if($env:FOREST_ADB_SERIAL){$env:FOREST_ADB_SERIAL}else{'127.0.0.1:16384'}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$candidate=Split-Path -Leaf $Folder
function Plan($name,$steps){$path="$Folder/Plans/forest-$name.json";@{directory="$Folder/Evidence/Forest";steps=$steps}|ConvertTo-Json -Depth 6|Set-Content $path;& $python Tools/Forest-DeviceReview.py $path;if($LASTEXITCODE -ne 0){throw 'Forest actual UI plan failed'}}
& Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant forest-vines
$remote="/sdcard/$candidate-forest-dew-resin-vines.mp4";[DateTime]::UtcNow.ToString('o')|Set-Content "$Folder/forest-dew-resin-vines-start.txt"
$record=Start-Process -FilePath $adb -ArgumentList '-P','5038','-s',$serial,'shell','screenrecord','--size','1080x1920','--bit-rate','4000000','--time-limit','65',$remote -WindowStyle Hidden -PassThru
Plan 'dew-resin-vines' @(@{tap=@(150,410)},@{shot='15-creek-before-vines'},@{tap=@(300,1140)},@{shot='16-dew-inspection'},@{tap=@(850,1605)},@{shot='17-actual-dew-gather'},@{tap=@(900,300)},@{shot='18-vine-requirements'},@{tap=@(850,1600)},@{shot='19-vine-path-open'},@{tap=@(900,300)},@{shot='20-grove-new-path'},@{tap=@(300,1140)},@{shot='21-resin-inspection'},@{tap=@(850,1765)},@{shot='22-actual-resin-gather'})
$record.WaitForExit();if($record.ExitCode -ne 0){throw 'Forest actual recorder failed'}
& $adb -P 5038 -s $serial pull $remote "$Folder/Evidence/forest-dew-resin-vines.mp4"
for($i=0;$i -lt 3;$i++){
    & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant "forest-dialogue$i"
    Plan "dialogue$i" @(@{shot="23-forest-dialogue-$i-top"},@{swipe=@(850,1740,850,1040,700)},@{shot="24-forest-dialogue-$i-bottom"})
}
& Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant forest-full
Plan 'nursery-full' @(@{tap=@(150,410)},@{tap=@(540,400)},@{shot='25-both-species-mature-full-warehouse'},@{tap=@(305,1310)},@{shot='26-nursery-full-written-status'},@{back=$true},@{tap=@(180,300)},@{tap=@(300,1140)},@{shot='27-full-warehouse-forage-disabled'})
& Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant forest-report
$steps=@(@{shot='28-expanded-offline-report-top'});for($i=0;$i -lt 5;$i++){$steps+=@{swipe=@(850,1740,850,1040,700)}};$steps+=@{shot='29-expanded-offline-report-bottom'}
Plan 'expanded-report' $steps
[DateTime]::UtcNow.ToString('o')|Set-Content "$Folder/forest-states-completed.txt"
