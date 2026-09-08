#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs both directions of the Apache OpenNLP <-> NOpenNLP model compatibility
    check (issue #46).

.DESCRIPTION
    Each runtime trains the same five models from the same corpora with the same
    hyperparameters, serializes them, and records the inference output it got.
    The other runtime then loads those model files and must produce the same
    output. Both directions are run end to end and the script exits non-zero if
    either fails.

    Requires a JDK, Maven and the .NET SDK. Everything generated goes under the
    gitignored work/ folder; no models are committed.

.PARAMETER TargetFramework
    Which target framework to build and test the .NET side on. Defaults to the
    test project's own default (net10.0). Set COMPAT_TFM to the same effect.

.EXAMPLE
    ./run-compat.ps1
    ./run-compat.ps1 -TargetFramework net8.0
#>
[CmdletBinding()]
param(
    [string] $TargetFramework = $env:COMPAT_TFM
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Guarantee a non-zero exit code on any failure so CI fails the job. A bare throw
# is not always reflected in the process exit code across PowerShell hosts.
trap {
    Write-Host "==> Compatibility check FAILED: $_" -ForegroundColor Red
    exit 1
}

$Here = Split-Path -Parent $PSCommandPath
$RepositoryRoot = Resolve-Path (Join-Path $Here '..' '..' '..')
$Work = Join-Path $Here 'work'
$JavaModels = Join-Path $Work 'java'
$DotNetModels = Join-Path $Work 'dotnet'
$TestProject = Join-Path $RepositoryRoot 'src' 'NOpenNLP.Tools.Tests' 'NOpenNLP.Tools.Tests.csproj'

# Only pass --framework when the caller asked for a specific one; otherwise the
# project's own default applies.
$FrameworkArgs = if ($TargetFramework) { @('--framework', $TargetFramework) } else { @() }

function Invoke-DotNetTest {
    param(
        [Parameter(Mandatory)] [string] $Filter,
        [Parameter(Mandatory)] [string] $Description
    )

    # dotnet test exits 0 when a --filter matches nothing, and also when the only
    # matching test reports Inconclusive. Either would let a broken pipeline pass
    # silently, so the output is captured and "Passed: 1" is required.
    $output = & dotnet test $TestProject @FrameworkArgs --configuration Release --no-build `
        --logger 'console;verbosity=normal' --filter $Filter 2>&1
    $exitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Host $_ }

    if ($exitCode -ne 0) {
        throw "$Description failed"
    }

    if (-not ($output -match 'Passed:\s*1\b')) {
        throw "$Description did not run (filtered out, or skipped as inconclusive)"
    }
}

function Invoke-Maven {
    param([Parameter(Mandatory)] [string[]] $Arguments)

    & mvn -f (Join-Path $Here 'pom.xml') @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Maven failed: $($Arguments -join ' ')"
    }
}

Write-Host "==> Building the .NET test project$(if ($TargetFramework) { " ($TargetFramework)" })"
& dotnet build $TestProject @FrameworkArgs --configuration Release
if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed' }

Write-Host ''
Write-Host '==> Direction 1: NOpenNLP trains, Apache OpenNLP reads'
Write-Host "    NOpenNLP writing models into $DotNetModels"
$env:NOPENNLP_COMPAT_WRITE_DIR = $DotNetModels
try {
    Invoke-DotNetTest -Filter 'FullyQualifiedName~JavaCompatibilityTest.TestWriteModelsForJava' `
        -Description 'TestWriteModelsForJava'
} finally {
    Remove-Item Env:NOPENNLP_COMPAT_WRITE_DIR -ErrorAction SilentlyContinue
}

Write-Host "    Apache OpenNLP reading them"
Invoke-Maven @('-q', 'test', "-Dnopennlp.model.dir=$DotNetModels")

Write-Host ''
Write-Host '==> Direction 2: Apache OpenNLP trains, NOpenNLP reads'
Write-Host "    Apache OpenNLP writing models into $JavaModels"
Invoke-Maven @('-q', 'compile', 'exec:java', "-Dexec.args=$JavaModels")

Write-Host "    NOpenNLP reading them"
$env:NOPENNLP_COMPAT_READ_DIR = $JavaModels
try {
    Invoke-DotNetTest -Filter 'FullyQualifiedName~JavaCompatibilityTest.TestReadJavaTrainedModels' `
        -Description 'TestReadJavaTrainedModels'
} finally {
    Remove-Item Env:NOPENNLP_COMPAT_READ_DIR -ErrorAction SilentlyContinue
}

Write-Host ''
Write-Host '==> Both directions passed.' -ForegroundColor Green
exit 0
