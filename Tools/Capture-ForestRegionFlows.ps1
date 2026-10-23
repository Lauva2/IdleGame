param([string]$Folder='Docs/Verification/3d-d18-forest04')
$ErrorActionPreference='Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
try {
 foreach($case in @(@{name='Long';w=1080;h=2340;s=1.0;video='720x1560'},@{name='Narrow';w=720;h=1600;s=0.6666667;video='720x1600'})){
  & $adb -P 5038 -s 127.0.0.1:16385 shell wm size ($case.w.ToString()+'x'+$case.h)
  & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant late
  [DateTime]::UtcNow.ToString('o') | Set-Content ($Folder+'/flow-'+$case.name+'-start.txt')
  $remote='/sdcard/forest-region-'+$case.name+'.mp4'
  $record=Start-Process -FilePath $adb -ArgumentList @('-P','5038','-s','127.0.0.1:16385','shell','screenrecord','--size',$case.video,'--bit-rate','1600000','--time-limit','60',$remote) -PassThru -WindowStyle Hidden
  $s=$case.s
  $steps=@(@{shot='11-flow-overview'})
  foreach($region in @(@{x=390;name='garden'},@{x=660;name='workshop'},@{x=930;name='shop'})){
   $steps+=@{tap=@([int]($region.x*$s),[int](300*$s))};$steps+=@{shot=('12-flow-'+$region.name)}
  }
  $steps+=@(@{tap=@([int](1000*$s),[int](50*$s))},@{shot='13-flow-settings'},@{back=$true},@{shot='14-flow-back-to-shop'},@{tap=@([int](200*$s),[int](1690*$s))},@{shot='15-flow-facility-footer'},@{tap=@([int](540*$s),($case.h-[int](70*$s)))},@{tap=@([int](180*$s),($case.h-[int](70*$s)))},@{shot='16-flow-page-return-keeps-footer'})
  for($i=0;$i -lt 4;$i++){$steps+=@{swipe=@([int](1000*$s),[int](1900*$s),[int](1000*$s),[int](2200*$s),650)}}
  $steps+=@(@{shot='17-flow-scrolled-back-top'},@{tap=@([int](390*$s),[int](300*$s))},@{shot='18-flow-garden-jump'},@{tap=@([int](660*$s),[int](300*$s))},@{shot='19-flow-workshop-jump'},@{tap=@([int](930*$s),[int](300*$s))},@{shot='20-flow-shop-jump'})
  @{directory=($Folder+'/Evidence/'+$case.name);steps=$steps}|ConvertTo-Json -Depth 7|Set-Content Builds/d18-region-flow-plan.json
  & $python Tools/Forest-DeviceReview.py Builds/d18-region-flow-plan.json
  if($LASTEXITCODE -ne 0){throw 'Actual region flow capture failed'}
  $record.WaitForExit()
  & $adb -P 5038 -s 127.0.0.1:16385 pull $remote ($Folder+'/Evidence/'+$case.name+'/region-flow.mp4')
  & $adb -P 5038 -s 127.0.0.1:16385 shell dumpsys window displays | Set-Content ($Folder+'/fit-'+$case.name+'.txt')
 }
} finally { & $adb -P 5038 -s 127.0.0.1:16385 shell wm size reset }
