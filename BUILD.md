# Building and testing

```
dotnet build NOpenNLP.slnx
dotnet test NOpenNLP.slnx
```

Tests run against `net10.0` by default, so the current SDK alone is enough. CI
additionally runs them against every target the library ships, on Linux, Windows
and macOS:

| Test client | Library under test |
|---|---|
| `net10.0` | `net10.0` |
| `net9.0` | `netstandard2.0` |
| `net8.0` | `net8.0` |

`netstandard2.0` has no runtime of its own, so `net9.0` is designated as its test
client. The matrix covers the library; `NOpenNLP.Cli` ships as a `net10.0` dotnet
tool and so has a single target, and its tests run once rather than on every leg.

To reproduce the full matrix locally (this needs the .NET 8, 9 and 10 runtimes
installed):

```
dotnet test NOpenNLP.slnx -p:TestFrameworks=true
```

## Strong naming

Every shipping assembly is strong named with `NOpenNLP.snk`, checked in at the
repository root and applied to all projects by `Directory.Build.props`. Nothing
needs to be installed or configured to build a signed assembly.

The key is in the repository on purpose, as Lucene.NET's and J2N's are. A strong
name identifies an assembly; it does not authenticate one. Anyone can rebuild
this source under a key of their own, and a key that every contributor's build
needs could not be kept secret in any case, so committing it gives up nothing
that was ever protected.

Authenticating the published artifact is a separate mechanism: NuGet package
signing, which the released packages already carry. `NOpenNLP.Tools` 1.9.5-beta.1
holds an author signature under a DigiCert code signing certificate, plus the
repository counter-signature nuget.org applies to everything it accepts. That,
not the strong name, is what tells a consumer where a package came from.

The public key is part of the assembly identity, so it must not change once a
version is published: a new key breaks every existing binding.

Signing also means a friend assembly has to be named by its public key, not just
its simple name. `Directory.Build.props` supplies that to every
`InternalsVisibleTo` item through an `ItemDefinitionGroup`, so a project naming a
friend still writes nothing but the simple name.

`NOpenNLP.Benchmarks` is the one project that opts out. It references the real
Apache OpenNLP through IKVM, whose cross-compiled output carries no strong name,
and a signed assembly cannot reference an unsigned one.

## Documentation site

`websites/apidocs` holds a [docfx](https://dotnet.github.io/docfx/) site: the API
reference generated from the two shipping projects, plus the guides under
`websites/apidocs/docs`. A `Docs` workflow builds it on every push and pull
request and publishes it to GitHub Pages from `main`.

```
dotnet tool restore
dotnet docfx websites/apidocs/docfx.json --serve
```

The namespace documentation comes from `package.md` files sitting beside the
ported code, one per namespace, carrying the text of upstream's
`package-info.java` and `package.html`. docfx binds each to its namespace through
the `uid` in its front matter, the way Lucene.NET does it. That binding fails
silently, so `build/verify-package-docs.ps1` checks after a build that every one
of them reached the page it names; the workflow runs it, and it also fails on any
broken link or cross-reference docfx reported.

## Comparing the CLI against Apache OpenNLP

`build/regress.ps1` runs the same invocation through both the `nopennlp` tool and
the real `opennlp` command and diffs stdout and the exit code, so a change in the
CLI's observable behaviour shows up as a failing case. It is a developer tool
rather than part of CI: it needs PowerShell 7, a JVM, the OpenNLP 1.9.5 jar, a
clone of the upstream source for its test corpora, and the models
`build/download-test-models.ps1` fetches. Its value is at rebase time, after
pulling a new upstream release. Its scratch space is `_artifacts/regress`, which
is gitignored and cleared at the start of each run.

Differences that are expected are normalized away rather than reported: the
command and product name, timings, absolute paths, and the order of a tool's
options and of its format lists. Java derives those last two from `Class.getMethods()`
and `HashMap` iteration, neither of which the JDK specifies, so the script sorts
both sides before diffing. Floating-point numbers are compared to a relative
tolerance of 1e-9, because upstream's trainers use `StrictMath` (fdlibm) where the
port uses `Math`, and the two disagree in the last ulp on a few percent of inputs.
Everything else is compared verbatim, and a clean run passes every case. The
comment-based help at the top of the script covers the setup and the details.

## Model compatibility with Apache OpenNLP

`src/java/model-compat` checks, in both directions, that models are portable
between the two implementations: NOpenNLP trains five models and Apache OpenNLP
1.9.5 loads them on a real JVM, then Apache OpenNLP trains the same five and
NOpenNLP loads those. Each side records the inference output it got, including
the probabilities behind it, and the other must reproduce it exactly.

```
src/java/model-compat/run-compat.sh     # or .bat / .ps1
```

It needs a JDK, Maven, the .NET SDK and PowerShell 7, but no downloaded models:
everything is trained on demand from corpora committed alongside it. A separate
`Model Compatibility` workflow runs it on Linux and Windows, scoped to the paths
that can affect serialization. Loading a model that another runtime wrote is the
only check that can catch a serialization difference, since both halves of a
.NET-only round trip share any bug in it. See
[the harness README](src/java/model-compat/README.md) for the details.
