param(
 [string]$Folder='Docs/Verification/3d-d19-experience01',
 [ValidateSet('States','SafeTall')][string]$Mode='States'
)
$ErrorActionPreference='Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
$serial=if($env:FOREST_ADB_SERIAL){$env:FOREST_ADB_SERIAL}else{'127.0.0.1:16385'}
$adb='Tools/AndroidSdk/platform-tools/adb.exe'
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
function Plan($name,$steps){
 New-Item -ItemType Directory -Force "$Folder/Plans/Supplement"|Out-Null
 $path="$Folder/Plans/Supplement/$name.json"
 @{directory=($Folder+'/Evidence/Supplement');steps=$steps}|ConvertTo-Json -Depth 7|Set-Content $path
 & $python Tools/Forest-DeviceReview.py $path
 if($LASTEXITCODE -ne 0){throw 'Supplement capture failed'}
}
if($Mode -eq 'States'){
 & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant states
 Plan 'states' @(@{tap=@(930,300)},@{shot='01-shelf-empty-reserved'},@{tap=@(200,1690)},@{shot='02-shelf-written-states'},@{tap=@(540,1850)},@{shot='03-order-shortage'})
 & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant unrestored
 Plan 'before' @(@{shot='04-annex-before-overview'},@{tap=@(930,300)},@{shot='05-annex-before-shop'},@{tap=@(660,300)},@{shot='06-basic-curtain-rug'})
 & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant late
 Plan 'after' @(@{shot='07-annex-after-overview'},@{tap=@(930,300)},@{shot='08-annex-after-shop'},@{tap=@(660,300)},@{shot='09-reward-curtain-rug'},@{tap=@(1000,50)},@{shot='10-settings-default'})
}else{
 & $adb -P 5038 -s $serial shell cmd overlay list | Set-Content "$Folder/supplement-cutouts-before.txt"
 try{
  & $adb -P 5038 -s $serial shell wm size reset
  & $adb -P 5038 -s $serial shell cmd overlay enable com.android.internal.display.cutout.emulation.tall
  if($LASTEXITCODE -ne 0){throw 'Official cutout overlay unavailable'}
  & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant late
  Plan 'safe-tall' @(@{shot='11-safe-overview'},@{tap=@(390,384)},@{shot='12-safe-garden'},@{tap=@(885,1628)},@{shot='13-safe-real-zoom-in'},@{tap=@(775,1628)},@{shot='14-safe-real-zoom-out'},@{tap=@(998,1628)},@{shot='15-safe-real-recenter'},@{tap=@(930,384)},@{shot='16-safe-shop'},@{tap=@(1000,134)},@{shot='17-safe-settings'})
  & $adb -P 5038 -s $serial shell dumpsys window displays | Set-Content "$Folder/supplement-fit-SafeTall.txt"
 }finally{
  & $adb -P 5038 -s $serial shell wm size reset
  & $adb -P 5038 -s $serial shell cmd overlay disable com.android.internal.display.cutout.emulation.tall
  & $adb -P 5038 -s $serial shell wm size | Set-Content "$Folder/supplement-size-restored.txt"
  & $adb -P 5038 -s $serial shell cmd overlay list | Set-Content "$Folder/supplement-cutouts-restored.txt"
 }
}
# This suite uses explicitly prepared states. Restore the latest ordinary pair
# once all candidate capture has finished, with Restore-ForestOrdinary.ps1.
