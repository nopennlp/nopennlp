# NOpenNLP.Tools

A C# port of [Apache OpenNLP](https://opennlp.apache.org/) 1.9.5 — a machine
learning toolkit for natural language processing.

This package is the port of the `opennlp-tools` module: inference, training,
evaluation and the corpus format readers. For the command line tools, install
[NOpenNLP.Cli](https://www.nuget.org/packages/NOpenNLP.Cli) instead.

The API reference and guides are at
[nopennlp.github.io/nopennlp](https://nopennlp.github.io/nopennlp/).

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

Models are the ones Apache OpenNLP publishes; this port loads the same binary
format:

```csharp
using NOpenNLP.Tools.Tokenize;

using var stream = File.OpenRead("en-token.bin");
var tokenizer = new TokenizerME(new TokenizerModel(stream));
var tokens = tokenizer.Tokenize("Dr. Smith went to Washington.");
```

## What is included

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

Note: the UIMA library (`opennlp-uima` upstream) will likely never be ported.
There is no .NET port of UIMA (which is also not a goal of this project),
and there only seems to be some casual and research interest in creating one.

## Status

Work in progress. Prerelease versions may make breaking changes as the port
works toward a complete 1.9.5.

Source, issues and contributing guidelines are at
[github.com/nopennlp/nopennlp](https://github.com/nopennlp/nopennlp).

## Attribution

This product contains a modified C# port of Apache OpenNLP 1.9.5, specifically
the `opennlp-tools` module. The original Java source is:

> Apache OpenNLP
> Copyright 2017 The Apache Software Foundation

The ported source has been modified from the original: translated from Java to
C#, renamed to the `NOpenNLP.Tools` namespace, and adapted to .NET idioms and
APIs. Ported files carry the original Apache Software Foundation license header
along with a note marking them as modified. See the NOTICE file in this package
for details.

NOpenNLP is not affiliated with, endorsed by, or a product of The Apache
Software Foundation.

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
