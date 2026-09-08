# Getting started

NOpenNLP targets `net10.0`, `net8.0` and `netstandard2.0`. The only runtime
dependency is [J2N](https://github.com/NightOwl888/J2N).

```
dotnet add package NOpenNLP.Tools
```

## Tokenizing

The tokenizers that need no model are ready to use:

```csharp
using NOpenNLP.Tools.Tokenize;

var tokens = SimpleTokenizer.INSTANCE.Tokenize("Hello, world!");
// ["Hello", ",", "world", "!"]
```

## Loading a model

The trained models Apache OpenNLP publishes load as they are; the port reads the
same file format. See [model compatibility](porting-notes.md#model-compatibility)
for how that is verified.

```csharp
using System.IO;
using NOpenNLP.Tools.Tokenize;

using var stream = File.OpenRead("en-token.bin");
var tokenizer = new TokenizerME(new TokenizerModel(stream));

var tokens = tokenizer.Tokenize("Dr. Smith went to Washington.");
```

The same shape applies to the other tools: a `*Model` reads the file, and a
`*ME` class runs inference against it. <xref:NOpenNLP.Tools.Sentdetect.SentenceDetectorME>,
<xref:NOpenNLP.Tools.Postag.POSTaggerME>, <xref:NOpenNLP.Tools.Lemmatizer.LemmatizerME>,
<xref:NOpenNLP.Tools.Chunker.ChunkerME> and <xref:NOpenNLP.Tools.Namefind.NameFinderME>
all follow it.

## What is covered

The port covers the `opennlp-tools` module, which is inference, training,
evaluation and the corpus format readers, plus the command line tools. The UIMA
integration, the Morfologik addon and the brat annotator service are not ported.
