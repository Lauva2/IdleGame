param([string]$Folder='Docs/Verification/3d-d21-forest05',[switch]$JourneyLayout)
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
$serial=if($env:FOREST_ADB_SERIAL){$env:FOREST_ADB_SERIAL}else{'127.0.0.1:16384'}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$cutout='com.android.internal.display.cutout.emulation.tall'
New-Item -ItemType Directory -Force -Path "$Folder/Plans/FishFit"|Out-Null
function A([string[]]$p){& $adb -P 5038 -s $serial @p;if($LASTEXITCODE -ne 0){throw 'Actual fit device command failed'}}
try{
 foreach($case in @(@{name='Long';w=1080;h=2340;s=1.0;top=0},@{name='Narrow';w=720;h=1600;s=2.0/3;top=0},@{name='SafeTall';w=1080;h=1920;s=1.0;top=84})){
    A @('shell','cmd','overlay','disable',$cutout);A @('shell','wm','size',($case.w.ToString()+'x'+$case.h))
    if($case.top){A @('shell','cmd','overlay','enable',$cutout)}
    & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant fishing-filled
    $s=$case.s;$h=$case.h;$top=$case.top
    $speciesY=if($JourneyLayout){933}else{775}
    $steps=@(@{tap=@([int](900*$s),[int]($h-70*$s))},@{wait=2},@{tap=@([int](900*$s),[int](300*$s+$top))},@{wait=3},@{shot='01-fish-list'},@{tap=@([int](600*$s),[int]($speciesY*$s+$top))},@{shot='02-species-top'})
    for($i=0;$i -lt 3;$i++){$steps+=@{swipe=@([int](850*$s),[int]($h-180*$s),[int](850*$s),[int]($h-850*$s),650)}}
    $steps+=@(@{shot='03-species-four-quality-bottom'},@{tap=@([int](850*$s),[int]($h-110*$s))},@{shot='04-actual-go-fishing'},@{tap=@([int](686*$s),[int]($h-118*$s))},@{shot='05-fishing-zoom-out'},@{swipe=@([int](550*$s),[int](700*$s+$top),[int](650*$s),[int](780*$s+$top),700)},@{shot='06-fishing-real-pan'},@{tap=@([int](966*$s),[int]($h-118*$s))},@{wait=14},@{shot='07-recenter-and-catch'},@{back=$true},@{tap=@([int](540*$s),[int](400*$s+$top))},@{shot='08-greenhouse'},@{tap=@([int](180*$s),[int](400*$s+$top))},@{shot='09-main-courtyard'},@{tap=@([int](390*$s),[int](300*$s+$top))},@{shot='10-garden'},@{tap=@([int](660*$s),[int](300*$s+$top))},@{shot='11-workshop'},@{tap=@([int](930*$s),[int](300*$s+$top))},@{shot='12-shop'},@{tap=@([int](1000*$s),[int](50*$s+$top))},@{shot='13-settings'})
    if($JourneyLayout){
        $steps+=@(@{back=$true},@{tap=@([int](950*$s),[int](160*$s+$top))},@{shot='14-handbook'},@{tap=@([int](900*$s),[int]($h-401*$s))},@{shot='15-handbook-greenhouse-destination'},@{tap=@([int](950*$s),[int](160*$s+$top))},@{tap=@([int](180*$s),[int]($h-114*$s))},@{shot='16-chapter-progress-top'})
        for($i=0;$i -lt 4;$i++){$steps+=@{swipe=@([int](850*$s),[int]($h-180*$s),[int](850*$s),[int]($h-850*$s),650)}}
        $steps+=@{shot='17-chapter-progress-footer'}
    }
    $plan="$Folder/Plans/FishFit/$($case.name).json";@{directory="$Folder/Evidence/FishFit/$($case.name)";steps=$steps}|ConvertTo-Json -Depth 6|Set-Content $plan
    & $python Tools/Forest-DeviceReview.py $plan;if($LASTEXITCODE -ne 0){throw 'Actual fish fit plan failed'}
    A @('shell','wm','size')|Set-Content "$Folder/fish-fit-$($case.name)-size.txt";A @('shell','dumpsys','window','displays')|Set-Content "$Folder/fish-fit-$($case.name)-window.txt"
 }
}finally{A @('shell','wm','size','reset');A @('shell','cmd','overlay','disable',$cutout);A @('shell','wm','size')|Set-Content "$Folder/fish-fit-display-restored.txt"}
