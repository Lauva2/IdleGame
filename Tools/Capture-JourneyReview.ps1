param([Parameter(Mandatory=$true)][string]$Folder,[string[]]$Variants=@('journey-entry','journey-berry','journey-dew','journey-resin','journey-partial','journey-complete','journey-deliver'))
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
$serial=$env:FOREST_ADB_SERIAL;if(!$serial){throw 'Explicit ADB serial required'}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$candidate=Split-Path -Leaf $Folder
New-Item -ItemType Directory -Force -Path "$Folder/Plans/Journey","$Folder/Evidence/Journey" | Out-Null
foreach($variant in $Variants){
    if($variant -notmatch '^journey-(entry|berry|dew|resin|partial|complete|deliver)$'){throw 'Unknown journey variant'}
    if(Test-Path -LiteralPath "$Folder/Evidence/Journey/$variant"){throw 'Preserve completed/partial evidence; choose another batch folder'}
    & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant $variant
    if($LASTEXITCODE -ne 0){throw 'Journey fixture activation failed'}
    & $adb -P 5038 -s $serial pull /sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save "$Folder/$variant-before.save"
    $steps=@(
      @{tap=@(950,160)},@{shot='01-handbook'},
      @{tap=@(900,1287)},@{shot='02-main-destination'},@{back=$true},
      @{tap=@(950,160)},@{tap=@(900,1519)},@{shot='03-secondary-destination'},@{tap=@(50,250)},
      @{tap=@(900,1850)},@{tap=@(900,300)},@{shot='04-fish-progress'},
      @{tap=@(850,650)},@{shot='05-clue-or-record-top'}
    )
    for($i=0;$i -lt 4;$i++){$steps+=@{swipe=@(1000,1740,1000,1050,600)}}
    $steps+=@{shot='06-clue-or-record-footer'}
    if($variant -eq 'journey-entry'){$steps+=@(@{tap=@(850,1810)},@{shot='07-locked-habitat-condition'},@{back=$true})}
    $steps+=@(@{back=$true},@{tap=@(180,1850)})
    $plan="$Folder/Plans/Journey/$variant.json"
    @{directory="$Folder/Evidence/Journey/$variant";steps=$steps}|ConvertTo-Json -Depth 7|Set-Content $plan
    $remote="/sdcard/$candidate-$variant.mp4"
    [DateTime]::UtcNow.ToString('o')|Set-Content "$Folder/$variant-start.txt"
    $record=Start-Process -FilePath $adb -ArgumentList '-P','5038','-s',$serial,'shell','screenrecord','--size','1080x1920','--bit-rate','4000000','--time-limit','40',$remote -WindowStyle Hidden -PassThru
    & $python Tools/Forest-DeviceReview.py $plan
    if($LASTEXITCODE -ne 0){throw 'Journey UI plan failed'}
    $record.WaitForExit();if($record.ExitCode -ne 0){throw 'Journey recorder failed'}
    & $adb -P 5038 -s $serial pull $remote "$Folder/Evidence/Journey/$variant.mp4"
    & $adb -P 5038 -s $serial pull /sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save "$Folder/$variant-after.save"
    & 'C:/Program Files/Unity/Hub/Editor/2022.3.8f1c1/Editor/Data/MonoBleedingEdge/bin/mono.exe' Tools/TestOutput/CompareOnline.exe "$Folder/$variant-before.save" "$Folder/$variant-after.save" | Tee-Object -FilePath "$Folder/$variant-online-replay.txt"
    if($LASTEXITCODE -ne 0){throw 'Actual journey interval state replay failed'}
    Write-Output "COMPLETE $variant (explicit prepared state, actual UI, original media)"
}
