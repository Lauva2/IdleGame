param([Parameter(Mandatory=$true)][string]$Folder)
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
$serial=$env:FOREST_ADB_SERIAL;if(!$serial){throw 'Explicit ADB serial required'}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
if(Test-Path "$Folder/Evidence/FacilitiesFit"){throw 'Keep original native facility evidence'}
function A([string[]]$p){& $adb -P 5038 -s $serial @p;if($LASTEXITCODE -ne 0){throw 'Facility fit ADB failed'}}
function Plan($name,$steps){$p="$Folder/Plans/facilities-$name.json";@{directory="$Folder/Evidence/FacilitiesFit/$name";steps=$steps}|ConvertTo-Json -Depth 7|Set-Content $p;& $python Tools/Forest-DeviceReview.py $p;if($LASTEXITCODE -ne 0){throw 'Actual facility fit plan failed'}}
try{
 A @('shell','wm','size','reset')
 foreach($safe in @($false,$true)){
  $profileName=if($safe){'SafeTall'}else{'Default'}
  A @('shell','cmd','overlay',$(if($safe){'enable'}else{'disable'}),'com.android.internal.display.cutout.emulation.tall')
  foreach($region in @(@{name='garden';x=390},@{name='workshop';x=660},@{name='shop';x=930})){
   & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant late
   if($LASTEXITCODE -ne 0){throw 'Facility fixture activation failed'}
   $top=if($safe){84}else{0}
   $steps=@(@{tap=@($region.x,(300+$top))},@{shot='01-actual-world'})
   # Begin below every live-world viewport, so this scrolls facilities rather
   # than panning a scene and leaving another region's old tab out of sight.
   for($i=0;$i -lt 3;$i++){$steps+=@{drag_hold=@(1000,1740,1000,600,500,650)}}
   $steps+=@{shot='02-actual-facilities-bottom'}
   if($region.name -eq 'workshop'){
    $steps+=@(@{tap=@(190,1465)},@{shot='03-warehouse-top'})
    for($i=0;$i -lt 6;$i++){$steps+=@{drag_hold=@(1000,1740,1000,600,500,650)}}
    $steps+=@{shot='04-warehouse-actual-upgrade-footer'}
   }
   Plan ($profileName+'-'+$region.name) $steps
   A @('shell','dumpsys','window','displays')|Set-Content "$Folder/facilities-$profileName-$($region.name)-window.txt"
  }
 }
}finally{A @('shell','wm','size','reset');A @('shell','cmd','overlay','disable','com.android.internal.display.cutout.emulation.tall')}
