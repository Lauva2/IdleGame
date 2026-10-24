param([string]$Folder='Docs/Verification/3d-d21-forest05')
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
$serial=if($env:FOREST_ADB_SERIAL){$env:FOREST_ADB_SERIAL}else{'127.0.0.1:16384'}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$qa=(Resolve-Path -LiteralPath $Folder).Path;$root=(Resolve-Path Docs/Verification).Path+[IO.Path]::DirectorySeparatorChar
if(-not $qa.StartsWith($root,[StringComparison]::OrdinalIgnoreCase)){throw 'Ordinary restore folder outside verification'}
$candidate=Split-Path -Leaf $qa;if($candidate -notmatch '^[a-zA-Z0-9-]+$'){throw 'Unsafe candidate'}
$expected=(Get-Content -LiteralPath "$qa/ordinary-fishing-audit.json" -Raw|ConvertFrom-Json).afterSha256
if((Get-FileHash -LiteralPath "$qa/normal-restorable.save" -Algorithm SHA256).Hash -ne $expected){throw 'Latest documented ordinary identity differs; do not roll back'}
function A([string[]]$p){& $adb -P 5038 -s $serial @p;if($LASTEXITCODE -ne 0){throw ('Ordinary device restore failed: '+($p -join ' '))}}
A @('shell','am','force-stop','com.forestatelier.curioshop');A @('shell','wm','size','reset');A @('shell','cmd','overlay','disable','com.android.internal.display.cutout.emulation.tall')
$base='/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save'
foreach($suffix in @('','.bak')){A @('pull',($base+$suffix),"$qa/prepared-last-before-restore.save$suffix")}
$tmp=& $adb -P 5038 -s $serial shell ls ($base+'.tmp') 2>$null
if($LASTEXITCODE -eq 0){A @('pull',($base+'.tmp'),"$qa/prepared-last-before-restore.save.tmp");A @('shell','mv',($base+'.tmp'),($base+'.review-preserved-temp'))}
foreach($suffix in @('','.bak')){A @('push',"$qa/normal-restorable.save$suffix",($base+$suffix))}
A @('shell','sha256sum',$base,($base+'.bak'))|Set-Content "$qa/ordinary-restored-device-sha256.txt"
$monoRoot='C:/Program Files/Unity/Hub/Editor/2022.3.8f1c1/Editor/Data/MonoBleedingEdge';$core=Get-ChildItem -LiteralPath Assets/Scripts/Core -Filter '*.cs'|Select-Object -ExpandProperty FullName
& "$monoRoot/bin/mono.exe" "$monoRoot/lib/mono/4.5/csc.exe" /nologo /langversion:9 /out:Tools/TestOutput/CompareOffline.exe ('/r:'+$monoRoot+'/lib/mono/4.5/System.Web.Extensions.dll') @core Tools/Compare-AndroidOffline.cs
if($LASTEXITCODE -ne 0){throw 'Restore verifier compilation failed'}
$remote="/sdcard/$candidate-final-startup.mp4";[DateTime]::UtcNow.ToString('o')|Set-Content "$qa/final-startup-start.txt"
$record=Start-Process -FilePath $adb -ArgumentList '-P','5038','-s',$serial,'shell','screenrecord','--size','1080x1920','--bit-rate','4000000','--time-limit','18',$remote -WindowStyle Hidden -PassThru
A @('shell','am','start','-W','-n','com.forestatelier.curioshop/com.unity3d.player.UnityPlayerActivity')|Set-Content "$qa/ordinary-final-start.txt"
Start-Sleep -Seconds 8;A @('pull',$base,"$qa/ordinary-final-report.save")
& "$monoRoot/bin/mono.exe" Tools/TestOutput/CompareOffline.exe "$qa/normal-restorable.save" "$qa/ordinary-final-report.save"|Tee-Object "$qa/ordinary-final-offline-replay.txt"
if($LASTEXITCODE -ne 0){throw 'Latest ordinary restoration did not replay correctly'}
$record.WaitForExit();if($record.ExitCode -ne 0){throw 'Final startup recorder failed'}
A @('pull',$remote,"$qa/Evidence/final-startup.mp4")
$steps=@(@{shot='40-final-ordinary-report'});for($i=0;$i -lt 5;$i++){$steps+=@{swipe=@(850,1740,850,1040,650)}}
$steps+=@(@{shot='41-final-ordinary-report-bottom'},@{tap=@(850,1780)},@{tap=@(900,1850)},@{tap=@(900,300)},@{tap=@(535,515)},@{shot='42-final-ordinary-five-species-ten-fish'},@{tap=@(180,1850)},@{shot='43-final-ordinary-overview'})
@{directory="$Folder/Evidence/Ordinary";steps=$steps}|ConvertTo-Json -Depth 6|Set-Content "$qa/Plans/final-ordinary-restore.json"
& $python Tools/Forest-DeviceReview.py "$qa/Plans/final-ordinary-restore.json"
if($LASTEXITCODE -ne 0){throw 'Actual final ordinary UI capture failed'}
A @('shell','input','keyevent','KEYCODE_HOME');Start-Sleep -Seconds 2
foreach($suffix in @('','.bak')){A @('pull',($base+$suffix),"$qa/ordinary-final.save$suffix")}
A @('shell','am','start','-W','-n','com.forestatelier.curioshop/com.unity3d.player.UnityPlayerActivity')|Set-Content "$qa/ordinary-final-foreground.txt"
$gamePid=& $adb -P 5038 -s $serial shell pidof com.forestatelier.curioshop
if($gamePid -notmatch '^\d+$'){throw 'No final game PID'}
A @('logcat',"--pid=$gamePid",'-d','-s','Unity','AndroidRuntime')|Set-Content "$qa/final-game-logcat.txt"
A @('shell','wm','size')|Set-Content "$qa/display-final.txt";A @('shell','cmd','overlay','list')|Set-Content "$qa/cutout-final.txt"
[DateTime]::UtcNow.ToString('o')|Set-Content "$qa/ordinary-final-restore-completed.txt"
