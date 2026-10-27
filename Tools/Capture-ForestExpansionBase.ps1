param([Parameter(Mandatory=$true)][string]$Folder)
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
$serial=$env:FOREST_ADB_SERIAL;if(!$serial){throw 'Explicit ADB serial required'}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$candidate=Split-Path -Leaf $Folder
function Plan($name){
 # Reuse the recorded input plan; capture fresh framebuffer from the new APK.
 $p=Get-Content "Docs/Verification/3d-d21-forest05/Plans/forest-$name.json" -Raw|ConvertFrom-Json
 if($name -eq 'locked'){$p.steps[2].tap=& Tools/Get-ForestDockPoint.ps1 -Spot 0}
 $p.directory="$Folder/Evidence/Forest"
 $p|ConvertTo-Json -Depth 7|Set-Content "$Folder/Plans/forest-$name.json"
 & $python Tools/Forest-DeviceReview.py "$Folder/Plans/forest-$name.json"
 if($LASTEXITCODE -ne 0){throw 'Fresh forest base UI capture failed'}
}
& Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant forest-edge
$remote="/sdcard/$candidate-forest-greenhouse.mp4"
[DateTime]::UtcNow.ToString('o')|Set-Content "$Folder/forest-greenhouse-start.txt"
$record=Start-Process -FilePath $adb -ArgumentList '-P','5038','-s',$serial,'shell','screenrecord','--size','1080x1920','--bit-rate','4000000','--time-limit','65',$remote -WindowStyle Hidden -PassThru
Plan 'locked';Plan 'greenhouse';Plan 'nursery'
$record.WaitForExit();if($record.ExitCode -ne 0){throw 'Fresh greenhouse recorder failed'}
& $adb -P 5038 -s $serial pull $remote "$Folder/Evidence/forest-greenhouse.mp4"
& $adb -P 5038 -s $serial pull /sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save "$Folder/prepared-after-greenhouse.save"
& Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant forest-pavilion
$remote="/sdcard/$candidate-pavilion-repair.mp4"
[DateTime]::UtcNow.ToString('o')|Set-Content "$Folder/pavilion-repair-start.txt"
$record=Start-Process -FilePath $adb -ArgumentList '-P','5038','-s',$serial,'shell','screenrecord','--size','1080x1920','--bit-rate','4000000','--time-limit','35',$remote -WindowStyle Hidden -PassThru
Plan 'pavilion'
$record.WaitForExit();if($record.ExitCode -ne 0){throw 'Fresh pavilion recorder failed'}
& $adb -P 5038 -s $serial pull $remote "$Folder/Evidence/pavilion-repair.mp4"
& $adb -P 5038 -s $serial pull /sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save "$Folder/prepared-after-pavilion.save"
