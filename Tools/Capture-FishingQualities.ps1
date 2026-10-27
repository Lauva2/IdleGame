param([string]$Folder='Docs/Verification/3d-d21-forest05',[switch]$PresentationLayout)
$ErrorActionPreference='Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
$serial=if($env:FOREST_ADB_SERIAL){$env:FOREST_ADB_SERIAL}else{'127.0.0.1:16384'}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$candidate=Split-Path -Leaf $Folder
foreach($suffix in @('','.bak')){if(-not(Test-Path -LiteralPath "$Folder/normal-restorable.save$suffix")){throw 'Protect newest legitimate fishing progress before prepared seeds'}}
New-Item -ItemType Directory -Force -Path "$Folder/Plans/Qualities","$Folder/Evidence/Qualities"|Out-Null
for($q=0;$q -lt 4;$q++){
    & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant "fishing-seed-q$q"
    if($LASTEXITCODE -ne 0){throw 'Prepared seed activation failed'}
    $remote="/sdcard/$candidate-fishing-quality-q$q.mp4"
    [DateTime]::UtcNow.ToString('o')|Set-Content "$Folder/quality-q$q-start.txt"
    $record=Start-Process -FilePath $adb -ArgumentList '-P','5038','-s',$serial,'shell','screenrecord','--size','1080x1920','--bit-rate','4000000','--time-limit','32',$remote -WindowStyle Hidden -PassThru
    $recentY=if($PresentationLayout){1690}else{1630}
    $steps=@(@{tap=@(150,410)},@{tap=(& Tools/Get-ForestDockPoint.ps1 -Spot 0)},@{shot="q$q-wait"},@{wait=14},@{shot="q$q-actual-first-landed"},@{tap=@(590,$recentY)},@{shot="q$q-individual-detail-top"},@{swipe=@(850,1740,850,1050,700)},@{swipe=@(850,1740,850,1050,700)},@{shot="q$q-individual-detail-bottom"})
    @{directory="$Folder/Evidence/Qualities";steps=$steps}|ConvertTo-Json -Depth 6|Set-Content "$Folder/Plans/Qualities/q$q.json"
    & $python Tools/Forest-DeviceReview.py "$Folder/Plans/Qualities/q$q.json"
    if($LASTEXITCODE -ne 0){throw 'Actual auto-land quality plan failed'}
    & $adb -P 5038 -s $serial pull /sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save "$Folder/quality-q$q-after.save"
    $record.WaitForExit();if($record.ExitCode -ne 0){throw 'Quality recorder failed'}
    & $adb -P 5038 -s $serial pull $remote "$Folder/Evidence/fishing-quality-q$q.mp4"
    if($LASTEXITCODE -ne 0){throw 'Quality recording pull failed'}
}
# Prepared RNG only. These four catches are never restored as ordinary progress.
