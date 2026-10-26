param([Parameter(Mandatory=$true)][string]$Folder,[switch]$Fit)
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
$serial=$env:FOREST_ADB_SERIAL;if(!$serial){throw 'Explicit device required'}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$cutout='com.android.internal.display.cutout.emulation.tall';$candidate=Split-Path -Leaf $Folder
function A([string[]]$p){& $adb -P 5038 -s $serial @p;if($LASTEXITCODE -ne 0){throw 'Fishing presentation command failed'}}
function Plan($name,$steps,$dir){$p="$Folder/Plans/presentation-$name.json";@{directory=$dir;steps=$steps}|ConvertTo-Json -Depth 7|Set-Content $p;& $python Tools/Forest-DeviceReview.py $p;if($LASTEXITCODE -ne 0){throw 'Fishing input failed'}}
New-Item -ItemType Directory -Force -Path "$Folder/Plans"|Out-Null
if($Fit){
 try{
  foreach($profile in @(@{n='Default';w=1080;h=1920;s=1;top=0},@{n='Long';w=1080;h=2340;s=1;top=0},@{n='Narrow';w=720;h=1600;s=2.0/3;top=0},@{n='SafeTall';w=1080;h=1920;s=1;top=84})){
   $n=$profile.n;$s=$profile.s;$h=$profile.h;$top=$profile.top;$dir="$Folder/Evidence/PresentationFit/$n"
   # The official cutout also shrinks the 3D viewport. Use its measured
   # pavilion projection rather than adding the entire 84px to the marker.
   $fishY=if($top){1275}else{[int](1250*$s)}
   if(Test-Path $dir){throw 'Keep existing fitting evidence'}
   A @('shell','cmd','overlay','disable',$cutout);A @('shell','wm','size',($profile.w.ToString()+'x'+$h))
   if($top){A @('shell','cmd','overlay','enable',$cutout)}
   & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant fishing-filled
   $remote="/sdcard/$candidate-presentation-fit-$n.mp4"
   $record=Start-Process -FilePath $adb -ArgumentList '-P','5038','-s',$serial,'shell','screenrecord','--bit-rate','4000000','--time-limit','65',$remote -WindowStyle Hidden -PassThru
   $steps=@(@{tap=@([int](150*$s),[int](410*$s+$top))},@{tap=@([int](780*$s),$fishY)},@{shot='01-compact-pavilion'},@{tap=@([int](500*$s),[int]($h-85*$s))},@{shot='02-details-and-three-baits'},@{tap=@([int](930*$s),[int]($h-390*$s))},@{shot='03-changed-berry-bait'},@{tap=@([int](500*$s),[int]($h-85*$s))},@{shot='04-expanded-context'},@{tap=@([int](500*$s),[int]($h-85*$s))},@{tap=@([int](717*$s),[int]($h-85*$s))},@{shot='05-zoom-out'},@{swipe=@([int](550*$s),[int](700*$s+$top),[int](650*$s),[int](800*$s+$top),700)},@{shot='06-real-pan'},@{tap=@([int](977*$s),[int]($h-85*$s))},@{wait=14},@{shot='07-recenter-and-actual-catch'},@{tap=@([int](1000*$s),[int](50*$s+$top))},@{shot='08-settings-directly-from-fishing'},@{back=$true},@{shot='09-navigation-after-settings'},@{tap=@([int](780*$s),$fishY)},@{shot='10-reentered-original-spot'},@{tap=@([int](590*$s),[int]($h-230*$s))},@{shot='11-individual-top'},@{drag_hold=@([int](1000*$s),[int]($h-180*$s),[int](1000*$s),[int]($h-700*$s),400,600)},@{shot='12-individual-bottom'},@{back=$true},@{shot='13-global-navigation-restored'},@{tap=@([int](1000*$s),[int](50*$s+$top))},@{shot='14-settings-accessible'})
   Plan ('fit-'+$n) $steps $dir
   $record.WaitForExit();if($record.ExitCode -ne 0){throw 'Fitting recorder failed'}
   A @('pull',$remote,"$Folder/Evidence/presentation-fit-$n.mp4")
   A @('shell','wm','size')|Set-Content "$Folder/presentation-$n-size.txt"
   A @('shell','dumpsys','window','displays')|Set-Content "$Folder/presentation-$n-window.txt"
   Write-Output "COMPLETE fishing fitting $n"
  }
 }finally{A @('shell','wm','size','reset');A @('shell','cmd','overlay','disable',$cutout)}
}else{
 foreach($variant in @('fishing-filled','fishing-simple')){
  & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant $variant
  $dir="$Folder/Evidence/PresentationSpots/$variant";if(Test-Path $dir){throw 'Keep original spot evidence'}
  $remote="/sdcard/$candidate-presentation-spots-$variant.mp4"
  $record=Start-Process -FilePath $adb -ArgumentList '-P','5038','-s',$serial,'shell','screenrecord','--size','1080x1920','--bit-rate','4000000','--time-limit','100',$remote -WindowStyle Hidden -PassThru
  $steps=@(@{tap=@(150,410)})
  foreach($spot in @(@{n=2;x=900;mx=780;my=1250;bx=930},@{n=1;x=540;mx=320;my=1380;bx=540},@{n=0;x=180;mx=860;my=880;bx=180})){
   $k=$spot.n
   if($k -ne 2){$steps+=@{tap=@($spot.x,300)}}
   $steps+=@(@{shot="spot-$k-map"},@{tap=@($spot.mx,$spot.my)},@{shot="spot-$k-compact"},@{tap=@(500,1835)},@{shot="spot-$k-details"},@{tap=@($spot.bx,1530)},@{wait=14},@{shot="spot-$k-actual-catch"},@{tap=@(500,1835)},@{shot="spot-$k-bait-and-context"},@{back=$true},@{shot="spot-$k-leave"})
  }
  Plan $variant $steps $dir
  A @('pull','/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save',"$Folder/presentation-spots-$variant-after.save")
  $record.WaitForExit();if($record.ExitCode -ne 0){throw 'Spot recorder failed'}
  A @('pull',$remote,"$Folder/Evidence/presentation-spots-$variant.mp4")
  Write-Output "COMPLETE actual three spots $variant"
 }
}
