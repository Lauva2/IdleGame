param([Parameter(Mandatory=$true)][string]$Folder,[Parameter(Mandatory=$true)][ValidatePattern('^[a-z0-9-]+$')][string]$Tag)
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
if(!$env:FOREST_ADB_SERIAL){throw 'Explicit device required'}
$serial=$env:FOREST_ADB_SERIAL;$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
New-Item -ItemType Directory -Force -Path "$Folder/Evidence/Startup" | Out-Null
$output="$Folder/Evidence/Startup/$Tag.mp4";if(Test-Path $output){throw 'Keep original startup recording'}
function A([string[]]$p){& $adb -P 5038 -s $serial @p;if($LASTEXITCODE -ne 0){throw 'Startup device command failed'}}
A @('shell','am','force-stop','com.forestatelier.curioshop')
$remote="/sdcard/forest-$Tag.mp4"
$record=Start-Process -FilePath $adb -ArgumentList '-P','5038','-s',$serial,'shell','screenrecord','--size','1080x1920','--bit-rate','5000000','--time-limit','18',$remote -PassThru -WindowStyle Hidden
Start-Sleep -Milliseconds 350
[DateTime]::UtcNow.ToString('o')|Set-Content "$Folder/$Tag-launch-utc.txt"
A @('shell','am','start','-W','-n','com.forestatelier.curioshop/com.unity3d.player.UnityPlayerActivity')|Set-Content "$Folder/$Tag-am-start.txt"
$record.WaitForExit();if($record.ExitCode -ne 0){throw 'Startup recording failed'}
A @('pull',$remote,$output)
$gamePid=(A @('shell','pidof','com.forestatelier.curioshop')).Trim()
A @('logcat','-d','--pid',$gamePid,'-v','threadtime')|Set-Content "$Folder/$Tag-logcat.txt"
A @('pull','/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save',"$Folder/$Tag-after.save")
Write-Output "COMPLETE actual uncut startup $Tag"
