param([Parameter(Mandatory=$true)][string]$Folder,[Parameter(Mandatory=$true)][ValidateSet('Flow','Fit','States','Reload')][string]$Group,[ValidateSet('Default','Long','Narrow','SafeTall')][string[]]$Profiles=@('Default','Long','Narrow','SafeTall'),[ValidatePattern('^[a-z0-9-]*$')][string]$RunTag='')
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
if(!$env:FOREST_ADB_SERIAL){throw 'Explicit device required'}
$serial=$env:FOREST_ADB_SERIAL;$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$candidate=Split-Path -Leaf $Folder;$cutout='com.android.internal.display.cutout.emulation.tall'
foreach($name in @('normal-restorable.save','normal-restorable.save.bak','ordinary-fishing-audit.json')){if(!(Test-Path "$Folder/$name")){throw 'Protect and audit all latest ordinary progress before fixtures'}}
function A([string[]]$p){& $adb -P 5038 -s $serial @p;if($LASTEXITCODE -ne 0){throw 'Moment review device action failed'}}
function Plan($name,$steps,$dir){
 $p="$Folder/Plans/moment-$name.json";if(Test-Path $p){throw 'Keep previous plan'}
 @{directory=$dir;steps=$steps}|ConvertTo-Json -Depth 8|Set-Content $p
 & $python Tools/Forest-DeviceReview.py $p;if($LASTEXITCODE -ne 0){throw 'Moment UI plan failed'}
}
function Nav($id,$s,$h,$top){
 @(@{tap=@([int](950*$s),[int](160*$s+$top))},@{tap=@([int](925*$s),[int]($h-115*$s))},@{tap=@([int](900*$s),[int]($h-(640-212*$id)*$s))})
}
function RecordStart($name,$seconds){
 $file="$Folder/Evidence/$name.mp4";if(Test-Path $file){throw 'Keep original recording'}
 $remote="/sdcard/$candidate-$name.mp4"
 [DateTime]::UtcNow.ToString('o')|Set-Content "$Folder/$name-start.txt"
 $process=Start-Process -FilePath $adb -ArgumentList '-P','5038','-s',$serial,'shell','screenrecord','--bit-rate','4000000','--time-limit',([string]$seconds),$remote -WindowStyle Hidden -PassThru
 @{process=$process;remote=$remote;file=$file}
}
function RecordEnd($record){$record.process.WaitForExit();if($record.process.ExitCode -ne 0){throw 'Moment recorder failed'};A @('pull',$record.remote,$record.file)}
try{
 A @('shell','cmd','overlay','disable',$cutout);A @('shell','wm','size','reset')
 if($Group -eq 'Flow'){
  foreach($variant in @('start','simple')){
   $dir="$Folder/Evidence/MomentFlow/$variant";if(Test-Path $dir){throw 'Keep prior flow'}
   & Tools/Activate-MomentReview.ps1 -Folder $Folder -Variant $variant
   $record=RecordStart ('moment-flow-'+$variant) 150
   $steps=@()
   foreach($id in 0..2){
    $steps+=Nav $id 1 1920 0
    $steps+=@(@{shot="event-$id-00-clue"},@{tap=@(540,1080)},@{wait=1},@{shot="event-$id-01-found"})
    $object=@(@(728,942),@(626,903),@(783,997))[$id]
    $steps+=@(@{tap=$object},@{shot="event-$id-02-choice"},@{tap=@(270,1770)},@{wait=1},@{shot="event-$id-03-choice0-world"})
    $steps+=Nav $id 1 1920 0
    $steps+=@(@{tap=@(540,1080)},@{shot="event-$id-04-revisit-card"},@{tap=@(800,1770)},@{wait=1},@{shot="event-$id-05-choice1-world"})
   }
   # Region limits may deliberately keep the object visible after these drags.
   # Neutral names avoid asserting that an offscreen/ghost test succeeded solely
   # because the legacy gesture sequence was replayed.
   $steps+=@(@{tap=@(775,1640)},@{shot='30-world-zoom-out'},@{swipe=@(540,800,100,1500,900)},@{swipe=@(540,800,100,1500,900)},@{shot='31-after-two-boundary-drags'},@{tap=@(540,1315)},@{shot='32-after-tap-at-prior-position'},@{tap=@(998,1640)},@{shot='33-recenter'})
   $steps+=@(@{tap=@(950,160)},@{shot='34-handbook-finished'},@{tap=@(925,1805)},@{shot='35-journal-top'},@{drag_hold=@(1000,1750,1000,1100,500,600)},@{shot='36-journal-footer'},@{back=$true})
   Plan ('flow-'+$variant) $steps $dir
   A @('pull','/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save',"$Folder/moment-flow-$variant-after.save")
   RecordEnd $record
   Write-Output "COMPLETE moment flow $variant"
  }
 }elseif($Group -eq 'Fit'){
  foreach($profile in @(@{n='Default';w=1080;h=1920;s=1.0;top=0},@{n='Long';w=1080;h=2340;s=1.0;top=0},@{n='Narrow';w=720;h=1600;s=2.0/3;top=0},@{n='SafeTall';w=1080;h=1920;s=1.0;top=84})){
   if($profile.n -notin $Profiles){continue}
   $n=$profile.n;$s=$profile.s;$h=$profile.h;$top=$profile.top
   $stem=if($RunTag){$n+'-'+$RunTag}else{$n};$dir="$Folder/Evidence/MomentFit/$stem"
   if(Test-Path $dir){throw 'Keep prior fitting'}
   A @('shell','cmd','overlay','disable',$cutout);A @('shell','wm','size',($profile.w.ToString()+'x'+$h))
   if($top){A @('shell','cmd','overlay','enable',$cutout)}
   & Tools/Activate-MomentReview.ps1 -Folder $Folder -Variant await
   $record=RecordStart ('moment-fit-'+$stem) 100
   $steps=@(@{tap=@([int](950*$s),[int](160*$s+$top))},@{shot='00-handbook-and-four-footer-links'},@{tap=@([int](925*$s),[int]($h-115*$s))},@{shot='01-all-three-pending-cards'},@{drag_hold=@([int](1000*$s),[int]($h-170*$s),[int](1000*$s),[int]($h-800*$s),500,600)},@{shot='02-third-card-and-footer'},@{back=$true})
   # Forest exploration is capped at1280 design pixels on tall screens.
   # Resolve the actual 3D center rather than hitting its lower marker or air.
   $viewHeight=[Math]::Min(1280*$s,[Math]::Max(850*$s,$h-$top-664*$s))
   $centerY=[int](448*$s+$top+$viewHeight/2)
   foreach($id in 0..2){
    $steps+=Nav $id $s $h $top
    $steps+=@(@{shot="event-$id-10-actual-object"},@{tap=@([int](540*$s),$centerY)},@{shot="event-$id-11-choice-card"},@{tap=@([int](270*$s),[int]($h-150*$s))},@{wait=1},@{shot="event-$id-12-choice0-world"})
    $steps+=Nav $id $s $h $top
    $steps+=@(@{tap=@([int](540*$s),$centerY)},@{tap=@([int](800*$s),[int]($h-150*$s))},@{wait=1},@{shot="event-$id-13-choice1-world"})
   }
   $steps+=@(@{tap=@([int](950*$s),[int](160*$s+$top))},@{tap=@([int](925*$s),[int]($h-115*$s))},@{shot='30-completed-journal'},@{back=$true})
   Plan ('fit-'+$stem) $steps $dir
   RecordEnd $record
   A @('pull','/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save',"$Folder/moment-fit-$stem-after.save")
   A @('shell','wm','size')|Set-Content "$Folder/moment-$stem-size.txt"
   A @('shell','dumpsys','window','displays')|Set-Content "$Folder/moment-$stem-window.txt"
   Write-Output "COMPLETE moment fitting $stem"
  }
 }elseif($Group -eq 'States'){
  foreach($variant in @('locked','partial','finished')){
   & Tools/Activate-MomentReview.ps1 -Folder $Folder -Variant $variant
   $steps=@(@{tap=@(950,160)},@{shot='01-handbook'},@{tap=@(925,1805)},@{shot='02-journal-top'},@{drag_hold=@(1000,1750,1000,1100,500,600)},@{shot='03-journal-third-card-footer'},@{back=$true})
   if($variant -eq 'finished'){
    foreach($link in @(@{x=150;shot='04-chapter-from-footer'},@{x=415;shot='05-discovery-from-footer'},@{x=675;shot='06-fish-from-footer'})){
     $steps+=@(@{tap=@(950,160)},@{tap=@($link.x,1805)},@{shot=$link.shot},@{tap=@(50,250)})
    }
   }
   Plan ('journal-'+$variant) $steps "$Folder/Evidence/MomentStates/$variant"
   A @('pull','/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save',"$Folder/moment-state-$variant-after.save")
   Write-Output "COMPLETE moment journal $variant"
  }
 }else{
  & Tools/Activate-MomentReview.ps1 -Folder $Folder -Variant await
  Plan 'reload-before' ((Nav 2 1 1920 0)+@(@{tap=@(540,1080)},@{shot='01-pending-choice-before-close'},@{back=$true},@{shot='02-closed-without-choice'})) "$Folder/Evidence/MomentReload"
  A @('shell','input','keyevent','KEYCODE_HOME');Start-Sleep -Seconds 2
  A @('pull','/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save',"$Folder/moment-reload-before.save")
  & Tools/Capture-PresentationStartup.ps1 -Folder $Folder -Tag moment-pending-reload
  A @('pull','/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save',"$Folder/moment-reload-loaded.save")
  Plan 'reload-after' ((Nav 2 1 1920 0)+@(@{shot='03-same-guestbook-after-load'},@{tap=@(540,1080)},@{shot='04-pending-choice-kept'},@{tap=@(800,1770)},@{wait=1},@{shot='05-star-after-deliberate-choice'})) "$Folder/Evidence/MomentReload"
  A @('pull','/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save',"$Folder/moment-reload-after.save")
  Write-Output 'COMPLETE actual pending-choice reload'
 }
}finally{A @('shell','wm','size','reset');A @('shell','cmd','overlay','disable',$cutout)}
