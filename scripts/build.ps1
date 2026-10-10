[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskOutput = Join-Path $taskRoot 'build\CodexOceanBoot'
New-Item -ItemType Directory -Force -Path $taskOutput | Out-Null
$taskFramework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$taskCompiler = Join-Path $taskFramework 'csc.exe'
if (!(Test-Path -LiteralPath $taskCompiler)) { throw 'The .NET Framework x64 compiler is missing.' }
$taskArgs = @('/nologo','/target:winexe','/platform:x64','/optimize+',('/out:'+(Join-Path $taskOutput 'CodexOceanBoot.exe')),('/win32manifest:'+(Join-Path $taskRoot 'src\app.manifest')))
foreach ($taskRef in @('System.dll','System.Core.dll','System.Web.Extensions.dll','System.Xaml.dll','System.Drawing.dll','System.Windows.Forms.dll')) { $taskArgs += '/reference:'+$taskRef }
foreach ($taskRef in @('WindowsBase.dll','PresentationCore.dll','PresentationFramework.dll')) { $taskArgs += '/reference:'+(Join-Path $taskFramework ('WPF\'+$taskRef)) }
$taskArgs += @(Get-ChildItem -LiteralPath (Join-Path $taskRoot 'src') -Filter '*.cs' | ForEach-Object { $_.FullName })
& $taskCompiler @taskArgs
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
if (!(Test-Path -LiteralPath (Join-Path $taskOutput 'launcher.json'))) { Copy-Item -LiteralPath (Join-Path $taskRoot 'config\launcher.example.json') -Destination (Join-Path $taskOutput 'launcher.json') }
New-Item -ItemType Directory -Force -Path (Join-Path $taskOutput 'media'),(Join-Path $taskOutput 'licenses') | Out-Null
Get-ChildItem -LiteralPath (Join-Path $taskRoot 'media') -File | Copy-Item -Destination (Join-Path $taskOutput 'media') -Force
Get-ChildItem -LiteralPath (Join-Path $taskRoot 'licenses') -File | Copy-Item -Destination (Join-Path $taskOutput 'licenses') -Force
foreach ($taskFile in @('LICENSE','THIRD_PARTY_NOTICES.md','README.md','Start.cmd','Preview.cmd','Settings.cmd')) { if (Test-Path -LiteralPath (Join-Path $taskRoot $taskFile)) { Copy-Item -LiteralPath (Join-Path $taskRoot $taskFile) -Destination $taskOutput -Force } }
Write-Output ('Built: '+(Join-Path $taskOutput 'CodexOceanBoot.exe'))
