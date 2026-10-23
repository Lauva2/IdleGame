param([string]$Folder='Docs/Verification/3d-d18-forest04')
$ErrorActionPreference='Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
$adb='Tools/AndroidSdk/platform-tools/adb.exe'
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
function CapturePlan($steps,$suffix=''){
 @{directory=($Folder+'/Evidence'+$suffix);steps=$steps}|ConvertTo-Json -Depth 7|Set-Content Builds/d18-supplement-plan.json
 & $python Tools/Forest-DeviceReview.py Builds/d18-supplement-plan.json
 if($LASTEXITCODE -ne 0){throw 'Supplement plan failed'}
}
& Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant late
CapturePlan @(@{shot='66-fresh-stocked-overview'},@{tap=@(390,300)},@{shot='67-fresh-twelve-garden'},@{tap=@(930,300)},@{shot='68-fresh-stocked-shop'},@{tap=@(540,1850)},@{shot='76-ready-order-available'},@{tap=@(350,770)},@{shot='77-order-accepted-ready'})
try {
 & $adb -P 5038 -s 127.0.0.1:16385 shell wm size 720x1600
 foreach($sample in @(@{variant='report';stem='04-report'},@{variant='chapter';stem='05-chapter'},@{variant='dialogue1';stem='06-longest-dialogue'})){
  & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant $sample.variant
  CapturePlan @(@{shot=($sample.stem+'-top')},@{swipe=@(560,1480,560,960,650)},@{swipe=@(560,1480,560,960,650)},@{shot=($sample.stem+'-bottom')}) '/Narrow'
 }
 & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant states
 CapturePlan @(@{tap=@(360,1550)},@{shot='07-order-shortage'}) '/Narrow'
 & $adb -P 5038 -s 127.0.0.1:16385 shell wm size reset
 & $adb -P 5038 -s 127.0.0.1:16385 shell cmd overlay enable com.android.internal.display.cutout.emulation.tall
 & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant late
 CapturePlan @(@{shot='01-overview'},@{tap=@(390,384)},@{shot='02-garden'},@{tap=@(930,384)},@{shot='03-shop'},@{tap=@(1000,134)},@{shot='04-settings'},@{back=$true},@{tap=@(390,384)},@{shot='05-garden-full-controls'},@{tap=@(885,1628)},@{shot='06-garden-real-zoom-in'},@{tap=@(775,1628)},@{shot='07-garden-real-zoom-out'},@{tap=@(998,1628)},@{shot='08-garden-real-recenter'}) '/SafeTall'
 & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant chapter
 CapturePlan @(@{shot='09-chapter-top'},@{swipe=@(850,1740,850,1010,650)},@{shot='10-chapter-bottom'}) '/SafeTall'
 & $adb -P 5038 -s 127.0.0.1:16385 shell dumpsys window displays | Set-Content "$Folder/fit-SafeTall.txt"
}finally{
 & $adb -P 5038 -s 127.0.0.1:16385 shell wm size reset
 & $adb -P 5038 -s 127.0.0.1:16385 shell cmd overlay disable com.android.internal.display.cutout.emulation.tall
 & $adb -P 5038 -s 127.0.0.1:16385 shell wm size | Set-Content "$Folder/display-restored.txt"
 & $adb -P 5038 -s 127.0.0.1:16385 shell cmd overlay list | Set-Content "$Folder/cutout-restored.txt"
}
