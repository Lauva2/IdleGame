param([Parameter(Mandatory=$true)][string]$Folder)
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
if(!$env:FOREST_ADB_SERIAL){throw 'Explicit device required'}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path;$serial=$env:FOREST_ADB_SERIAL
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$mono='C:/Program Files/Unity/Hub/Editor/2022.3.8f1c1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
foreach($name in @('normal-held.save','normal-held.save.bak','build-info.json')){if(!(Test-Path "$Folder/$name")){throw 'Protect ordinary pair and freeze APK first'}}
if(Test-Path "$Folder/smoke-fixture-applied.txt"){throw 'Preserve earlier smoke run'}
function A([string[]]$p){& $adb -P 5038 -s $serial @p;if($LASTEXITCODE -ne 0){throw 'Supply smoke ADB failed'}}
A @('shell','am','force-stop','com.forestatelier.curioshop')
& Tools/Archive-ReviewTemporary.ps1 -Folder $Folder -Variant supply-smoke
& $python Tools/Prepare-SupplySmoke.py $Folder;if($LASTEXITCODE -ne 0){throw 'Smoke input failed'}
& $mono Tools/TestOutput/ValidatePreparedState.exe "$Folder/Prepared/supply-smoke.save" > "$Folder/smoke-prepared-validation.txt"
if($LASTEXITCODE -ne 0){throw 'Smoke input violates game rules'}
# Write the marker before replacement so interruption still requires restoration.
[DateTime]::UtcNow.ToString('o')|Set-Content "$Folder/smoke-fixture-applied.txt"
foreach($suffix in @('','.bak')){A @('push',"$Folder/Prepared/supply-smoke.save",('/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save'+$suffix))}
A @('shell','am','start','-W','-n','com.forestatelier.curioshop/com.unity3d.player.UnityPlayerActivity')|Set-Content "$Folder/smoke-am-start.txt"
Start-Sleep -Seconds 10
. Tools/Forest-ReviewRecorder.ps1
$record=Start-ForestReviewRecorder $Folder 'supply-smoke' 120
$plan="$Folder/smoke-input-plan.json"
@{directory="$Folder/Evidence/Smoke";steps=@(
 @{shot='01-started-current-goal'},@{tap=@(400,145)},@{shot='02-top-goal-ingredient'},@{back=$true},
 @{tap=@(530,1850)},@{shot='03-order-stock-and-reservation'},@{tap=@(850,853)},@{shot='04-supply-route-reserved'},
 @{tap=@(850,1600)},@{shot='05-ingredient-plot-destination'},@{back=$true},@{tap=@(530,1850)},@{tap=@(850,853)},
 @{shot='06-route-footer'},@{tap=@(850,1770)},@{shot='07-recipe-destination'},@{back=$true})}|ConvertTo-Json -Depth 8|Set-Content $plan
& $python Tools/Forest-DeviceReview.py $plan
if($LASTEXITCODE -ne 0){throw 'Smoke input failed; ordinary pair must still be restored'}
Stop-ForestReviewRecorder $record
A @('shell','dumpsys','window','displays')|Set-Content "$Folder/smoke-foreground-window.txt"
A @('shell','input','keyevent','KEYCODE_HOME');Start-Sleep -Milliseconds 800
A @('pull','/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save',"$Folder/smoke-after.save")
'[]'|Set-Content "$Folder/smoke-core-actions.json"
& $mono Tools/TestOutput/CompareMomentActions.exe "$Folder/Prepared/supply-smoke.save" "$Folder/smoke-after.save" "$Folder/smoke-core-actions.json" --from-fixture > "$Folder/smoke-full-state-replay.txt"
if($LASTEXITCODE -ne 0){throw 'Smoke interval replay failed; restore ordinary pair before any new input'}
Write-Output 'COMPLETE bounded supply smoke; restore ordinary pair next'
