# NOpenNLP

[![Build and Test](https://github.com/nopennlp/nopennlp/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/nopennlp/nopennlp/actions/workflows/build-and-test.yml)

A C# port of [Apache OpenNLP](https://opennlp.apache.org/) 1.9.1 — a machine
learning toolkit for natural language processing.

## Status

Early work in progress. The port currently covers the **inference** APIs of the
`opennlp-tools` module: loading pre-trained `.bin` models and running them.

| Included | Not yet ported |
|---|---|
| Tokenization, sentence detection, POS tagging | Model training |
| Lemmatization, chunking, name finding (NER) | Command-line tools |
| Maxent / Perceptron / Naive Bayes inference | Corpus format readers |
| Model loading and feature generation | Document categorization, parser, stemmers |

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
client. To reproduce the full matrix locally (this needs the .NET 8, 9 and 10
runtimes installed):

```
dotnet test NOpenNLP.slnx -p:TestFrameworks=true
```

## Attribution

This product contains a modified C# port of Apache OpenNLP 1.9.1, specifically
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
