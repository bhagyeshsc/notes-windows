param([string]$OutputDirectory = '')
$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = $project
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$wpf = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF'
$gac = 'C:\Windows\Microsoft.NET\assembly\GAC_MSIL'
$dest = if($OutputDirectory){[IO.Path]::GetFullPath($OutputDirectory)}else{Join-Path $repo 'dist'}
if (!(Test-Path -LiteralPath $compiler)) { throw '.NET Framework C# compiler was not found.' }
New-Item -ItemType Directory -Force -Path $dest | Out-Null
$references = @(
    "/reference:$($wpf)\WindowsBase.dll",
    "/reference:$($wpf)\PresentationCore.dll",
    "/reference:$($wpf)\PresentationFramework.dll",
    "/reference:$($gac)\System.Xaml\v4.0_4.0.0.0__b77a5c561934e089\System.Xaml.dll"
)
$appArguments = @('/nologo','/target:winexe','/platform:x64','/optimize+',('/win32icon:'+(Join-Path $project 'App.ico')),('/resource:'+(Join-Path $project 'App.ico')+',Notes.App.ico'),('/resource:'+(Join-Path $project 'LICENSE')+',Notes.LICENSE'),('/out:' + (Join-Path $dest 'Notes.exe')),'/reference:System.dll','/reference:System.Core.dll','/reference:System.Windows.Forms.dll','/reference:System.Drawing.dll') + $references + @((Join-Path $project 'Notes.cs'),(Join-Path $project 'Design.cs'),(Join-Path $project 'Links.cs'),(Join-Path $project 'Scrollbars.cs'))
& $compiler $appArguments
if ($LASTEXITCODE -ne 0) { throw 'Notes.exe compilation failed.' }
$installerArguments = @('/nologo','/target:winexe','/platform:x64','/optimize+',('/win32icon:'+(Join-Path $project 'App.ico')),('/out:' + (Join-Path $dest 'Notes-Setup.exe')),'/reference:System.dll','/reference:System.Core.dll','/reference:System.Windows.Forms.dll',('/resource:' + (Join-Path $dest 'Notes.exe') + ',Notes.exe'),('/resource:'+(Join-Path $project 'LICENSE')+',Notes.LICENSE'),(Join-Path $project 'Setup.cs'))
& $compiler $installerArguments
if ($LASTEXITCODE -ne 0) { throw 'Notes-Setup.exe compilation failed.' }
Copy-Item -LiteralPath (Join-Path $project 'README.md') -Destination $dest -Force
Copy-Item -LiteralPath (Join-Path $project 'LICENSE') -Destination $dest -Force
Copy-Item -LiteralPath (Join-Path $project 'build.ps1') -Destination (Join-Path $dest 'Build.ps1') -Force
$sourceZip = Join-Path $dest 'Notes-Source.zip'
if (Test-Path -LiteralPath $sourceZip) { Remove-Item -LiteralPath $sourceZip -Force }
Compress-Archive -Path (Join-Path $project 'Notes.cs'),(Join-Path $project 'Design.cs'),(Join-Path $project 'Links.cs'),(Join-Path $project 'Scrollbars.cs'),(Join-Path $project 'App.ico'),(Join-Path $project 'BuildAssets.cs'),(Join-Path $project 'Setup.cs'),(Join-Path $project 'build.ps1'),(Join-Path $project 'README.md'),(Join-Path $project 'LICENSE') -DestinationPath $sourceZip
Write-Host "Built app, installer, README, and source archive in $dest"
