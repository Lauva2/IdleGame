param([Parameter(Mandatory=$true)][ValidateRange(0,2)][int]$Spot,[int]$Width=1080,[int]$Height=1920,[int]$Top=0)
# Project the actual centre of the existing dock's physics target in a reset
# forest camera. Independent of decorative UI badge layout; no game state edits.
$scale=$Width/1080.0
$viewHeight=[Math]::Min(1280*$scale,[Math]::Max(850*$scale,$Height-$Top-664*$scale))
$centers=@(@(-45,38),@(-45,55),@(-60,69))
$origins=@(@(-39.2,46),@(-48.8,50.8),@(-53.4,70))
$yaw=@(0,180,30)[$Spot]*[Math]::PI/180
$dx=$origins[$Spot][0]+[Math]::Sin($yaw)*.2-$centers[$Spot][0]
$dy=.8-1.3
$dz=$origins[$Spot][1]+[Math]::Cos($yaw)*.2-$centers[$Spot][1]-.7
$horizontal=($dx*25+$dz)/[Math]::Sqrt(626)
$vertical=(-27*$dx+626*$dy+675*$dz)/[Math]::Sqrt(1355*626)
[int[]]@([int][Math]::Round($Width/2+$horizontal*$viewHeight/23.6),[int][Math]::Round($Top+448*$scale+$viewHeight/2-$vertical*$viewHeight/23.6))
