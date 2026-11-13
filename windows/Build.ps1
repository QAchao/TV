param(
    [Parameter(Mandatory)][string]$RuntimePath,
    [string]$Output = (Join-Path $PSScriptRoot 'dist/LibreTV.exe'),
    [string]$NodePath = (Get-Command node.exe -ErrorAction Stop).Source
)

$ErrorActionPreference = 'Stop'
$runtime = (Resolve-Path -LiteralPath $RuntimePath).Path
$stage = Join-Path ([IO.Path]::GetTempPath()) ('libretv-build-' + [guid]::NewGuid().ToString('N'))
$outputFile = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Output)
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$source = Split-Path -Parent $PSScriptRoot
New-Item -ItemType Directory -Path $stage | Out-Null
try {
    $payload = New-Item -ItemType Directory -Path (Join-Path $stage 'runtime')
    foreach ($name in @('server.js', 'package.json', 'node_modules', '.next', 'public')) {
        Copy-Item -LiteralPath (Join-Path $runtime $name) -Destination $payload.FullName -Recurse
    }
    Copy-Item -LiteralPath $NodePath -Destination (Join-Path $payload.FullName 'node.exe')
    Copy-Item -LiteralPath (Join-Path $source 'LICENSE') -Destination (Join-Path $payload.FullName 'LibreTV-LICENSE.txt')
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Node-LICENSE.txt') -Destination $payload.FullName
    $defaults = Join-Path $payload.FullName 'defaults.env'
    $local = Join-Path $runtime '.env.local'
    $entries = if (Test-Path -LiteralPath $local) {
        Get-Content -LiteralPath $local -Encoding utf8 | Where-Object {
            $_ -match '^DEFAULT_(SOURCES|LIVE_SOURCES|SUBSCRIPTIONS)='
        }
    }
    if (!$entries) { $entries = Get-Content (Join-Path $PSScriptRoot 'sources.env') -Encoding utf8 }
    [IO.File]::WriteAllLines($defaults, [string[]]$entries, [Text.UTF8Encoding]::new($false))
    $archive = Join-Path $stage 'runtime.zip'
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::CreateFromDirectory($payload.FullName, $archive)
    New-Item -ItemType Directory -Path (Split-Path -Parent $outputFile) -Force | Out-Null
    $icon = Join-Path $source 'public/favicon.ico'
    $arguments = @(
        '/nologo', '/target:winexe', '/platform:x64', '/optimize+', "/out:$outputFile", "/win32icon:$icon",
        '/reference:System.Windows.Forms.dll', '/reference:System.IO.Compression.dll',
        '/reference:System.IO.Compression.FileSystem.dll', "/resource:$archive,LibreTV.Runtime.zip",
        (Join-Path $PSScriptRoot 'Launcher.cs')
    )
    & $compiler @arguments
    if ($LASTEXITCODE -ne 0) { throw 'Windows launcher compilation failed.' }
    Get-Item -LiteralPath $outputFile
} finally {
    $resolvedStage = (Resolve-Path -LiteralPath $stage).Path
    if ($resolvedStage -eq $stage) { Remove-Item -LiteralPath $resolvedStage -Recurse -Force }
}
