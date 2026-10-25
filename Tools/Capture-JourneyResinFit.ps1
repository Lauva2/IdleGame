param([Parameter(Mandatory=$true)][string]$Folder)
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
$serial=$env:FOREST_ADB_SERIAL;if(!$serial){throw 'Explicit ADB serial required'}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
function A([string[]]$p){& $adb -P 5038 -s $serial @p;if($LASTEXITCODE -ne 0){throw 'Actual resin fit command failed'}}
try{
 foreach($case in @(@{n='Long';w=1080;h=2340;s=1.0;top=0},@{n='Narrow';w=720;h=1600;s=2.0/3;top=0},@{n='SafeTall';w=1080;h=1920;s=1.0;top=84})){
  A @('shell','cmd','overlay','disable','com.android.internal.display.cutout.emulation.tall');A @('shell','wm','size',($case.w.ToString()+'x'+$case.h))
  if($case.top){A @('shell','cmd','overlay','enable','com.android.internal.display.cutout.emulation.tall')}
  & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant journey-resin
  $s=$case.s;$h=$case.h;$top=$case.top
  $p="$Folder/Plans/resin-fit-$($case.n).json"
  @{directory="$Folder/Evidence/ResinFit/$($case.n)";steps=@(@{tap=@([int](950*$s),[int](160*$s+$top))},@{shot='01-handbook'},@{tap=@([int](900*$s),[int]($h-633*$s))},@{shot='02-actual-resin-description'})}|ConvertTo-Json -Depth 7|Set-Content $p
  & $python Tools/Forest-DeviceReview.py $p;if($LASTEXITCODE -ne 0){throw 'Actual resin fit plan failed'}
  A @('shell','dumpsys','window','displays')|Set-Content "$Folder/resin-fit-$($case.n)-window.txt"
 }
}finally{A @('shell','wm','size','reset');A @('shell','cmd','overlay','disable','com.android.internal.display.cutout.emulation.tall')}
