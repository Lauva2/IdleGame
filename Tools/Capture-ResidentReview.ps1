param([Parameter(Mandatory=$true)][string]$Folder,[ValidateSet('Fit','Ambient')][string]$Group='Fit',[ValidateSet('Default','Long','Narrow','SafeTall')][string[]]$Profiles=@('Default','Long','Narrow','SafeTall'))
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
if(!$env:FOREST_ADB_SERIAL){throw 'Explicit device required'}
foreach($name in @('normal-restorable.save','normal-restorable.save.bak','ordinary-fishing-audit.json')){if(!(Test-Path "$Folder/$name")){throw 'Protect and audit latest ordinary progress first'}}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path;$serial=$env:FOREST_ADB_SERIAL
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
. Tools/Forest-ReviewRecorder.ps1
function A([string[]]$p){& $adb -P 5038 -s $serial @p;if($LASTEXITCODE -ne 0){throw 'Resident review ADB failed'}}
function Plan($name,$steps){
 $p="$Folder/Plans/residents-$name.json";if(Test-Path $p){throw 'Preserve previous plan'}
 @{directory="$Folder/Evidence/Residents/$name";steps=$steps}|ConvertTo-Json -Depth 8|Set-Content $p
 & $python Tools/Forest-DeviceReview.py $p;if($LASTEXITCODE -ne 0){throw 'Resident review inputs failed'}
}
try{
 if($Group -eq 'Fit'){
  foreach($profile in @(@{n='Default';w=1080;h=1920;s=1.0;top=0},@{n='Long';w=1080;h=2340;s=1.0;top=0},@{n='Narrow';w=720;h=1600;s=2.0/3;top=0},@{n='SafeTall';w=1080;h=1920;s=1.0;top=84})){
   if($profile.n -notin $Profiles){continue}
   $n=$profile.n;$s=$profile.s;$h=$profile.h;$top=$profile.top
   A @('shell','cmd','overlay','disable','com.android.internal.display.cutout.emulation.tall');A @('shell','wm','size',($profile.w.ToString()+'x'+$h))
   if($top){A @('shell','cmd','overlay','enable','com.android.internal.display.cutout.emulation.tall')}
   & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant simple
   if($LASTEXITCODE -ne 0){throw 'Resident fixture failed'}
   $viewHeight=[Math]::Min(1280,[Math]::Max(900,($h-$top)/$s-584))
   $entryY=[int]($top+(244+112+$viewHeight+48)*$s)
   $steps=@(@{tap=@([int](930*$s),[int](300*$s+$top))},@{shot='00-simple-five-visible'},@{tap=@([int](530*$s),$entryY)},@{shot='01-known-neighbours'})
   for($resident=0;$resident -lt 5;$resident++){
    if($resident -gt 0){$steps+=@{tap=@([int](530*$s),$entryY)}}
    $steps+=@(@{tap=@([int](850*$s),[int]($h-(740-154*$resident)*$s))},@{shot="note-$resident-full"},@{tap=@([int](850*$s),[int]($h-90*$s))},@{shot="note-$resident-closed"})
   }
   $record=Start-ForestReviewRecorder $Folder ('resident-fit-'+$n)
   Plan ('fit-'+$n) $steps
   Stop-ForestReviewRecorder $record
   A @('shell','wm','size')|Set-Content "$Folder/resident-$n-size.txt"
   A @('shell','dumpsys','window','displays')|Set-Content "$Folder/resident-$n-window.txt"
   A @('shell','input','keyevent','KEYCODE_HOME');Start-Sleep -Seconds 1
   A @('pull','/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save',"$Folder/resident-fit-$n-after.save")
   Write-Output "COMPLETE resident fit $n"
  }
 }else{
  A @('shell','wm','size','reset');A @('shell','cmd','overlay','disable','com.android.internal.display.cutout.emulation.tall')
  & Tools/Activate-MomentReview.ps1 -Folder $Folder -Variant finished
  $preparedState=Get-Content -LiteralPath "$Folder/Prepared/moment-finished.save" -Tail 1|ConvertFrom-Json
  $firstLocation=[int]$preparedState.forest.location
  @(@{kind='visit';id=$firstLocation;arg=0;expected=$true},@{kind='visit';id=0;arg=0;expected=$true},@{kind='visit';id=1;arg=0;expected=$true},@{kind='visit';id=2;arg=0;expected=$true})|ConvertTo-Json -Depth 4|Set-Content -LiteralPath "$Folder/resident-ambient-actions.json"
  $record=Start-ForestReviewRecorder $Folder 'resident-ambient-regions'
  $steps=@(@{shot='courtyard-rest'},@{wait=9},@{shot='courtyard-cycle'},@{tap=@(390,300)},@{wait=9},@{shot='garden-and-mature-feedback'},@{tap=@(660,300)},@{wait=9},@{shot='workshop-and-feedback'},@{tap=@(150,410)})
  for($i=0;$i -lt 3;$i++){$steps+=@(@{tap=@((170+350*$i),290)},@{shot="forest-$i-start"},@{wait=9},@{shot="forest-$i-cycle"})}
  Plan 'ambient' $steps
  Stop-ForestReviewRecorder $record
  A @('shell','input','keyevent','KEYCODE_HOME');Start-Sleep -Seconds 1
  A @('pull','/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save',"$Folder/resident-ambient-after.save")
 }
}finally{A @('shell','wm','size','reset');A @('shell','cmd','overlay','disable','com.android.internal.display.cutout.emulation.tall')}
