param([Parameter(Mandatory=$true)][string]$Folder,[Parameter(Mandatory=$true)][ValidateSet('start','simple','partial','finished','locked','await')][string]$Variant)
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
if(!$env:FOREST_ADB_SERIAL){throw 'Explicit device required'}
$qa=(Resolve-Path $Folder).Path;$guard=(Resolve-Path Docs/Verification).Path+[IO.Path]::DirectorySeparatorChar
if(!$qa.StartsWith($guard,[StringComparison]::OrdinalIgnoreCase)){throw 'Fixture outside verification'}
foreach($name in @('normal-held.save','normal-held.save.bak')){if(!(Test-Path "$qa/$name")){throw 'Protect ordinary primary and backup first'}}
$monoRoot='C:/Program Files/Unity/Hub/Editor/2022.3.8f1c1/Editor/Data/MonoBleedingEdge';$core=Get-ChildItem Assets/Scripts/Core -Filter '*.cs'|Select-Object -ExpandProperty FullName
$prior="$qa/Prepared/moment-$Variant.save";if(Test-Path $prior){$hash=(Get-FileHash $prior -Algorithm SHA256).Hash;New-Item -ItemType Directory -Force "$qa/Prepared/History"|Out-Null;Copy-Item $prior "$qa/Prepared/History/moment-$Variant-$hash.save";Copy-Item "$qa/Prepared/moment-$Variant-scope.json" "$qa/Prepared/History/moment-$Variant-$hash-scope.json"}
& "$monoRoot/bin/mono.exe" "$monoRoot/lib/mono/4.5/csc.exe" /nologo /langversion:9 /out:Tools/TestOutput/PrepareMomentReview.exe ('/r:'+$monoRoot+'/lib/mono/4.5/System.Web.Extensions.dll') @core Tools/Prepare-MomentReview.cs
if($LASTEXITCODE -ne 0){throw 'Preparer compilation failed'}
& "$monoRoot/bin/mono.exe" Tools/TestOutput/PrepareMomentReview.exe $qa $Variant
if($LASTEXITCODE -ne 0){throw 'Invalid prepared state; will not push'}
$adb='Tools/AndroidSdk/platform-tools/adb.exe';$serial=$env:FOREST_ADB_SERIAL
function A([string[]]$p){& $adb -P 5038 -s $serial @p;if($LASTEXITCODE -ne 0){throw 'Moment fixture device action failed'}}
A @('shell','am','force-stop','com.forestatelier.curioshop')
& Tools/Archive-ReviewTemporary.ps1 -Folder $Folder -Variant ('moment-'+$Variant)
foreach($suffix in @('','.bak')){A @('push',$prior,('/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save'+$suffix))}
@{utc=[DateTime]::UtcNow.ToString('o');variant=('moment-'+$Variant);sha256=(Get-FileHash $prior -Algorithm SHA256).Hash;prepared=$true}|ConvertTo-Json -Compress|Add-Content "$qa/prepared-input-identity.jsonl" -Encoding utf8
A @('shell','am','start','-W','-n','com.forestatelier.curioshop/com.unity3d.player.UnityPlayerActivity')
Start-Sleep -Seconds 10
Write-Output ('COMPLETE prepared moment-'+$Variant)
