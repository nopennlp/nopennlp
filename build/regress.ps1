#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Compares the ported `nopennlp` CLI against the real Apache OpenNLP CLI.

.DESCRIPTION
    Runs the same invocation through both and diffs stdout and the exit code.

    This is a developer tool, not part of CI: it needs a JVM, the OpenNLP jar, a
    clone of the upstream source for its test corpora, and the downloaded SF
    models. Its value is at rebase time -- after pulling a new upstream release,
    run it to see what changed in the CLI's observable behaviour.

    Requires PowerShell 7 or later, and runs on Windows, macOS and Linux.

    Differences that are expected, and are normalized away rather than reported:
      - the command and product name ("opennlp"/"OpenNLP" vs "nopennlp"/"NOpenNLP")
      - timings ("Execution time: 0.193 seconds", "done (0.020s)", "Runtime: 1.2s",
        "Done indexing in 0.07 s.")
      - absolute paths, which differ per run because each side is given its own
        copy of the corpora
      - throughput lines, which depend on machine speed
      - the order of a tool's options and of its format lists: the [.fmt|.fmt]
        alternation on a Usage line, the help|fmt|fmt list on a converter's
        Usage line, and the (fmt,fmt) list in a converter's summary on the
        no-argument usage screen. Java derives all of these from reflection --
        Class.getMethods() and HashMap iteration -- neither of which the JDK
        specifies; the port uses declaration and registration order. See the
        ConvertTo-Canonical* functions below.
      - the last digits of a floating-point number. Upstream's trainers call
        StrictMath.log and StrictMath.exp, which are fdlibm and specified bit
        for bit; the port calls Math.Log and Math.Exp, which go to the
        platform's C library and disagree with fdlibm in the last ulp on
        roughly 7% of log inputs and 10% of exp inputs (measured on 300,000
        random inputs each). A training log's loglikelihoods therefore drift
        in their last one or two digits. Two numbers are treated as equal
        when they agree to $NumericTolerance relative, which is far tighter
        than any regression in the model would show and far looser than the
        ulp-level noise. Integers, and everything that is not a number, are
        still compared verbatim.

    A clean run passes every case. A failure is a real difference.

.PARAMETER Jar
    The Apache OpenNLP 1.9.4 tools jar. Defaults to the Maven local repository.

.PARAMETER NOpenNLP
    The ported CLI to compare against. Defaults to the tool installed into
    _artifacts/regress-tool by the usage example below.

.PARAMETER Resources
    A clone of the upstream OpenNLP source, at opennlp-tools/src/test/resources/opennlp/tools,
    for the test corpora.

.PARAMETER Models
    The pre-trained SF models, as fetched by build/download-test-models.ps1.

.PARAMETER WorkDirectory
    Scratch space for the generated inputs and captured output. Defaults to
    _artifacts/regress, which is gitignored. Cleared at the start of each run.

.PARAMETER Verbose
    Print the first 20 lines of each failing diff.

.EXAMPLE
    build/download-test-models.ps1                       # once, for the SF models
    dotnet pack src/NOpenNLP.Cli/NOpenNLP.Cli.csproj -c Release
    dotnet tool install --tool-path _artifacts/regress-tool `
        --add-source src/NOpenNLP.Cli/bin/Release NOpenNLP.Cli --version <version>
    build/regress.ps1 -Verbose
#>
[CmdletBinding()]
param(
    [string] $Jar,
    [string] $NOpenNLP,
    [string] $Resources,
    [string] $Models,
    [string] $WorkDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$homeDirectory = if ($IsWindows) { $env:USERPROFILE } else { $env:HOME }

if (-not $Jar) {
    $Jar = Join-Path $homeDirectory '.m2' 'repository' 'org' 'apache' 'opennlp' `
        'opennlp-tools' '1.9.4' 'opennlp-tools-1.9.4.jar'
}

if (-not $NOpenNLP) {
    # dotnet tool install writes nopennlp.exe on Windows and a nopennlp shell
    # shim elsewhere.
    $toolName = if ($IsWindows) { 'nopennlp.exe' } else { 'nopennlp' }
    $NOpenNLP = Join-Path $repositoryRoot '_artifacts' 'regress-tool' $toolName
}

if (-not $Resources) {
    $Resources = Join-Path $homeDirectory 'git' 'opennlp' 'opennlp-tools' 'src' 'test' `
        'resources' 'opennlp' 'tools'
}

if (-not $Models) {
    $Models = Join-Path $repositoryRoot 'testdata' 'models-sf'
}

if (-not $WorkDirectory) {
    $WorkDirectory = Join-Path $repositoryRoot '_artifacts' 'regress'
}

foreach ($required in $Jar, $NOpenNLP, $Resources, $Models) {
    if (-not (Test-Path -LiteralPath $required)) {
        Write-Error "not found: $required`nsee the usage notes at the top of this script"
        exit 2
    }
}

if (Test-Path -LiteralPath $WorkDirectory) {
    Remove-Item -LiteralPath $WorkDirectory -Recurse -Force
}
New-Item -ItemType Directory -Path $WorkDirectory -Force | Out-Null

$script:Pass = 0
$script:Fail = 0

# Relative tolerance for comparing floating-point numbers; see the notes above.
$NumericTolerance = 1e-9
$script:FailedCases = [System.Collections.Generic.List[string]]::new()

# Java orders a tool's options by Class.getMethods() and its formats by HashMap
# iteration. The JDK specifies neither, so both are implementation details rather
# than contract; the port uses declaration order and registration order instead.
# The two functions below sort both sides so a diff reports genuine differences
# in options, value names, descriptions and formats.

function ConvertTo-CanonicalUsage {
    <#
    Sorts the format lists and the option list on a Usage line. A converter's
    Usage line is "Usage: opennlp FooConverter help|fmt|fmt [help|options...]"
    and has no options; every other tool's is "Usage: opennlp Foo[.fmt|.fmt]
    -opt value [-opt value]".
    #>
    param([string] $Line)

    $converter = [regex]::Match($Line, '^(Usage: \S+ \S+ )(\S+\|\S+)( \[help\|options\.\.\.\])$')
    if ($converter.Success) {
        $formats = $converter.Groups[2].Value -split '\|' | Sort-Object -CaseSensitive
        return $converter.Groups[1].Value + ($formats -join '|') + $converter.Groups[3].Value
    }

    $match = [regex]::Match($Line, '^(Usage: \S+ \S+?)(\[\.[^\]]*\])?(\s.*)$')
    if (-not $match.Success -or $match.Groups[3].Value -notmatch '-') {
        return $Line
    }

    $head = $match.Groups[1].Value
    $formats = $match.Groups[2].Value
    $rest = $match.Groups[3].Value

    if ($formats) {
        $parts = $formats.Substring(1, $formats.Length - 2) -split '\|'
        $formats = '[' + (($parts | Sort-Object -CaseSensitive) -join '|') + ']'
    }

    # An option is "-name value", optionally wrapped in brackets when optional.
    $options = [regex]::Matches($rest, '\[-\w+(?: [^\[\]]+?)?\]|-\w+(?: \S+)?') |
        ForEach-Object { $_.Value.Trim() } |
        Where-Object { $_ }

    return $head + $formats + ' ' + (($options | Sort-Object -CaseSensitive) -join ' ')
}

function ConvertTo-CanonicalSummary {
    <#
    Sorts the format list in a converter's one-line summary on the no-argument
    usage screen: "converts foreign data formats (fmt,fmt) to native OpenNLP format".
    #>
    param([string] $Line)

    return [regex]::Replace($Line, '(converts foreign data formats \()([^)]*)(\))', {
        param($m)
        $formats = $m.Groups[2].Value -split ',' | Sort-Object -CaseSensitive
        $m.Groups[1].Value + ($formats -join ',') + $m.Groups[3].Value
    })
}

function ConvertTo-CanonicalDescriptionBlock {
    <# Sorts the 'Arguments description:' entries, each a name line plus optional detail. #>
    param([string[]] $Block)

    $entries = [System.Collections.Generic.List[string[]]]::new()
    $current = [System.Collections.Generic.List[string]]::new()

    foreach ($line in $Block) {
        if (-not $line.StartsWith("`t`t")) {
            if ($current.Count -gt 0) {
                $entries.Add($current.ToArray())
            }
            $current = [System.Collections.Generic.List[string]]::new()
        }
        $current.Add($line)
    }

    if ($current.Count -gt 0) {
        $entries.Add($current.ToArray())
    }

    # Sort by the whole entry, as Python's list comparison does: the name line
    # first, then the detail lines as a tie-break.
    $sorted = $entries | Sort-Object -CaseSensitive -Property { $_ -join "`n" }

    return @($sorted | ForEach-Object { $_ })
}

function ConvertTo-Canonical {
    param([string[]] $Lines)

    $out = [System.Collections.Generic.List[string]]::new()
    $block = [System.Collections.Generic.List[string]]::new()

    foreach ($line in $Lines) {
        if ($line.StartsWith("`t")) {
            $block.Add($line)
            continue
        }

        if ($block.Count -gt 0) {
            $out.AddRange([string[]] (ConvertTo-CanonicalDescriptionBlock -Block $block.ToArray()))
            $block = [System.Collections.Generic.List[string]]::new()
        }

        $out.Add((ConvertTo-CanonicalSummary -Line (ConvertTo-CanonicalUsage -Line $line)))
    }

    if ($block.Count -gt 0) {
        $out.AddRange([string[]] (ConvertTo-CanonicalDescriptionBlock -Block $block.ToArray()))
    }

    return $out.ToArray()
}

function ConvertTo-Normalized {
    param([string[]] $Lines)

    return @($Lines | ForEach-Object {
        $line = $_
        $line = $line -creplace 'NOpenNLP', 'OpenNLP'
        $line = $line -creplace 'nopennlp', 'opennlp'
        $line = $line -replace 'Execution time: [0-9.]+ seconds', 'Execution time: T'
        $line = $line -replace 'done \([0-9.]+s\)', 'done (Ts)'
        $line = $line -replace 'Runtime: [0-9.]+s', 'Runtime: Ts'
        $line = $line -replace 'Done indexing in [0-9.]+ s\.', 'Done indexing in T s.'
        $line = $line -replace 'Average: [0-9.]+ ', 'Average: N '
        $line = $line -replace 'current: [0-9.]+ .*$', 'current: N'
        # Absolute paths differ per run because each side is given its own copy of
        # the corpora. Windows paths use backslashes and a drive letter.
        $line = $line -replace '[A-Za-z]:\\[^ ]*', 'PATH'
        $line = $line -replace '/[^ ]*/(java|cs)-[a-z0-9]+', 'PATH'
        $line = $line -replace '/var/folders/[^ ]*', 'PATH'
        $line = $line -replace '/tmp/[^ ]*', 'PATH'
        $line = $line -replace '/private/var/folders/[^ ]*', 'PATH'
        $line = $line -replace '[0-9]+\.[0-9]+ (sent|doc|token)s?/s', 'N $1s/s'
        $line
    })
}

function Test-LineEquivalent {
    <#
    True when two lines are identical, or differ only in floating-point numbers
    that agree to $NumericTolerance relative. Integers must match exactly, and so
    must everything around the numbers.
    #>
    param([string] $Java, [string] $Cs)

    if ($Java -ceq $Cs) {
        return $true
    }

    $number = '-?[0-9]+\.[0-9]+(?:[eE][-+]?[0-9]+)?'
    if (($Java -replace $number, '#') -cne ($Cs -replace $number, '#')) {
        return $false
    }

    $javaNumbers = [regex]::Matches($Java, $number)
    $csNumbers = [regex]::Matches($Cs, $number)
    $culture = [System.Globalization.CultureInfo]::InvariantCulture

    for ($i = 0; $i -lt $javaNumbers.Count; $i++) {
        $a = [double]::Parse($javaNumbers[$i].Value, $culture)
        $b = [double]::Parse($csNumbers[$i].Value, $culture)
        $scale = [Math]::Max([Math]::Max([Math]::Abs($a), [Math]::Abs($b)), 1.0)
        if ([Math]::Abs($a - $b) -gt $NumericTolerance * $scale) {
            return $false
        }
    }

    return $true
}

function Compare-Output {
    <#
    Compares two outputs line by line and returns one description per differing
    line, so the caller can count them and print them. Line counts that differ
    are reported as a difference of their own, after the lines both sides have.
    #>
    param([string[]] $Java, [string[]] $Cs)

    $differences = [System.Collections.Generic.List[string]]::new()
    $shared = [Math]::Min($Java.Count, $Cs.Count)

    for ($i = 0; $i -lt $shared; $i++) {
        if (-not (Test-LineEquivalent -Java $Java[$i] -Cs $Cs[$i])) {
            $differences.Add("line $($i + 1):`n      java: $($Java[$i])`n      cs:   $($Cs[$i])")
        }
    }

    if ($Java.Count -ne $Cs.Count) {
        $differences.Add("line count: java=$($Java.Count) cs=$($Cs.Count)")
    }

    return , [string[]] $differences.ToArray()
}

function Get-ComparableOutput {
    param([string] $Path)

    # -Raw then split, rather than Get-Content's line array, so that a trailing
    # newline difference is visible and an empty file reads as no lines at all.
    $text = Get-Content -LiteralPath $Path -Raw -Encoding utf8
    if ($null -eq $text) {
        return , [string[]] @()
    }

    $lines = $text -split "`r?`n"
    # A returned array unrolls in the pipeline, so an empty result would arrive as
    # $null and Compare-Object would refuse to bind it. The comma keeps it an array.
    return , [string[]] @(ConvertTo-Canonical -Lines (ConvertTo-Normalized -Lines $lines))
}

function Invoke-Case {
    <#
    .SYNOPSIS
        Runs one invocation through both CLIs and compares stdout and the exit code.
    #>
    param(
        [Parameter(Mandatory)] [string] $Name,
        # Empty for no stdin; the port and Java both read an empty stream then.
        [AllowEmptyString()] [string] $StdinFile,
        [Parameter(ValueFromRemainingArguments)] [string[]] $Arguments
    )

    $javaOut = Join-Path $WorkDirectory 'j.out'
    $javaErr = Join-Path $WorkDirectory 'j.err'
    $csOut = Join-Path $WorkDirectory 'c.out'
    $csErr = Join-Path $WorkDirectory 'c.err'

    # Start-Process is the only way to redirect stdin from a file without the
    # shell; an empty stdin is emulated with an empty file, since /dev/null and
    # NUL are not the same name on the two platforms.
    if (-not $StdinFile) {
        $StdinFile = Join-Path $WorkDirectory 'empty.in'
        if (-not (Test-Path -LiteralPath $StdinFile)) {
            New-Item -ItemType File -Path $StdinFile -Force | Out-Null
        }
    }

    $javaArguments = @('-cp', $Jar, 'opennlp.tools.cmdline.CLI') + $Arguments

    $javaProcess = Start-Process -FilePath 'java' -ArgumentList $javaArguments `
        -RedirectStandardInput $StdinFile -RedirectStandardOutput $javaOut `
        -RedirectStandardError $javaErr -NoNewWindow -Wait -PassThru
    $javaExit = $javaProcess.ExitCode

    $csProcess = Start-Process -FilePath $NOpenNLP -ArgumentList $Arguments `
        -RedirectStandardInput $StdinFile -RedirectStandardOutput $csOut `
        -RedirectStandardError $csErr -NoNewWindow -Wait -PassThru
    $csExit = $csProcess.ExitCode

    $ok = $true
    $detail = @()

    if ($javaExit -ne $csExit) {
        $ok = $false
        $detail += "exit code: java=$javaExit cs=$csExit"
    }

    $javaLines = Get-ComparableOutput -Path $javaOut
    $csLines = Get-ComparableOutput -Path $csOut

    $differences = Compare-Output -Java $javaLines -Cs $csLines

    if ($differences.Count -gt 0) {
        $ok = $false
        $detail += 'stdout differs'
    }

    if ($ok) {
        $script:Pass++
        Write-Host "  PASS  $Name"
    }
    else {
        $script:Fail++
        $script:FailedCases.Add($Name)
        Write-Host "  FAIL  $Name  ($($detail -join ' '))"

        if ($VerbosePreference -ne 'SilentlyContinue') {
            Write-Host '    --- stdout differences ---'
            $differences | Select-Object -First 20 | ForEach-Object {
                Write-Host "    $_"
            }
        }
    }
}

Write-Host '== usage and help =='
Invoke-Case -Name 'usage (no args)' -StdinFile ''

$tools = @(
    'Doccat', 'DoccatTrainer', 'DoccatEvaluator', 'DoccatCrossValidator', 'DoccatConverter',
    'LanguageDetector', 'LanguageDetectorTrainer', 'LanguageDetectorConverter',
    'LanguageDetectorCrossValidator', 'LanguageDetectorEvaluator',
    'DictionaryBuilder', 'SimpleTokenizer', 'TokenizerME', 'TokenizerTrainer',
    'TokenizerMEEvaluator', 'TokenizerCrossValidator', 'TokenizerConverter',
    'DictionaryDetokenizer', 'SentenceDetector', 'SentenceDetectorTrainer',
    'SentenceDetectorEvaluator', 'SentenceDetectorCrossValidator', 'SentenceDetectorConverter',
    'TokenNameFinder', 'TokenNameFinderTrainer', 'TokenNameFinderEvaluator',
    'TokenNameFinderCrossValidator', 'TokenNameFinderConverter', 'CensusDictionaryCreator',
    'POSTagger', 'POSTaggerTrainer', 'POSTaggerEvaluator', 'POSTaggerCrossValidator',
    'POSTaggerConverter', 'LemmatizerME', 'LemmatizerTrainerME', 'LemmatizerEvaluator',
    'ChunkerME', 'ChunkerTrainerME', 'ChunkerEvaluator', 'ChunkerCrossValidator', 'ChunkerConverter',
    'Parser', 'ParserTrainer', 'ParserEvaluator', 'ParserConverter',
    'BuildModelUpdater', 'CheckModelUpdater', 'TaggerModelReplacer',
    'EntityLinker', 'NGramLanguageModel'
)

foreach ($tool in $tools) {
    Invoke-Case -Name "$tool help" -StdinFile '' -Arguments $tool, 'help'
}

Write-Host ''
Write-Host '== error paths =='
Invoke-Case -Name 'unknown tool' -StdinFile '' -Arguments 'NoSuchTool'
Invoke-Case -Name 'unknown format' -StdinFile '' `
    -Arguments 'TokenizerTrainer.nosuchformat', '-model', 'm.bin'
Invoke-Case -Name 'format on a basic tool' -StdinFile '' -Arguments 'SimpleTokenizer.conllu', 'x'

Write-Host ''
Write-Host '== inference over the official models =='

# The corpora are read as bytes by the tools under test; write LF endings on
# every platform so the two sides see identical input and a Windows run does not
# diff against a macOS one.
function Write-InputFile {
    param(
        [Parameter(Mandatory)] [string] $Path,
        [Parameter(Mandatory)] [string] $Content
    )

    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

$sentences = Join-Path $WorkDirectory 'sents.txt'
Write-InputFile -Path $sentences -Content "Mr. Smith went to Washington. He arrived on Jan. 3rd.`n"
Invoke-Case -Name 'SimpleTokenizer' -StdinFile $sentences -Arguments 'SimpleTokenizer'
Invoke-Case -Name 'TokenizerME' -StdinFile $sentences `
    -Arguments 'TokenizerME', (Join-Path $Models 'en-token.bin')
Invoke-Case -Name 'SentenceDetector' -StdinFile $sentences `
    -Arguments 'SentenceDetector', (Join-Path $Models 'en-sent.bin')

$tokens = Join-Path $WorkDirectory 'tokens.txt'
Write-InputFile -Path $tokens -Content "Mr. Smith went to Washington .`n"
Invoke-Case -Name 'POSTagger maxent' -StdinFile $tokens `
    -Arguments 'POSTagger', (Join-Path $Models 'en-pos-maxent.bin')
Invoke-Case -Name 'POSTagger perceptron' -StdinFile $tokens `
    -Arguments 'POSTagger', (Join-Path $Models 'en-pos-perceptron.bin')
Invoke-Case -Name 'TokenNameFinder person' -StdinFile $tokens `
    -Arguments 'TokenNameFinder', (Join-Path $Models 'en-ner-person.bin')
Invoke-Case -Name 'TokenNameFinder multi' -StdinFile $tokens `
    -Arguments 'TokenNameFinder', (Join-Path $Models 'en-ner-person.bin'),
        (Join-Path $Models 'en-ner-location.bin')

$posTokens = Join-Path $WorkDirectory 'postokens.txt'
Write-InputFile -Path $posTokens -Content "Mr._NNP Smith_NNP went_VBD to_TO Washington_NNP ._.`n"
Invoke-Case -Name 'ChunkerME' -StdinFile $posTokens `
    -Arguments 'ChunkerME', (Join-Path $Models 'en-chunker.bin')

Write-Host ''
Write-Host '== converters =='
$conllu = Join-Path $Resources 'formats' 'conllu' 'de-ud-train-sample.conllu'
$conll02 = Join-Path $Resources 'formats' 'conll2002-nl.sample'
$adSample = Join-Path $Resources 'formats' 'ad.sample'

Invoke-Case -Name 'POSTaggerConverter conllu' -StdinFile '' `
    -Arguments 'POSTaggerConverter', 'conllu', '-data', $conllu, '-encoding', 'UTF-8'
Invoke-Case -Name 'TokenizerConverter conllu' -StdinFile '' `
    -Arguments 'TokenizerConverter', 'conllu', '-data', $conllu, '-encoding', 'UTF-8'
Invoke-Case -Name 'SentenceDetectorConverter conllu' -StdinFile '' `
    -Arguments 'SentenceDetectorConverter', 'conllu', '-data', $conllu, '-encoding', 'UTF-8'
Invoke-Case -Name 'TokenNameFinderConverter conll02' -StdinFile '' `
    -Arguments 'TokenNameFinderConverter', 'conll02', '-data', $conll02, '-lang', 'nld',
        '-types', 'per,loc,org,misc', '-encoding', 'UTF-8'
Invoke-Case -Name 'TokenNameFinderConverter ad' -StdinFile '' `
    -Arguments 'TokenNameFinderConverter', 'ad', '-data', $adSample, '-lang', 'por',
        '-encoding', 'UTF-8'
Invoke-Case -Name 'ChunkerConverter ad' -StdinFile '' `
    -Arguments 'ChunkerConverter', 'ad', '-data', $adSample, '-lang', 'por', '-encoding', 'UTF-8'
Invoke-Case -Name 'POSTaggerConverter ad' -StdinFile '' `
    -Arguments 'POSTaggerConverter', 'ad', '-data', $adSample, '-lang', 'por', '-encoding', 'UTF-8'
Invoke-Case -Name 'converter help (no format)' -StdinFile '' -Arguments 'POSTaggerConverter'
Invoke-Case -Name 'converter format help' -StdinFile '' `
    -Arguments 'POSTaggerConverter', 'conllu', 'help'

Write-Host ''
Write-Host '== training =='

$tokenTrain = Join-Path $WorkDirectory 'token.train'
Copy-Item -LiteralPath (Join-Path $Resources 'tokenize' 'token.train') -Destination $tokenTrain
Invoke-Case -Name 'TokenizerTrainer' -StdinFile '' `
    -Arguments 'TokenizerTrainer', '-model', (Join-Path $WorkDirectory 'tok.bin'),
        '-lang', 'eng', '-data', $tokenTrain, '-encoding', 'UTF-8'

$sentenceTrain = Join-Path $WorkDirectory 'Sentences.txt'
Copy-Item -LiteralPath (Join-Path $Resources 'sentdetect' 'Sentences.txt') -Destination $sentenceTrain
Invoke-Case -Name 'SentenceDetectorTrainer' -StdinFile '' `
    -Arguments 'SentenceDetectorTrainer', '-model', (Join-Path $WorkDirectory 'sent.bin'),
        '-lang', 'eng', '-data', $sentenceTrain, '-encoding', 'UTF-8'

$nerTrain = Join-Path $WorkDirectory 'AnnotatedSentences.txt'
Copy-Item -LiteralPath (Join-Path $Resources 'namefind' 'AnnotatedSentences.txt') -Destination $nerTrain
Invoke-Case -Name 'TokenNameFinderTrainer' -StdinFile '' `
    -Arguments 'TokenNameFinderTrainer', '-model', (Join-Path $WorkDirectory 'ner.bin'),
        '-lang', 'eng', '-data', $nerTrain, '-encoding', 'ISO-8859-1'

$posTrain = Join-Path $WorkDirectory 'pos.train'
Copy-Item -LiteralPath (Join-Path $Resources 'postag' 'AnnotatedSentences.txt') -Destination $posTrain
Invoke-Case -Name 'POSTaggerTrainer' -StdinFile '' `
    -Arguments 'POSTaggerTrainer', '-model', (Join-Path $WorkDirectory 'pos.bin'),
        '-lang', 'eng', '-data', $posTrain, '-encoding', 'UTF-8'

$chunkTrain = Join-Path $WorkDirectory 'test.txt'
Copy-Item -LiteralPath (Join-Path $Resources 'chunker' 'test.txt') -Destination $chunkTrain
Invoke-Case -Name 'ChunkerTrainerME' -StdinFile '' `
    -Arguments 'ChunkerTrainerME', '-model', (Join-Path $WorkDirectory 'chunk.bin'),
        '-lang', 'eng', '-data', $chunkTrain, '-encoding', 'UTF-8'

Write-Host ''
Write-Host '== evaluation =='
Invoke-Case -Name 'TokenizerMEEvaluator' -StdinFile '' `
    -Arguments 'TokenizerMEEvaluator', '-model', (Join-Path $Models 'en-token.bin'),
        '-data', $tokenTrain, '-encoding', 'UTF-8'
Invoke-Case -Name 'SentenceDetectorEvaluator' -StdinFile '' `
    -Arguments 'SentenceDetectorEvaluator', '-model', (Join-Path $Models 'en-sent.bin'),
        '-data', $sentenceTrain, '-encoding', 'UTF-8'
Invoke-Case -Name 'POSTaggerEvaluator' -StdinFile '' `
    -Arguments 'POSTaggerEvaluator', '-model', (Join-Path $Models 'en-pos-maxent.bin'),
        '-data', $posTrain, '-encoding', 'UTF-8'
Invoke-Case -Name 'ChunkerEvaluator' -StdinFile '' `
    -Arguments 'ChunkerEvaluator', '-model', (Join-Path $Models 'en-chunker.bin'),
        '-data', $chunkTrain, '-encoding', 'UTF-8'
Invoke-Case -Name 'TokenNameFinderEvaluator' -StdinFile '' `
    -Arguments 'TokenNameFinderEvaluator', '-model', (Join-Path $Models 'en-ner-person.bin'),
        '-data', $nerTrain, '-encoding', 'ISO-8859-1'

Write-Host ''
Write-Host '== cross validation =='
Invoke-Case -Name 'TokenizerCrossValidator' -StdinFile '' `
    -Arguments 'TokenizerCrossValidator', '-lang', 'eng', '-data', $tokenTrain,
        '-encoding', 'UTF-8', '-folds', '2'
Invoke-Case -Name 'SentenceDetectorCrossValidator' -StdinFile '' `
    -Arguments 'SentenceDetectorCrossValidator', '-lang', 'eng', '-data', $sentenceTrain,
        '-encoding', 'UTF-8', '-folds', '2'
Invoke-Case -Name 'POSTaggerCrossValidator' -StdinFile '' `
    -Arguments 'POSTaggerCrossValidator', '-lang', 'eng', '-data', $posTrain,
        '-encoding', 'UTF-8', '-folds', '2'
Invoke-Case -Name 'ChunkerCrossValidator' -StdinFile '' `
    -Arguments 'ChunkerCrossValidator', '-lang', 'eng', '-data', $chunkTrain,
        '-encoding', 'UTF-8', '-folds', '2'

Write-Host ''
Write-Host '== dictionary tools =='
$dictionary = Join-Path $WorkDirectory 'dict.txt'
Write-InputFile -Path $dictionary -Content "foo`nbar`nbaz`n"
Invoke-Case -Name 'DictionaryBuilder' -StdinFile '' `
    -Arguments 'DictionaryBuilder', '-inputFile', $dictionary,
        '-outputFile', (Join-Path $WorkDirectory 'dict.xml'), '-encoding', 'UTF-8'

Write-Host ''
Write-Host '======================================'
Write-Host "PASS: $script:Pass   FAIL: $script:Fail"

if ($script:FailedCases.Count -gt 0) {
    Write-Host 'failing cases:'
    foreach ($case in $script:FailedCases) {
        Write-Host "  - $case"
    }
}

exit ($script:Fail -gt 0 ? 1 : 0)
