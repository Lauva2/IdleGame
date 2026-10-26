param([Parameter(Mandatory=$true)][string]$Folder)
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
$mono='C:/Program Files/Unity/Hub/Editor/2022.3.8f1c1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
foreach($name in @('production-garden','production-workshop','production-shop','busy-garden','reduced-garden','reduced-workshop','market','offscreen-sales')){
 $log="$Folder/$name-online-replay.txt"
 if(Test-Path $log){
  if(!(Get-Content -LiteralPath $log -Raw).Contains('PASS actual Android online replay:')){throw 'Keep earlier failed replay and resolve it explicitly'}
  Write-Output ('KEEP existing passed replay '+$name);continue
 }
 & $mono Tools/TestOutput/CompareOnline.exe "$Folder/$name-before.save" "$Folder/$name-after.save"|Tee-Object -FilePath $log
 if($LASTEXITCODE -ne 0){throw ('Actual interval replay failed '+$name)}
}
if(Test-Path "$Folder/negative-counters-audit.json"){
 if(!(Get-Content "$Folder/negative-counters-audit.json" -Raw|ConvertFrom-Json).allPassed){throw 'Retained blocked audit failed'}
 Write-Output 'KEEP existing passed blocked audit'
}else{
 & $python Tools/Audit-ForestBlocked.py $Folder
 if($LASTEXITCODE -ne 0){throw 'Blocked cases failed'}
}
if(Test-Path "$Folder/market-audit.json"){
 $market=Get-Content "$Folder/market-audit.json" -Raw|ConvertFrom-Json
 if(!$market.hasFiveMinuteSample -or $market.coinDelta -ne $market.salesDelta -or $market.fatalOrSaveErrorMatches.Count -gt 0 -or @($market.newProductsSold|Where-Object {$_ -le 0}).Count -gt 0){throw 'Retained market audit failed'}
 Write-Output 'KEEP existing passed market audit'
}else{
 & $python Tools/Audit-ForestMarket.py $Folder --segmented-startup
 if($LASTEXITCODE -ne 0){throw 'Current market/performance audit failed'}
}
if(Test-Path "$Folder/prepared-quality-audit.json"){
 $quality=Get-Content "$Folder/prepared-quality-audit.json" -Raw|ConvertFrom-Json
 if($quality.samples.Count -ne 4 -or @($quality.samples|Where-Object {!$_.passed}).Count -gt 0){throw 'Retained quality audit failed'}
 Write-Output 'KEEP existing passed quality audit'
}else{
 & $python Tools/Audit-FishingQualities.py $Folder
 if($LASTEXITCODE -ne 0){throw 'Actual prepared quality landings failed'}
}
if(Test-Path "$Folder/prepared-notice-audit.json"){
 if(!(Get-Content "$Folder/prepared-notice-audit.json" -Raw|ConvertFrom-Json).allPassed){throw 'Retained notice audit failed'}
 Write-Output 'KEEP existing passed notice audit'
}else{
 & $python Tools/Audit-FishingNotices.py $Folder
 if($LASTEXITCODE -ne 0){throw 'Actual prepared notice landings failed'}
}
Write-Output 'PASS all retained real intervals and prepared fish landing audits'
