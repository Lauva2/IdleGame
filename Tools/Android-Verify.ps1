param(
    [ValidateSet('Info','Capture','Tap','Swipe','Start','Background','Stop','Logs','Size','ResetSize')][string]$Action = 'Info',
    [string]$Name = 'screen',
    [int]$X = 0, [int]$Y = 0, [int]$X2 = 0, [int]$Y2 = 0,
    [string]$Serial = 'emulator-5554',
    [int]$AdbServerPort = 5037,
    [string]$AdbPath = 'C:/Program Files/Unity/Hub/Editor/2022.3.8f1c1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe',
    [string]$Evidence = 'Docs/Verification/dev01/Evidence/Android-compat-r01'
)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
$appPackage = 'com.forestatelier.curioshop'
New-Item -ItemType Directory -Force -Path $Evidence | Out-Null
switch ($Action) {
    'Info' {
        $keys = @('ro.product.model','ro.build.version.release','ro.build.version.sdk','ro.product.cpu.abilist')
        $data = @(('Serial: ' + $Serial), ('Captured: ' + [DateTime]::UtcNow.ToString('o')))
        foreach ($key in $keys) { $value = & $adbPath -P $AdbServerPort -s $Serial shell getprop $key; $data += ($key + ': ' + $value) }
        $data += (& $adbPath -P $AdbServerPort -s $Serial shell wm size)
        $data += (& $adbPath -P $AdbServerPort -s $Serial shell wm density)
        $data | Set-Content -LiteralPath (Join-Path $Evidence 'device.txt') -Encoding utf8
        $data
    }
    'Capture' {
        $focus = & $adbPath -P $AdbServerPort -s $Serial shell dumpsys window
        if (-not ($focus | Select-String -Pattern ('mCurrentFocus=.*' + [regex]::Escape($appPackage)))) { throw 'Forest Shop is not the focused app; refusing unrelated screen capture.' }
        if ($Name -notmatch '^[a-zA-Z0-9_-]+$') { throw 'Use a simple evidence file name' }
        $destination = Join-Path (Resolve-Path -LiteralPath $Evidence).Path ($Name + '.png')
        $start = [Diagnostics.ProcessStartInfo]::new()
        $start.FileName = $adbPath
        $start.Arguments = '-P ' + $AdbServerPort + ' -s ' + $Serial + ' exec-out screencap -p'
        $start.UseShellExecute = $false
        $start.CreateNoWindow = $true
        $start.RedirectStandardOutput = $true
        $process = [Diagnostics.Process]::Start($start)
        $stream = [IO.File]::Create($destination)
        try { $process.StandardOutput.BaseStream.CopyTo($stream) } finally { $stream.Dispose() }
        $process.WaitForExit()
        if ($process.ExitCode -ne 0) { throw 'Device screenshot failed' }
        Write-Output $destination
    }
    'Tap' { & $adbPath -P $AdbServerPort -s $Serial shell input tap $X $Y }
    'Swipe' { & $adbPath -P $AdbServerPort -s $Serial shell input swipe $X $Y $X2 $Y2 600 }
    'Start' { & $adbPath -P $AdbServerPort -s $Serial shell am start -n ($appPackage + '/com.unity3d.player.UnityPlayerActivity') }
    'Background' { & $adbPath -P $AdbServerPort -s $Serial shell input keyevent KEYCODE_HOME }
    'Stop' { & $adbPath -P $AdbServerPort -s $Serial shell am force-stop $appPackage }
    'Logs' { & $adbPath -P $AdbServerPort -s $Serial logcat -d -s Unity AndroidRuntime | Set-Content -LiteralPath (Join-Path $Evidence 'logcat.txt') -Encoding utf8 }
    'Size' { & $adbPath -P $AdbServerPort -s $Serial shell wm size ($X.ToString() + 'x' + $Y.ToString()) }
    'ResetSize' { & $adbPath -P $AdbServerPort -s $Serial shell wm size reset }
}
