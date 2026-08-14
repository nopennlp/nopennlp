# NOpenNLP benchmarks

Measures the ported code against the Apache OpenNLP 1.9.4 it was ported from,
one operation at a time.

There are two projects, and they answer different questions:

| Project | Runs | Answers |
| --- | --- | --- |
| `src/NOpenNLP.Benchmarks` (this one) | NOpenNLP and Java OpenNLP **in the same .NET process**, the latter cross-compiled by [IKVM](https://github.com/ikvmnet/ikvm) | "If you are on .NET today and using OpenNLP through IKVM, what does switching to NOpenNLP get you?" |
| `src/java/opennlp-benchmarks` | Java OpenNLP on a **real JVM**, under JMH | "How does the port compare to OpenNLP as it actually runs in production Java?" |

Both matter. IKVM adds its own overhead, so the in-process comparison flatters
the port; the JVM numbers are the honest measure of the algorithm. Read them
together, and see [the JMH project's README](../java/opennlp-benchmarks/README.md)
for how to line the two up.

## Prerequisites

The pre-trained models, which are the same ones the integration tests use:

```powershell
./build/download-test-models.ps1
```

That fetches ~46 MB into a gitignored `testdata/`. The benchmarks fail with a
clear message if they are missing — unlike the tests, which report inconclusive,
because a benchmark that silently skipped would print an empty summary that
reads like a successful run.

Set `NOPENNLP_TEST_DATA_DIR` to keep the models elsewhere.

## Running

```bash
# Everything. Takes a while: 28 benchmarks, each loading real models.
dotnet run --project src/NOpenNLP.Benchmarks -c Release -- --filter '*'

# One tool.
dotnet run --project src/NOpenNLP.Benchmarks -c Release -- --filter '*Tokenizer*'

# Interactive menu.
dotnet run --project src/NOpenNLP.Benchmarks -c Release

# What is available.
dotnet run --project src/NOpenNLP.Benchmarks -c Release -- --list flat

# A quick, rough answer. Fine for spotting a large regression, not for
# reporting numbers.
dotnet run --project src/NOpenNLP.Benchmarks -c Release -- --filter '*' --job short
```

Results land in `BenchmarkDotNet.Artifacts/results/` as GitHub-flavoured
Markdown and HTML.

## What is covered

| Benchmark | Measures |
| --- | --- |
| `SentenceDetectorBenchmarks` | `SentenceDetectorME.SentDetect` |
| `TokenizerBenchmarks` | `TokenizerME`, `SimpleTokenizer`, `WhitespaceTokenizer` |
| `POSTaggerBenchmarks` | `POSTaggerME.Tag`, against both the maxent and the perceptron model |
| `ChunkerBenchmarks` | `ChunkerME.Chunk` |
| `NameFinderBenchmarks` | `NameFinderME.Find`, over all seven entity models |
| `ModelLoadingBenchmarks` | Constructing each model from its `.bin` — the zip container, the manifest and the model readers |

Every benchmark runs on the same text as its Java counterpart, and on the same
text the integration tests use, so a benchmark result and a test failure can be
read against each other.

## Reading the results

Each Java/NOpenNLP pair is its own category with its own baseline, so a ratio
always compares like with like. Without that, BenchmarkDotNet divides every row
in a class by whichever single benchmark carried `Baseline = true`, and
`SimpleTokenizer` ends up reported as a ratio against `TokenizerME` — a
comparison between two different algorithms, which answers nothing.

Allocation is tracked alongside time. The Java code allocates freely and IKVM
turns that into managed allocation, so a port that is no faster but allocates
far less is still a real improvement, and one that quietly allocates more is a
regression worth seeing.

## Why this is not in CI

The IKVM dependency unpacks to over 4 GB in the NuGet cache. Building this on
all nine test legs to produce numbers nobody reads is not worth it, and
benchmark numbers from a shared CI runner are too noisy to gate on anyway.

The project is in `NOpenNLP.slnx` so it opens with the solution, but CI restores
the library and test projects individually rather than the solution. If you add
a CI job that restores the solution as a whole, you will pull IKVM onto every
leg.
