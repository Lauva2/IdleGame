param(
 [string]$Folder='Docs/Verification/3d-d20-shopping03',
 [ValidateSet('Credits','Facilities')][string]$Mode='Credits'
)
$ErrorActionPreference='Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
$serial=if($env:FOREST_ADB_SERIAL){$env:FOREST_ADB_SERIAL}else{'127.0.0.1:16384'}
$adb='Tools/AndroidSdk/platform-tools/adb.exe'
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
foreach($suffix in @('', '.bak')){
 if(-not(Test-Path -LiteralPath "$Folder/normal-restorable.save$suffix")){throw 'Newest ordinary pair must be protected before prepared details'}
}
function AdbRun([string[]]$Arguments){
 & $adb -P 5038 -s $serial @Arguments
 if($LASTEXITCODE -ne 0){throw 'ADB review details failed'}
}
function Plan($name,$steps,$suffix=''){
 New-Item -ItemType Directory -Force "$Folder/Plans/Details"|Out-Null
 $path="$Folder/Plans/Details/$name.json"
 @{directory=($Folder+'/Evidence/Supplement'+$suffix);steps=$steps}|ConvertTo-Json -Depth 7|Set-Content $path
 & $python Tools/Forest-DeviceReview.py $path
 if($LASTEXITCODE -ne 0){throw 'Actual review details input failed'}
}
if($Mode -eq 'Credits'){
 try{
  foreach($profile in @(@{name='Default';w=1080;h=1920;s=1.0},@{name='Long';w=1080;h=2340;s=1.0},@{name='Narrow';w=720;h=1600;s=0.6666667})){
   AdbRun @('shell','wm','size',($profile.w.ToString()+'x'+$profile.h))
   & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant late
   $s=$profile.s; $offset=$profile.h-[int](1920*$s)
   $steps=@(@{tap=@([int](1000*$s),[int](50*$s))},@{shot='22-settings'},@{tap=@([int](850*$s),([int](1620*$s)+$offset))},@{shot='23-credits-top'})
   for($i=0;$i -lt 8;$i++){$steps+=@{swipe=@([int](850*$s),($profile.h-[int](200*$s)),[int](850*$s),($profile.h-[int](850*$s)),650)}}
   $steps+=@(@{wait=2},@{shot='24-credits-actual-final-line'})
   Plan ('credits-'+$profile.name) $steps ('/'+$profile.name)
  }
 }finally{AdbRun @('shell','wm','size','reset')}
}else{
 AdbRun @('shell','wm','size','reset')
 & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant late
 $steps=@(@{tap=@(660,300)},@{tap=@(200,1690)},@{shot='25-workshop-full-facilities'},@{tap=@(190,1465)},@{shot='26-warehouse-top'},@{swipe=@(850,1720,850,1070,650)},@{swipe=@(850,1720,850,1070,650)},@{shot='27-warehouse-bottom'},@{back=$true},@{tap=@(540,1465)},@{shot='28-expansion-summary'},@{back=$true},@{tap=@(890,1465)},@{shot='29-decoration-four-choices'})
 foreach($row in @(@{y=1030;name='sign'},@{y=1270;name='curtain'},@{y=1510;name='rug'},@{y=1750;name='pot'})){
  $steps+=@(@{tap=@(850,$row.y)},@{shot=('30-selected-basic-'+$row.name)})
 }
 $steps+=@(@{back=$true},@{shot='34-workshop-after-real-switches'})
 for($i=0;$i -lt 4;$i++){$steps+=@{swipe=@(1000,1700,1000,2050,650)}}
 $steps+=@(@{tap=@(930,300)},@{shot='35-shop-after-real-switches'},@{tap=@(130,300)},@{shot='36-overview-after-real-switches'})
 Plan 'facilities-and-decoration' $steps
 AdbRun @('shell','input','keyevent','KEYCODE_HOME')
 Start-Sleep -Seconds 2
 AdbRun @('pull','/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save',"$Folder/prepared-after-decoration.save")
}
# Caller restores the newest ordinary pair after the complete isolated suite.
