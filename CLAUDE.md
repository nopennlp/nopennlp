# Claude Code instructions for NOpenNLP

The goal of this project is to create a faithful .NET port of Apache OpenNLP.

The current version of OpenNLP being targeted is: 1.9.4

## C# Conversion

When converting the original Java code to C#, take care to:

- Add proper license attribution (see other files for an example)
- Use 4 spaces for tabs.
- Keep code in the same order, file structure, etc. as upstream as much as possible. This eases porting future updates.
- Projects match the Java names with the following transformation: replace hyphen with period, `opennlp` with `NOpenNLP`, and use .NET-style casing. i.e. `opennlp-tools` becomes `NOpenNLP.Tools`
- Namespaces should match their file structure and the upstream Java package, i.e. `src/NOpenNLP.Tools/Util/Model/BaseModel.cs` should be in `NOpenNLP.Tools.Util.Model`.
- Use C# naming conventions and style. Methods start with a capital letter, use Int32 instead of Int in method names, etc.
- Rename interfaces to start with the letter I
- Do not include more `using` declarations than needed
- Use file-scoped namespaces
- Replace `Iterable` with a proper `IEnumerable` implementation
- Replace `static final` fields with `const` where possible; otherwise `static readonly`
- Replace javadoc comments/tags with proper XML doc comment equivalents
- Ensure XML doc comments are correctly formatted; i.e. `<br>` is invalid, it must be self-closing. `<p>` should be replaced with `<para/>`.
- Use the latest C# (up to C# 14) features where it makes sense and does not harm readability, such as collection expressions and primary constructors
- Use C# type keywords, like `string` instead of `String`
- Put blank lines between members in a file. Fields may be grouped together without blank lines between them.
- Only use features available in our greatest-common-denominator TFM, `netstandard2.0`, or polyfill as necessary.
- Use J2N equivalents where retaining Java behavior might be important
- Use J2N collection types internally (i.e. List, Dictionary, etc) but do not expose them in the public API; expose the .NET BCL equivalent interfaces they implement (i.e. `IList<T>`). When using J2N collection types, alias the namespace with `JCG` via `using JCG = J2N.Collections.Generic;`
- When private fields are not mutated, and not already marked `final` in Java, make them `readonly` with a comment: `// NOpenNLP: made readonly`
- Java `Map` and C# dictionaries differ in ways that silently change behavior. Watch for:
  - `map.get(key)` returns `null` for an absent key; the C# indexer throws
    `KeyNotFoundException`. Port these with `TryGetValue`, and preserve what the
    Java code did with the `null` — note that string concatenation renders it as
    the text `"null"`, which some feature generators emit verbatim as a feature.
  - `map.put(key, value)` overwrites an existing key and returns the previous
    value; `Add` throws on a duplicate key. Use the indexer or J2N's `Put`.
  - `map.remove`/`containsKey` on an absent key are non-throwing in both, but
    check the return value is used the same way.
  - The same applies to `List.get` on an out-of-range index and to any other
    Java API that returns `null` where the .NET counterpart throws.
- When deviating from upstream OpenNLP, add a brief comment like `// NOpenNLP-specific: {justification of change}`
- Try to make ported code nullable-friendly with nullable-reference annotations. Trace all usages throughout the codebase to determine proper nullability. Do not default to the safe choice of making everything nullable.
- Convert `Get`- and `Set`-style methods into C# properties. Use auto-properties where it makes sense. i.e. `string GetLanguage();` would become `string Language { get; }`
- For one-line methods and property getters, use expression bodies, i.e. `public int Count => field.Count;`. If the expression or method/property signature is long, move the arrow to the next line, indented 4 spaces from the line above it.
- Rename `size()` methods to be `Count` if it can be converted to a property and represents the length/size of something.
- Place `: this(...)` and `: base(...)` constructor calls on a new line
- Chop very long lines (> 120 chars wide)

## Unit Test Porting

Tests are ported from a local clone of the upstream Apache OpenNLP repository, at
the targeted version. The conversion rules above apply to tests as well, plus:

- Mirror the upstream path: `opennlp-tools/src/test/java/opennlp/tools/util/SpanTest.java`
  becomes `src/NOpenNLP.Tools.Tests/Util/SpanTest.cs` in namespace `NOpenNLP.Tools.Util`.
- Keep the upstream test methods, their order, and their assertions. Do not add,
  merge, split, or "improve" test cases; a ported test should fail if and only if
  the upstream one would.
- Name methods with C# casing: `testIsAllLetters` becomes `TestIsAllLetters`.
- Use NUnit with `[Test]`. Prefer `ClassicAssert` (from `NUnit.Framework.Legacy`)
  since it maps 1:1 onto JUnit's `Assert.assertX`, keeping ported tests comparable
  to upstream. Use the constraint model (`Assert.That`) only where `ClassicAssert`
  cannot express the assertion, and note why with a `// NOpenNLP:` comment.
- JUnit-to-NUnit mappings:
  - `@Test(expected = FooException.class)` becomes `Assert.Throws<FooException>(...)`
    in the test body. Map the exception to its .NET counterpart, i.e.
    `NullPointerException` to `ArgumentNullException`, `NumberFormatException` to
    `FormatException`, `IOException` to `IOException`.
  - `@Before`/`@After` become `[SetUp]`/`[TearDown]`; `@BeforeClass`/`@AfterClass`
    become `[OneTimeSetUp]`/`[OneTimeTearDown]`.
  - `@Ignore` becomes `[Ignore("reason")]`.
  - `assertArrayEquals` becomes `CollectionAssert.AreEqual`, or
    `Assert.That(actual, Is.EqualTo(expected).Within(delta))` for floating-point arrays.
- When a test must deviate (different exception type, an assertion with no direct
  equivalent, a Java-specific behavior), keep the test and add a `// NOpenNLP:`
  comment explaining what upstream did and why the port differs.
- Skip tests whose dependencies are not yet ported (training, `ObjectStream`
  sample streams, evaluation) rather than stubbing out the missing pieces.
- Test resources come from `opennlp-tools/src/test/resources` upstream. Add them as
  `EmbeddedResource` under a path mirroring upstream, and load them through a
  shared helper rather than re-implementing lookup per test.
- The test project targets `net10.0` by default; CI sets `TestFrameworks=true` to
  also run `net9.0` (against `netstandard2.0`) and `net8.0`. Ported tests must
  compile and pass on all three.
- Tests covering defects specific to the port, which upstream does not cover, go
  in `Support/PortRegressionTest.cs` rather than alongside the ported tests.
- Mark any test that does not exist in Apache OpenNLP with `[NOpenNLPSpecific]`
  (`Support/NOpenNLPSpecificAttribute.cs`), which puts it in the `NOPENNLP`
  category. This keeps the ported suite separable from what the port added:
  `--filter TestCategory!=NOPENNLP` should leave only tests with an upstream
  counterpart. Apply it to the class when every test in the file is
  port-specific, or to the individual `[Test]` when a mostly-ported file gains
  one. It marks *added* tests, not deviations — a ported test that differs from
  upstream keeps its `// NOpenNLP:` comment and no attribute.

## Benchmarks

Two projects measure the port against the Apache OpenNLP release it targets.
They answer different questions and both need to stay current as the port grows:

- `src/NOpenNLP.Benchmarks` — BenchmarkDotNet. Runs NOpenNLP and the real Java
  OpenNLP in one .NET process, the latter cross-compiled from the Maven Central
  jar by IKVM via `MavenReference`. This is the comparison a .NET caller
  choosing between the two actually faces.
- `src/java/opennlp-benchmarks` — JMH, Java OpenNLP on a real JVM. IKVM adds
  overhead of its own, so the in-process numbers flatter the port; these are the
  honest baseline. Treat a large win over IKVM as unproven until the JMH number
  confirms it.

When adding coverage:

- **Port a tool, then benchmark it.** Anything with a public inference entry
  point belongs here. Add the case to both projects in the same change; a C#
  benchmark with no JMH counterpart cannot be checked against real Java.
- **Mirror the two sides exactly.** `BenchmarkData.cs` and `BenchmarkData.java`
  are line-for-line counterparts — same models, same sample text, same tokens
  and tags. Change them together. The same goes for the benchmark classes:
  `FooBenchmarks.cs` pairs with `FooBenchmark.java`, measuring the same call.
- **Reuse the shared sample text and the downloaded models.** Both projects read
  `testdata/models-sf/` via `NOPENNLP_TEST_DATA_DIR`, populated by
  `build/download-test-models.ps1`, and run on the same Penn Treebank WSJ
  sentences the integration tests use, so a benchmark result and a test failure
  can be read against each other. Add a model to the download script rather than
  fetching one in a benchmark.
- **Fail, do not skip, on a missing model.** The tests report inconclusive
  because a developer who has not run the download script has broken nothing.
  A benchmark that skipped would print an empty summary that reads like a
  successful run.
- **One `[BenchmarkCategory]` per comparison, each with its own
  `Baseline = true`,** plus `[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]`
  on the class. With a single baseline per class, BenchmarkDotNet divides every
  row by it, so unrelated algorithms get reported as a ratio against each other.
- **Keep `[GlobalSetup]` out of the measurement.** Load models there, not in the
  benchmark body, unless loading is the thing being measured — which
  `ModelLoadingBenchmarks` does separately, because it is a distinct code path
  and the cost every short-lived process pays.
- **Avoid `[IterationSetup]` / `[IterationCleanup]` on microbenchmarks.** They
  force BenchmarkDotNet to `InvocationCount=1, UnrollFactor=1`, so one call is
  timed per iteration and the result is swamped by timer and setup overhead. The
  name finder benchmark originally cleared adaptive state that way and reported
  ~390 us/op against a true cost near 36 us/op — a fake 13x gap against real
  Java. If a benchmark's number looks anomalous next to its neighbours, check
  the `InvocationCount` in the run log before believing it.
- **Handle per-document state identically on both sides, and prefer handling it
  in the input.** `NameFinderME` accumulates adaptive state, but every
  invocation replays the same sentence, so the state saturates after the first
  call and clearing changes nothing measurable. Neither harness clears it now.
  State handling that differs between the two makes the numbers incomparable.
- **Sanity-check a surprising result against a plain stopwatch loop** before
  reporting it as a finding. A twenty-line console app that calls the same
  method in a loop settles whether a number is the code or the harness, and it
  is the fastest way to tell those apart.
- **Do not add these to CI.** The IKVM dependency unpacks to over 4 GB, and
  benchmark numbers from a shared runner are too noisy to gate on. CI restores
  the library and test projects individually, not the solution — keep it that
  way, and keep the NuGet cache key narrowed to those two projects.
- **The JMH build targets Java 11 bytecode** so the jar runs on 11 through the
  current LTS, rather than requiring whatever JDK is installed. It also names
  the JMH annotation processor explicitly: JDK 23 turned implicit annotation
  processing off, and without that the build silently produces a jar with no
  harness that fails only at run time.
