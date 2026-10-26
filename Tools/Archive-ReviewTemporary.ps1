param([Parameter(Mandatory=$true)][string]$Folder,[Parameter(Mandatory=$true)][ValidatePattern('^[a-z0-9-]+$')][string]$Variant)
# A valid .tmp has priority during crash recovery. An intentionally replaced
# fixture must preserve and move aside any prior temporary file as well.
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
if(!$env:FOREST_ADB_SERIAL){throw 'Explicit device required'}
$qa=(Resolve-Path -LiteralPath $Folder).Path;$guard=(Resolve-Path Docs/Verification).Path+[IO.Path]::DirectorySeparatorChar
if(!$qa.StartsWith($guard,[StringComparison]::OrdinalIgnoreCase)){throw 'Archive outside verification'}
foreach($name in @('normal-held.save','normal-held.save.bak')){if(!(Test-Path "$qa/$name")){throw 'Protect ordinary pair before prepared inputs'}}
$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path;$serial=$env:FOREST_ADB_SERIAL
$remote='/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save.tmp'
$found=& $adb -P 5038 -s $serial shell ls $remote 2>$null
if($LASTEXITCODE -eq 0){
 $tag=$Variant+'-'+[DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfff')
 $dir=Join-Path $qa 'Prepared/History';New-Item -ItemType Directory -Force $dir|Out-Null
 $local=Join-Path $dir ($tag+'-prior.tmp');if(Test-Path -LiteralPath $local){throw 'Preserve existing temporary archive'}
 & $adb -P 5038 -s $serial pull $remote $local;if($LASTEXITCODE -ne 0){throw 'Prior temporary file not protected'}
 $hash=(Get-FileHash -LiteralPath $local -Algorithm SHA256).Hash
 & $adb -P 5038 -s $serial shell mv -n $remote ($remote+'.review-preserved-'+$tag)
 if($LASTEXITCODE -ne 0){throw 'Cannot isolate prior temporary input'}
 @{utc=[DateTime]::UtcNow.ToString('o');variant=$Variant;file=('Prepared/History/'+(Split-Path -Leaf $local));sha256=$hash;scope='Prior temporary bytes preserved locally and moved aside on device before explicit fixture replacement; may be partial. Not ordinary restoration or a game defect claim.'}|ConvertTo-Json -Compress|Add-Content "$qa/prepared-temporary-archives.jsonl" -Encoding utf8
 Write-Output 'PRESERVED prior temporary input before prepared replacement'
}
# A missing .tmp is the usual case, not a failed native command for the caller.
$global:LASTEXITCODE=0
