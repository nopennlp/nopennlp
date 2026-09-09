# NOpenNLP.Cli

The command line tools of [NOpenNLP](https://github.com/nopennlp/nopennlp), a C#
port of [Apache OpenNLP](https://opennlp.apache.org/) 1.9.5 — a machine learning
toolkit for natural language processing.

This package installs the tools as a
[dotnet tool](https://learn.microsoft.com/dotnet/core/tools/global-tools) named
`nopennlp`. To use the library from your own code, reference
[NOpenNLP.Tools](https://www.nuget.org/packages/NOpenNLP.Tools) instead.

## Install

```
dotnet tool install --global NOpenNLP.Cli
```

Requires the .NET 10 runtime.

## Usage

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

Models are the ones Apache OpenNLP publishes; this port reads the same binary
format.

The package name follows Apache OpenNLP's own post-1.x layout, which moved these
tools into an `opennlp-cli` module.

## Docker

The repository also builds an image with the tools already installed, for trying
them without a .NET SDK or a tool install. See the
[repository README](https://github.com/nopennlp/nopennlp#docker).

## Status

Work in progress. Prerelease versions may make breaking changes as the port
works toward a complete 1.9.5.

Source, issues and contributing guidelines are at
[github.com/nopennlp/nopennlp](https://github.com/nopennlp/nopennlp).

## Attribution

This product contains a modified C# port of Apache OpenNLP 1.9.5. The original
Java source is:

> Apache OpenNLP
> Copyright 2017 The Apache Software Foundation

The ported source has been modified from the original: translated from Java to
C#, renamed to the `NOpenNLP` namespaces, and adapted to .NET idioms and APIs.
Ported files carry the original Apache Software Foundation license header along
with a note marking them as modified. See the NOTICE file in this package for
details.

NOpenNLP is not affiliated with, endorsed by, or a product of The Apache
Software Foundation.

## License

Licensed under the [Apache License, Version 2.0](https://www.apache.org/licenses/LICENSE-2.0).
