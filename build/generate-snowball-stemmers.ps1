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

    Provenance was established empirically, by running OpenNLP 1.9.4's own
    stemmers on a JVM and our generated C# over the same 1,113,209 words from the
    snowball-data vocabularies. 17 of the 21 languages are byte-identical.

    Four are not - finnish, hungarian, french and indonesian - because OpenNLP
    ships generated code older than any Snowball revision that can be pinned
    here. See "Deviations" below; this is a deliberate, documented divergence.

    Generation happens here rather than in MSBuild on purpose. The compiler is C
    that needs `make`, there are no prebuilt binaries or GitHub releases for it,
    and the output is deterministic and changes essentially never. Putting a C
    toolchain in the critical path of every build and CI run to reproduce
    byte-identical files is a bad trade, so the generated .cs is committed and
    reviewed like any other source.

    The Snowball compiler is C built with make, so by default this runs itself
    inside a Linux container and only PowerShell 7 and Docker are needed. That
    makes the generation identical on Windows, macOS and Linux, rather than
    depending on whichever C toolchain the developer happens to have - on
    Windows it would otherwise need WSL or MSYS2.

    The container is stock `mcr.microsoft.com/dotnet/sdk:10.0` with git and
    build-essential added at run time. Nothing is built into an image and no
    Dockerfile is needed; the repository is bind-mounted and the generated files
    land directly in the working tree.

.PARAMETER WorkingDirectory
    Where to clone and build the Snowball compiler. Defaults to a temporary
    directory that is reused across runs so repeat invocations are cheap. Under
    Docker this lives inside the container and the clone is not reused.

.PARAMETER OutputDirectory
    Where to write the generated stemmers. Defaults to
    src/NOpenNLP.Tools/Stemmer/Snowball/Generated beside the repository root.

.PARAMETER NoDocker
    Run directly on this machine instead of in a container. Requires git and a C
    compiler with make on PATH. Use this if you already have the toolchain and
    want to skip the container, or when running inside one.

.PARAMETER Image
    The container image to generate in. Defaults to the .NET 10 SDK image, which
    the repository already uses elsewhere.

.EXAMPLE
    build/generate-snowball-stemmers.ps1

.EXAMPLE
    build/generate-snowball-stemmers.ps1 -NoDocker
#>
[CmdletBinding()]
param(
    [string] $WorkingDirectory,
    [string] $OutputDirectory,
    [switch] $NoDocker,
    [string] $Image = 'mcr.microsoft.com/dotnet/sdk:10.0'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepositoryUrl = 'https://github.com/snowballstem/snowball'
$Namespace = 'NOpenNLP.Tools.Stemmer.Snowball'
$ParentClass = 'AbstractSnowballStemmer'

# Deviations from Apache OpenNLP 1.9.4
# ------------------------------------
#
# 17 of 21 languages match OpenNLP 1.9.4 byte-for-byte over 1,113,209 words:
# arabic, catalan, danish, dutch, english, german, greek, irish, italian,
# norwegian, porter, romanian, russian, spanish, swedish, turkish, portuguese.
#
# Four differ. In every case OpenNLP ships generated code predating the fix, and
# our output is the corrected behavior:
#
#   finnish     953 of 84,399 (1.13%). OpenNLP fails to conflate the case forms
#               of a single noun: aarteeseen/aarteiden/aarteisiin/aarteet - all
#               inflections of "aarre" (treasure) - stem to aartees/aarteid/
#               aarteis/aart, four stems for one word. Ours maps all four to
#               aart.
#   hungarian   639 of 29,881 (2.14%). OpenNLP leaves the ablative and delative
#               case suffixes (-rol/-rol, -tol/-tol) entirely unstripped, so the
#               word stems to itself: adatvedelemrol -> adatvedelemrol. Ours
#               strips them. Every one of the 639 is of this shape.
#   french       79 of 21,653 (0.36%). Snowball commit 86ceab9, "Recognize
#               suffixes that begin with diaereses". Every differing word
#               contains e-diaeresis or i-diaeresis; aigu and aigue are one
#               adjective, which ours conflates and OpenNLP does not.
#   indonesian 3,902 of 64,586 (6.04%). This one runs the other way: ours stems
#               LESS, on 3,168 words, and that is the fix. demokrasi/organisasi
#               are loanwords whose final -i belongs to the root; OpenNLP strips
#               it and corrupts the stem. 2,344 of the 3,168 match the
#               -asi/-ksi/-si loanword shape. Genuine -i suffixes are still
#               stripped by both (mengambili -> ambil).
#
# All four move toward more conflation, which is what a stemmer is for: Finnish
# goes from 44,601 distinct stem groups to 43,844 over the same vocabulary,
# Hungarian from 16,041 to 15,503.
#
# These four cannot be fixed by choosing a different pin. OpenNLP's
# finnishStemmer.java and hungarianStemmer.java were committed 2013-11-20,
# predating the Snowball git repository itself (the project was on SVN at
# tartarus.org then), and the .sbl sources that generated them exist at no
# revision. Generating Finnish from all 10 revisions of finnish.sbl in the repo
# and hashing the Among data reproduces OpenNLP's at none of them - the oldest
# available already differs, e.g. Among("tta",4,9) upstream vs Among("tta",4,2).
# Matching OpenNLP for these four would mean hand-porting ~4,400 lines of
# generated labeled-break control flow, which is precisely what generating the
# stemmers exists to avoid.
#
# All 18 ported upstream tests pass, because none of their assertions touch an
# affected word. Nothing inside opennlp-tools consumes IStemmer - it is a leaf,
# public-API-only package - so no other ported behavior depends on this.
#
# Key is the .sbl basename; value is the git revision to generate it from.
$Algorithms = [ordered] @{
    'arabic'     = 'v2.0.0'
    'catalan'    = 'v2.0.0'
    'danish'     = 'v2.0.0'
    'dutch'      = 'v2.0.0'
    'english'    = 'v2.0.0'
    'finnish'    = 'v2.0.0'
    'french'     = 'v2.0.0'
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

# Re-invoke this same script inside a container. There is deliberately only one
# implementation: the algorithm list, the pins and the generation logic below are
# the single source of truth, and the container path just runs them somewhere
# with a C toolchain.
if (-not $NoDocker) {
    if (-not (Get-Command 'docker' -ErrorAction SilentlyContinue)) {
        throw 'docker is required but was not found on PATH. Install Docker, or pass -NoDocker to generate with a local C toolchain.'
    }

    # $OutputDirectory has already been defaulted above, so ask what the caller
    # actually passed rather than whether it has a value.
    if ($PSBoundParameters.ContainsKey('OutputDirectory')) {
        throw '-OutputDirectory cannot be combined with the default Docker run, because the container writes through a fixed mount. Pass -NoDocker as well to choose your own output directory.'
    }

    Write-Host "Generating in $Image ..."

    # The apt install is part of the run rather than a Dockerfile so there is no
    # image to build, tag or keep current. It costs a few seconds against a
    # generation that is run rarely.
    $script = @(
        'set -e'
        'export DEBIAN_FRONTEND=noninteractive'
        'apt-get update -qq'
        'apt-get install -y -qq git build-essential > /dev/null'
        'pwsh -NoProfile -File /repo/build/generate-snowball-stemmers.ps1 -NoDocker'
    ) -join "`n"

    # Generated files must be owned by the invoking user, not root. Docker
    # Desktop on Windows and macOS maps ownership for us, but a native Linux
    # daemon does not, so pass the real uid/gid there.
    $dockerArgs = [System.Collections.Generic.List[string]]::new()
    $dockerArgs.AddRange([string[]] @('run', '--rm', '-v', "${repositoryRoot}:/repo", '-w', '/repo'))

    if ($IsLinux) {
        $dockerArgs.AddRange([string[]] @('-u', "$(id -u):$(id -g)"))
    }

    $dockerArgs.AddRange([string[]] @($Image, 'bash', '-lc', $script))

    # Not Invoke-Native: that captures output, and this is the long-running step
    # where the apt install and the per-language progress should be visible as
    # they happen.
    & docker @dockerArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Generation in $Image failed with exit code $LASTEXITCODE."
    }

    return
}

foreach ($tool in @('git', 'make')) {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) {
        throw "$tool is required but was not found on PATH. Omit -NoDocker to generate in a container instead."
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
