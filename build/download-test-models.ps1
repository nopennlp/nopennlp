#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Downloads the pre-trained Apache OpenNLP 1.5 models used by the integration tests.

.DESCRIPTION
    Fetches the models into testdata/models-sf, verifying each against a known
    SHA-256.

    The download is idempotent: a file that already exists and matches its
    checksum is left alone, so this is cheap to re-run and safe to point at a
    restored CI cache. Nothing here is committed; testdata/ is gitignored.

    Requires PowerShell 7 or later, and runs on Windows, macOS and Linux.

.PARAMETER TargetDirectory
    Where to place the models. Defaults to testdata/models-sf beside the
    repository root.

.EXAMPLE
    build/download-test-models.ps1
#>
[CmdletBinding()]
param(
    [string] $TargetDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$BaseUrl = 'https://opennlp.sourceforge.net/models-1.5'

if (-not $TargetDirectory) {
    $repositoryRoot = Split-Path -Parent $PSScriptRoot
    $TargetDirectory = Join-Path $repositoryRoot 'testdata' | Join-Path -ChildPath 'models-sf'
}

# Model name and expected SHA-256. The parser model is deliberately absent: it
# is 34 MB, and the parser is not ported, so nothing would exercise it.
$Models = [ordered] @{
    'en-sent.bin'             = 'bd6adffc85d66ccffd09ad1545ab798248193672c4da5c6669150e6a3b35e5b1'
    'en-token.bin'            = '2d0dd64ffb3d084382d7bdb65e7bd004c5001ba5503c36413d97c3e46321437c'
    'en-chunker.bin'          = '7861a0c2f134d9c12a022a1ba501e88bc7039f6db72b4140e1bafd1fb5ef76cc'
    'en-pos-maxent.bin'       = '645a094f45a866687a617385233fd23ae8b0f5fa8b1b76996781a50c17bdcf3d'
    'en-pos-perceptron.bin'   = '0b49b7d9bdb9f888aed85e9f41fbcfd6cab607805ba9cd2370e1e5af4e540db8'
    'en-ner-person.bin'       = '687a9263d96b37fced707c9f2ac0560f9edaf54658856395555901924f64dbe4'
    'en-ner-location.bin'     = '8fe39e48633f4a86c4132d9c54b396a2d8e0460c1d71e3562dacf976984f447b'
    'en-ner-organization.bin' = '0136c12afe1ac357142260c39bb879b7c9d121e41024114db5a6455b4fd5ba00'
    'en-ner-date.bin'         = '1207030923852e1c244919d8f15d9e78c217323728fcf909029abd1703967855'
    'en-ner-money.bin'        = 'b80d577d7d319038457e19f814438965aee9ef5cd1f4f175418d4aece8e504b8'
    'en-ner-percentage.bin'   = 'dbc57162ba9784ae7a851393584aa7193aa2eee6ce2ec962fa937c9fa5e08137'
    'en-ner-time.bin'         = '8a815e6e6d353ee4c478f85dc19b201361e955a9820487f2cf3a2f43c9c78274'
}

function Test-FileHashMatches {
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [string] $ExpectedHash
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $false
    }

    $actual = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
    return $actual -eq $ExpectedHash
}

New-Item -ItemType Directory -Path $TargetDirectory -Force | Out-Null

$downloaded = 0
$cached = 0

foreach ($name in $Models.Keys) {
    $expected = $Models[$name]
    $path = Join-Path $TargetDirectory $name

    if (Test-FileHashMatches -Path $path -ExpectedHash $expected) {
        $cached++
        continue
    }

    Write-Host "Downloading $name"
    $temporaryPath = "$path.tmp"

    try {
        # Invoke-WebRequest throws on a non-success status, so an HTML error page
        # is never mistaken for a model. The progress bar is suppressed because it
        # is slow in PowerShell 7 and meaningless in CI logs.
        $previousProgressPreference = $ProgressPreference
        $ProgressPreference = 'SilentlyContinue'
        try {
            Invoke-WebRequest -Uri "$BaseUrl/$name" -OutFile $temporaryPath `
                -MaximumRetryCount 3 -RetryIntervalSec 2 -TimeoutSec 300
        }
        finally {
            $ProgressPreference = $previousProgressPreference
        }

        $actual = (Get-FileHash -LiteralPath $temporaryPath -Algorithm SHA256).Hash
        if ($actual -ne $expected) {
            throw "Checksum mismatch for ${name}:`n  expected $expected`n  actual   $actual"
        }

        # Move into place only once verified, so an interrupted run never leaves a
        # partial file that a later run would treat as complete.
        Move-Item -LiteralPath $temporaryPath -Destination $path -Force
        $downloaded++
    }
    finally {
        if (Test-Path -LiteralPath $temporaryPath) {
            Remove-Item -LiteralPath $temporaryPath -Force
        }
    }
}

Write-Host "Models ready in $TargetDirectory ($downloaded downloaded, $cached already cached)."
