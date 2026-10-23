param([string]$Folder='Docs/Verification/3d-d18-forest02')
$ErrorActionPreference='Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
function Plan($steps,$suffix=''){
    @{directory=($Folder+'/Evidence'+$suffix);steps=$steps}|ConvertTo-Json -Depth 7|Set-Content Builds/d18-static-plan.json
    & $python Tools/Forest-DeviceReview.py Builds/d18-static-plan.json
    if($LASTEXITCODE -ne 0){throw 'Capture plan failed'}
}
& Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant states
Plan @(@{tap=@(930,300)},@{shot='52-empty-reserved-shelf-symbols'},@{tap=@(200,1690)},@{shot='53-empty-reserved-written-status'},@{tap=@(540,1850)},@{shot='54-shortage-order'})
& Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant unrestored
Plan @(@{shot='55-annex-before-overview'},@{tap=@(930,300)},@{shot='56-annex-before-shop-basic-sign-pot'},@{tap=@(660,300)},@{shot='57-basic-curtain-rug'})
for($resident=0;$resident -lt 5;$resident++){
    & Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant "dialogue$resident"
    Plan @(@{shot="58-dialogue-$resident"},@{swipe=@(850,1730,850,1080,650)},@{shot="59-dialogue-$resident-bottom"})
}
& Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant report
Plan @(@{shot='60-prepared-report-top'},@{swipe=@(850,1740,850,1010,600)},@{swipe=@(850,1740,850,1010,600)},@{shot='61-prepared-report-bottom'})
& Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant chapter
Plan @(@{shot='62-prepared-chapter-top'},@{swipe=@(850,1740,850,1010,600)},@{shot='63-prepared-chapter-bottom'},@{back=$true},@{tap=@(930,300)},@{shot='64-annex-after-shop-reward-sign-pot'},@{tap=@(660,300)},@{shot='65-reward-curtain-rug'})
