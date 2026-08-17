/*
 * Copyright 2026 NOpenNLP Contributors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System.IO;
using System.Text;
using NOpenNLP.Tools.Ml.Naivebayes;
using NOpenNLP.Tools.Chunker;
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Postag;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Featuregen;
using NOpenNLP.Tools.Util.Normalizer;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Version = NOpenNLP.Tools.Util.Version;

namespace NOpenNLP.Tools.Support;

/// <summary>
/// Regression tests for defects specific to the .NET port, which the upstream
/// Apache OpenNLP test suite does not cover.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source. Each test here
/// fails against the pre-fix code, unlike the ported upstream tests, which pass
/// either way.
/// </remarks>
[NOpenNLPSpecific]
public class PortRegressionTest
{
    /// <summary>
    /// The embedded opennlp.version resource separates its key and value with
    /// ':'. A loader that only split on '=' skipped the entry silently and
    /// reported the fallback development version.
    /// </summary>
    /// <remarks>
    /// Upstream's VersionTest only round-trips CurrentVersion() through Parse(),
    /// so it passes even when the resource fails to load. This pins the value.
    /// </remarks>
    [Test]
    public void TestCurrentVersionIsReadFromEmbeddedResource()
    {
        Version current = Version.CurrentVersion();

        ClassicAssert.AreEqual(1, current.Major);
        ClassicAssert.AreEqual(9, current.Minor);
        ClassicAssert.AreEqual(4, current.Revision);
        ClassicAssert.IsFalse(current.IsSnapshot);
        ClassicAssert.AreEqual("1.9.4", current.ToString());
    }

    /// <summary>
    /// Properties.Load must accept '=', ':' and whitespace as separators, and
    /// treat both '#' and '!' as comment markers, as java.util.Properties does.
    /// </summary>
    [Test]
    public void TestPropertiesAcceptsJavaSeparators()
    {
        var properties = new Properties();
        using (var stream = new System.IO.MemoryStream([.. "# a comment\n! another comment\nequals=1\ncolon: 2\nspace 3\n"u8]))
        {
            properties.Load(stream);
        }

        ClassicAssert.AreEqual("1", properties.GetProperty("equals"));
        ClassicAssert.AreEqual("2", properties.GetProperty("colon"));
        ClassicAssert.AreEqual("3", properties.GetProperty("space"));
        ClassicAssert.AreEqual(3, properties.Count);
    }

    /// <summary>
    /// Sequence declared GetHashCode/Equals as new virtual members rather than
    /// overrides, so it fell back to reference equality and its list fields were
    /// compared by reference.
    /// </summary>
    [Test]
    public void TestSequenceUsesValueEquality()
    {
        Sequence first = new Sequence();
        first.Add("a", 0.5);
        first.Add("b", 0.25);

        Sequence second = new Sequence();
        second.Add("a", 0.5);
        second.Add("b", 0.25);

        ClassicAssert.AreEqual(first, second);
        ClassicAssert.AreEqual(first.GetHashCode(), second.GetHashCode());

        // Reached through an object reference, which is what the missing
        // override actually broke.
        object boxed = second;
        ClassicAssert.IsTrue(first.Equals(boxed));
    }

    /// <summary>
    /// LogProbabilities declared its members as new virtual methods rather than
    /// overrides. Calls through a Probabilities-typed reference — which is how
    /// NaiveBayesModel holds it — therefore ran the base implementation's
    /// linear-space arithmetic instead of the log-space overrides.
    /// </summary>
    [Test]
    public void TestLogProbabilitiesDispatchesThroughBaseReference()
    {
        // Declared as the base type, exactly as NaiveBayesModel declares it.
        Probabilities<string> probabilities = new LogProbabilities<string>();
        probabilities.Set("a", 0.5d);

        // The log-space override stores the logarithm of the value it is given.
        // The base implementation would store 0.5 and return log(0.5) only after
        // taking a logarithm of its own, so this pins which one ran.
        ClassicAssert.AreEqual(System.Math.Log(0.5d), probabilities.GetLog("a"), 1e-12);

        // An absent key is negative infinity in log space, not an exception and
        // not the base class's behavior.
        ClassicAssert.AreEqual(double.NegativeInfinity, probabilities.GetLog("missing"), 0d);

        // Two labels, so normalization is meaningful: log space must still yield
        // a correctly normalized linear probability through the base reference.
        Probabilities<string> pair = new LogProbabilities<string>();
        pair.Set("x", 0.25d);
        pair.Set("y", 0.75d);
        ClassicAssert.AreEqual(0.25d, pair.Get("x").Value, 1e-12);
        ClassicAssert.AreEqual(0.75d, pair.Get("y").Value, 1e-12);
    }

    /// <summary>
    /// The default feature descriptors were missing from the port entirely, and
    /// the code loading them passed a Java classpath path
    /// ("/opennlp/tools/namefind/ner-default-features.xml") to J2N, which resolves
    /// a bare file name relative to the requesting type's namespace and returns
    /// null for a slash path. Either fault alone makes the default feature
    /// generators unreachable.
    /// </summary>
    /// <remarks>
    /// Upstream covers this only indirectly through training tests, which are not
    /// ported, so nothing else pins it.
    /// </remarks>
    [Test]
    public void TestTokenNameFinderFactoryLoadsDefaultFeatureDescriptor()
    {
        // No descriptor supplied, so the factory must fall back to the embedded
        // default. Before the fix this threw InvalidOperationException.
        IAdaptiveFeatureGenerator? generator = new TokenNameFinderFactory().CreateFeatureGenerators();

        ClassicAssert.IsNotNull(generator);
    }

    /// <summary>
    /// The POS default descriptor had the same two faults as the NER one, plus a
    /// third: the lookup passed typeof(TokenNameFinderFactory), which resolves
    /// against the Namefind namespace and so could never locate a Postag resource.
    /// </summary>
    [Test]
    public void TestPOSTaggerFactoryLoadsDefaultFeatureDescriptor()
    {
        // Passing null feature generator bytes forces the embedded default to load.
        POSTaggerFactory factory = new POSTaggerFactory(null, [], null);

        IAdaptiveFeatureGenerator generator = factory.CreateFeatureGenerators();

        ClassicAssert.IsNotNull(generator);
    }

    /// <summary>
    /// Runs real inference against a pre-trained model, which nothing else in the
    /// suite does. Everything that reads a model — the zip container, the maxent
    /// model reader, GISModel.Eval, the beam search, the context generator — has to
    /// be correct end to end for this to produce the right chunks.
    /// </summary>
    /// <remarks>
    /// Authored for NOpenNLP. Upstream covers inference only in tests that train
    /// their own model, or in SourceForgeModelEval, which needs models and a corpus
    /// that are not in the repository; none of those are portable. chunker170default.bin
    /// ships in the upstream test resources, so it is the one pre-trained model
    /// available to pin numerical behavior against.
    /// <para/>
    /// A defect in the maxent arithmetic or the beam search yields plausible but
    /// wrong tags rather than an exception, so the expected values below were
    /// checked to be a linguistically sensible parse, not merely recorded.
    /// </remarks>
    [Test]
    public void TestChunkerInferenceAgainstPretrainedModel()
    {
        ChunkerModel model = new ChunkerModel(
            TestResources.OpenResource("/opennlp/tools/chunker/chunker170default.bin"));
        ChunkerME chunker = new ChunkerME(model);

        // A Penn-Treebank style sentence, of the kind this model was trained on.
        string[] tokens =
        [
            "Rockwell", "said", "the", "agreement", "calls", "for", "it", "to",
            "supply", "200", "additional", "so-called", "shipsets", "for", "the",
            "planes", "."
        ];
        string[] tags =
        [
            "NNP", "VBD", "DT", "NN", "VBZ", "IN", "PRP", "TO", "VB", "CD", "JJ",
            "JJ", "NNS", "IN", "DT", "NNS", "."
        ];

        string[] chunks = chunker.Chunk(tokens, tags);

        // "Rockwell" / "said" / "the agreement" / "calls" / "for" / "it" /
        // "to supply" / "200 additional so-called shipsets" / "for" / "the planes"
        string[] expected =
        [
            "B-NP", "B-VP", "B-NP", "I-NP", "B-VP", "B-SBAR", "B-NP", "B-VP",
            "I-VP", "B-NP", "I-NP", "I-NP", "I-NP", "B-PP", "B-NP", "I-NP", "O"
        ];
        CollectionAssert.AreEqual(expected, chunks);

        // The same result expressed as spans, which exercises the BIO decoding.
        Span[] spans = chunker.ChunkAsSpans(tokens, tags);
        ClassicAssert.AreEqual(10, spans.Length);
        ClassicAssert.AreEqual(new Span(2, 4, "NP"), spans[2]);
        // The four token noun phrase, which only decodes correctly if the whole
        // B-NP/I-NP run was tagged correctly.
        ClassicAssert.AreEqual(new Span(9, 13, "NP"), spans[7]);

        // A confident decision, not a near-tie that happened to fall the right way.
        ClassicAssert.Greater(chunker.Probs()[0], 0.9d);
    }

    /// <summary>
    /// ShrinkCharSequenceNormalizer must trim as Java's String.trim() does,
    /// removing only characters &lt;= U+0020. .NET's Trim() removes all Unicode
    /// whitespace and would additionally strip a leading or trailing NBSP,
    /// changing the character ngrams the language detector is given.
    /// </summary>
    /// <remarks>
    /// Upstream's ShrinkCharSequenceNormalizerTest only uses ASCII spaces, so it
    /// passes either way.
    /// </remarks>
    [Test]
    public void TestShrinkNormalizerTrimsOnlyJavaWhitespace()
    {
        var normalizer = ShrinkCharSequenceNormalizer.GetInstance();

        // U+00A0 NBSP is whitespace to .NET but not to Java's String.trim().
        ClassicAssert.AreEqual(" hello ", normalizer.Normalize(" hello "));

        // ASCII whitespace is still trimmed, as upstream does.
        ClassicAssert.AreEqual("hello", normalizer.Normalize(" hello\t"));
    }

    /// <summary>
    /// NumberCharSequenceNormalizer must match only ASCII digits, as Java's \d
    /// does by default. .NET's \d is Unicode-aware and would also replace digits
    /// from other scripts, discarding text upstream keeps.
    /// </summary>
    /// <remarks>
    /// Upstream's NumberCharSequenceNormalizerTest only uses ASCII digits, so it
    /// passes either way.
    /// </remarks>
    [Test]
    public void TestNumberNormalizerMatchesAsciiDigitsOnly()
    {
        var normalizer = NumberCharSequenceNormalizer.GetInstance();

        // Arabic-Indic digits are \d under .NET's Unicode-aware default, but not
        // under Java's ASCII-only \d.
        ClassicAssert.AreEqual("a١٢b", normalizer.Normalize("a١٢b"));

        // ASCII digits are still replaced, as upstream does.
        ClassicAssert.AreEqual("a b", normalizer.Normalize("a12b"));
    }

    /// <summary>
    /// Java's Properties.load leaves the stream open. The port wrapped the stream
    /// in a StreamReader without leaveOpen, so loading closed the caller's stream.
    /// </summary>
    /// <remarks>
    /// PropertiesSerializer.Create is contractually required to leave the stream
    /// open, and EntityLinkerProperties documents the same, so a closing loader
    /// breaks any caller that reads further from the stream afterwards.
    /// </remarks>
    [Test]
    public void TestPropertiesLoadLeavesStreamOpen()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("key=value\n"));

        var properties = new Properties();
        properties.Load(stream);

        ClassicAssert.AreEqual("value", properties.GetProperty("key"));
        ClassicAssert.IsTrue(stream.CanRead, "Properties.Load must not close the caller's stream.");
    }
}
