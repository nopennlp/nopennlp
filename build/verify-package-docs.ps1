#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Checks a rendered docfx site for broken references and for namespace
    documentation that failed to attach.

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

.EXAMPLE
    dotnet docfx websites/apidocs/docfx.json --log docfx.log --logLevel warning
    build/verify-package-docs.ps1 -LogFile docfx.log
#>
[CmdletBinding()]
param(
    [string] $SiteDirectory,
    [string] $SourceDirectory,
    [string] $LogFile
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

Write-Host "All $($packageDocs.Count) package.md files reached their namespace pages." -ForegroundColor Green
