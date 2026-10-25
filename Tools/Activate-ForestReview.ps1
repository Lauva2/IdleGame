param(
    [Parameter(Mandatory=$true)][string]$Folder,
    [Parameter(Mandatory=$true)][ValidateSet('late','visits','initial','unrestored','states','chapter','report','dialogue0','dialogue1','dialogue2','dialogue3','dialogue4','blocked-full','blocked-input','blocked-reserve','busy','simple','resume','forest-entry','forest-edge','forest-creek','forest-grove','forest-growth','forest-full','forest-cooling','forest-ending','forest-report','forest-simple','forest-pavilion','forest-visits','forest-vines','forest-dialogue0','forest-dialogue1','forest-dialogue2','fishing-empty','fishing-filled','fishing-simple','fishing-seed-q0','fishing-seed-q1','fishing-seed-q2','fishing-seed-q3','fishing-notice-new','fishing-notice-length','fishing-notice-weight','fishing-notice-both','fishing-notice-ordinary','journey-entry','journey-berry','journey-dew','journey-resin','journey-partial','journey-complete','journey-deliver','journey-window')][string]$Variant
)
$ErrorActionPreference='Stop'
$serial=if($env:FOREST_ADB_SERIAL){$env:FOREST_ADB_SERIAL}else{'127.0.0.1:16385'}
Set-Location (Split-Path -Parent $PSScriptRoot)
$verifiedFolder=[IO.Path]::GetFullPath($Folder)
$reviewRoot=[IO.Path]::GetFullPath('Docs/Verification')+[IO.Path]::DirectorySeparatorChar
if(-not $verifiedFolder.StartsWith($reviewRoot,[StringComparison]::OrdinalIgnoreCase)){throw 'Fixture folder must be inside Docs/Verification'}
foreach($file in @('normal-held.save','normal-held.save.bak')){if(-not(Test-Path -LiteralPath (Join-Path $verifiedFolder $file))){throw 'Ordinary main and backup must be preserved first'}}
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$adb='E:/UnityProject/IdleGame/Tools/AndroidSdk/platform-tools/adb.exe'
& $adb -P 5038 -s $serial shell am force-stop com.forestatelier.curioshop
$prior=Join-Path $verifiedFolder "Prepared/$Variant.save"
if(Test-Path -LiteralPath $prior){
    $history=Join-Path $verifiedFolder 'Prepared/History';New-Item -ItemType Directory -Force -Path $history|Out-Null
    $identity=(Get-FileHash -LiteralPath $prior -Algorithm SHA256).Hash
    Copy-Item -LiteralPath $prior -Destination (Join-Path $history "$Variant-$identity.save")
    $scope=Join-Path $verifiedFolder "Prepared/$Variant-scope.json"
    if(Test-Path -LiteralPath $scope){Copy-Item -LiteralPath $scope -Destination (Join-Path $history "$Variant-$identity-scope.json")}
}
$monoRoot='C:/Program Files/Unity/Hub/Editor/2022.3.8f1c1/Editor/Data/MonoBleedingEdge'
$coreSources=Get-ChildItem -LiteralPath Assets/Scripts/Core -Filter '*.cs' | Select-Object -ExpandProperty FullName
if($Variant.StartsWith('journey-')){
    & ($monoRoot+'/bin/mono.exe') ($monoRoot+'/lib/mono/4.5/csc.exe') /nologo /langversion:9 /out:Tools/TestOutput/PrepareJourneyReview.exe ('/r:'+$monoRoot+'/lib/mono/4.5/System.Web.Extensions.dll') @coreSources Tools/Prepare-JourneyReview.cs
    if($LASTEXITCODE -ne 0){throw 'Journey preparer did not compile'}
    & ($monoRoot+'/bin/mono.exe') Tools/TestOutput/PrepareJourneyReview.exe $Folder $Variant
}elseif($Variant.StartsWith('fishing-')){
    & ($monoRoot+'/bin/mono.exe') ($monoRoot+'/lib/mono/4.5/csc.exe') /nologo /langversion:9 /out:Tools/TestOutput/PrepareFishingReview.exe ('/r:'+$monoRoot+'/lib/mono/4.5/System.Web.Extensions.dll') @coreSources Tools/Prepare-FishingReview.cs
    if($LASTEXITCODE -ne 0){throw 'Fishing preparer did not compile'}
    & ($monoRoot+'/bin/mono.exe') Tools/TestOutput/PrepareFishingReview.exe $Folder $Variant
}else{
    $generator=if($Variant.StartsWith('forest-')){'Tools/Prepare-ForestExpansionReview.py'}else{'Tools/Prepare-ForestReview.py'}
    & $python $generator $Folder $Variant
}
if($LASTEXITCODE -ne 0){throw 'Prepared fixture generation failed'}
& ($monoRoot+'/bin/mono.exe') ($monoRoot+'/lib/mono/4.5/csc.exe') /nologo /langversion:9 /out:Tools/TestOutput/ValidatePreparedState.exe ('/r:'+$monoRoot+'/lib/mono/4.5/System.Web.Extensions.dll') @coreSources Tools/Validate-PreparedState.cs
if($LASTEXITCODE -ne 0){throw 'Fixture validator did not compile'}
& ($monoRoot+'/bin/mono.exe') Tools/TestOutput/ValidatePreparedState.exe (Join-Path $verifiedFolder "Prepared/$Variant.save")
if($LASTEXITCODE -ne 0){throw 'Fixture violates actual game rules; will not push'}
@{utc=[DateTime]::UtcNow.ToString('o');variant=$Variant;sha256=(Get-FileHash -LiteralPath (Join-Path $verifiedFolder "Prepared/$Variant.save") -Algorithm SHA256).Hash;prepared=$true} | ConvertTo-Json -Compress | Add-Content -LiteralPath (Join-Path $verifiedFolder 'prepared-input-identity.jsonl') -Encoding utf8
foreach($name in @('forest-shop.save','forest-shop.save.bak')){
    & $adb -P 5038 -s $serial push (Join-Path $verifiedFolder "Prepared/$Variant.save") "/sdcard/Android/data/com.forestatelier.curioshop/files/$name"
    if($LASTEXITCODE -ne 0){throw 'Prepared fixture push failed'}
}
& $adb -P 5038 -s $serial shell am start -W -n com.forestatelier.curioshop/com.unity3d.player.UnityPlayerActivity
([DateTime]::UtcNow.ToString('o')+' PREPARED '+$Variant) | Add-Content -LiteralPath (Join-Path $verifiedFolder 'prepared-activation.txt')
Start-Sleep -Seconds 10
