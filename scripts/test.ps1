[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build.ps1')
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskOutput = Join-Path $taskRoot 'build\CodexOceanBoot'
$taskCompiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$taskTests = Join-Path $taskOutput 'CoreTests.exe'
& $taskCompiler /nologo /target:exe /platform:x64 ('/out:'+$taskTests) ('/reference:'+(Join-Path $taskOutput 'CodexOceanBoot.exe')) /reference:System.Web.Extensions.dll (Join-Path $taskRoot 'tests\CoreTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
& $taskTests $taskRoot
if ($LASTEXITCODE -ne 0) { throw 'Core tests failed.' }
$taskForbidden = @(Get-ChildItem -LiteralPath (Join-Path $taskRoot 'src') -Filter '*.cs' | Select-String -Pattern 'Native.SetWindowPos\(target|Native.SetWindowPos\(candidate|\.Kill\(|AutoReplaceEntrypoints|ScanAllLocalDrives|ScreenFrames')
if ($taskForbidden.Count -gt 0) { throw 'Forbidden target mutation or legacy integration found.' }
$taskManifest = [IO.File]::ReadAllText((Join-Path $taskRoot 'media\manifest.json'),[Text.Encoding]::UTF8) | ConvertFrom-Json
foreach ($taskClip in $taskManifest.clips) {
 $taskFile = Join-Path $taskRoot ('media\'+$taskClip.file)
 if ((Get-FileHash -LiteralPath $taskFile -Algorithm SHA256).Hash -ine $taskClip.sha256) { throw ('Media hash mismatch: '+$taskClip.file) }
}
Write-Output 'PASS: target-window invariants and three upstream video checksums.'
