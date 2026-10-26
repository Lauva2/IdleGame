param([Parameter(Mandatory=$true)][string]$Folder,[Parameter(Mandatory=$true)][ValidateSet('Entry','Flow','Restore')][string]$Phase)
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
$serial=$env:FOREST_ADB_SERIAL;if(!$serial){throw 'Explicit device required'}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$cutout='com.android.internal.display.cutout.emulation.tall'
$dir="$Folder/Evidence/PresentationFit/SafeTall-r02"
function A([string[]]$p){& $adb -P 5038 -s $serial @p;if($LASTEXITCODE -ne 0){throw 'Safe-area device command failed'}}
function Plan($name,$steps){
 $p="$Folder/Plans/presentation-safetall-r02-$name.json"
 if(Test-Path $p){throw 'Preserve original correction plan'}
 @{directory=$dir;steps=$steps}|ConvertTo-Json -Depth 7|Set-Content $p
 & $python Tools/Forest-DeviceReview.py $p
 if($LASTEXITCODE -ne 0){throw 'Safe-area input failed'}
}
if($Phase -eq 'Entry'){
 if(Test-Path $dir){throw 'Preserve earlier correction evidence'}
 A @('shell','wm','size','1080x1920');A @('shell','cmd','overlay','enable',$cutout)
 & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant fishing-filled
 Plan 'entry' @(@{tap=@(150,494)},@{shot='00-actual-safe-area-map'},@{tap=@(780,1275)},@{shot='01-confirmed-compact-pavilion'})
 A @('shell','dumpsys','window','displays')|Set-Content "$Folder/presentation-SafeTall-r02-window.txt"
 Write-Output 'ENTRY requires visual confirmation before Flow'
}elseif($Phase -eq 'Flow'){
 if(!(Test-Path "$dir/01-confirmed-compact-pavilion.png")){throw 'Capture and inspect the actual entry first'}
 $output="$Folder/Evidence/presentation-fit-SafeTall-r02.mp4"
 if(Test-Path $output){throw 'Preserve original corrected recording'}
 $remote='/sdcard/'+(Split-Path -Leaf $Folder)+'-presentation-safetall-r02.mp4'
 $record=Start-Process -FilePath $adb -ArgumentList '-P','5038','-s',$serial,'shell','screenrecord','--bit-rate','4000000','--time-limit','65',$remote -WindowStyle Hidden -PassThru
 Plan 'flow' @(@{shot='02-compact-recording-start'},@{tap=@(500,1835)},@{shot='03-details-and-three-baits'},@{tap=@(930,1530)},@{shot='04-changed-berry-bait'},@{tap=@(500,1835)},@{shot='05-expanded-context'},@{tap=@(500,1835)},@{tap=@(717,1835)},@{shot='06-zoom-out'},@{swipe=@(550,784,650,884,700)},@{shot='07-real-pan'},@{tap=@(977,1835)},@{wait=14},@{shot='08-recenter-and-actual-catch'},@{tap=@(1000,134)},@{shot='09-settings-directly-from-fishing'},@{back=$true},@{shot='10-navigation-after-settings'},@{tap=@(780,1275)},@{shot='11-reentered-original-spot'},@{tap=@(590,1690)},@{shot='12-individual-top'},@{drag_hold=@(1000,1740,1000,1220,400,600)},@{shot='13-individual-bottom'},@{back=$true},@{shot='14-global-navigation-restored'},@{tap=@(1000,134)},@{shot='15-settings-accessible'})
 $record.WaitForExit();if($record.ExitCode -ne 0){throw 'Corrected native recorder failed'}
 A @('pull',$remote,$output)
 A @('pull','/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save',"$Folder/presentation-SafeTall-r02-after.save")
 Write-Output 'COMPLETE corrected safe-area native flow'
}else{
 A @('shell','wm','size','reset');A @('shell','cmd','overlay','disable',$cutout)
 A @('shell','wm','size')|Set-Content "$Folder/presentation-r02-display-restored.txt"
}
