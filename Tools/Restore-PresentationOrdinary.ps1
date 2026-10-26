param([Parameter(Mandatory=$true)][string]$Folder,[Parameter(Mandatory=$true)][ValidateSet('Restore','Claim')][string]$Phase)
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
$serial=$env:FOREST_ADB_SERIAL;if(!$serial){throw 'Explicit device required'}
$qa=(Resolve-Path $Folder).Path;$guard=(Resolve-Path Docs/Verification).Path+[IO.Path]::DirectorySeparatorChar
if(!$qa.StartsWith($guard,[StringComparison]::OrdinalIgnoreCase)){throw 'Restore outside verification'}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$monoRoot='C:/Program Files/Unity/Hub/Editor/2022.3.8f1c1/Editor/Data/MonoBleedingEdge'
$base='/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save'
function A([string[]]$p){& $adb -P 5038 -s $serial @p;if($LASTEXITCODE -ne 0){throw 'Ordinary restore device command failed'}}
function Plan($name,$steps){
 $p="$qa/Plans/$name.json";if(Test-Path $p){throw 'Preserve ordinary capture plan'}
 @{directory="$Folder/Evidence/Ordinary";steps=$steps}|ConvertTo-Json -Depth 7|Set-Content $p
 & $python Tools/Forest-DeviceReview.py $p
 if($LASTEXITCODE -ne 0){throw 'Ordinary UI capture failed'}
}
if($Phase -eq 'Restore'){
 if(Test-Path "$qa/ordinary-restored-device-sha256.txt"){throw 'Do not repeat a completed restoration or roll back progress'}
 $audit=Get-Content "$qa/ordinary-fishing-audit.json" -Raw|ConvertFrom-Json
 if((Get-FileHash "$qa/normal-restorable.save" -Algorithm SHA256).Hash -ne $audit.afterSha256){throw 'Protected save is not the latest ordinary progress'}
 $identity=Get-Content "$qa/ordinary-restorable-identity.json" -Raw|ConvertFrom-Json
 foreach($row in $identity.files){if((Get-FileHash "$qa/$($row.file)" -Algorithm SHA256).Hash -ne $row.sha256){throw 'Protected ordinary pair changed'}}
 A @('shell','am','force-stop','com.forestatelier.curioshop')
 A @('shell','wm','size','reset');A @('shell','cmd','overlay','disable','com.android.internal.display.cutout.emulation.tall')
 foreach($suffix in @('','.bak')){A @('pull',($base+$suffix),"$qa/prepared-last-before-restore.save$suffix")}
 $temporary=& $adb -P 5038 -s $serial shell ls ($base+'.tmp') 2>$null
 if($LASTEXITCODE -eq 0){A @('pull',($base+'.tmp'),"$qa/prepared-last-before-restore.save.tmp");A @('shell','mv',($base+'.tmp'),($base+'.review-preserved-temp'))}
 foreach($suffix in @('','.bak')){A @('push',"$qa/normal-restorable.save$suffix",($base+$suffix))}
 A @('shell','sha256sum',$base,($base+'.bak'))|Set-Content "$qa/ordinary-restored-device-sha256.txt"
 $device=Get-Content "$qa/ordinary-restored-device-sha256.txt"
 for($i=0;$i -lt 2;$i++){if(!$device[$i].StartsWith($identity.files[$i].sha256,[StringComparison]::OrdinalIgnoreCase)){throw 'Actual device restored bytes differ'}}
 & Tools/Capture-PresentationStartup.ps1 -Folder $Folder -Tag final-cold-start
 if($LASTEXITCODE -ne 0){throw 'Final actual cold start failed'}
 Copy-Item "$qa/final-cold-start-after.save" "$qa/ordinary-final-report.save"
 $output=& "$monoRoot/bin/mono.exe" Tools/TestOutput/CompareOffline.exe "$qa/normal-restorable.save" "$qa/ordinary-final-report.save"
 if($LASTEXITCODE -ne 0){throw 'Full ordinary offline replay failed'}
 ($output -join "`n").TrimStart([char]0xFEFF)|Set-Content "$qa/ordinary-final-offline-replay.txt" -Encoding utf8
 $steps=@(@{shot='40-final-ordinary-report'});for($i=0;$i -lt 5;$i++){$steps+=@{swipe=@(850,1740,850,1040,650)}}
 $steps+=@{shot='41-final-ordinary-report-footer'}
 Plan 'presentation-final-report' $steps
 Write-Output 'RESTORED; inspect actual report footer before Claim'
}else{
 if(!(Test-Path "$qa/ordinary-final-report.save") -or (Test-Path "$qa/ordinary-final.save")){throw 'Restore must precede a single actual Claim'}
 Plan 'presentation-final-claim' @(@{tap=@(850,1780)},@{wait=2},@{shot='42-final-ordinary-after-claim'},@{tap=@(900,1850)},@{tap=@(900,300)},@{tap=@(535,751)},@{shot='43-final-ordinary-fish-progress'},@{tap=@(180,1850)},@{shot='44-final-ordinary-overview'})
 A @('shell','input','keyevent','KEYCODE_HOME');Start-Sleep -Seconds 2
 foreach($suffix in @('','.bak')){A @('pull',($base+$suffix),"$qa/ordinary-final.save$suffix")}
 A @('shell','am','start','-W','-n','com.forestatelier.curioshop/com.unity3d.player.UnityPlayerActivity')|Set-Content "$qa/ordinary-final-foreground.txt"
 A @('shell','wm','size')|Set-Content "$qa/display-final.txt"
 A @('shell','cmd','overlay','list')|Set-Content "$qa/cutout-final.txt"
 $gamePid=(A @('shell','pidof','com.forestatelier.curioshop')).Trim()
 A @('logcat','-d','--pid',$gamePid,'-v','threadtime')|Set-Content "$qa/final-game-logcat.txt"
 [DateTime]::UtcNow.ToString('o')|Set-Content "$qa/ordinary-final-restore-completed.txt"
 & $python Tools/Audit-JourneyRestoration.py $Folder
 if($LASTEXITCODE -ne 0){throw 'Final ordinary state audit failed'}
 Write-Output 'COMPLETE latest ordinary restoration and claim'
}
