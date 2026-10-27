param([Parameter(Mandatory=$true)][string]$Folder,[Parameter(Mandatory=$true)][ValidateSet('Courtyard','Forest')][string]$Group,[ValidateSet('Default','Long','Narrow','SafeTall')][string[]]$Profiles=@('Default','Long','Narrow','SafeTall'),[ValidatePattern('^[a-z0-9-]*$')][string]$RunTag='')
$ErrorActionPreference='Stop';Set-Location (Split-Path -Parent $PSScriptRoot)
if(!$env:FOREST_ADB_SERIAL){throw 'Explicit device required'}
$serial=$env:FOREST_ADB_SERIAL;$adb=(Resolve-Path Tools/AndroidSdk/platform-tools/adb.exe).Path
$python='C:/Users/Lauva/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
$candidate=Split-Path -Leaf $Folder;$cutout='com.android.internal.display.cutout.emulation.tall'
foreach($name in @('normal-restorable.save','normal-restorable.save.bak','ordinary-fishing-audit.json')){if(!(Test-Path "$Folder/$name")){throw 'Protect and audit latest ordinary progress first'}}
function A([string[]]$p){$value=& $adb -P 5038 -s $serial @p;if($LASTEXITCODE -ne 0){throw 'Life capture ADB failed'};return $value}
function Plan($name,$steps){
 $file="$Folder/Plans/life-$Group-$name.json";if(Test-Path $file){throw 'Keep prior input plan'}
 @{directory="$Folder/Evidence/Life/$Group/$name";steps=$steps}|ConvertTo-Json -Depth 8|Set-Content $file
 & $python Tools/Forest-DeviceReview.py $file;if($LASTEXITCODE -ne 0){throw 'Life capture UI failed'}
}
function RecordStart($name){
 $file="$Folder/Evidence/life-$Group-$name.mp4";if(Test-Path $file){throw 'Keep original recording'}
 $remote="/sdcard/$candidate-life-$Group-$name.mp4"
 $process=Start-Process -FilePath $adb -ArgumentList '-P','5038','-s',$serial,'shell','screenrecord','--bit-rate','4000000','--time-limit','180',$remote -PassThru -WindowStyle Hidden
 Start-Sleep -Milliseconds 700
 $recordMatches=@();foreach($recordId in ((A @('shell','pidof','screenrecord')) -split '\s+')){
  if($recordId -notmatch '^\d+$'){continue}
  $command=(A @('shell','cat',"/proc/$recordId/cmdline")) -join ''
  if($command.Contains($remote)){$recordMatches+=$recordId}
 }
 if($recordMatches.Count -ne 1){throw 'Cannot identify this exact screenrecord process'}
 @{process=$process;remote=$remote;file=$file;recordId=$recordMatches[0];utc=[DateTime]::UtcNow.ToString('o')}
}
function RecordEnd($record){
 $command=(A @('shell','cat',"/proc/$($record.recordId)/cmdline")) -join ''
 if(!$command.Contains($record.remote)){throw 'Recorder identity changed'}
 A @('shell','kill','-2',$record.recordId)|Out-Null
 $record.process.WaitForExit();if($record.process.ExitCode -ne 0){throw 'Recorder failed'}
 A @('pull',$record.remote,$record.file)
 @{startUtc=$record.utc;endUtc=[DateTime]::UtcNow.ToString('o');file=$record.file;method='SIGINT to the exact owned screenrecord PID after verifying /proc cmdline; original unedited movie.'}|ConvertTo-Json|Set-Content ($record.file+'.json')
}
function CameraSteps($region,$s,$h,$top,$forest){
 $designH=($h-$top)/$s
 $viewTop=if($forest){204}elseif($region -eq 1){214}else{112}
 $height=if($forest){[Math]::Min(1280,[Math]::Max(850,$designH-664))}else{[Math]::Min($(if($region -eq 0){1320}else{1280}),[Math]::Max(900,$designH-368-$viewTop-104))}
 $buttonY=[int]($top+(244+$viewTop+$height-64)*$s)
 $dragTop=[int]($top+(244+$viewTop+$height*.34)*$s);$dragBottom=[int]($top+(244+$viewTop+$height*.70)*$s)
 $steps=@(@{shot="region-$region-00-default"})
 for($i=0;$i -lt 7;$i++){$steps+=@{tap=@([int](775*$s),$buttonY)}}
 $steps+=@{shot="region-$region-01-wide-cap"}
 for($i=0;$i -lt 12;$i++){$steps+=@{tap=@([int](885*$s),$buttonY)}}
 $steps+=@{shot="region-$region-02-near-cap"}
 for($i=0;$i -lt 2;$i++){$steps+=@{swipe=@([int](650*$s),$dragTop,[int](180*$s),$dragBottom,500)}}
 $steps+=@{shot="region-$region-03-boundary-a"};$mid=[int](($dragTop+$dragBottom)/2)
 for($i=0;$i -lt 4;$i++){$steps+=@{swipe=@([int](180*$s),$mid,[int](920*$s),$mid,500)}}
 $steps+=@{shot="region-$region-04-boundary-b"}
 for($i=0;$i -lt 4;$i++){$steps+=@{swipe=@([int](540*$s),$dragBottom,[int](540*$s),$dragTop,500)}}
 $steps+=@{shot="region-$region-05-boundary-c"}
 for($i=0;$i -lt 4;$i++){$steps+=@{swipe=@([int](920*$s),$mid,[int](180*$s),$mid,500)}}
 $steps+=@(@{shot="region-$region-06-boundary-d"},@{tap=@([int](998*$s),$buttonY)},@{shot="region-$region-07-recenter"})
 return $steps
}
try{
 foreach($profile in @(@{n='Default';w=1080;h=1920;s=1.0;top=0},@{n='Long';w=1080;h=2340;s=1.0;top=0},@{n='Narrow';w=720;h=1600;s=2.0/3;top=0},@{n='SafeTall';w=1080;h=1920;s=1.0;top=84})){
  if($profile.n -notin $Profiles){continue}
  $n=$profile.n+$(if($RunTag){'-'+$RunTag}else{''});$s=$profile.s;$h=$profile.h;$top=$profile.top
  A @('shell','cmd','overlay','disable',$cutout)|Out-Null;A @('shell','wm','size',($profile.w.ToString()+'x'+$h))|Out-Null
  if($top){A @('shell','cmd','overlay','enable',$cutout)|Out-Null}
  if($Group -eq 'Courtyard'){& Tools/Activate-ForestReview.ps1 -Folder $Folder -Variant late}else{& Tools/Activate-MomentReview.ps1 -Folder $Folder -Variant finished}
  if($LASTEXITCODE -ne 0){throw 'Prepared fixture failed'}
  $record=RecordStart $n;$steps=@()
  if($Group -eq 'Courtyard'){
   for($region=0;$region -lt 4;$region++){
    $steps+=@{tap=@([int]((130+263*$region)*$s),[int](300*$s+$top))}
    $steps+=CameraSteps $region $s $h $top $false
   }
  }else{
   $steps+=@{tap=@([int](150*$s),[int](410*$s+$top))}
   for($id=0;$id -lt 3;$id++){
    $steps+=@{tap=@([int]((170+350*$id)*$s),[int](290*$s+$top))}
    $steps+=CameraSteps ($id+4) $s $h $top $true
   }
   $steps+=@{tap=@([int](530*$s),[int](390*$s+$top))}
   $steps+=CameraSteps 7 $s $h $top $true
   $steps+=@(@{tap=@([int](950*$s),[int](160*$s+$top))},@{tap=@([int](925*$s),[int]($h-115*$s))},@{tap=@([int](900*$s),[int]($h-216*$s))},@{shot='30-leaf-focused'})
   $height=[Math]::Min(1280*$s,[Math]::Max(850*$s,$h-$top-664*$s));$center=[int](448*$s+$top+$height/2)
   $steps+=@(@{tap=@([int](540*$s),$center)},@{shot='31-bookmark-choice-card'},@{tap=@([int](800*$s),[int]($h-150*$s))},@{wait=3},@{shot='32-star-focused'})
  }
  Plan $n $steps
  RecordEnd $record
  A @('shell','wm','size')|Set-Content "$Folder/life-$Group-$n-size.txt"
  A @('shell','dumpsys','window','displays')|Set-Content "$Folder/life-$Group-$n-window.txt"
  A @('shell','input','keyevent','KEYCODE_HOME')|Out-Null;Start-Sleep -Seconds 1
  A @('pull','/sdcard/Android/data/com.forestatelier.curioshop/files/forest-shop.save',"$Folder/life-$Group-$n-after.save")
  Write-Output "COMPLETE life $Group $n"
 }
}finally{A @('shell','wm','size','reset')|Out-Null;A @('shell','cmd','overlay','disable',$cutout)|Out-Null}
