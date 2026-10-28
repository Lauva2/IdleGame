param([Parameter(Mandatory=$true)][string]$Folder)
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
if(!$env:FOREST_ADB_SERIAL){throw 'Explicit device required'}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path;$serial=$env:FOREST_ADB_SERIAL
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
function A([string[]]$p){$result=& $adb -P 5038 -s $serial @p;if($LASTEXITCODE -ne 0){throw 'Foreground fit ADB failed'};return $result}
try{
 foreach($p in @(@{n='Default';w=1080;h=1920;s=1.0;top=0},@{n='Long';w=1080;h=2340;s=1.0;top=0},@{n='Narrow';w=720;h=1600;s=2.0/3;top=0},@{n='SafeTall';w=1080;h=1920;s=1.0;top=84})){
  $n=$p.n;$s=$p.s;$h=$p.h;$top=$p.top
  $dir="$Folder/Evidence/Residents/front-$n";if(Test-Path $dir){throw 'Preserve earlier foreground evidence'}
  A @('shell','cmd','overlay','disable','com.android.internal.display.cutout.emulation.tall')|Out-Null
  A @('shell','wm','size',($p.w.ToString()+'x'+$h))|Out-Null
  if($top){A @('shell','cmd','overlay','enable','com.android.internal.display.cutout.emulation.tall')|Out-Null}
  & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant simple
  if($LASTEXITCODE -ne 0){throw 'Foreground fit fixture failed'}
  $viewHeight=[Math]::Min(1280,[Math]::Max(900,($h-$top)/$s-584))
  $entryY=[int]($top+(404+$viewHeight)*$s)
  $plan="$Folder/Plans/resident-front-$n.json"
  @{directory=$dir;steps=@(@{tap=@([int](930*$s),[int](300*$s+$top))},@{tap=@([int](530*$s),$entryY)},@{tap=@([int](850*$s),[int]($h-432*$s))},@{shot='00-longest-note-foreground'})}|ConvertTo-Json -Depth 7|Set-Content $plan
  & $python Tools/Forest-DeviceReview.py $plan
  if($LASTEXITCODE -ne 0){throw 'Foreground note input failed'}
  $window=A @('shell','dumpsys','window','displays')
  if(!(($window -join "`n") -match 'mCurrentFocus=.*com\.forestatelier\.curioshop')){throw 'Game is not foreground'}
  $window|Set-Content "$Folder/resident-front-$n-window.txt"
  A @('shell','dumpsys','activity','top')|Set-Content "$Folder/resident-front-$n-activity.txt"
  A @('shell','wm','size')|Set-Content "$Folder/resident-front-$n-size.txt"
  A @('shell','settings','get','system','font_scale')|Set-Content "$Folder/resident-front-$n-font-scale.txt"
  $close="$Folder/Plans/resident-front-$n-close.json"
  @{directory=$dir;steps=@(@{tap=@([int](850*$s),[int]($h-90*$s))},@{shot='01-actual-footer-close'})}|ConvertTo-Json -Depth 7|Set-Content $close
  & $python Tools/Forest-DeviceReview.py $close
  if($LASTEXITCODE -ne 0){throw 'Foreground footer close failed'}
  Write-Output "COMPLETE game-foreground fit $n"
 }
}finally{A @('shell','wm','size','reset')|Out-Null;A @('shell','cmd','overlay','disable','com.android.internal.display.cutout.emulation.tall')|Out-Null}
