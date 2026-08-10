# Claude Code instructions for NOpenNLP

The goal of this project is to create a faithful .NET port of Apache OpenNLP.

The current version of OpenNLP being targeted is: 1.9.1

## C# Conversion

When converting the original Java code to C#, take care to:

- Add proper license attribution (see other files for an example)
- Keep code in the same order, file structure, etc. as upstream as much as possible. This eases porting future updates.
- Projects match the Java names with the following transformation: replace hyphen with period, `opennlp` with `NOpenNLP`, and use .NET-style casing. i.e. `opennlp-tools` becomes `NOpenNLP.Tools`
- Namespaces should match their file structure and the upstream Java package, i.e. `src/NOpenNLP.Tools/Util/Model/BaseModel.cs` should be in `NOpenNLP.Tools.Util.Model`.
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
- Use J2N collection types internally (i.e. List, Dictionary, etc) but do not expose them in the public API. Expose the .NET BCL equivalent interfaces they implement (i.e. `IList<T>`)
- When private fields are not mutated, and not already marked `final` in Java, make them `readonly` with a comment: `// NOpenNLP: made readonly`
- When deviating from upstream OpenNLP, add a brief comment like `// NOpenNLP-specific: {justification of change}`
- Try to make ported code nullable-friendly with nullable-reference annotations. Trace all usages throughout the codebase to determine proper nullability. Do not default to the safe choice of making everything nullable.
- Convert `Get`- and `Set`-style methods into C# properties. Use auto-properties where it makes sense. i.e. `string GetLanguage();` would become `string Language { get; }`
- Place `: this(...)` and `: base(...)` constructor calls on a new line
- Chop very long lines (> 120 chars wide)
