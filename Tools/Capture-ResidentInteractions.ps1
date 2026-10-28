param([Parameter(Mandatory=$true)][string]$Folder,[ValidateSet('Rays','Progress')][string]$Group='Rays')
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
if(!$env:FOREST_ADB_SERIAL){throw 'Explicit device required'}
foreach($name in @('normal-restorable.save','normal-restorable.save.bak','ordinary-fishing-audit.json')){if(!(Test-Path "$Folder/$name")){throw 'Protect latest ordinary progress first'}}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path;$serial=$env:FOREST_ADB_SERIAL
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
. Tools/Forest-ReviewRecorder.ps1
function A([string[]]$p){& $adb -P 5038 -s $serial @p;if($LASTEXITCODE -ne 0){throw 'Resident input ADB failed'}}
function Plan($name,$steps){
 $p="$Folder/Plans/resident-$name.json";if(Test-Path $p){throw 'Preserve prior plan'}
 @{directory="$Folder/Evidence/Residents/$name";steps=$steps}|ConvertTo-Json -Depth 8|Set-Content $p
 & $python Tools/Forest-DeviceReview.py $p;if($LASTEXITCODE -ne 0){throw 'Resident real input failed'}
}
A @('shell','wm','size','reset');A @('shell','cmd','overlay','disable','com.android.internal.display.cutout.emulation.tall')
if($Group -eq 'Rays'){
 & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant simple
 $record=Start-ForestReviewRecorder $Folder 'resident-rays'
 $steps=@(@{tap=@(930,300)},@{shot='00-five-world-targets'})
 $points=@(@(233,1145),@(313,1140),@(484,1140),@(647,1160),@(764,1174))
 for($i=0;$i -lt 5;$i++){$steps+=@(@{tap=$points[$i]},@{shot="01-ray-$i-note"},@{back=$true},@{shot="02-ray-$i-closed"})}
 $steps+=@(@{drag_hold=@(647,1160,650,900,650,400)},@{shot='10-drag-from-resident-no-note'},@{tap=@(647,1160)},@{shot='11-prior-position-after-drag'},@{back=$true},@{tap=@(998,1572)},@{shot='12-recentered'},@{tap=@(764,1174)},@{shot='13-recentered-bear-note'},@{back=$true})
 for($i=0;$i -lt 5;$i++){$steps+=@{tap=@(775,1572)}}
 $steps+=@(@{shot='14-wide'},@{drag_hold=@(300,900,700,1020,600,400)},@{shot='15-wide-drag'},@{tap=@(998,1572)},@{tap=@(484,1140)},@{shot='16-after-zoom-pan-resident-note'},@{back=$true})
 Plan 'rays' $steps
 Stop-ForestReviewRecorder $record
 A @('shell','input','keyevent','KEYCODE_HOME');Start-Sleep -Seconds 1
 A @('pull','/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save',"$Folder/resident-rays-after.save")
}else{
 foreach($variant in @('greeting-first','greeting-middle','greeting-after')){
  & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant $variant
  $record=Start-ForestReviewRecorder $Folder $variant
  $steps=@(@{tap=@(930,300)},@{shot='00-progress-world'},@{tap=@(530,1684)},@{shot='01-recognition-gate'})
  $known=if($variant -eq 'greeting-first'){1}elseif($variant -eq 'greeting-middle'){3}else{5}
  for($i=0;$i -lt $known;$i++){
   if($i -gt 0){$steps+=@{tap=@(530,1684)}}
   # EndSheet shrinks the list to the actual number of known residents.
   $height=[Math]::Min(940,[Math]::Max(400,187+154*$known))
   $rowY=1920-$height+200+154*$i
   $steps+=@(@{tap=@(850,[int]$rowY)},@{shot="02-progress-note-$i"},@{back=$true})
  }
  Plan $variant $steps
  Stop-ForestReviewRecorder $record
  A @('shell','input','keyevent','KEYCODE_HOME');Start-Sleep -Seconds 1
  A @('pull','/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save',"$Folder/resident-$variant-after.save")
 }
}
