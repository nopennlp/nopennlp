#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Regenerates the Snowball stemmers in src/NOpenNLP.Tools/Stemmer/Snowball/Generated.

.DESCRIPTION
    Apache OpenNLP does not hand-write its Snowball stemmers: it runs the
    Snowball compiler over the .sbl algorithm sources and commits the generated
    Java. This script does the same thing with the compiler's C# backend, so the
    port generates what upstream generates instead of translating 24,000 lines
    of generated Java by hand.

    Two version numbers are in play and they are unrelated:

      * Apache OpenNLP 1.9.4 - what the port targets.
      * Snowball 2.0.0       - the compiler that produced OpenNLP 1.9.4's Java.

    We pin Snowball 2.0.0 *because* we target OpenNLP 1.9.4. Snowball 3.x
    changed several algorithms and would silently produce different stems: 45.8%
    of Dutch words differ, because 3.0.0 switched the default Dutch algorithm to
    Kraaij-Pohlmann. OpenNLP's own test asserts stem("sterlabcertificaat") ==
    "sterlabcertificat", which holds under 2.0.0 and fails under 3.x.

    Provenance was established empirically, by extracting the Among string
    literals from OpenNLP 1.9.4's shipped Java and comparing them against output
    generated from each Snowball revision. See $Algorithms below for the result.

    Generation happens here rather than in MSBuild on purpose. The compiler is C
    that needs `make`, there are no prebuilt binaries or GitHub releases for it,
    and the output is deterministic and changes essentially never. Putting a C
    toolchain in the critical path of every build and CI run to reproduce
    byte-identical files is a bad trade, so the generated .cs is committed and
    reviewed like any other source.

    Requires PowerShell 7 or later, git, and a C compiler with make. Runs on
    macOS and Linux; on Windows it needs a Unix-like toolchain (WSL or MSYS2).

.PARAMETER WorkingDirectory
    Where to clone and build the Snowball compiler. Defaults to a temporary
    directory that is reused across runs so repeat invocations are cheap.

.PARAMETER OutputDirectory
    Where to write the generated stemmers. Defaults to
    src/NOpenNLP.Tools/Stemmer/Snowball/Generated beside the repository root.

.EXAMPLE
    build/generate-snowball-stemmers.ps1
#>
[CmdletBinding()]
param(
    [string] $WorkingDirectory,
    [string] $OutputDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepositoryUrl = 'https://github.com/snowballstem/snowball'
$Namespace = 'NOpenNLP.Tools.Stemmer.Snowball'
$ParentClass = 'AbstractSnowballStemmer'

# The 21 algorithms OpenNLP 1.9.4 ships, and the Snowball revision each one was
# generated from. Snowball tags are not enough on their own: OpenNLP's stemmers
# were committed between 2013 and 2020 and are of mixed vintage.
#
#   19 languages  matched v2.0.0 byte-for-byte (by Among-literal hash).
#   french        matched commit 697c294 (2018-03-17), just before v2.0.0. The
#                 v2.0.0 French picked up 86ceab9, "Recognize suffixes that
#                 begin with diaereses", which OpenNLP predates.
#   arabic        did NOT match any Snowball revision by Among-literal hash
#                 (OpenNLP has 247, v2.0.0 has 225, v3.1.1 has 220), which
#                 initially looked like a different algorithm. It is not. The
#                 hash compares generated source, and source shape is not
#                 behavior: running OpenNLP 1.9.4's own arabicStemmer and
#                 v2.0.0's side by side over 200,000 Arabic words produced
#                 identical output on every single one. v2.0.0 and v3.1.1 also
#                 agree with each other over the same 200,000. Arabic is
#                 generated from v2.0.0 like everything else.
#
# French is nonetheless generated from v2.0.0, not from 697c294, because the C#
# backend did not exist before v2.0.0 - 697c294 cannot emit C# at all. The cost
# of that is bounded and was measured rather than assumed: over the full 21,653
# word French vocabulary the two revisions differ on 79 words (0.36%), every one
# of them containing a diaeresis, which is exactly what 86ceab9 set out to fix
# (aiguë -> aiguë under 697c294, aigu under v2.0.0). OpenNLP 1.9.4's own French
# assertions - accomplissaient, examinateurs, prevoyant - are identical under
# both, so the ported test is unaffected.
#
# Key is the .sbl basename; value is the git revision to generate it from.
$Algorithms = [ordered] @{
    'arabic'     = 'v2.0.0'
    'catalan'    = 'v2.0.0'
    'danish'     = 'v2.0.0'
    'dutch'      = 'v2.0.0'
    'english'    = 'v2.0.0'
    'finnish'    = 'v2.0.0'
    'french'     = 'v2.0.0'   # see the note above: 697c294 predates the C# backend
    'german'     = 'v2.0.0'
    'greek'      = 'v2.0.0'
    'hungarian'  = 'v2.0.0'
    'indonesian' = 'v2.0.0'
    'irish'      = 'v2.0.0'
    'italian'    = 'v2.0.0'
    'norwegian'  = 'v2.0.0'
    'porter'     = 'v2.0.0'
    'portuguese' = 'v2.0.0'
    'romanian'   = 'v2.0.0'
    'russian'    = 'v2.0.0'
    'spanish'    = 'v2.0.0'
    'swedish'    = 'v2.0.0'
    'turkish'    = 'v2.0.0'
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot

if (-not $WorkingDirectory) {
    $WorkingDirectory = Join-Path ([System.IO.Path]::GetTempPath()) 'nopennlp-snowball'
}

if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $repositoryRoot 'src' |
        Join-Path -ChildPath 'NOpenNLP.Tools' |
        Join-Path -ChildPath 'Stemmer' |
        Join-Path -ChildPath 'Snowball' |
        Join-Path -ChildPath 'Generated'
}

function Invoke-Native {
    param(
        [Parameter(Mandatory)] [string] $Command,
        [Parameter(Mandatory)] [string[]] $Arguments,
        [string] $WorkingDirectory
    )

    $output = & $Command @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "$Command $($Arguments -join ' ') failed with exit code ${LASTEXITCODE}:`n$output"
    }

    return $output
}

foreach ($tool in @('git', 'make')) {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
        throw "$tool is required but was not found on PATH."
    }
}

# Converts a .sbl basename to the C# class name: dutch -> DutchStemmer. Snowball
# names Java classes dutchStemmer; C# convention is PascalCase, and -n sets it.
function Get-StemmerClassName {
    param([Parameter(Mandatory)] [string] $Algorithm)

    $culture = [System.Globalization.CultureInfo]::InvariantCulture
    return $culture.TextInfo.ToTitleCase($Algorithm) + 'Stemmer'
}

$checkout = Join-Path $WorkingDirectory 'snowball'

if (-not (Test-Path -LiteralPath $checkout)) {
    Write-Host "Cloning $RepositoryUrl ..."
    New-Item -ItemType Directory -Path $WorkingDirectory -Force | Out-Null
    Invoke-Native -Command 'git' -Arguments @('clone', '--quiet', $RepositoryUrl, $checkout) | Out-Null
}
else {
    Write-Host "Reusing existing checkout at $checkout"
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

# Generating is cheap but rebuilding the compiler is not, so group the languages
# by revision and switch the checkout once per distinct revision rather than
# once per language.
$byRevision = [ordered] @{}
foreach ($algorithm in $Algorithms.Keys) {
    $revision = $Algorithms[$algorithm]
    if (-not $byRevision.Contains($revision)) {
        $byRevision[$revision] = [System.Collections.Generic.List[string]]::new()
    }
    $byRevision[$revision].Add($algorithm)
}

$generated = 0

foreach ($revision in $byRevision.Keys) {
    Write-Host ''
    Write-Host "Snowball $revision"

    Invoke-Native -Command 'git' -Arguments @('-C', $checkout, 'checkout', '--quiet', '--detach', $revision) | Out-Null

    # A stale object tree from the previous revision would otherwise be linked
    # into the new binary.
    Invoke-Native -Command 'make' -Arguments @('-C', $checkout, 'clean') | Out-Null
    Invoke-Native -Command 'make' -Arguments @('-C', $checkout, 'snowball') | Out-Null

    $compiler = Join-Path $checkout 'snowball'
    if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
        throw "The Snowball compiler was not produced at $compiler."
    }

    foreach ($algorithm in $byRevision[$revision]) {
        $source = Join-Path $checkout 'algorithms' "$algorithm.sbl"
        if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
            throw "Algorithm source $source does not exist at revision $revision."
        }

        $className = Get-StemmerClassName -Algorithm $algorithm
        $outputBase = Join-Path $OutputDirectory $className

        Invoke-Native -Command $compiler -Arguments @(
            $source
            '-cs'
            '-o', $outputBase
            '-n', $className
            '-P', $Namespace
            '-p', $ParentClass
        ) | Out-Null

        $outputFile = "$outputBase.cs"
        if (-not (Test-Path -LiteralPath $outputFile -PathType Leaf)) {
            throw "Generation of $algorithm did not produce $outputFile."
        }

        # Record where the file came from. Without this the next person to run
        # the script has no way to know a language was pinned to something other
        # than the default, and regenerating from master would quietly change
        # stemming output.
        $describe = (Invoke-Native -Command 'git' -Arguments @(
            '-C', $checkout, 'rev-parse', '--short', 'HEAD')).Trim()

        $banner = @(
            "// Generated by build/generate-snowball-stemmers.ps1 - do not edit."
            "// Source:   algorithms/$algorithm.sbl"
            "// Compiler: Snowball $revision ($describe), C# backend"
            "//"
            "// Regenerate with: build/generate-snowball-stemmers.ps1"
            ""
        ) -join [Environment]::NewLine

        $content = Get-Content -LiteralPath $outputFile -Raw
        Set-Content -LiteralPath $outputFile -Value ($banner + $content) -NoNewline

        Write-Host "  $algorithm -> $className.cs"
        $generated++
    }
}

Write-Host ''
Write-Host "Generated $generated stemmers into $OutputDirectory"
