param([string]$Folder='Docs/Verification/3d-d18-forest04')
$ErrorActionPreference='Stop'
$serial=if($env:FOREST_ADB_SERIAL){$env:FOREST_ADB_SERIAL}else{'127.0.0.1:16385'}
Set-Location (Split-Path -Parent $PSScriptRoot)
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$candidate=[IO.Path]::GetFileName($Folder.TrimEnd('/','\'))
if($candidate -notmatch '^[a-zA-Z0-9-]+$'){throw 'Unsafe candidate name'}
& Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant visits
& $adb -P 5038 -s $serial shell pidof com.forestatelier.curioshop | Set-Content "$Folder/record-process-start.txt"
foreach($clip in @('visits-1','visits-2')){
 [DateTime]::UtcNow.ToString('o')|Set-Content ($Folder+'/'+$clip+'-start.txt')
 $remote='/sdcard/'+$candidate+'-'+$clip+'.mp4'
 & $adb -P 5038 -s $serial shell screenrecord --size 720x1280 --bit-rate 1600000 --time-limit 160 $remote
 & $adb -P 5038 -s $serial pull $remote ($Folder+'/Evidence/'+$clip+'.mp4')
 if($LASTEXITCODE -ne 0){throw 'Video pull failed'}
}
& $adb -P 5038 -s $serial shell pidof com.forestatelier.curioshop | Set-Content "$Folder/record-process-end.txt"
& $adb -P 5038 -s $serial pull /sdcard/Android/data/com.forestatelier.curioshop/files/performance-v2.csv "$Folder/performance-visits.csv"
& Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant late
[DateTime]::UtcNow.ToString('o')|Set-Content "$Folder/interactions-start.txt"
$remote='/sdcard/'+$candidate+'-interactions.mp4'
$recorder=Start-Process -FilePath $adb -ArgumentList @('-P','5038','-s',$serial,'shell','screenrecord','--size','720x1280','--bit-rate','1600000','--time-limit','160',$remote) -PassThru -WindowStyle Hidden
& $python Tools/Forest-DeviceReview.py "$Folder/Plans/full-plan.json"
if($LASTEXITCODE -ne 0){throw 'Full plan failed'}
$recorder.WaitForExit()
& $adb -P 5038 -s $serial pull $remote "$Folder/Evidence/interactions.mp4"
& Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant initial
& $python Tools/Forest-DeviceReview.py "$Folder/Plans/initial-plan.json"
if($LASTEXITCODE -ne 0){throw 'Initial plan failed'}
& Tools/Capture-ForestStaticReview.ps1 -Folder $Folder
