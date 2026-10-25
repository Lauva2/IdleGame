param(
 [string]$Folder='Docs/Verification/3d-d19-experience01',
 [ValidateSet('Visitors','Production','Negative','Reduced')][string]$Mode='Visitors'
)
$ErrorActionPreference='Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
$serial=if($env:FOREST_ADB_SERIAL){$env:FOREST_ADB_SERIAL}else{'127.0.0.1:16385'}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$target=(Resolve-Path $Folder).Path
$reviewRoot=(Resolve-Path Docs/Verification).Path+[IO.Path]::DirectorySeparatorChar
if(-not $target.StartsWith($reviewRoot,[StringComparison]::OrdinalIgnoreCase)){throw 'Outside verification directory'}
foreach($file in @('normal-held.save','normal-held.save.bak','normal-restorable.save','normal-restorable.save.bak')){
 if(-not(Test-Path -LiteralPath "$target/$file")){throw 'Latest ordinary main and backup must be preserved first'}
}
function AdbRun([string[]]$Arguments){
 & $adb -P 5038 -s $serial @Arguments
 if($LASTEXITCODE -ne 0){throw ('ADB failed: '+($Arguments -join ' '))}
}
function Plan($name,$steps){
 New-Item -ItemType Directory -Force "$target/Plans/Dynamics" | Out-Null
 $path="$target/Plans/Dynamics/$name.json"
 @{directory=($Folder+'/Evidence/Dynamics');steps=$steps}|ConvertTo-Json -Depth 7|Set-Content $path
 & $python Tools/Forest-DeviceReview.py $path
 if($LASTEXITCODE -ne 0){throw 'Dynamic plan failed'}
}
function Record($name,$seconds){
 [DateTime]::UtcNow.ToString('o')|Set-Content "$target/$name-start.txt"
 AdbRun @('shell','pidof','com.forestatelier.curioshop')|Set-Content "$target/$name-pid.txt"
 AdbRun @('pull','/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save',"$target/$name-before.save")
 $remote='/sdcard/'+(Split-Path -Leaf $Folder)+'-'+$name+'.mp4'
 AdbRun @('shell','screenrecord','--size','1080x1920','--bit-rate','4000000','--time-limit',([string]$seconds),$remote)
 AdbRun @('pull',$remote,"$target/Evidence/$name.mp4")
 AdbRun @('pull','/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save',"$target/$name-after.save")
}
AdbRun @('shell','wm','size','reset')
if($Mode -eq 'Visitors'){
 & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant visits
 Plan 'visitor-camera' @(@{tap=@(930,300)},@{shot='07-fixed-shop-camera'})
 AdbRun @('shell','pidof','com.forestatelier.curioshop')|Set-Content "$target/visitors-process-start.txt"
 Record 'shop-closeup-1' 160
 Record 'shop-closeup-2' 160
 AdbRun @('shell','pidof','com.forestatelier.curioshop')|Set-Content "$target/visitors-process-end.txt"
 AdbRun @('pull','/sdcard/Android/data/com.forestatelier.curioshop/files/performance-v2.csv',"$target/performance-visitors.csv")
 AdbRun @('shell','dumpsys','meminfo','com.forestatelier.curioshop')|Set-Content "$target/meminfo-visitors.txt"
}elseif($Mode -eq 'Production'){
 & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant late
 foreach($region in @(@{x=390;name='garden'},@{x=660;name='workshop'},@{x=930;name='shop'})){
  Plan ('production-'+$region.name) @(@{tap=@($region.x,300)},@{shot=('production-'+$region.name+'-before')})
  Record ('production-'+$region.name) 30
  Plan ('production-'+$region.name+'-after') @(@{shot=('production-'+$region.name+'-after')})
 }
 & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant busy
 Plan 'busy-garden' @(@{tap=@(390,300)},@{shot='busy-twelve-garden'})
 Record 'busy-garden' 30
}elseif($Mode -eq 'Reduced'){
 & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant simple
 Plan 'reduced-garden' @(@{tap=@(390,300)},@{shot='reduced-garden-before'})
 Record 'reduced-garden' 30
 Plan 'reduced-workshop' @(@{tap=@(660,300)},@{shot='reduced-workshop'})
 Record 'reduced-workshop' 30
}else{
 foreach($case in @(@{variant='blocked-full';x=390},@{variant='blocked-input';x=660},@{variant='blocked-reserve';x=930})){
  & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant $case.variant
  Plan $case.variant @(@{tap=@($case.x,300)},@{shot=($case.variant+'-before')})
  Record $case.variant 20
  Plan ($case.variant+'-after') @(@{shot=($case.variant+'-after')})
 }
}
# Caller restores its latest ordinary pair after the entire isolated suite.
