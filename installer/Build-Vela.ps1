[CmdletBinding()]
param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '../bin/vela-release'),
    [string]$MSBuild = 'msbuild',
    [string]$InstallerCompiler = (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6/ISCC.exe'),
    [string[]]$MSBuildArguments = @()
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$delivery = [IO.Directory]::CreateDirectory([IO.Path]::GetFullPath($OutputDirectory)).FullName
$tempRoot = (Resolve-Path ([IO.Path]::GetTempPath())).Path
$scratch = New-Item -ItemType Directory -Path (Join-Path $tempRoot ('vela-build-' + [guid]::NewGuid().ToString('N')))
$payload = Join-Path $scratch.FullName 'payload'

try {
    # These executables are used by the application or exposed as its command-line interface.
    $helpers = @('BCU-console', 'OculusHelper', 'ScriptHelper', 'SteamHelper', 'StoreAppHelper',
        'UninstallerAutomatizer', 'UniversalUninstaller', 'WinUpdateHelper')
    $publishArguments = @('/restore', '/t:Publish', '/v:minimal', '/p:Configuration=Release', '/p:Platform=x64',
        '/p:RuntimeIdentifier=win-x64', '/p:SelfContained=true', '/p:GenerateSatelliteAssembliesForCore=true',
        "/p:PublishDir=$payload/")
    foreach ($project in ($helpers + 'BulkCrapUninstaller')) {
        Write-Host "Publishing $project for Windows x64"
        & $MSBuild (Join-Path $root "source/$project/$project.csproj") @publishArguments @MSBuildArguments
        if ($LASTEXITCODE -ne 0) { throw "MSBuild failed for $project with exit code $LASTEXITCODE." }
    }

    Rename-Item -LiteralPath (Join-Path $payload 'BCUninstaller.exe') -NewName 'Vela.exe'
    $cleanLogs = Join-Path $payload 'CleanLogs.bat'
    if (Test-Path -LiteralPath $cleanLogs) { Remove-Item -LiteralPath $cleanLogs }

    foreach ($executable in ($helpers + 'Vela')) {
        $bytes = [IO.File]::ReadAllBytes((Join-Path $payload "$executable.exe"))
        $header = [BitConverter]::ToInt32($bytes, 0x3c)
        if ([BitConverter]::ToUInt16($bytes, $header + 4) -ne 0x8664) {
            throw "$executable.exe is not an x64 executable."
        }

        $assembly = if ($executable -eq 'Vela') { 'BCUninstaller' } else { $executable }
        $runtime = Get-Content -LiteralPath (Join-Path $payload "$assembly.runtimeconfig.json") -Raw | ConvertFrom-Json
        if ('Microsoft.NETCore.App' -notin $runtime.runtimeOptions.includedFrameworks.name) {
            throw "$executable.exe was not published self-contained."
        }
    }

    $info = (Get-Item -LiteralPath (Join-Path $payload 'Vela.exe')).VersionInfo
    $version = "$($info.FileMajorPart).$($info.FileMinorPart).$($info.FileBuildPart)"
    $baseName = "Vela_${version}_win-x64"
    $installer = Join-Path $scratch.FullName "${baseName}_setup.exe"
    $archive = Join-Path $scratch.FullName "${baseName}_portable.zip"

    & $InstallerCompiler /DVelaX64 "/DInputDir=$payload" "/O$($scratch.FullName)" /Qp (Join-Path $PSScriptRoot 'BcuSetup.iss')
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed with exit code $LASTEXITCODE." }
    if ((Get-Item -LiteralPath $installer).VersionInfo.ProductName.Trim() -ne 'Vela') {
        throw 'The installer product name did not match Vela.'
    }

    [IO.Compression.ZipFile]::CreateFromDirectory($payload, $archive, [IO.Compression.CompressionLevel]::Optimal, $false)
    $files = @(Get-Item -LiteralPath $installer,$archive)
    $hashes = foreach ($file in $files) {
        $digest = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        "$digest  $($file.Name)"
    }
    $checksum = Join-Path $scratch.FullName 'SHA256SUMS.txt'
    [IO.File]::WriteAllText($checksum, ($hashes -join [char]10) + [char]10, [Text.Encoding]::ASCII)
    Copy-Item -LiteralPath @($installer,$archive,$checksum) -Destination $delivery
} finally {
    $prefix = $tempRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $resolved = (Resolve-Path -LiteralPath $scratch.FullName).Path
    if ($resolved.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase) -and
        (Split-Path -Leaf $resolved) -match '^vela-build-[a-f0-9]{32}$') {
        Remove-Item -LiteralPath $resolved -Recurse
    }
}
