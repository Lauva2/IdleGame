param([string]$Folder='Docs/Verification/3d-d18-forest04')
$ErrorActionPreference='Stop'
$serial=if($env:FOREST_ADB_SERIAL){$env:FOREST_ADB_SERIAL}else{'127.0.0.1:16385'}
Set-Location (Split-Path -Parent $PSScriptRoot)
$target=(Resolve-Path -LiteralPath $Folder).Path
$qaRoot=(Resolve-Path -LiteralPath 'Docs/Verification').Path
if(-not $target.StartsWith($qaRoot+[IO.Path]::DirectorySeparatorChar)){throw 'Candidate must be inside verification directory'}
$adb='Tools/AndroidSdk/platform-tools/adb.exe'
$device='/sdcard/Android/data/com.forestatelier.curioshop/files'
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
function AdbRun([string[]]$Arguments){
 & $adb -P 5038 -s $serial @Arguments
 if($LASTEXITCODE -ne 0){throw ('ADB failed: '+($Arguments -join ' '))}
}
foreach($suffix in @('', '.bak')){
 if(-not(Test-Path -LiteralPath "$target/normal-restorable.save$suffix")){throw 'Latest ordinary pair missing'}
}
AdbRun @('shell','am','force-stop','com.forestatelier.curioshop')
AdbRun @('shell','wm','size','reset')
AdbRun @('shell','cmd','overlay','disable','com.android.internal.display.cutout.emulation.tall')
$records=@()
foreach($suffix in @('', '.bak')){
 $file="$target/normal-restorable.save$suffix"
 AdbRun @('push',$file,"$device/forest-shop.save$suffix")
 $expected=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash
 $actual=(& $adb -P 5038 -s $serial shell sha256sum "$device/forest-shop.save$suffix")
 if($LASTEXITCODE -ne 0 -or -not $actual.StartsWith($expected,[StringComparison]::OrdinalIgnoreCase)){throw 'Restored bytes do not match latest ordinary backup'}
 $records+="$expected  forest-shop.save$suffix <= normal-restorable.save$suffix"
}
$records | Set-Content "$target/ordinary-restored-sha256.txt"
AdbRun @('shell','am','start','-W','-n','com.forestatelier.curioshop/com.unity3d.player.UnityPlayerActivity')
Start-Sleep -Seconds 10
AdbRun @('pull',"$device/forest-shop.save","$target/ordinary-restored-report.save")
& 'C:/Program Files/Unity/Hub/Editor/2022.3.8f1c1/Editor/Data/MonoBleedingEdge/bin/mono.exe' Tools/TestOutput/CompareOffline.exe "$target/normal-restorable.save" "$target/ordinary-restored-report.save" | Tee-Object "$target/ordinary-restored-replay.txt"
if($LASTEXITCODE -ne 0){throw 'Actual offline replay differs'}
@{directory=($Folder+'/Evidence');steps=@(@{shot='69-ordinary-restored-report-top'},@{swipe=@(850,1740,850,1010,600)},@{swipe=@(850,1740,850,1010,600)},@{shot='70-ordinary-restored-report-bottom'})}|ConvertTo-Json -Depth 7|Set-Content Builds/d18-restored-plan.json
& $python Tools/Forest-DeviceReview.py Builds/d18-restored-plan.json
if($LASTEXITCODE -ne 0){throw 'Report capture failed'}
AdbRun @('shell','dumpsys','meminfo','com.forestatelier.curioshop') | Set-Content "$target/meminfo-restored.txt"
AdbRun @('shell','pidof','com.forestatelier.curioshop') | Set-Content "$target/ordinary-final-pid.txt"
