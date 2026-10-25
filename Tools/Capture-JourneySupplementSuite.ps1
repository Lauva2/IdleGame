param([Parameter(Mandatory=$true)][string]$Folder)
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
if(!$env:FOREST_ADB_SERIAL){throw 'Explicit ADB serial required'}
function Step([string]$Name,[scriptblock]$Run){
 $log=Join-Path $Folder ('capture-'+$Name+'.log');if(Test-Path -LiteralPath $log){throw ('Keep prior capture log '+$Name)}
 try{& $Run *> $log;if($LASTEXITCODE -ne 0){throw ('Native command failed '+$Name)};Write-Output ('COMPLETE '+$Name)}catch{Get-Content -LiteralPath $log -Tail 16;throw}
}
Step 'legacy' { & Tools/Capture-JourneyLegacy.ps1 -Folder $Folder }
Step 'panels' { & Tools/Capture-D19Panels.ps1 -Folder $Folder }
Step 'facilities-fit' { & Tools/Capture-JourneyFacilities.ps1 -Folder $Folder }
Step 'initial' { & Tools/Capture-ForestInitial.ps1 -Folder $Folder }
Step 'credits' { & Tools/Capture-ForestReviewDetails.ps1 -Folder $Folder -Mode Credits }
Step 'decorations' { & Tools/Capture-ForestReviewDetails.ps1 -Folder $Folder -Mode Facilities }
Step 'fish-gallery' { & Tools/Capture-FishingGallery.ps1 -Folder $Folder -RunTag r01 -JourneyLayout }
Step 'fish-qualities' { & Tools/Capture-FishingQualities.ps1 -Folder $Folder }
Step 'fish-fit' { & Tools/Capture-FishingAdaptation.ps1 -Folder $Folder -JourneyLayout }
Step 'resin-fit' { & Tools/Capture-JourneyResinFit.ps1 -Folder $Folder }
Step 'fish-spots' { & Tools/Capture-FishingSpotSupplement.ps1 -Folder $Folder }
