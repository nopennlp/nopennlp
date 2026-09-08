---
_layout: landing
---

# NOpenNLP

A C# port of [Apache OpenNLP](https://opennlp.apache.org/) 1.9.5, a machine
learning toolkit for natural language processing.

```csharp
using NOpenNLP.Tools.Tokenize;

var tokens = SimpleTokenizer.INSTANCE.Tokenize("Hello, world!");
// ["Hello", ",", "world", "!"]
```

Types and members keep their upstream OpenNLP names, so the Java documentation
and examples carry over directly with .NET casing conventions applied
(`TokenizerME.tokenize` becomes `TokenizerME.Tokenize`).

- [Getting started](docs/getting-started.md) installs the package and runs the
  first few tools.
- [The command line tools](docs/command-line.md) covers the `nopennlp` command.
- [API documentation](api/NOpenNLP.Tools.Tokenize.yml) is generated from the ported source.

NOpenNLP is not affiliated with, endorsed by, or a product of The Apache
Software Foundation. See [NOTICE](https://github.com/nopennlp/nopennlp/blob/main/NOTICE).
