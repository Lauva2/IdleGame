param([Parameter(Mandatory=$true)][string]$Folder,[ValidateSet('Legacy','Journey','Fishing','Presentation','All')][string]$Group='All',[switch]$Compact)
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
if(!$env:FOREST_ADB_SERIAL){throw 'Explicit device required'}
function Step([string]$Name,[scriptblock]$Run){
 $log="$Folder/capture-$Name.log";if(Test-Path $log){throw ('Preserve prior capture log '+$Name)}
 try{& $Run *> $log;if($LASTEXITCODE -ne 0){throw ('Native command failed '+$Name)};Write-Output ('COMPLETE '+$Name)}catch{Get-Content $log -Tail 18;throw}
}
if($Group -in @('Legacy','All')){
 Step 'legacy' { & Tools/Capture-JourneyLegacy.ps1 -Folder $Folder }
 Step 'panels' { & Tools/Capture-D19Panels.ps1 -Folder $Folder }
 Step 'facilities-fit' { & Tools/Capture-JourneyFacilities.ps1 -Folder $Folder }
 Step 'initial' { & Tools/Capture-ForestInitial.ps1 -Folder $Folder }
 Step 'credits' { & Tools/Capture-ForestReviewDetails.ps1 -Folder $Folder -Mode Credits }
 Step 'decorations' { & Tools/Capture-ForestReviewDetails.ps1 -Folder $Folder -Mode Facilities }
}
if($Group -in @('Journey','All')){
 Step 'journey' { & Tools/Capture-JourneyReview.ps1 -Folder $Folder -StillsOnly:$Compact }
 Step 'resin-fit' { & Tools/Capture-JourneyResinFit.ps1 -Folder $Folder }
 Step 'pavilion-fit' { & Tools/Capture-JourneyPavilion.ps1 -Folder $Folder }
}
if($Group -in @('Fishing','All')){
 Step 'fish-gallery' { & Tools/Capture-FishingGallery.ps1 -Folder $Folder -RunTag r01 -JourneyLayout -StillsOnly:$Compact }
 Step 'fish-qualities' { & Tools/Capture-FishingQualities.ps1 -Folder $Folder -PresentationLayout }
 Step 'fish-notices' { & Tools/Capture-FishingNotices.ps1 -Folder $Folder -PresentationLayout }
 Step 'fish-fit' { & Tools/Capture-FishingAdaptation.ps1 -Folder $Folder -JourneyLayout }
}
if($Group -in @('Presentation','All')){
 Step 'presentation-fit' { & Tools/Capture-PresentationFishing.ps1 -Folder $Folder -Fit }
 Step 'presentation-spots' { & Tools/Capture-PresentationFishing.ps1 -Folder $Folder }
 Step 'zoom-sales' { & Tools/Capture-JourneyZoomSales.ps1 -Folder $Folder }
}
