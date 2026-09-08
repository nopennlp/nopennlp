# Model compatibility harness (Apache OpenNLP 1.9.5 <-> NOpenNLP)

This self-contained Maven project proves **two-way** model compatibility between
Apache OpenNLP 1.9.5 running on a real JVM and NOpenNLP, for [issue #46].

[issue #46]: https://github.com/nopennlp/nopennlp/issues/46

## What it checks

Each runtime trains the same five models from the same corpora with the same
hyperparameters, serializes them, and records the inference output it got. The
**other** runtime then loads those model files and must produce the same output
from the same inputs.

Loading alone would be a weak check: a model reader that silently mangled a
parameter table would still open the file without complaint. So each side
records both the discrete output a caller sees (sentences, tokens, tags, spans)
and the probabilities behind it. Probabilities are the part that actually proves
the model was read correctly, because a misread model will frequently still land
on the same argmax for an easy case while scoring it differently.

The five tools, chosen to cover both trainers and both scoring paths:

| Tool | Trainer | What it exercises |
| --- | --- | --- |
| Sentence detector | maxent (GIS) | the maxent model reader, EOS scanning |
| Tokenizer | maxent (GIS) | the same reader, plus span arithmetic |
| Name finder | perceptron | the perceptron reader, the BIO sequence codec, adaptive feature state |
| POS tagger | maxent (GIS) | beam search, including the n-best sequences |
| Chunker | maxent (GIS) | beam search chained onto the POS tagger's real output |

Nothing states an expected *answer*. The models are trained on a few hundred
sentences and their output is not interesting in its own right; what matters is
that the two runtimes agree. A disagreement is therefore reported as a diff
between two concrete outputs rather than as a failure against a magic constant
nobody can check.

## The shared contract

Four pairs of files, each a deliberate line-for-line counterpart. **They must
stay in sync** — the comparison is only meaningful if both sides train on the
same bytes with the same settings and then ask the same questions:

| Java | .NET |
| --- | --- |
| [`CompatCorpus.java`](src/main/java/org/nopennlp/compat/CompatCorpus.java) | [`CompatCorpus.cs`](../../NOpenNLP.Tools.Tests/Compat/CompatCorpus.cs) |
| [`CompatResults.java`](src/main/java/org/nopennlp/compat/CompatResults.java) | [`CompatResults.cs`](../../NOpenNLP.Tools.Tests/Compat/CompatResults.cs) |
| [`CompatModels.java`](src/main/java/org/nopennlp/compat/CompatModels.java) | [`CompatModels.cs`](../../NOpenNLP.Tools.Tests/Compat/CompatModels.cs) |
| [`TestNOpenNLPCompatibility.java`](src/test/java/org/nopennlp/compat/TestNOpenNLPCompatibility.java) | [`JavaCompatibilityTest.cs`](../../NOpenNLP.Tools.Tests/Compat/JavaCompatibilityTest.cs) |

The training corpora in [`corpora/`](corpora) are copies of the Apache OpenNLP
test resources; see [`corpora/NOTICE`](corpora/NOTICE) for the provenance and
for why they live here rather than being read from either project's own test
resources.

## Requirements

- **A JDK 11 or later.** The project targets Java 11 bytecode, matching
  [`opennlp-benchmarks`](../opennlp-benchmarks/README.md).
- **Maven 3.6 or later.**
- **The .NET SDK**, to build and run the NOpenNLP side.
- **PowerShell 7 or later** (`pwsh`) to run the driver script.

No pre-trained models are needed: everything is trained from the committed
corpora on demand. That also means this needs no network access beyond the
initial Maven dependency resolution.

## No committed models

Models are **never** committed. Every model is trained fresh into the gitignored
`work/` folder:

- `work/java/` — models trained by Apache OpenNLP (read by NOpenNLP)
- `work/dotnet/` — models trained by NOpenNLP (read by Apache OpenNLP)

Each folder also gets a `results.txt`, the inference output that runtime
recorded for the other to compare against.

## Running

The driver script runs **both** directions end to end and exits non-zero if
either fails:

```sh
./run-compat.sh                          # macOS / Linux
.\run-compat.bat                         # Windows (cmd)
./run-compat.ps1                         # any platform (PowerShell)

./run-compat.sh -TargetFramework net8.0  # pin the .NET side's framework
```

All the logic lives in `run-compat.ps1`; the `.sh` and `.bat` files are thin
wrappers that locate `pwsh` and forward their arguments.

### Direction 1: NOpenNLP trains, Apache OpenNLP reads

```sh
# NOpenNLP writes work/dotnet/
NOPENNLP_COMPAT_WRITE_DIR=$PWD/work/dotnet dotnet test \
  ../../NOpenNLP.Tools.Tests/NOpenNLP.Tools.Tests.csproj \
  --filter FullyQualifiedName~JavaCompatibilityTest.TestWriteModelsForJava

# Apache OpenNLP reads them
mvn -q test -Dnopennlp.model.dir=$PWD/work/dotnet
```

In Java a **missing** model directory makes the test **fail**, not skip: when
Java is asked to read NOpenNLP's models, their absence means the pipeline that
was supposed to produce them is broken.

### Direction 2: Apache OpenNLP trains, NOpenNLP reads

```sh
# Apache OpenNLP writes work/java/
mvn -q compile exec:java -Dexec.args=$PWD/work/java

# NOpenNLP reads them
NOPENNLP_COMPAT_READ_DIR=$PWD/work/java dotnet test \
  ../../NOpenNLP.Tools.Tests/NOpenNLP.Tools.Tests.csproj \
  --filter FullyQualifiedName~JavaCompatibilityTest.TestReadJavaTrainedModels
```

In .NET a **missing** model directory makes the test **inconclusive** (skipped),
because a JDK may legitimately not be present. That is a deliberate asymmetry,
and it is why the driver script and the CI workflow both require `Passed: 1`
rather than trusting the exit code: a skipped test exits 0 and would otherwise
turn a broken pipeline green.

## The CI-safe baseline

`JavaCompatibilityTest.TestNOpenNLPRoundTrip` trains, writes and reads the same
five models entirely within .NET. It proves nothing about Java, but it runs on
every build with no JDK, so a change that breaks the shared contract fails
immediately rather than waiting for the compatibility job.

## What this found

The harness paid for itself on its first run. Apache OpenNLP could not load a
single NOpenNLP-written model, for three reasons that no amount of .NET-only
testing would have surfaced:

1. **Class names.** The port recorded its factory, sequence codec and artifact
   serializers under their .NET names (`NOpenNLP.Tools.Sentdetect.SentenceDetectorFactory`).
   Apache OpenNLP's `ExtensionLoader` rejects any class name outside the allowed
   `opennlp.` package, so it failed before reading a single byte of the model.
   The port's own loader already resolved both spellings, so writing the Java
   name costs nothing and is what upstream does.
2. **Version.** The embedded `opennlp.version` resource still said `1.9.4` after
   the port moved to 1.9.5, so every model was stamped with the wrong version.
3. **Boolean casing.** .NET's `bool.ToString()` writes `True`; Java's
   `Boolean.toString()` writes `true`. Both runtimes parse either, so this one
   was harmless — but it made the model bytes gratuitously different from what
   Apache OpenNLP would have written for the same inputs.

The first two are covered by `PortRegressionTest` so they cannot regress without
a JDK present.

## Not wired into the main CI build

Run by the separate [`model-compatibility.yml`](../../../.github/workflows/model-compatibility.yml)
workflow, not by `build-and-test.yml`. It needs a JDK and a Maven repository
that the main build does not otherwise require, and it is scoped to the paths
that can actually affect model serialization.
