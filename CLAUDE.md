# Claude Code instructions for NOpenNLP

The goal of this project is to create a faithful .NET port of Apache OpenNLP.

The current version of OpenNLP being targeted is: 1.9.1

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
