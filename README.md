# NOpenNLP

[![Build and Test](https://github.com/nopennlp/nopennlp/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/nopennlp/nopennlp/actions/workflows/build-and-test.yml)

A C# port of [Apache OpenNLP](https://opennlp.apache.org/) 1.9.4 — a machine
learning toolkit for natural language processing.

## Status

Work in progress. The port covers the `opennlp-tools` module — inference,
training, evaluation and the corpus format readers — and the command line tools,
which install as the `nopennlp` dotnet tool.

| Included | Not yet ported |
|---|---|
| Tokenization, sentence detection, POS tagging | UIMA integration |
| Lemmatization, chunking, name finding (NER) | Morfologik addon |
| Language detection, document categorization | brat annotator service |
| Maxent / Perceptron / Naive Bayes inference | |
| Model loading and feature generation | |
| Parsing, entity linking, word vectors | |
| Porter and Snowball stemmers | |
| Corpus format readers and converters | |
| Model training, evaluation, cross validation | |
| The `nopennlp` command line tools | |

### Corpus format readers

`NOpenNLP.Tools.Formats` reads third-party corpora into streams of native
`*Sample` objects: CoNLL 2002, CoNLL 2003, CoNLL-X, CoNLL-U, brat, MUC, NKJP,
Leipzig, LETSMT, French Treebank, OntoNotes, Irish Sentence Bank, Moses,
Évalita, BioNLP/NLPBA 2004, 20 Newsgroups, Census90, and the Portuguese Árvores
Deitadas (AD) corpus, plus converters between sample types.

The matching `*SampleStreamFactory` classes are not ported. They exist to expose
these readers to the OpenNLP command-line tools through the `cmdline`
`ObjectStreamFactory` SPI, and they arrive with that package; constructing a
reader directly does not need them.

### Stemmers

Apache OpenNLP does not hand-write its Snowball stemmers; it generates them with
the Snowball compiler and commits the output. This port does the same thing with
that compiler's C# backend, pinned to Snowball 2.0.0, rather than translating the
generated Java by hand.

`build/generate-snowball-stemmers.ps1` regenerates them. It runs the compiler in
a Linux container by default, so PowerShell 7 and Docker are the only
prerequisites and the result is identical on Windows, macOS and Linux — verified
byte-for-byte against a native macOS run. Pass `-NoDocker` to use a local C
toolchain instead.

Measured over the full Snowball vocabularies (1,113,209 words), 17 of the 21
languages produce output identical to OpenNLP 1.9.4. Finnish, Hungarian, French
and Indonesian differ on 0.4% to 6% of words, because OpenNLP ships generated
code predating several upstream fixes and the sources behind it survive at no
revision that can be pinned. In each case the port has the corrected behavior -
OpenNLP's Finnish gives one noun four different stems, and its Hungarian leaves
the ablative and delative case suffixes unstripped. The measurements, the
reasoning, and the individual cases are documented in the generation script and
pinned by `SnowballDeviationTest`.

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

The name follows Apache OpenNLP's own post-1.9.4 layout, which moved these tools
into an `opennlp-cli` module.

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

## Attribution

This product contains a modified C# port of Apache OpenNLP 1.9.4, specifically
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
