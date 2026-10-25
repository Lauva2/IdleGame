param([Parameter(Mandatory=$true)][string]$Folder,[string]$RunTag='')
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
$serial=if($env:FOREST_ADB_SERIAL){$env:FOREST_ADB_SERIAL}else{'127.0.0.1:16384'}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$candidate=Split-Path -Leaf $Folder
if($RunTag -notmatch '^[a-zA-Z0-9-]*$'){throw 'Unsafe notice tag'}
$noticeSuffix=if($RunTag){'-'+$RunTag}else{''}
$evidence="$Folder/Evidence/Notices$noticeSuffix"
foreach($suffix in @('','.bak')){if(-not(Test-Path -LiteralPath "$Folder/normal-restorable.save$suffix")){throw 'Protect latest legitimate progress first'}}
if(Test-Path -LiteralPath $evidence){throw 'Preserve original notice evidence; use another candidate or explicit supplement'}
New-Item -ItemType Directory -Force -Path "$Folder/Plans/Notices$noticeSuffix",$evidence|Out-Null
foreach($kind in @('new','length','weight','both','ordinary')){
 & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant "fishing-notice-$kind"
 if($LASTEXITCODE -ne 0){throw 'Notice fixture activation failed'}
 $stem="notice$noticeSuffix-$kind"
 $remote="/sdcard/$candidate-fishing-motion-$stem.mp4"
 [DateTime]::UtcNow.ToString('o')|Set-Content "$Folder/$stem-start.txt"
 $record=Start-Process -FilePath $adb -ArgumentList '-P','5038','-s',$serial,'shell','screenrecord','--size','1080x1920','--bit-rate','4000000','--time-limit','28',$remote -WindowStyle Hidden -PassThru
 $steps=@(@{tap=@(150,410)},@{tap=@(860,880)},@{shot="$kind-start"},@{wait=14},@{shot="$kind-actual-first-landed"},@{tap=@(590,1630)},@{shot="$kind-individual-top"},@{drag_hold=@(1000,1740,1000,1150,500,700)},@{shot="$kind-individual-bottom"})
 @{directory=$evidence;steps=$steps}|ConvertTo-Json -Depth 6|Set-Content "$Folder/Plans/Notices$noticeSuffix/$kind.json"
 & $python Tools/Forest-DeviceReview.py "$Folder/Plans/Notices$noticeSuffix/$kind.json"
 if($LASTEXITCODE -ne 0){throw 'Native notice plan failed'}
 & $adb -P 5038 -s $serial pull /sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save "$Folder/$stem-after.save"
 if($LASTEXITCODE -ne 0){throw 'Native actual catch save pull failed'}
 $record.WaitForExit();if($record.ExitCode -ne 0){throw 'Notice recorder failed'}
 & $adb -P 5038 -s $serial pull $remote "$Folder/Evidence/fishing-motion-$stem.mp4"
 if($LASTEXITCODE -ne 0){throw 'Native notice recording pull failed'}
}
