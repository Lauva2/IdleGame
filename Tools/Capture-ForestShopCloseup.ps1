param([string]$Folder='Docs/Verification/3d-d18-forest04')
$ErrorActionPreference='Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$candidate=[IO.Path]::GetFileName($Folder.TrimEnd('/','\'))
if($candidate -notmatch '^[a-zA-Z0-9-]+$'){throw 'Unsafe candidate name'}
$target=(Resolve-Path -LiteralPath $Folder).Path
$qaRoot=(Resolve-Path -LiteralPath 'Docs/Verification').Path
if(-not $target.StartsWith($qaRoot+[IO.Path]::DirectorySeparatorChar)){throw 'Outside verification directory'}
$device='/sdcard/Android/data/com.forestatelier.curioshop/files'
function AdbRun([string[]]$Arguments){
 & $adb -P 5038 -s 127.0.0.1:16385 @Arguments
 if($LASTEXITCODE -ne 0){throw ('ADB failed: '+($Arguments -join ' '))}
}
AdbRun @('shell','input','keyevent','KEYCODE_HOME')
Start-Sleep -Seconds 2
AdbRun @('shell','am','force-stop','com.forestatelier.curioshop')
foreach($suffix in @('', '.bak')){AdbRun @('pull',"$device/forest-shop.save$suffix","$target/normal-before-closeup.save$suffix")}
try {
 & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant visits
 @{directory=($Folder+'/Evidence/Closeup');steps=@(@{tap=@(930,300)},@{shot='00-fixed-shop-camera'})}|ConvertTo-Json -Depth 6|Set-Content Builds/d18-closeup-plan.json
 & $python Tools/Forest-DeviceReview.py Builds/d18-closeup-plan.json
 if($LASTEXITCODE -ne 0){throw 'Closeup camera setup failed'}
 AdbRun @('shell','pidof','com.forestatelier.curioshop') | Set-Content "$target/closeup-process-start.txt"
 foreach($clip in @('shop-closeup-1','shop-closeup-2')){
  [DateTime]::UtcNow.ToString('o')|Set-Content "$target/$clip-start.txt"
  $remote='/sdcard/'+$candidate+'-'+$clip+'.mp4'
  AdbRun @('shell','screenrecord','--size','1080x1920','--bit-rate','4000000','--time-limit','160',$remote)
  AdbRun @('pull',$remote,"$target/Evidence/$clip.mp4")
 }
 AdbRun @('shell','pidof','com.forestatelier.curioshop') | Set-Content "$target/closeup-process-end.txt"
} finally {
 AdbRun @('shell','am','force-stop','com.forestatelier.curioshop')
 $records=@()
 foreach($suffix in @('', '.bak')){
  $file="$target/normal-before-closeup.save$suffix"
  AdbRun @('push',$file,"$device/forest-shop.save$suffix")
  $expected=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash
  $actual=(& $adb -P 5038 -s 127.0.0.1:16385 shell sha256sum "$device/forest-shop.save$suffix")
  if($LASTEXITCODE -ne 0 -or -not $actual.StartsWith($expected,[StringComparison]::OrdinalIgnoreCase)){throw 'Latest ordinary bytes do not match'}
  $records+="$expected  forest-shop.save$suffix <= normal-before-closeup.save$suffix"
 }
 $records|Set-Content "$target/closeup-restored-sha256.txt"
 AdbRun @('shell','am','start','-W','-n','com.forestatelier.curioshop/com.unity3d.player.UnityPlayerActivity')
 Start-Sleep -Seconds 10
 AdbRun @('pull',"$device/forest-shop.save","$target/closeup-restored-report.save")
 & 'C:/Program Files/Unity/Hub/Editor/2022.3.8f1c1/Editor/Data/MonoBleedingEdge/bin/mono.exe' Tools/TestOutput/CompareOffline.exe "$target/normal-before-closeup.save" "$target/closeup-restored-report.save" | Tee-Object "$target/closeup-restored-replay.txt"
 if($LASTEXITCODE -ne 0){throw 'Closeup isolation ordinary replay failed'}
 @{directory=($Folder+'/Evidence/Closeup');steps=@(@{shot='90-ordinary-report-top'},@{swipe=@(850,1740,850,1010,600)},@{swipe=@(850,1740,850,1010,600)},@{shot='91-ordinary-report-bottom'})}|ConvertTo-Json -Depth 6|Set-Content Builds/d18-closeup-restored-plan.json
 & $python Tools/Forest-DeviceReview.py Builds/d18-closeup-restored-plan.json
 if($LASTEXITCODE -ne 0){throw 'Ordinary report capture failed'}
}
