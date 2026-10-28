# Dot-source for bounded original recordings. Stop only the exact owned Android
# recorder after checking its command line, never all screenrecord processes.
function Start-ForestReviewRecorder([string]$Folder,[string]$Name,[int]$Seconds=180){
 $qa=[IO.Path]::GetFullPath($Folder)
 $root=[IO.Path]::GetFullPath('Docs/Verification')+[IO.Path]::DirectorySeparatorChar
 if(!$qa.StartsWith($root,[StringComparison]::OrdinalIgnoreCase) -or $Name -notmatch '^[a-zA-Z0-9-]+$' -or $Seconds -lt 1 -or $Seconds -gt 180){throw 'Invalid recorder scope'}
 if(!$env:FOREST_ADB_SERIAL){throw 'Explicit recorder device required'}
 $file=Join-Path $qa "Evidence/$Name.mp4"
 if(Test-Path -LiteralPath $file){throw 'Preserve original movie'}
 $adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
 $remote='/sdcard/'+(Split-Path -Leaf $qa)+'-'+$Name+'.mp4'
 $process=Start-Process -FilePath $adb -ArgumentList '-P','5038','-s',$env:FOREST_ADB_SERIAL,'shell','screenrecord','--bit-rate','4000000','--time-limit',([string]$Seconds),$remote -PassThru -WindowStyle Hidden
 Start-Sleep -Milliseconds 700
 $ids=& $adb -P 5038 -s $env:FOREST_ADB_SERIAL shell pidof screenrecord
 $owned=@()
 foreach($idText in ($ids -split '\s+')){
  if($idText -notmatch '^\d+$'){continue}
  $command=(& $adb -P 5038 -s $env:FOREST_ADB_SERIAL shell cat "/proc/$idText/cmdline") -join ''
  if($command.Contains($remote)){$owned+=$idText}
 }
 if($owned.Count -ne 1){throw 'Cannot identify exact owned recorder'}
 @{process=$process;recordId=$owned[0];remote=$remote;file=$file;adb=$adb;serial=$env:FOREST_ADB_SERIAL;startUtc=[DateTime]::UtcNow.ToString('o');limit=$Seconds}
}
function Stop-ForestReviewRecorder($Record){
 $Record.process.Refresh()
 if(!$Record.process.HasExited){
  $command=(& $Record.adb -P 5038 -s $Record.serial shell cat "/proc/$($Record.recordId)/cmdline") -join ''
  if(!$command.Contains($Record.remote)){throw 'Owned recorder identity changed'}
  & $Record.adb -P 5038 -s $Record.serial shell kill -2 $Record.recordId
  if($LASTEXITCODE -ne 0){throw 'Owned recorder stop failed'}
  if(!$Record.process.WaitForExit(15000)){throw 'Owned recorder has not finished'}
 }
 if($Record.process.ExitCode -ne 0){throw 'Owned recorder failed'}
 & $Record.adb -P 5038 -s $Record.serial pull $Record.remote $Record.file
 if($LASTEXITCODE -ne 0){throw 'Original recording pull failed'}
 @{startUtc=$Record.startUtc;endUtc=[DateTime]::UtcNow.ToString('o');timeLimit=$Record.limit;file=$Record.file;sha256=(Get-FileHash -LiteralPath $Record.file -Algorithm SHA256).Hash;method='Native original screenrecord, bounded by exact owned PID SIGINT or its fixed time limit. No editing.'}|ConvertTo-Json|Set-Content -LiteralPath ($Record.file+'.json')
}
