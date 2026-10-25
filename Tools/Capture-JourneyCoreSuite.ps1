param([Parameter(Mandatory=$true)][string]$Folder)
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
if(!$env:FOREST_ADB_SERIAL){throw 'Explicit ADB serial required'}
function Step([string]$Name,[scriptblock]$Run){
    $log=Join-Path $Folder ('capture-'+$Name+'.log')
    if(Test-Path -LiteralPath $log){throw ('Preserve prior capture log '+$Name)}
    try{
        [DateTime]::UtcNow.ToString('o')|Set-Content (Join-Path $Folder ($Name+'-suite-start.txt'))
        & $Run *> $log
        if($LASTEXITCODE -ne 0){throw ('Native command failed '+$Name)}
        Write-Output ('COMPLETE '+$Name)
    }catch{Get-Content -LiteralPath $log -Tail 15;throw}
}
Step 'window' { & Tools/Capture-JourneyWindow.ps1 -Folder $Folder }
Step 'interfaces' { & Tools/Capture-ForestInterfaces.ps1 -Folder $Folder }
Step 'forest-base' { & Tools/Capture-ForestExpansionBase.ps1 -Folder $Folder }
Step 'forest-states' { & Tools/Capture-ForestExpansionStates.ps1 -Folder $Folder }
Step 'production' { & Tools/Capture-D19Dynamics.ps1 -Folder $Folder -Mode Production }
Step 'negative' { & Tools/Capture-D19Dynamics.ps1 -Folder $Folder -Mode Negative }
Step 'reduced' { & Tools/Capture-D19Dynamics.ps1 -Folder $Folder -Mode Reduced }
Step 'market' { & Tools/Capture-ForestMarket.ps1 -Folder $Folder -ClipPrefix market-native }
# Final market interval is the performance sample. Do not decode/compact frames
# or build/modify sources during this capture. This is not the formal30min run.
