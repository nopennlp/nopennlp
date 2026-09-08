#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Checks a rendered docfx site for broken references, for namespace
    documentation that failed to attach, and for doc comments that render as a
    literal code block.

.DESCRIPTION
    The namespace documentation is ported from Apache OpenNLP's package-info.java
    and package.html files into package.md files that sit beside the ported code,
    the way Lucene.NET does it. docfx attaches each one to a namespace through the
    `uid` in its front matter, using the `overwrite` section of docfx.json.

    That binding is silent when it breaks. A uid naming a namespace that was
    renamed, or one that no type has been ported into yet, is dropped without a
    warning, and the build still succeeds; the page simply renders with no
    summary. Renaming a namespace in the port would therefore delete its
    documentation without anything failing.

    So this reads each package.md, resolves the uid to the page docfx produced,
    and requires the text to actually be there. Run it after `docfx build`.

    Given -LogFile it also fails on docfx's own warnings, which are the ones that
    mean a link or cross-reference does not resolve. docfx has no way to scope
    --warningsAsErrors to those: it would equally fail on the C# compiler
    warnings that surface while it builds the assemblies, which are pre-existing
    defects in the ported source and not this site's concern. The structured log
    separates them, because docfx tags its own diagnostics with a code and
    relays compiler output without one.

.PARAMETER SiteDirectory
    The rendered site. Defaults to the _site directory docfx.json writes to.

.PARAMETER SourceDirectory
    Where to search for package.md files. Defaults to src.

.PARAMETER LogFile
    The structured log from `docfx --log <file> --logLevel warning`. When given,
    any docfx-issued warning in it fails the check.

.PARAMETER MetadataDirectory
    The YAML docfx generates from the assemblies. Defaults to the api directory
    beside the site. Scanned for summaries that would render as a code block.

.EXAMPLE
    dotnet docfx websites/apidocs/docfx.json --log docfx.log --logLevel warning
    build/verify-package-docs.ps1 -LogFile docfx.log
#>
[CmdletBinding()]
param(
    [string] $SiteDirectory,
    [string] $SourceDirectory,
    [string] $LogFile,
    [string] $MetadataDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $SiteDirectory) {
    $SiteDirectory = Join-Path $repoRoot 'websites/apidocs/_site'
}
if (-not $SourceDirectory) {
    $SourceDirectory = Join-Path $repoRoot 'src'
}
if (-not $MetadataDirectory) {
    $MetadataDirectory = Join-Path $repoRoot 'websites/apidocs/api'
}

if (-not (Test-Path $SiteDirectory)) {
    throw "No rendered site at '$SiteDirectory'. Run 'dotnet docfx websites/apidocs/docfx.json' first."
}

$packageDocs = @(Get-ChildItem -Path $SourceDirectory -Filter 'package.md' -Recurse -File)
if ($packageDocs.Count -eq 0) {
    throw "No package.md files under '$SourceDirectory'. The namespace documentation ported from upstream is missing."
}

$failures = @()

if ($LogFile) {
    if (-not (Test-Path $LogFile)) {
        throw "No docfx log at '$LogFile'. Pass the file given to 'docfx --log'."
    }

    foreach ($line in (Get-Content -Path $LogFile)) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $entry = $line | ConvertFrom-Json

        if ($entry.severity -ne 'warning' -and $entry.severity -ne 'error') { continue }

        # No code means docfx is relaying the C# compiler rather than reporting
        # on the documentation. Those warnings are defects in the ported source
        # and are tracked there, not here.
        $code = $entry.PSObject.Properties['code']
        if (-not $code -or [string]::IsNullOrWhiteSpace($code.Value)) { continue }

        $failures += "docfx $($entry.severity) $($code.Value): $($entry.message)"
    }
}

# A doc comment whose text docfx emits as a multi-line YAML string gets every
# continuation line indented four spaces, and four spaces in Markdown means a
# code block. The whole summary then renders as literal text, with <xref> and
# <p> tags showing as markup rather than as links and paragraphs. It is silent:
# the page builds, and only looks wrong.
#
# The trigger is a blank /// line inside a summary, or a <br/> inside a <code>
# block. Fixes are to write <para/> on its own line instead of leaving the line
# blank, and to let a <code> block use real line breaks. Use <c> for an inline
# code span; <code> is a block element and swallows whatever follows it.
if (Test-Path $MetadataDirectory) {
    $metadataFiles = @(Get-ChildItem -Path $MetadataDirectory -Filter '*.yml' -File)
    foreach ($file in $metadataFiles) {
        $yaml = Get-Content -Path $file.FullName -Raw

        foreach ($match in [regex]::Matches($yaml, '(?m)^\s*(summary|remarks|example): "((?:[^"\\]|\\.)*)"\s*$')) {
            $value = $match.Groups[2].Value
            if ($value -notmatch '(^|\\n)\s{4,}\S') { continue }

            # Name the member from the nearest preceding uid.
            $uid = 'unknown'
            $preceding = [regex]::Matches($yaml.Substring(0, $match.Index), '(?m)^\s*- uid: (\S+)\s*$')
            if ($preceding.Count -gt 0) {
                $uid = $preceding[$preceding.Count - 1].Groups[1].Value
            }

            $failures += "$uid has a $($match.Groups[1].Value) that docfx indented, so it renders as a literal code block. Remove the blank /// line inside it, or the <br/> tags inside its <code> block."
        }
    }
}

foreach ($doc in $packageDocs) {
    $relative = [IO.Path]::GetRelativePath($repoRoot, $doc.FullName)
    $content = Get-Content -Path $doc.FullName -Raw

    $uidMatch = [regex]::Match($content, '(?m)^uid:\s*(\S+)\s*$')
    if (-not $uidMatch.Success) {
        $failures += "$relative has no 'uid:' in its front matter, so docfx has nothing to attach it to."
        continue
    }
    $uid = $uidMatch.Groups[1].Value

    # The body is everything after the license comment. Take its first sentence
    # as the needle: enough to prove the overwrite applied, short enough not to
    # break on how docfx wraps and links the rest.
    $body = ($content -split '-->', 2)[-1].Trim()
    if ([string]::IsNullOrWhiteSpace($body)) {
        $failures += "$relative has no body below the license header."
        continue
    }

    # Cross-references render as the link text docfx chooses, so compare on the
    # plain prose ahead of the first one.
    $firstLine = ($body -split "`n")[0].Trim()
    $needle = ($firstLine -split '<xref:')[0].Trim()
    if ($needle.Length -lt 12) {
        $failures += "$relative starts with a cross-reference, leaving too little plain text to verify. Lead with prose."
        continue
    }

    $page = Join-Path $SiteDirectory "api/$uid.html"
    if (-not (Test-Path $page)) {
        $failures += "$relative names uid '$uid', but docfx rendered no page for it. The namespace may have been renamed or may hold no ported types."
        continue
    }

    $rendered = Get-Content -Path $page -Raw
    # Strip tags so the comparison is against prose, not markup, and decode the
    # entities docfx escapes on the way out.
    $text = [regex]::Replace($rendered, '<[^>]+>', ' ')
    $text = [System.Net.WebUtility]::HtmlDecode($text)
    $text = [regex]::Replace($text, '\s+', ' ')
    $expected = [regex]::Replace($needle, '\s+', ' ')

    if ($text -notlike "*$expected*") {
        $failures += "$relative did not reach the page for '$uid'. docfx dropped the overwrite; check that the uid names a namespace that has ported types."
    }
}

if ($failures.Count -gt 0) {
    Write-Host "The rendered site has problems:" -ForegroundColor Red
    foreach ($failure in $failures) {
        Write-Host "  - $failure" -ForegroundColor Red
    }
    exit 1
}

Write-Host "All $($packageDocs.Count) package.md files reached their namespace pages, and no doc comment renders as a code block." -ForegroundColor Green
