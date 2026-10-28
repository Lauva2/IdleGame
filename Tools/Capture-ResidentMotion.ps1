param([Parameter(Mandatory=$true)][string]$Folder,[Parameter(Mandatory=$true)][ValidateSet('Moving','Toggle')][string]$Group)
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
if(!$env:FOREST_ADB_SERIAL){throw 'Explicit device required'}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path;$serial=$env:FOREST_ADB_SERIAL
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
. Tools/Forest-ReviewRecorder.ps1
function A([string[]]$p){& $adb -P 5038 -s $serial @p;if($LASTEXITCODE -ne 0){throw 'Resident motion ADB failed'}}
function Plan($name,$steps){
 $path="$Folder/Plans/resident-$name.json";if(Test-Path $path){throw 'Preserve prior plan'}
 @{directory="$Folder/Evidence/Residents/$name";steps=$steps}|ConvertTo-Json -Depth 8|Set-Content $path
 & $python Tools/Forest-DeviceReview.py $path;if($LASTEXITCODE -ne 0){throw 'Resident motion input failed'}
}
A @('shell','wm','size','reset');A @('shell','cmd','overlay','disable','com.android.internal.display.cutout.emulation.tall')
if($Group -eq 'Moving'){
 & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant visits
 $record=Start-ForestReviewRecorder $Folder 'resident-moving'
 Plan 'moving' @(@{tap=@(930,300)},@{shot='00-normal-route'},@{wait=8},@{shot='01-browsing-actor'},@{tap=@(308,971)},@{shot='02-actual-world-greeting'},@{wait=14},@{shot='03-world-continues-behind-card'},@{back=$true},@{shot='04-return-to-moving-route'},@{tap=@(308,971)},@{shot='05-prior-position-later'},@{back=$true})
 Stop-ForestReviewRecorder $record
}else{
 & Tools/Activate-MomentReview.ps1 -Folder $Folder -Variant finished
 $record=Start-ForestReviewRecorder $Folder 'resident-toggle'
 Plan 'toggle-before' @(@{shot='00-normal-courtyard'},@{wait=9},@{tap=@(1000,50)},@{shot='01-setting-before'},@{tap=@(850,1471)},@{shot='02-setting-actually-simplified'})
 A @('pull','/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save',"$Folder/resident-toggle-simple.save")
 Plan 'toggle' @(@{back=$true},@{wait=9},@{shot='03-simple-courtyard-rest'},@{tap=@(930,300)},@{wait=3},@{shot='04-simple-all-five'},@{tap=@(390,300)},@{wait=3},@{shot='05-simple-garden'},@{tap=@(660,300)},@{wait=3},@{shot='06-simple-workshop'},@{tap=@(150,410)},@{tap=@(170,290)},@{wait=9},@{shot='07-simple-forest-rest'},@{tap=@(1000,50)},@{shot='08-setting-simple-kept'},@{tap=@(850,1471)},@{shot='09-setting-actually-full'},@{back=$true},@{wait=9},@{shot='10-normal-forest-cycle'})
 Stop-ForestReviewRecorder $record
 $fixture=Get-Content -LiteralPath "$Folder/Prepared/moment-finished.save" -Tail 1|ConvertFrom-Json
 @(@{kind='visit';id=[int]$fixture.forest.location;arg=0;expected=$true},@{kind='visit';id=0;arg=0;expected=$true})|ConvertTo-Json -Depth 4|Set-Content "$Folder/resident-toggle-actions.json"
}
A @('shell','input','keyevent','KEYCODE_HOME');Start-Sleep -Seconds 1
A @('pull','/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save',"$Folder/resident-$($Group.ToLowerInvariant())-after.save")
