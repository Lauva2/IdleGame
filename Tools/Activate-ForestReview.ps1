param(
    [Parameter(Mandatory=$true)][string]$Folder,
    [Parameter(Mandatory=$true)][ValidateSet('late','visits','initial','unrestored','states','chapter','report','dialogue0','dialogue1','dialogue2','dialogue3','dialogue4')][string]$Variant
)
$ErrorActionPreference='Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
$verifiedFolder=[IO.Path]::GetFullPath($Folder)
$reviewRoot=[IO.Path]::GetFullPath('Docs/Verification')+[IO.Path]::DirectorySeparatorChar
if(-not $verifiedFolder.StartsWith($reviewRoot,[StringComparison]::OrdinalIgnoreCase)){throw 'Fixture folder must be inside Docs/Verification'}
foreach($file in @('normal-held.save','normal-held.save.bak')){if(-not(Test-Path -LiteralPath (Join-Path $verifiedFolder $file))){throw 'Ordinary main and backup must be preserved first'}}
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$adb='E:/UnityProject/IdleGame/Tools/AndroidSdk/platform-tools/adb.exe'
& $adb -P 5038 -s 127.0.0.1:16385 shell am force-stop com.forestatelier.curioshop
& $python Tools/Prepare-ForestReview.py $Folder $Variant
if($LASTEXITCODE -ne 0){throw 'Prepared fixture generation failed'}
$monoRoot='C:/Program Files/Unity/Hub/Editor/2022.3.8f1c1/Editor/Data/MonoBleedingEdge'
& ($monoRoot+'/bin/mono.exe') ($monoRoot+'/lib/mono/4.5/csc.exe') /nologo /langversion:9 /out:Tools/TestOutput/ValidatePreparedState.exe ('/r:'+$monoRoot+'/lib/mono/4.5/System.Web.Extensions.dll') Assets/Scripts/Core/GameData.cs Assets/Scripts/Core/Economy.cs Assets/Scripts/Core/Persistence.cs Tools/Validate-PreparedState.cs
if($LASTEXITCODE -ne 0){throw 'Fixture validator did not compile'}
& ($monoRoot+'/bin/mono.exe') Tools/TestOutput/ValidatePreparedState.exe (Join-Path $verifiedFolder "Prepared/$Variant.save")
if($LASTEXITCODE -ne 0){throw 'Fixture violates actual game rules; will not push'}
foreach($name in @('forest-shop.save','forest-shop.save.bak')){
    & $adb -P 5038 -s 127.0.0.1:16385 push (Join-Path $verifiedFolder "Prepared/$Variant.save") "/sdcard/Android/data/com.forestatelier.curioshop/files/$name"
    if($LASTEXITCODE -ne 0){throw 'Prepared fixture push failed'}
}
& $adb -P 5038 -s 127.0.0.1:16385 shell am start -W -n com.forestatelier.curioshop/com.unity3d.player.UnityPlayerActivity
([DateTime]::UtcNow.ToString('o')+' PREPARED '+$Variant) | Add-Content -LiteralPath (Join-Path $verifiedFolder 'prepared-activation.txt')
Start-Sleep -Seconds 10
