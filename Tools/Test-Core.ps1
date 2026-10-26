$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
$monoRoot = 'C:/Program Files/Unity/Hub/Editor/2022.3.8f1c1/Editor/Data/MonoBleedingEdge'
$monoExe = Join-Path $monoRoot 'bin/mono.exe'
$compiler = Join-Path $monoRoot 'lib/mono/4.5/csc.exe'
New-Item -ItemType Directory -Force -Path 'Tools/TestOutput' | Out-Null
$coreSources = Get-ChildItem -LiteralPath 'Assets/Scripts/Core' -Filter '*.cs' | Select-Object -ExpandProperty FullName
& $monoExe $compiler /nologo /langversion:9 /target:exe /out:Tools/TestOutput/CoreTests.exe ('/r:' + (Join-Path $monoRoot 'lib/mono/4.5/System.Web.Extensions.dll')) @coreSources Tools/CoreTests.cs Tools/ForestCoreTests.cs Tools/FishingCoreTests.cs Tools/MomentCoreTests.cs
if ($LASTEXITCODE -ne 0) { throw 'Core test compilation failed' }
& $monoExe Tools/TestOutput/CoreTests.exe 2>&1 | Tee-Object -FilePath 'Tools/TestOutput/core-tests.log'
if ($LASTEXITCODE -ne 0) { throw 'Core tests failed' }
