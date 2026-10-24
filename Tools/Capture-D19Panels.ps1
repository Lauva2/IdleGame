param([string]$Folder='Docs/Verification/3d-d19-experience01')

# Explicit prepared-panel evidence, not natural progression or sound acceptance.
# Run only after installing the intended APK and preserving the latest ordinary
# main AND backup. Activate-ForestReview validates each fixture before pushing it.
# This tool restores display/cutout state, not ordinary saves; the caller must
# restore its newest preserved ordinary pair after the complete review suite.
$ErrorActionPreference='Stop'
$serial=if($env:FOREST_ADB_SERIAL){$env:FOREST_ADB_SERIAL}else{'127.0.0.1:16385'}
Set-Location (Split-Path -Parent $PSScriptRoot)
$target=(Resolve-Path -LiteralPath $Folder).Path
$reviewRoot=(Resolve-Path -LiteralPath 'Docs/Verification').Path+[IO.Path]::DirectorySeparatorChar
if(-not $target.StartsWith($reviewRoot,[StringComparison]::OrdinalIgnoreCase)){throw 'Panel evidence must stay inside Docs/Verification'}
foreach($file in @('normal-held.save','normal-held.save.bak')){
    if(-not(Test-Path -LiteralPath (Join-Path $target $file))){throw 'Ordinary main and backup must be preserved before panel preparation'}
}
$adb=(Resolve-Path -LiteralPath 'Tools/AndroidSdk/platform-tools/adb.exe').Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$tallPackage='com.android.internal.display.cutout.emulation.tall'
$evidenceRoot=Join-Path $target 'Evidence/Panels'
$planRoot=Join-Path $target 'Plans/D19Panels'
New-Item -ItemType Directory -Force -Path $evidenceRoot,$planRoot | Out-Null

function Invoke-PanelAdb([string[]]$Parameters){
    & $adb -P 5038 -s $serial @Parameters
    if($LASTEXITCODE -ne 0){throw ('ADB panel command failed: '+($Parameters -join ' '))}
}
function Record-PanelWindow([string]$Name){
    Invoke-PanelAdb @('shell','wm','size') | Set-Content -LiteralPath (Join-Path $target ('panels-size-'+$Name+'.txt'))
    Invoke-PanelAdb @('shell','wm','density') | Set-Content -LiteralPath (Join-Path $target ('panels-density-'+$Name+'.txt'))
    Invoke-PanelAdb @('shell','cmd','overlay','list') | Set-Content -LiteralPath (Join-Path $target ('panels-cutouts-'+$Name+'.txt'))
    Invoke-PanelAdb @('shell','dumpsys','window','displays') | Set-Content -LiteralPath (Join-Path $target ('panels-fit-'+$Name+'.txt'))
}
function Capture-PreparedPanel($Profile,$Sample){
    & Tools/Activate-ForestReview.ps1 -Folder $target -Variant $Sample.variant
    if($LASTEXITCODE -ne 0){throw ('Prepared panel activation failed: '+$Sample.variant)}
    $directory=Join-Path $evidenceRoot $Profile.name
    $plans=Join-Path $planRoot $Profile.name
    New-Item -ItemType Directory -Force -Path $directory,$plans | Out-Null
    # Stay in the lower part of the bottom sheet's scroll viewport, away from
    # its fixed title/close row. All coordinates are actual framebuffer pixels.
    $scale=$Profile.width/1080.0
    $x=[int][Math]::Round(850*$scale)
    $start=[int][Math]::Round($Profile.height-160*$scale)
    $end=[int][Math]::Round($Profile.height-620*$scale)
    $steps=@(@{shot=($Sample.stem+'-top')})
    for($i=0;$i -lt 4;$i++){$steps+=@{swipe=@($x,$start,$x,$end,650)}}
    $steps+=@(@{wait=1},@{shot=($Sample.stem+'-bottom')})
    $planFile=Join-Path $plans ($Sample.stem+'.json')
    @{directory=$directory;steps=$steps} | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath $planFile -Encoding utf8
    & $python Tools/Forest-DeviceReview.py $planFile
    if($LASTEXITCODE -ne 0){throw ('Prepared panel capture failed: '+$Profile.name+'/'+$Sample.stem)}
}

$originalSize=@(Invoke-PanelAdb @('shell','wm','size'))
$originalOverlays=@(Invoke-PanelAdb @('shell','cmd','overlay','list'))
$overrideMatch=[regex]::Match(($originalSize -join "`n"),'Override size:\s*(\d+x\d+)')
$originalOverride=if($overrideMatch.Success){$overrideMatch.Groups[1].Value}else{$null}
$cutoutRecords=@(foreach($line in $originalOverlays){
    $match=[regex]::Match($line,'^\s*\[([x ])\]\s+(com\.android\.internal\.display\.cutout\.emulation\.\S+)\s*$')
    if($match.Success){[pscustomobject]@{package=$match.Groups[2].Value;enabled=($match.Groups[1].Value -eq 'x')}}
})
if($tallPackage -notin @($cutoutRecords.package)){throw 'Official tall cutout overlay is unavailable; refusing to claim SafeTall coverage'}
$originalSize | Set-Content -LiteralPath (Join-Path $target 'panels-size-before.txt')
$originalOverlays | Set-Content -LiteralPath (Join-Path $target 'panels-cutouts-before.txt')
@{
    startedUtc=[DateTime]::UtcNow.ToString('o');prepared=$true
    scope='Reward dialogues, prepared offline report and prepared chapter ending. Not natural progression, the full longest story, actual away income or sound acceptance.'
    originalOverride=$originalOverride;originalCutouts=$cutoutRecords
    restoreResponsibility='Display/cutouts restored by this script; newest ordinary main+backup restored by the caller after its complete review suite.'
    evidence='Original ADB framebuffer PNG only, captured by Forest-DeviceReview.py; every action is timestamped in each profile actions.jsonl.'
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $target 'panels-capture-scope.json') -Encoding utf8

$defaultSamples=@(
    @{variant='dialogue0';stem='01-reward-dialogue-owl'},
    @{variant='dialogue1';stem='02-reward-dialogue-hedgehog'},
    @{variant='dialogue2';stem='03-reward-dialogue-squirrel'},
    @{variant='dialogue3';stem='04-reward-dialogue-rabbit'},
    @{variant='dialogue4';stem='05-reward-dialogue-bear'},
    @{variant='report';stem='06-prepared-offline-report'},
    @{variant='chapter';stem='07-prepared-chapter-ending'}
)
$profiles=@(
    @{name='Default';width=1080;height=1920;tall=$false},
    @{name='Long';width=1080;height=2340;tall=$false},
    @{name='Narrow';width=720;height=1600;tall=$false},
    @{name='SafeTall';width=1080;height=1920;tall=$true}
)
try{
    foreach($profile in $profiles){
        foreach($cutout in $cutoutRecords){Invoke-PanelAdb @('shell','cmd','overlay','disable',$cutout.package)}
        Invoke-PanelAdb @('shell','wm','size',($profile.width.ToString()+'x'+$profile.height))
        if($profile.tall){Invoke-PanelAdb @('shell','cmd','overlay','enable',$tallPackage)}
        $samples=if($profile.name -eq 'Default'){$defaultSamples}else{@($defaultSamples[1],$defaultSamples[5],$defaultSamples[6])}
        foreach($sample in $samples){Capture-PreparedPanel $profile $sample}
        Record-PanelWindow $profile.name
    }
    [DateTime]::UtcNow.ToString('o') | Set-Content -LiteralPath (Join-Path $target 'panels-capture-complete-utc.txt')
}finally{
    $restoreErrors=@()
    try{
        if($originalOverride){Invoke-PanelAdb @('shell','wm','size',$originalOverride)}
        else{Invoke-PanelAdb @('shell','wm','size','reset')}
    }catch{$restoreErrors+=$_.Exception.Message}
    foreach($cutout in $cutoutRecords){
        try{Invoke-PanelAdb @('shell','cmd','overlay','disable',$cutout.package)}catch{$restoreErrors+=$_.Exception.Message}
    }
    foreach($cutout in @($cutoutRecords | Where-Object enabled)){
        try{Invoke-PanelAdb @('shell','cmd','overlay','enable',$cutout.package)}catch{$restoreErrors+=$_.Exception.Message}
    }
    try{Record-PanelWindow 'restored'}catch{$restoreErrors+=$_.Exception.Message}
    @{restoredUtc=[DateTime]::UtcNow.ToString('o');originalOverride=$originalOverride;originalCutouts=$cutoutRecords;restoreErrors=$restoreErrors;ordinarySaveRestored=$false} |
        ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $target 'panels-display-restoration.json') -Encoding utf8
    if($restoreErrors.Count -gt 0){throw ('Panel display restoration needs attention: '+($restoreErrors -join ' | '))}
}
