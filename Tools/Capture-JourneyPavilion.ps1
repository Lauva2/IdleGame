param([Parameter(Mandatory=$true)][string]$Folder)
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
$serial=$env:FOREST_ADB_SERIAL;if(!$serial){throw 'Explicit ADB serial required'}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$candidate=Split-Path -Leaf $Folder
if(Test-Path "$Folder/Evidence/Pavilion"){throw 'Preserve original pavilion evidence'}
function Plan($name,$steps){
 $p="$Folder/Plans/$name.json"
 @{directory="$Folder/Evidence/Pavilion";steps=$steps}|ConvertTo-Json -Depth 7|Set-Content $p
 & $python Tools/Forest-DeviceReview.py $p
 if($LASTEXITCODE -ne 0){throw 'Actual pavilion plan failed'}
}
foreach($mode in @('filled','simple')){
 & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant ('fishing-'+$mode)
 if($LASTEXITCODE -ne 0){throw 'Pavilion fixture activation failed'}
 $remote="/sdcard/$candidate-fishing-motion-pavilion-$mode.mp4"
 [DateTime]::UtcNow.ToString('o')|Set-Content "$Folder/pavilion-$mode-start.txt"
 $record=Start-Process -FilePath $adb -ArgumentList '-P','5038','-s',$serial,'shell','screenrecord','--size','1080x1920','--bit-rate','4000000','--time-limit','65',$remote -WindowStyle Hidden -PassThru
 $steps=@(@{tap=@(150,410)},@{shot=($mode+'-01-actual-map')},@{tap=@(780,1250)},@{shot=($mode+'-02-pavilion-wait')},@{tap=@(500,1835)},@{shot=($mode+'-02b-expanded-details')},@{tap=@(930,1530)},@{wait=14},@{shot=($mode+'-03-actual-berry-bait-catch')})
 if($mode -eq 'filled'){
  $steps+=@(@{tap=@(500,1835)},@{tap=@(717,1835)},@{shot='filled-04-zoom-out'},@{swipe=@(550,700,650,800,700)},@{shot='filled-05-real-pan'},@{tap=@(977,1835)},@{shot='filled-06-recenter'})
 }
 Plan ('pavilion-'+$mode) $steps
 & $adb -P 5038 -s $serial pull /sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save "$Folder/pavilion-$mode-after.save"
 if($LASTEXITCODE -ne 0){throw 'Pavilion state pull failed'}
 $record.WaitForExit();if($record.ExitCode -ne 0){throw 'Pavilion recorder failed'}
 & $adb -P 5038 -s $serial pull $remote "$Folder/Evidence/fishing-motion-pavilion-$mode.mp4"
 if($LASTEXITCODE -ne 0){throw 'Pavilion movie pull failed'}
}
