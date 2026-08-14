# JMH benchmarks for Apache OpenNLP

Runs Apache OpenNLP 1.9.4 under [JMH](https://openjdk.org/projects/code-tools/jmh/)
on a real JVM, mirroring the BenchmarkDotNet benchmarks in
[`src/NOpenNLP.Benchmarks`](../../NOpenNLP.Benchmarks/README.md) case for case.

## Why this exists

The .NET benchmark project already compares NOpenNLP against Java OpenNLP by
cross-compiling the real jar with IKVM and running both in one process. That is
the right comparison for a .NET developer choosing between the two, but it is
not a fair measure of the *algorithm*: IKVM adds real overhead, so the port
looks better against IKVM than against Java as anyone actually runs it.

These benchmarks supply the missing number — OpenNLP on a JVM, with a JIT that
has warmed up and a GC tuned for it. Comparing the three tells you which part of
any difference is the port and which part is IKVM.

## Prerequisites

- **JDK 11 or later.** The build targets Java 11 bytecode (`maven.compiler.release`),
  so the jar runs on 11, 17, 21 and 25 LTS alike. OpenNLP 1.9.4 is Java 8
  bytecode, but `-source 8` is deprecated in current JDKs and slated for
  removal, and nothing here uses a language feature past 8 anyway.
- **Maven 3.6 or later.**
- **The models**, the same ones the .NET benchmarks and integration tests use:

  ```powershell
  ./build/download-test-models.ps1
  ```

  Set `NOPENNLP_TEST_DATA_DIR` to keep them elsewhere; otherwise the benchmarks
  walk up from the working directory looking for `testdata/`.

## Running

```bash
cd src/java/opennlp-benchmarks
mvn package

# Everything, from the repository root so testdata/ is found.
cd ../../..
java -jar src/java/opennlp-benchmarks/target/benchmarks.jar

# One class, or one method.
java -jar src/java/opennlp-benchmarks/target/benchmarks.jar TokenizerBenchmark
java -jar src/java/opennlp-benchmarks/target/benchmarks.jar 'ChunkerBenchmark.chunk'

# What is available.
java -jar src/java/opennlp-benchmarks/target/benchmarks.jar -l

# A quick, rough answer: one fork, fewer and shorter iterations.
java -jar src/java/opennlp-benchmarks/target/benchmarks.jar -f 1 -wi 2 -i 3 -r 2s -w 2s

# JSON, for diffing two runs.
java -jar src/java/opennlp-benchmarks/target/benchmarks.jar -rf json -rff target/jmh-result.json
```

Inference benchmarks report **microseconds per operation**; the model loading
benchmarks report **milliseconds**, because loading is three orders of magnitude
slower and would otherwise print as unreadably large numbers.

## Comparing against the .NET numbers

The two harnesses are not directly commensurable and should not be pasted into
one table without care:

- **Warmup.** JMH forks a JVM per trial and warms it until the JIT settles.
  BenchmarkDotNet does the equivalent for RyuJIT. Both report a steady state, so
  the comparison is meaningful — but neither tells you anything about startup,
  which for a short-lived process is often what actually matters. The model
  loading benchmarks are the closest thing here to a startup measurement.
- **Units.** JMH reports microseconds (milliseconds for model loading);
  BenchmarkDotNet picks a unit per table. Check the header before comparing.
- **Allocation.** BenchmarkDotNet's `MemoryDiagnoser` reports managed bytes per
  operation. JMH's equivalent is `-prof gc`, which is not on by default; add it
  if you want that column.

Run both on the same machine, otherwise you are measuring hardware.

## Keeping the two in sync

`BenchmarkData.java` is a deliberate line-for-line counterpart of
`BenchmarkData.cs`: the same models, the same sample text, the same tokens and
tags. If the two drift apart the numbers stop being comparable, which defeats
the point of the project. Change both together.

The same applies to the benchmark classes themselves — each one here has a
counterpart in the .NET project, named the same way and measuring the same call.
Notably, neither `NameFinderBenchmark` nor its .NET counterpart clears the name
finder's adaptive state, even though upstream expects a caller to clear it at a
document boundary. Every invocation replays the same sentence, so the state
saturates after the first call and clearing makes no measurable difference — and
on the BenchmarkDotNet side an iteration-level hook forces `InvocationCount=1`,
which inflated the reported cost by an order of magnitude. Both harnesses
therefore measure the warm case; do not read these as first-document latency.

## Not wired into CI

Built and run by hand. It needs a JDK and a Maven repository that CI does not
otherwise require, and benchmark numbers from a shared runner are too noisy to
gate on.
