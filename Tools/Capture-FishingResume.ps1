param([Parameter(Mandatory=$true)][string]$Folder)
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$serial=$env:FOREST_ADB_SERIAL;if(!$serial){throw 'Explicit ADB serial required'}
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
if(Test-Path "$Folder/prepared-activation.txt"){throw 'Ordinary resume must precede fixtures'}
if(Test-Path "$Folder/normal-restorable.save"){throw 'Preserve original protected checkpoint'}
# The observed background report is over the first forest region, not the yard.
@{directory="$Folder/Evidence/Ordinary";steps=@(@{tap=@(850,1780)},@{tap=@(860,880)},@{wait=1},@{shot='07-resume-actual-paused-cast'})}|ConvertTo-Json -Depth 5|Set-Content "$Folder/Plans/ordinary-resume.json"
& $python Tools/Forest-DeviceReview.py "$Folder/Plans/ordinary-resume.json"
if($LASTEXITCODE -ne 0){throw 'Actual resume UI plan failed'}
& $adb -P 5038 -s $serial pull /sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save "$Folder/ordinary-resume-start.save"
& $adb -P 5038 -s $serial shell screenrecord --size 1080x1920 --bit-rate 4000000 --time-limit 50 /sdcard/d22-fishing-resume.mp4
if($LASTEXITCODE -ne 0){throw 'Actual resume recording failed'}
& $adb -P 5038 -s $serial shell input keyevent KEYCODE_HOME
Start-Sleep -Seconds 1
foreach($suffix in @('','.bak')){
 & $adb -P 5038 -s $serial pull "/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save$suffix" "$Folder/normal-restorable.save$suffix"
 if($LASTEXITCODE -ne 0){throw 'Latest ordinary protection failed'}
}
& $adb -P 5038 -s $serial pull /sdcard/d22-fishing-resume.mp4 "$Folder/Evidence/fishing-motion-resume.mp4"
$files=@('normal-restorable.save','normal-restorable.save.bak')|ForEach-Object{$item=Get-Item "$Folder/$_";@{name=$_;bytes=$item.Length;sha256=(Get-FileHash $item.FullName -Algorithm SHA256).Hash}}
@{utc=[DateTime]::UtcNow.ToString('o');scope='Latest actual ordinary primary and backup protected after automatic resume, before any prepared fixture. Restore this pair, never an older baseline.';files=$files}|ConvertTo-Json -Depth 5|Set-Content "$Folder/ordinary-restorable-identity.json"
