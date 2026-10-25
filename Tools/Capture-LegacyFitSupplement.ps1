param([Parameter(Mandatory=$true)][string]$Folder)
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
$serial=$env:FOREST_ADB_SERIAL;if(!$serial){throw 'Explicit ADB serial required'}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$identity=Get-Content "$Folder/ordinary-fit-checkpoint-identity.json" -Raw|ConvertFrom-Json
foreach($f in $identity.files){if((Get-FileHash -LiteralPath "$Folder/$($f.name)" -Algorithm SHA256).Hash -ne $f.sha256){throw 'Newest ordinary checkpoint differs'}}
if(Test-Path "$Folder/Evidence/LegacyFit"){throw 'Preserve original supplement'}
function A([string[]]$p){& $adb -P 5038 -s $serial @p;if($LASTEXITCODE -ne 0){throw 'Fit supplement ADB command failed'}}
function Plan($name,$steps){
 @{directory="$Folder/Evidence/LegacyFit";steps=$steps}|ConvertTo-Json -Depth 7|Set-Content "$Folder/Plans/fit-$name.json"
 & $python Tools/Forest-DeviceReview.py "$Folder/Plans/fit-$name.json"
 if($LASTEXITCODE -ne 0){throw 'Native legacy fit plan failed'}
}
function Bottom($times=6){$steps=@();for($i=0;$i -lt $times;$i++){$steps+=@{drag_hold=@(1000,1640,1000,640,450,650)}};return $steps}
function Top { $steps=@();for($i=0;$i -lt 6;$i++){$steps+=@{drag_hold=@(1000,650,1000,1640,450,650)}};return $steps}
try{
 A @('shell','wm','size','reset');A @('shell','cmd','overlay','disable','com.android.internal.display.cutout.emulation.tall')
 & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant late
 $steps=@(@{tap=@(660,300)},@{tap=@(200,1690)},@{tap=@(190,1465)})+(Bottom)+@(@{shot='00-default-warehouse-actual-upgrade-footer'})
 Plan 'warehouse-default-bottom' $steps
 A @('shell','cmd','overlay','enable','com.android.internal.display.cutout.emulation.tall')
 & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant late
 $steps=@(@{tap=@(660,384)},@{tap=@(200,1740)},@{shot='01-safe-workshop-facilities'},@{tap=@(190,1465)},@{shot='02-safe-warehouse-top'})+(Bottom)+@(@{shot='03-safe-warehouse-actual-upgrade-footer'},@{back=$true})+(Top)+@(@{tap=@(930,384)},@{tap=@(200,1740)},@{shot='04-safe-shop-facilities'})+(Bottom)+@(@{shot='05-safe-shop-last-facilities'})+(Top)+@(@{tap=@(390,384)},@{tap=@(200,1740)},@{shot='06-safe-garden-facilities'})+(Bottom)+@(@{shot='07-safe-garden-last-facilities'})
 Plan 'safe-facilities' $steps
 A @('shell','dumpsys','window','displays')|Set-Content "$Folder/legacy-fit-safe-window.txt"
 foreach($sample in @(@{variant='forest-report';stem='08-safe-expanded-report'},@{variant='chapter';stem='09-safe-chapter'})){
  & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant $sample.variant
  $steps=@(@{shot=($sample.stem+'-top')})+(Bottom)+@(@{shot=($sample.stem+'-actual-footer')})
  Plan $sample.stem $steps
 }
}finally{
 A @('shell','am','force-stop','com.forestatelier.curioshop');A @('shell','wm','size','reset');A @('shell','cmd','overlay','disable','com.android.internal.display.cutout.emulation.tall')
 $base='/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save'
 foreach($suffix in @('','.bak')){A @('pull',($base+$suffix),"$Folder/prepared-before-fit-restore.save$suffix")}
 & $adb -P 5038 -s $serial shell ls ($base+'.tmp') 2>$null
 if($LASTEXITCODE -eq 0){A @('pull',($base+'.tmp'),"$Folder/prepared-before-fit-restore.save.tmp");A @('shell','mv',($base+'.tmp'),($base+'.review-fit-preserved-temp'))}
 foreach($suffix in @('','.bak')){A @('push',"$Folder/normal-before-fit.save$suffix",($base+$suffix))}
 A @('shell','sha256sum',$base,($base+'.bak'))|Set-Content "$Folder/fit-restored-device-sha256.txt"
 A @('shell','am','start','-W','-n','com.forestatelier.curioshop/com.unity3d.player.UnityPlayerActivity')|Set-Content "$Folder/fit-final-start.txt"
 Start-Sleep -Seconds 5;A @('pull',$base,"$Folder/fit-restore-report.save")
 $monoRoot='C:/Program Files/Unity/Hub/Editor/2022.3.8f1c1/Editor/Data/MonoBleedingEdge'
 & "$monoRoot/bin/mono.exe" Tools/TestOutput/CompareOffline.exe "$Folder/normal-before-fit.save" "$Folder/fit-restore-report.save"|Tee-Object "$Folder/fit-final-offline-replay.txt"
 if($LASTEXITCODE -ne 0){throw 'Latest fit checkpoint restoration mismatch'}
 Plan 'final-normal' (@(@{shot='90-latest-ordinary-report'})+(Bottom)+@(@{shot='91-latest-ordinary-report-footer'},@{tap=@(850,1780)},@{tap=@(900,1850)},@{tap=@(900,300)},@{tap=@(535,515)},@{shot='92-latest-ordinary-fish-progress'},@{tap=@(180,1850)},@{shot='93-latest-ordinary-overview'}))
 A @('shell','input','keyevent','KEYCODE_HOME');Start-Sleep -Seconds 1
 foreach($suffix in @('','.bak')){A @('pull',($base+$suffix),"$Folder/ordinary-after-fit-final.save$suffix")}
 A @('shell','am','start','-W','-n','com.forestatelier.curioshop/com.unity3d.player.UnityPlayerActivity')|Set-Content "$Folder/fit-final-foreground.txt"
 A @('shell','wm','size')|Set-Content "$Folder/fit-final-display.txt";A @('shell','cmd','overlay','list')|Set-Content "$Folder/fit-final-cutouts.txt"
 [DateTime]::UtcNow.ToString('o')|Set-Content "$Folder/fit-final-restored.txt"
}
