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

## Building, Testing, and Contributing

See [BUILD.md](BUILD.md) for building and testing instructions and tips.

See [CONTRIBUTING.md](CONTRIBUTING.md) for how you can help contribute to this project.

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
