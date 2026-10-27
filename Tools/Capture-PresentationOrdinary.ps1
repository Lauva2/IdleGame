param([Parameter(Mandatory=$true)][string]$Folder,[switch]$AfterMoments)
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
$serial=$env:FOREST_ADB_SERIAL;if(!$serial){throw 'Explicit device required'}
if((Test-Path "$Folder/prepared-activation.txt") -or (Test-Path "$Folder/prepared-input-identity.jsonl")){throw 'Ordinary test must precede all prepared states'}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$mono='C:/Program Files/Unity/Hub/Editor/2022.3.8f1c1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
New-Item -ItemType Directory -Force -Path "$Folder/Plans","$Folder/Evidence/Ordinary" | Out-Null
function A([string[]]$p){& $adb -P 5038 -s $serial @p;if($LASTEXITCODE -ne 0){throw 'Ordinary command failed'}}
function Plan($name,$steps){$p="$Folder/Plans/ordinary-$name.json";@{directory="$Folder/Evidence/Ordinary";steps=$steps}|ConvertTo-Json -Depth 7|Set-Content $p;& $python Tools/Forest-DeviceReview.py $p;if($LASTEXITCODE -ne 0){throw 'Ordinary inputs failed'}}
$dockTap=& Tools/Get-ForestDockPoint.ps1 -Spot 0
if($AfterMoments){Plan 'enter' @(@{shot='01-ordinary-after-moments'},@{tap=@(180,300)},@{tap=$dockTap},@{shot='03-ordinary-compact'})}
else{Plan 'enter' @(@{shot='01-update-report'},@{tap=@(850,1780)},@{shot='02-actual-claim'},@{tap=@(150,410)},@{tap=@(180,300)},@{tap=$dockTap},@{shot='03-ordinary-compact'})}
A @('pull','/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save',"$Folder/ordinary-fishing-start.save")
$candidate=Split-Path -Leaf $Folder;$remote="/sdcard/$candidate-ordinary-fishing.mp4"
$record=Start-Process -FilePath $adb -ArgumentList '-P','5038','-s',$serial,'shell','screenrecord','--size','1080x1920','--bit-rate','4000000','--time-limit','65',$remote -WindowStyle Hidden -PassThru
Plan 'details-and-camera' @(@{tap=@(500,1835)},@{shot='04-ordinary-details'},@{tap=@(500,1835)},@{tap=@(717,1835)},@{shot='05-ordinary-zoom'},@{swipe=@(550,700,640,790,700)},@{shot='06-ordinary-pan'},@{tap=@(977,1835)},@{wait=14},@{shot='07-ordinary-recenter-catch'})
$record.WaitForExit();if($record.ExitCode -ne 0){throw 'Ordinary recording failed'}
A @('shell','input','keyevent','KEYCODE_HOME');Start-Sleep -Seconds 1
foreach($suffix in @('','.bak')){A @('pull',('/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save'+$suffix),"$Folder/ordinary-continuous-end.save$suffix")}
A @('pull',$remote,"$Folder/Evidence/fishing-motion-ordinary.mp4")
& $mono Tools/TestOutput/CompareOnline.exe "$Folder/ordinary-fishing-start.save" "$Folder/ordinary-continuous-end.save" --continuous-fishing | Tee-Object -FilePath "$Folder/ordinary-online-replay.txt"
if($LASTEXITCODE -ne 0){throw 'Full ordinary foreground/fish replay failed'}
Start-Sleep -Seconds 35
A @('shell','am','start','-W','-n','com.forestatelier.curioshop/com.unity3d.player.UnityPlayerActivity')|Set-Content "$Folder/ordinary-background-am-start.txt"
Start-Sleep -Seconds 2
A @('pull','/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save',"$Folder/ordinary-background-report.save")
& $mono Tools/TestOutput/CompareOffline.exe "$Folder/ordinary-continuous-end.save" "$Folder/ordinary-background-report.save" | Tee-Object -FilePath "$Folder/ordinary-background-replay.txt"
if($LASTEXITCODE -ne 0){throw 'Background economy/fish replay failed'}
Plan 'leave-and-protect' @(@{shot='08-background-report'},@{tap=@(850,1780)},@{back=$true},@{shot='09-global-navigation-restored'},@{tap=@(1000,50)},@{shot='10-ordinary-settings'},@{back=$true})
A @('shell','input','keyevent','KEYCODE_HOME');Start-Sleep -Seconds 1
foreach($suffix in @('','.bak')){A @('pull',('/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save'+$suffix),"$Folder/normal-restorable.save$suffix")}
A @('shell','am','force-stop','com.forestatelier.curioshop')
$identity=@{utc=[DateTime]::UtcNow.ToString('o');files=@()}
foreach($suffix in @('','.bak')){$file="$Folder/normal-restorable.save$suffix";$identity.files+=@{file=(Split-Path -Leaf $file);sha256=(Get-FileHash $file -Algorithm SHA256).Hash}}
$identity|ConvertTo-Json -Depth 5|Set-Content "$Folder/ordinary-restorable-identity.json" -Encoding UTF8
Write-Output 'COMPLETE ordinary progression protected before all art fixtures'
