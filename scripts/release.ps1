param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateNotNullOrEmpty()]
    [string]$Version
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$publishRoot = Join-Path $repoRoot "publish"
$artifactsRoot = Join-Path $repoRoot "artifacts"
$stagingRoot = Join-Path $artifactsRoot "_staging"
$packageName = "RapidRedPanda-ISBM-Wrapper"
$cliProject = Join-Path $repoRoot "src/RapidRedPanda.Wrapper.Cli/RapidRedPanda.Wrapper.Cli.csproj"
$consoleProject = Join-Path $repoRoot "samples/csharp/RapidRedPanda.Wrapper.Console/RapidRedPanda.Wrapper.Console.csproj"

if ([string]::IsNullOrWhiteSpace($Version)) {
    throw "Version parameter is required. Usage: .\scripts\release.ps1 0.3.4"
}

function Remove-DirectoryIfExists {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
}

function Invoke-NativeCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $FilePath $($Arguments -join ' ')"
    }
}

function Copy-RootDocumentation {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PackageDirectory
    )

    $documentationFiles = @(
        "README.md",
        "CHANGELOG.md",
        "CONTRIBUTING.md",
        "SECURITY.md"
    )

    foreach ($documentationFile in $documentationFiles) {
        $sourcePath = Join-Path $repoRoot $documentationFile
        if (Test-Path -LiteralPath $sourcePath) {
            Copy-Item -LiteralPath $sourcePath -Destination $PackageDirectory -Force
        }
    }
}

function Copy-PythonSamples {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PackageDirectory
    )

    $pythonSamplesPath = Join-Path $repoRoot "samples/python"
    if (Test-Path -LiteralPath $pythonSamplesPath) {
        $samplesRoot = Join-Path $PackageDirectory "samples"
        $pythonSamplesDestination = Join-Path $samplesRoot "python"
        New-Item -ItemType Directory -Force -Path $samplesRoot | Out-Null
        Copy-Item -LiteralPath $pythonSamplesPath -Destination $samplesRoot -Recurse -Force

        $localPythonConfig = Join-Path $pythonSamplesDestination "sample_config.json"
        if (Test-Path -LiteralPath $localPythonConfig) {
            Remove-Item -LiteralPath $localPythonConfig -Force
        }

        $pythonCache = Join-Path $pythonSamplesDestination "__pycache__"
        if (Test-Path -LiteralPath $pythonCache) {
            Remove-Item -LiteralPath $pythonCache -Recurse -Force
        }
    }
}

function Copy-CSharpConsoleSample {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PackageDirectory,

        [Parameter(Mandatory = $true)]
        [string]$ConsolePublishDirectory
    )

    $samplesRoot = Join-Path $PackageDirectory "samples"
    $consoleDestination = Join-Path $samplesRoot "csharp-console"
    New-Item -ItemType Directory -Force -Path $consoleDestination | Out-Null
    Copy-Item -Path (Join-Path $ConsolePublishDirectory "*") -Destination $consoleDestination -Recurse -Force

    $localConsoleConfig = Join-Path $consoleDestination "appsettings.Development.json"
    if (Test-Path -LiteralPath $localConsoleConfig) {
        Remove-Item -LiteralPath $localConsoleConfig -Force
    }

    $consoleExampleConfig = Join-Path $repoRoot "samples/csharp/RapidRedPanda.Wrapper.Console/appsettings.example.json"
    if (Test-Path -LiteralPath $consoleExampleConfig) {
        Copy-Item -LiteralPath $consoleExampleConfig -Destination $consoleDestination -Force
    }

    $consoleReadme = Join-Path $repoRoot "samples/csharp/RapidRedPanda.Wrapper.Console/README.md"
    if (Test-Path -LiteralPath $consoleReadme) {
        Copy-Item -LiteralPath $consoleReadme -Destination $consoleDestination -Force
    }
}

function ConvertFrom-OctalString {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    $result = 0L
    foreach ($character in $Value.Trim([char]0, " ").ToCharArray()) {
        if ($character -lt "0" -or $character -gt "7") {
            break
        }

        $result = ($result * 8) + ([int][char]$character - [int][char]"0")
    }

    return $result
}

function Set-TarHeaderChecksum {
    param(
        [Parameter(Mandatory = $true)]
        [byte[]]$Bytes,

        [Parameter(Mandatory = $true)]
        [int]$Offset
    )

    for ($index = 148; $index -lt 156; $index++) {
        $Bytes[$Offset + $index] = 32
    }

    $checksum = 0
    for ($index = 0; $index -lt 512; $index++) {
        $checksum += $Bytes[$Offset + $index]
    }

    $checksumText = ([Convert]::ToString($checksum, 8).PadLeft(6, "0") + "`0 ")
    $checksumBytes = [System.Text.Encoding]::ASCII.GetBytes($checksumText)
    [Array]::Copy($checksumBytes, 0, $Bytes, $Offset + 148, 8)
}

function Set-TarGzEntryMode {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ArchivePath,

        [Parameter(Mandatory = $true)]
        [string]$EntryName,

        [Parameter(Mandatory = $true)]
        [string]$Mode
    )

    $compressedBytes = [System.IO.File]::ReadAllBytes($ArchivePath)
    $compressedStream = [System.IO.MemoryStream]::new($compressedBytes)
    $gzipStream = [System.IO.Compression.GZipStream]::new($compressedStream, [System.IO.Compression.CompressionMode]::Decompress)
    $tarStream = [System.IO.MemoryStream]::new()
    $gzipStream.CopyTo($tarStream)
    $gzipStream.Dispose()
    $compressedStream.Dispose()

    $tarBytes = $tarStream.ToArray()
    $tarStream.Dispose()

    $encoding = [System.Text.Encoding]::ASCII
    $offset = 0
    $entryFound = $false

    while ($offset + 512 -le $tarBytes.Length) {
        $header = $tarBytes[$offset..($offset + 511)]
        if (($header | Where-Object { $_ -ne 0 }).Count -eq 0) {
            break
        }

        $name = $encoding.GetString($tarBytes, $offset, 100).Trim([char]0)
        $prefix = $encoding.GetString($tarBytes, $offset + 345, 155).Trim([char]0)
        $fullName = if ($prefix) { "$prefix/$name" } else { $name }
        $sizeText = $encoding.GetString($tarBytes, $offset + 124, 12)
        $size = ConvertFrom-OctalString -Value $sizeText

        if ($fullName -eq $EntryName) {
            $modeText = $Mode.PadLeft(7, "0") + "`0"
            $modeBytes = $encoding.GetBytes($modeText)
            [Array]::Copy($modeBytes, 0, $tarBytes, $offset + 100, 8)
            Set-TarHeaderChecksum -Bytes $tarBytes -Offset $offset
            $entryFound = $true
            break
        }

        $dataBlocks = [Math]::Ceiling($size / 512)
        $offset += 512 + [int]($dataBlocks * 512)
    }

    if (-not $entryFound) {
        throw "Could not find tar entry '$EntryName' in '$ArchivePath'."
    }

    $outputStream = [System.IO.MemoryStream]::new()
    $outputGzipStream = [System.IO.Compression.GZipStream]::new($outputStream, [System.IO.Compression.CompressionMode]::Compress)
    $outputGzipStream.Write($tarBytes, 0, $tarBytes.Length)
    $outputGzipStream.Dispose()
    [System.IO.File]::WriteAllBytes($ArchivePath, $outputStream.ToArray())
    $outputStream.Dispose()
}

function Copy-PublishOutput {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Rid,

        [Parameter(Mandatory = $true)]
        [string]$CliPublishDirectory,

        [Parameter(Mandatory = $true)]
        [string]$ConsolePublishDirectory
    )

    $stageDir = Join-Path $stagingRoot $Rid
    $packageDirName = "$packageName-v$Version-$Rid"
    $packageDir = Join-Path $stageDir $packageDirName

    New-Item -ItemType Directory -Force -Path $packageDir | Out-Null
    Copy-Item -Path (Join-Path $CliPublishDirectory "*") -Destination $packageDir -Recurse -Force
    Copy-RootDocumentation -PackageDirectory $packageDir
    Copy-PythonSamples -PackageDirectory $packageDir
    Copy-CSharpConsoleSample -PackageDirectory $packageDir -ConsolePublishDirectory $ConsolePublishDirectory

    return $stageDir
}

Set-Location $repoRoot

Remove-DirectoryIfExists -Path $publishRoot
Remove-DirectoryIfExists -Path $artifactsRoot
New-Item -ItemType Directory -Force -Path $publishRoot | Out-Null
New-Item -ItemType Directory -Force -Path $artifactsRoot | Out-Null
New-Item -ItemType Directory -Force -Path $stagingRoot | Out-Null

Invoke-NativeCommand -FilePath "dotnet" -Arguments @("restore")
Invoke-NativeCommand -FilePath "dotnet" -Arguments @("build", "-c", "Release")

$runtimeIds = @("win-x64", "linux-x64", "linux-arm64")

foreach ($rid in $runtimeIds) {
    $runtimePublishRoot = Join-Path $publishRoot $rid
    $cliOutputDir = Join-Path $runtimePublishRoot "cli"
    $consoleOutputDir = Join-Path $runtimePublishRoot "csharp-console"

    Invoke-NativeCommand -FilePath "dotnet" -Arguments @(
        "publish",
        $cliProject,
        "-c",
        "Release",
        "-r",
        $rid,
        "--self-contained",
        "true",
        "-o",
        $cliOutputDir
    )

    Invoke-NativeCommand -FilePath "dotnet" -Arguments @(
        "publish",
        $consoleProject,
        "-c",
        "Release",
        "-r",
        $rid,
        "--self-contained",
        "true",
        "-o",
        $consoleOutputDir
    )

    $localConsoleConfig = Join-Path $consoleOutputDir "appsettings.Development.json"
    if (Test-Path -LiteralPath $localConsoleConfig) {
        Remove-Item -LiteralPath $localConsoleConfig -Force
    }
}

$winStageDir = Copy-PublishOutput -Rid "win-x64" -CliPublishDirectory (Join-Path $publishRoot "win-x64/cli") -ConsolePublishDirectory (Join-Path $publishRoot "win-x64/csharp-console")
$linuxX64StageDir = Copy-PublishOutput -Rid "linux-x64" -CliPublishDirectory (Join-Path $publishRoot "linux-x64/cli") -ConsolePublishDirectory (Join-Path $publishRoot "linux-x64/csharp-console")
$linuxArm64StageDir = Copy-PublishOutput -Rid "linux-arm64" -CliPublishDirectory (Join-Path $publishRoot "linux-arm64/cli") -ConsolePublishDirectory (Join-Path $publishRoot "linux-arm64/csharp-console")

$winPackage = Join-Path $artifactsRoot "$packageName-v$Version-win-x64.zip"
$linuxX64Package = Join-Path $artifactsRoot "$packageName-v$Version-linux-x64.tar.gz"
$linuxArm64Package = Join-Path $artifactsRoot "$packageName-v$Version-linux-arm64.tar.gz"

$winPackageRoot = "$packageName-v$Version-win-x64"
$linuxX64PackageRoot = "$packageName-v$Version-linux-x64"
$linuxArm64PackageRoot = "$packageName-v$Version-linux-arm64"

Compress-Archive -Path (Join-Path $winStageDir $winPackageRoot) -DestinationPath $winPackage -Force
Invoke-NativeCommand -FilePath "tar" -Arguments @("-czf", $linuxX64Package, "-C", $linuxX64StageDir, $linuxX64PackageRoot)
Invoke-NativeCommand -FilePath "tar" -Arguments @("-czf", $linuxArm64Package, "-C", $linuxArm64StageDir, $linuxArm64PackageRoot)
Set-TarGzEntryMode -ArchivePath $linuxX64Package -EntryName "$linuxX64PackageRoot/RapidRedPanda.Wrapper.Cli" -Mode "755"
Set-TarGzEntryMode -ArchivePath $linuxArm64Package -EntryName "$linuxArm64PackageRoot/RapidRedPanda.Wrapper.Cli" -Mode "755"
Set-TarGzEntryMode -ArchivePath $linuxX64Package -EntryName "$linuxX64PackageRoot/samples/csharp-console/RapidRedPanda.Wrapper.Console" -Mode "755"
Set-TarGzEntryMode -ArchivePath $linuxArm64Package -EntryName "$linuxArm64PackageRoot/samples/csharp-console/RapidRedPanda.Wrapper.Console" -Mode "755"

Remove-DirectoryIfExists -Path $stagingRoot

Write-Host "===================================="
Write-Host "Release packages created:"
Write-Host (Resolve-Path $winPackage)
Write-Host (Resolve-Path $linuxX64Package)
Write-Host (Resolve-Path $linuxArm64Package)
Write-Host "===================================="
