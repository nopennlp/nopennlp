# NOpenNLP

[![Build and Test](https://github.com/nopennlp/nopennlp/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/nopennlp/nopennlp/actions/workflows/build-and-test.yml)
[![Docs](https://github.com/nopennlp/nopennlp/actions/workflows/docs.yml/badge.svg)](https://nopennlp.github.io/nopennlp/)

A C# port of [Apache OpenNLP](https://opennlp.apache.org/) 1.9.5 — a machine
learning toolkit for natural language processing.

The API reference and guides are at
[nopennlp.github.io/nopennlp](https://nopennlp.github.io/nopennlp/).

## Status

Work in progress. The port covers the `opennlp-tools` module — inference,
training, evaluation and the corpus format readers — and the command line tools,
which install as the `nopennlp` dotnet tool.

| Included | Not yet ported |
|---|---|
| Tokenization, sentence detection, POS tagging | `eval` tests |
| Lemmatization, chunking, name finding (NER) | Morfologik addon |
| Language detection, document categorization | brat annotator service |
| Maxent / Perceptron / Naive Bayes inference | |
| Model loading and feature generation | |
| Parsing, entity linking, word vectors | |
| Porter and Snowball stemmers | |
| Corpus format readers and converters | |
| Model training, evaluation, cross validation | |
| The `nopennlp` command line tools | |

Note: the UIMA library (`opennlp-uima` upstream) will likely never be ported.
There is no .NET port of UIMA (which is also not a goal of this project),
and there only seems to be some casual and research interest in creating one.

## Usage

Targets `net10.0`, `net8.0`, and `netstandard2.0`. The only runtime dependency is
[J2N](https://github.com/NightOwl888/J2N).

```csharp
using NOpenNLP.Tools.Tokenize;

var tokens = SimpleTokenizer.INSTANCE.Tokenize("Hello, world!");
// ["Hello", ",", "world", "!"]
```

Types and members keep their upstream OpenNLP names so that the Java
documentation and examples carry over directly, with .NET casing conventions
applied (`TokenizerME.tokenize` becomes `TokenizerME.Tokenize`).

### Command line

The `NOpenNLP.Cli` package installs OpenNLP's command line tools as a
[dotnet tool](https://learn.microsoft.com/dotnet/core/tools/global-tools) named
`nopennlp`:

```
dotnet tool install --global NOpenNLP.Cli
```

It takes the same arguments as the `opennlp` command, so existing OpenNLP
command lines, scripts and documentation carry over by changing the command
name:

```
nopennlp                                    # lists the tools
nopennlp SimpleTokenizer < sentences.txt
nopennlp TokenizerTrainer -model en-token.bin -lang eng -data token.train
nopennlp POSTaggerTrainer.conllu -model pos.bin -lang deu -data corpus.conllu -tagset u
nopennlp TokenizerME en-token.bin < sentences.txt
```

As upstream, a `.format` suffix on a tool name selects the corpus format to read
(`POSTaggerTrainer.conllu`), any tool prints its help when invoked with `help`,
and converters take their format as the first argument
(`nopennlp POSTaggerConverter conllu -data corpus.conllu`).

The name follows Apache OpenNLP's own post-1.x layout, which moved these tools
into an `opennlp-cli` module.

### Docker

The `Dockerfile` at the repository root builds an image with the tools already
installed, for trying them without a .NET SDK or a tool install:

```
docker build -t nopennlp .
docker run --rm -it nopennlp
```

That drops into a shell in `/data` with `nopennlp` on the PATH. A single command
works too:

```
echo "Dr. Smith went to Washington." | docker run --rm -i nopennlp nopennlp SimpleTokenizer
```

The image carries no models. Mount a directory holding them onto `/data`, which
is where the container starts:

```
docker run --rm -it -v "$PWD/models:/data" nopennlp
```

> [!NOTE]
> Unlike upstream's Dockerfile, which takes a release tarball as a build argument,
> this one builds `NOpenNLP.Cli` from the source next to it and installs the
> package it just packed.

## Building and testing

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

### Documentation site

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

### Comparing the CLI against Apache OpenNLP

`build/regress.ps1` runs the same invocation through both the `nopennlp` tool and
the real `opennlp` command and diffs stdout and the exit code, so a change in the
CLI's observable behaviour shows up as a failing case. It is a developer tool
rather than part of CI: it needs PowerShell 7, a JVM, the OpenNLP 1.9.5 jar, a
clone of the upstream source for its test corpora, and the models
`build/download-test-models.ps1` fetches.

### Model compatibility with Apache OpenNLP

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

## Attribution

This product contains a modified C# port of Apache OpenNLP 1.9.5, specifically
the `opennlp-tools` module. The original Java source is:

> Apache OpenNLP
> Copyright 2017 The Apache Software Foundation

The ported source has been modified from the original: translated from Java to
C#, renamed to the `NOpenNLP.Tools` namespace, and adapted to .NET idioms and
APIs. Ported files carry the original Apache Software Foundation license header
along with a note marking them as modified. See [NOTICE](NOTICE) for details.

NOpenNLP is not affiliated with, endorsed by, or a product of The Apache
Software Foundation.

## License

Licensed under the [Apache License, Version 2.0](LICENSE).
