# Porting notes

This documentation is generated from the ported C# source, so what it describes
is what NOpenNLP does rather than what Apache OpenNLP does. The two agree
almost everywhere; where they do not, the reason is usually one of the
following.

## Naming

Types and members keep their upstream OpenNLP names with .NET casing applied, so
Java `TokenizerME.tokenize` becomes
<xref:NOpenNLP.Tools.Tokenize.TokenizerME>.`Tokenize`.
Java interfaces gain an `I` prefix: `Tokenizer` becomes
<xref:NOpenNLP.Tools.Tokenize.ITokenizer>. Getters and setters become
properties, and a `size()` method that reports a length becomes a `Count`
property.

Namespaces mirror the Java packages: `opennlp.tools.util.model` becomes
`NOpenNLP.Tools.Util.Model`. The command line tools stay in
`NOpenNLP.Tools.Cmdline`, matching upstream's package, even though they ship in
the separate `NOpenNLP.Cli` package.

## Deviations from upstream

Anywhere the port had to depart from the Java, the source carries a comment
marked `NOpenNLP:` or `NOpenNLP-specific:` explaining what changed and why.
Those comments are the authoritative record; this page does not restate them.

Java `Iterator<T>` is ported as `IEnumerable<T>` rather than `IEnumerator<T>`,
so a stream reads with `foreach` and callers do not have to dispose an
enumerator themselves.

## Model compatibility

Models are portable in both directions. A model NOpenNLP trains loads in Apache
OpenNLP 1.9.5 on a real JVM, and one Apache OpenNLP trains loads in NOpenNLP,
each side reproducing the other's inference output including the probabilities
behind it. That is checked by the harness under
[`src/java/model-compat`](https://github.com/nopennlp/nopennlp/tree/main/src/java/model-compat),
which is the only coverage that can catch a serialization difference: both
halves of a .NET-only round trip would share any bug in it.

## Stemmers

Apache OpenNLP generates its Snowball stemmers with the Snowball compiler and
commits the output; this port does the same with that compiler's C# backend,
pinned to Snowball 2.0.0, rather than translating the generated Java by hand.

Measured over the full Snowball vocabularies, 17 of the 21 languages produce
output identical to OpenNLP 1.9.5. Finnish, Hungarian, French and Indonesian
differ, because OpenNLP ships generated code predating several upstream fixes.
In each case the port has the corrected behavior. The measurements and the
individual cases are documented in `build/generate-snowball-stemmers.ps1`.
