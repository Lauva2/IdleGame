param([string]$Folder='Docs/Verification/3d-d18-forest02')
$ErrorActionPreference='Stop'
$serial=if($env:FOREST_ADB_SERIAL){$env:FOREST_ADB_SERIAL}else{'127.0.0.1:16385'}
Set-Location (Split-Path -Parent $PSScriptRoot)
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$adb='E:/UnityProject/IdleGame/Tools/AndroidSdk/platform-tools/adb.exe'
function Plan($steps,$suffix){
    @{directory=($Folder+'/Evidence/'+$suffix);steps=$steps}|ConvertTo-Json -Depth 7|Set-Content Builds/d18-adaptation-plan.json
    & $python Tools/Forest-DeviceReview.py Builds/d18-adaptation-plan.json
    if($LASTEXITCODE -ne 0){throw 'Adaptation capture failed'}
}
try{
    foreach($case in @(@{name='Long';w=1080;h=2340;s=1.0},@{name='Narrow';w=720;h=1600;s=0.6666667})){
        & $adb -P 5038 -s $serial shell wm size ($case.w.ToString()+'x'+$case.h)
        & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant late
        $steps=@(@{shot='01-overview'})
        foreach($region in @(@{x=390;name='garden'},@{x=660;name='workshop'},@{x=930;name='shop'})){
            $steps+=@{tap=@([int]($region.x*$case.s),[int](300*$case.s))}; $steps+=@{shot=('02-'+$region.name)}
        }
        $steps+=@(@{tap=@([int](1000*$case.s),[int](50*$case.s))},@{shot='03-settings'})
        Plan $steps $case.name
        & $adb -P 5038 -s $serial shell dumpsys window displays | Set-Content ($Folder+'/fit-'+$case.name+'.txt')
    }
    & $adb -P 5038 -s $serial shell wm size reset
    & $adb -P 5038 -s $serial shell cmd overlay enable com.android.internal.display.cutout.emulation.tall
    & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant late
    Plan @(@{shot='01-overview'},@{tap=@(390,384)},@{shot='02-garden'},@{tap=@(930,384)},@{shot='03-shop'},@{tap=@(1000,134)},@{shot='04-settings'}) 'SafeTall'
    & $adb -P 5038 -s $serial shell dumpsys window displays | Set-Content ($Folder+'/fit-SafeTall.txt')
}finally{
    & $adb -P 5038 -s $serial shell wm size reset
    & $adb -P 5038 -s $serial shell cmd overlay disable com.android.internal.display.cutout.emulation.tall
    & $adb -P 5038 -s $serial shell cmd overlay list | Select-String 'cutout' | Set-Content ($Folder+'/cutout-restored.txt')
}
