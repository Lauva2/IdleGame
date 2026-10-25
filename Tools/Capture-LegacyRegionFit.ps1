param([Parameter(Mandatory=$true)][string]$Folder)
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
$serial=$env:FOREST_ADB_SERIAL;if(!$serial){throw 'Explicit ADB serial required'}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
if(Test-Path "$Folder/normal-before-region-fit.save"){throw 'Keep latest region checkpoint'}
function A([string[]]$p){& $adb -P 5038 -s $serial @p;if($LASTEXITCODE -ne 0){throw 'Region fit ADB failed'}}
function Plan($name,$steps){@{directory="$Folder/Evidence/LegacyRegions-r02";steps=$steps}|ConvertTo-Json -Depth 7|Set-Content "$Folder/Plans/region-fit-$name.json";& $python Tools/Forest-DeviceReview.py "$Folder/Plans/region-fit-$name.json";if($LASTEXITCODE -ne 0){throw 'Actual region fit plan failed'}}
A @('shell','input','keyevent','KEYCODE_HOME');Start-Sleep -Seconds 1;A @('shell','am','force-stop','com.forestatelier.curioshop')
$base='/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save'
foreach($suffix in @('','.bak')){A @('pull',($base+$suffix),"$Folder/normal-before-region-fit.save$suffix")}
$state=Get-Content "$Folder/normal-before-region-fit.save" -Tail 1|ConvertFrom-Json
if($state.coins -lt 966230 -or $state.fishing.totalCaught -lt 39){throw 'Not latest ordinary progress'}
$files=@('normal-before-region-fit.save','normal-before-region-fit.save.bak')|ForEach-Object{$f=Get-Item "$Folder/$_";@{name=$_;bytes=$f.Length;sha256=(Get-FileHash -LiteralPath $f.FullName -Algorithm SHA256).Hash}}
@{utc=[DateTime]::UtcNow.ToString('o');coins=$state.coins;fish=$state.fishing.totalCaught;files=$files;scope='Newest normal checkpoint before corrected shop/garden fit evidence; restore this pair without rolling back earlier income.'}|ConvertTo-Json -Depth 5|Set-Content "$Folder/ordinary-region-checkpoint-identity.json"
try{
 A @('shell','cmd','overlay','enable','com.android.internal.display.cutout.emulation.tall')
 foreach($region in @(@{name='shop';x=930},@{name='garden';x=390})){
  # Each region starts from a fresh process/top-level page. A world drag cannot
  # restore the scrolled-away region tabs in the previous supplement.
  & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant late
  $steps=@(@{tap=@($region.x,384)},@{shot=($region.name+'-actual-world')},@{tap=@(200,1740)},@{shot=($region.name+'-actual-facilities-first')})
  for($i=0;$i -lt 6;$i++){$steps+=@{drag_hold=@(1000,1640,1000,640,450,650)}}
  $steps+=@{shot=($region.name+'-actual-facilities-bottom')}
  Plan $region.name $steps
 }
 A @('shell','dumpsys','window','displays')|Set-Content "$Folder/region-fit-safe-window.txt"
}finally{
 A @('shell','am','force-stop','com.forestatelier.curioshop');A @('shell','wm','size','reset');A @('shell','cmd','overlay','disable','com.android.internal.display.cutout.emulation.tall')
 foreach($suffix in @('','.bak')){A @('pull',($base+$suffix),"$Folder/prepared-before-region-fit-restore.save$suffix")}
 & $adb -P 5038 -s $serial shell ls ($base+'.tmp') 2>$null
 if($LASTEXITCODE -eq 0){A @('pull',($base+'.tmp'),"$Folder/prepared-before-region-fit-restore.save.tmp");A @('shell','mv',($base+'.tmp'),($base+'.review-region-preserved-temp'))}
 foreach($suffix in @('','.bak')){A @('push',"$Folder/normal-before-region-fit.save$suffix",($base+$suffix))}
 A @('shell','sha256sum',$base,($base+'.bak'))|Set-Content "$Folder/region-fit-restored-device-sha256.txt"
 A @('shell','am','start','-W','-n','com.forestatelier.curioshop/com.unity3d.player.UnityPlayerActivity')|Set-Content "$Folder/region-fit-final-start.txt"
 Start-Sleep -Seconds 5;A @('pull',$base,"$Folder/region-fit-restore-report.save")
 $mono='C:/Program Files/Unity/Hub/Editor/2022.3.8f1c1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
 & $mono Tools/TestOutput/CompareOffline.exe "$Folder/normal-before-region-fit.save" "$Folder/region-fit-restore-report.save"|Tee-Object "$Folder/region-fit-final-offline-replay.txt"
 if($LASTEXITCODE -ne 0){throw 'Newest region checkpoint restore mismatch'}
 Plan 'latest-normal' @(@{shot='90-latest-report'},@{tap=@(850,1780)},@{tap=@(900,1850)},@{tap=@(900,300)},@{tap=@(535,515)},@{shot='91-latest-fish-progress'},@{tap=@(180,1850)},@{shot='92-latest-normal-overview'})
 A @('shell','input','keyevent','KEYCODE_HOME');Start-Sleep -Seconds 1
 foreach($suffix in @('','.bak')){A @('pull',($base+$suffix),"$Folder/ordinary-after-region-fit-final.save$suffix")}
 A @('shell','am','start','-W','-n','com.forestatelier.curioshop/com.unity3d.player.UnityPlayerActivity')|Set-Content "$Folder/region-fit-final-foreground.txt"
 A @('shell','wm','size')|Set-Content "$Folder/region-fit-final-display.txt";A @('shell','cmd','overlay','list')|Set-Content "$Folder/region-fit-final-cutouts.txt"
 [DateTime]::UtcNow.ToString('o')|Set-Content "$Folder/region-fit-final-restored.txt"
}
