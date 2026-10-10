[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'test.ps1')
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$taskBuild = Join-Path $taskRoot 'build\CodexOceanBoot'
$taskDist = Join-Path $taskRoot 'dist'
$taskStaging = Join-Path $taskRoot ('.local\package-'+[Guid]::NewGuid().ToString('N')+'\CodexOceanBoot')
New-Item -ItemType Directory -Force -Path $taskStaging,$taskDist | Out-Null
foreach ($taskFile in @('CodexOceanBoot.exe','Start.cmd','Preview.cmd','Settings.cmd','LICENSE','THIRD_PARTY_NOTICES.md','README.md')) { Copy-Item -LiteralPath (Join-Path $taskBuild $taskFile) -Destination $taskStaging }
Copy-Item -LiteralPath (Join-Path $taskRoot 'config\launcher.example.json') -Destination (Join-Path $taskStaging 'launcher.json')
Copy-Item -LiteralPath (Join-Path $taskBuild 'media'),(Join-Path $taskBuild 'licenses') -Destination $taskStaging -Recurse
$taskZip = Join-Path $taskDist 'CodexOceanBoot-0.1.0-win-x64.zip'
Compress-Archive -LiteralPath $taskStaging -DestinationPath $taskZip -Force
$taskHash = (Get-FileHash -LiteralPath $taskZip -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText($taskZip+'.sha256',$taskHash+'  '+[IO.Path]::GetFileName($taskZip)+[Environment]::NewLine,[Text.Encoding]::ASCII)
Write-Output ('Packaged: '+$taskZip)
