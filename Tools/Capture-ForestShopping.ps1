param(
 [string]$Folder='Docs/Verification/3d-d20-shopping02',
 [ValidateSet('Visitors','Production','Negative','Reduced','Recovery','Wide','Detail','Offscreen')][string]$Mode='Visitors'
)
$ErrorActionPreference='Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
$serial=if($env:FOREST_ADB_SERIAL){$env:FOREST_ADB_SERIAL}else{'127.0.0.1:16384'}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$target=(Resolve-Path $Folder).Path
$candidate=Split-Path $target -Leaf
if($candidate -notmatch '^[a-zA-Z0-9-]+$'){throw 'Unsafe candidate name'}
foreach($f in @('normal-held.save','normal-held.save.bak','normal-restorable.save','normal-restorable.save.bak')){
 if(-not(Test-Path -LiteralPath "$target/$f")){throw 'Latest ordinary main and backup must be protected before fixtures'}
}
function AdbRun([string[]]$Arguments){
 & $adb -P 5038 -s $serial @Arguments
 if($LASTEXITCODE -ne 0){throw ('ADB failed: '+($Arguments -join ' '))}
}
function Plan($name,$steps){
 New-Item -ItemType Directory -Force "$target/Plans/Dynamics"|Out-Null
 $path="$target/Plans/Dynamics/$name.json"
 @{directory=($Folder+'/Evidence/Dynamics');steps=$steps}|ConvertTo-Json -Depth 7|Set-Content $path
 & $python Tools/Forest-DeviceReview.py $path
 if($LASTEXITCODE -ne 0){throw 'Actual dynamic input plan failed'}
}
function Record($name,$seconds){
 [DateTime]::UtcNow.ToString('o')|Set-Content "$target/$name-start.txt"
 AdbRun @('shell','pidof','com.forestatelier.curioshop')|Set-Content "$target/$name-pid.txt"
 AdbRun @('pull','/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save',"$target/$name-before.save")
 $remote='/sdcard/'+$candidate+'-'+$name+'.mp4'
 AdbRun @('shell','screenrecord','--size','1080x1920','--bit-rate','4000000','--time-limit',([string]$seconds),$remote)
 AdbRun @('pull',$remote,"$target/Evidence/$name.mp4")
 AdbRun @('pull','/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save',"$target/$name-after.save")
}
AdbRun @('shell','wm','size','reset')
if($Mode -eq 'Visitors' -or $Mode -eq 'Wide'){
 & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant visits
 if($Mode -eq 'Visitors'){
  Plan 'default-shop' @(@{tap=@(930,300)},@{shot='01-default-shop-with-real-number-badges'})
  $stem='shop-closeup'
 }else{Plan 'wide-shop' @(@{shot='02-wide-main-road'});$stem='visits-wide'}
 AdbRun @('shell','pidof','com.forestatelier.curioshop')|Set-Content "$target/$stem-process-start.txt"
 Record ($stem+'-1') 160
 Record ($stem+'-2') 160
 AdbRun @('shell','pidof','com.forestatelier.curioshop')|Set-Content "$target/$stem-process-end.txt"
 AdbRun @('pull','/sdcard/Android/data/com.forestatelier.curioshop/files/performance-v2.csv',"$target/performance-$stem.csv")
 AdbRun @('shell','dumpsys','meminfo','com.forestatelier.curioshop')|Set-Content "$target/meminfo-$stem.txt"
}elseif($Mode -eq 'Production'){
 & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant late
 foreach($area in @(@{x=390;name='garden'},@{x=660;name='workshop'},@{x=930;name='shop'})){
  Plan ('production-'+$area.name) @(@{tap=@($area.x,300)},@{shot=('production-'+$area.name+'-before')})
  Record ('production-'+$area.name) 30
  Plan ('production-'+$area.name+'-end') @(@{shot=('production-'+$area.name+'-after')})
 }
 & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant busy
 Plan 'busy-garden' @(@{tap=@(390,300)},@{shot='busy-twelve-garden'})
 Record 'busy-garden' 30
}elseif($Mode -eq 'Negative'){
 foreach($case in @(@{variant='blocked-full';x=390},@{variant='blocked-input';x=660},@{variant='blocked-reserve';x=930})){
  & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant $case.variant
  Plan $case.variant @(@{tap=@($case.x,300)},@{shot=($case.variant+'-before')})
  Record $case.variant 30
  Plan ($case.variant+'-end') @(@{shot=($case.variant+'-after')})
 }
}elseif($Mode -eq 'Reduced'){
 & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant simple
 foreach($area in @(@{x=660;name='workshop'},@{x=930;name='shop'})){
  Plan ('reduced-'+$area.name) @(@{tap=@($area.x,300)},@{shot=('reduced-'+$area.name)})
  Record ('reduced-'+$area.name) 30
 }
}elseif($Mode -eq 'Recovery'){
 & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant resume
 Plan 'resume-workshop' @(@{tap=@(660,300)},@{shot='resume-workshop-initially-short-of-input'})
 Record 'resume-workshop' 45
 Plan 'resume-workshop-end' @(@{shot='resume-workshop-after-real-harvest-and-craft'})
}elseif($Mode -eq 'Offscreen'){
 & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant visits
 Plan 'offscreen-start' @(@{tap=@(930,300)},@{shot='offscreen-before-real-drag'})
 $remote='/sdcard/'+$candidate+'-offscreen-sale.mp4'
 [DateTime]::UtcNow.ToString('o')|Set-Content "$target/offscreen-sale-start.txt"
 AdbRun @('pull','/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save',"$target/offscreen-sale-before.save")
 $record=Start-Process -FilePath $adb -ArgumentList @('-P','5038','-s',$serial,'shell','screenrecord','--size','1080x1920','--bit-rate','4000000','--time-limit','40',$remote) -PassThru -WindowStyle Hidden
 Plan 'offscreen-pan' @(@{swipe=@(550,700,550,1400,700)},@{swipe=@(550,700,550,1400,700)},@{wait=12},@{shot='offscreen-after-real-drag'},@{tap=@(998,1572)},@{wait=12},@{shot='offscreen-after-real-recenter'})
 $record.WaitForExit()
 if($record.ExitCode -ne 0){throw 'Offscreen screenrecord failed'}
 AdbRun @('pull',$remote,"$target/Evidence/offscreen-sale.mp4")
 AdbRun @('pull','/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save',"$target/offscreen-sale-after.save")
}else{
 & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant visits
 Plan 'checkout-detail' @(@{tap=@(930,300)},@{tap=@(885,1572)},@{swipe=@(550,1000,800,1060,700)},@{shot='03-actual-zoom-and-pan-checkout'})
 Record 'shop-closeup-detail' 90
}
# All fixtures remain isolated. Restore the newest ordinary pair after the suite.
