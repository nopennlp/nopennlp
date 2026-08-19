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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NOpenNLP.Tools.Chunker;
using NOpenNLP.Tools.Ml.Maxent;
using NOpenNLP.Tools.Ml.Maxent.Io;
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Ml.Naivebayes;
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Postag;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Featuregen;
using NOpenNLP.Tools.Util.Model;
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

    /// <summary>
    /// Java's Properties.store leaves the stream open, the same as its load. The
    /// port wrapped the stream in a StreamWriter without leaveOpen, so storing
    /// closed the caller's stream.
    /// </summary>
    /// <remarks>
    /// This is the write-side counterpart of
    /// <see cref="TestPropertiesLoadLeavesStreamOpen"/>, which was fixed first.
    /// TrainingParameters.Serialize writes through this method, and the model
    /// serializers write several artifacts into a single zip stream in sequence,
    /// so a closing writer truncates everything written after the first one.
    /// </remarks>
    [Test]
    public void TestPropertiesStoreLeavesStreamOpen()
    {
        using var stream = new MemoryStream();

        var properties = new Properties();
        properties["key"] = "value";
        properties.Store(stream, null);

        ClassicAssert.IsTrue(stream.CanWrite, "Properties.Store must not close the caller's stream.");

        // The entry is readable back off the same stream, which a closed writer
        // would have prevented.
        stream.Position = 0;
        var roundTripped = new Properties();
        roundTripped.Load(stream);
        ClassicAssert.AreEqual("value", roundTripped.GetProperty("key"));
    }

    /// <summary>
    /// Java's Properties.store writes ISO-8859-1 and escapes everything outside it as
    /// \uXXXX, so the file is ASCII in practice. Writing raw UTF-8 instead produced a
    /// manifest that Java's Properties.load decodes as ISO-8859-1 and turns into
    /// mojibake. The expected bytes here are what a real JVM produced for the same
    /// input, and Java reads them back to the original string.
    /// </summary>
    [Test]
    public void TestPropertiesStoreEscapesLikeJava()
    {
        using var stream = new MemoryStream();

        var properties = new Properties();
        properties.SetProperty("Unicode", "caf\u00e9 \u4e2d\u6587");
        properties.SetProperty("Weird", "value with = and : sep");
        properties.Store(stream, null);

        // Latin-1 rather than UTF-8, because the file must be ASCII-only by now.
        string written = Encoding.GetEncoding(28591).GetString(stream.ToArray());

        StringAssert.Contains("Unicode=caf\\u00E9 \\u4E2D\\u6587", written);
        StringAssert.Contains("Weird=value with \\= and \\: sep", written);
    }

    /// <summary>
    /// The escaping above has to survive a round-trip, so Load unescapes what Store
    /// wrote and, in the other direction, reads a manifest Java produced.
    /// </summary>
    [Test]
    public void TestPropertiesRoundTripsEscapedValues()
    {
        using var stream = new MemoryStream();

        var properties = new Properties();
        properties.SetProperty("Unicode", "caf\u00e9 \u4e2d\u6587");
        properties.SetProperty("Weird", "value with = and : sep");
        properties.Store(stream, null);

        stream.Position = 0;
        var roundTripped = new Properties();
        roundTripped.Load(stream);

        ClassicAssert.AreEqual("caf\u00e9 \u4e2d\u6587", roundTripped.GetProperty("Unicode"));
        ClassicAssert.AreEqual("value with = and : sep", roundTripped.GetProperty("Weird"));
    }

    /// <summary>
    /// The exact bytes java.util.Properties.store produced for the same entries,
    /// captured from a JVM, must load back to the original strings.
    /// </summary>
    [Test]
    public void TestPropertiesLoadsJavaWrittenEscapes()
    {
        const string javaWritten =
            "#\n" +
            "#Tue Aug 18 08:50:58 MDT 2026\n" +
            "Language=en\n" +
            "Unicode=caf\\u00E9 \\u4E2D\\u6587\n" +
            "Weird=value with \\= and \\: sep\n";

        using var stream = new MemoryStream(Encoding.GetEncoding(28591).GetBytes(javaWritten));

        var properties = new Properties();
        properties.Load(stream);

        ClassicAssert.AreEqual("en", properties.GetProperty("Language"));
        ClassicAssert.AreEqual("caf\u00e9 \u4e2d\u6587", properties.GetProperty("Unicode"));
        ClassicAssert.AreEqual("value with = and : sep", properties.GetProperty("Weird"));
    }

    /// <summary>
    /// TrainingParameters.Serialize must leave the caller's stream open, since
    /// Java's Properties.store does.
    /// </summary>
    [Test]
    public void TestTrainingParametersSerializeLeavesStreamOpen()
    {
        using var stream = new MemoryStream();

        TrainingParameters parameters = TrainingParameters.DefaultParams();
        parameters.Serialize(stream);

        ClassicAssert.IsTrue(stream.CanWrite,
            "TrainingParameters.Serialize must not close the caller's stream.");

        stream.Position = 0;
        TrainingParameters roundTripped = new(stream);

        ClassicAssert.AreEqual("MAXENT", roundTripped.Algorithm());
        ClassicAssert.AreEqual(100, roundTripped.GetIntParameter(TrainingParameters.ITERATIONS_PARAM, -1));
        ClassicAssert.AreEqual(5, roundTripped.GetIntParameter(TrainingParameters.CUTOFF_PARAM, -1));
    }

    /// <summary>
    /// The deprecated <see cref="TrainingParameters"/> string-map constructor infers
    /// each value's type by trying Integer.parseInt, then Double.parseDouble, then
    /// boolean, then string. The branch that claims a value decides the type stored,
    /// which in turn decides how the value is rendered back into a model manifest.
    /// </summary>
    /// <remarks>
    /// Java's Integer.parseInt does not skip surrounding whitespace, so " 100 "
    /// falls through to the double branch and renders as "100.0". The port first
    /// used NumberStyles.Integer, which allows whitespace, so it stored an int and
    /// rendered "100" -- and GetDoubleParameter then threw where Java returned 100.0.
    /// Verified against Integer.parseInt/Double.parseDouble on a JVM.
    /// </remarks>
    [Test]
#pragma warning disable CS0618 // Type or member is obsolete
    public void TestTrainingParametersInfersJavaTypesForNumericStrings()
    {
        TrainingParameters tp = new(new Dictionary<string, string>
        {
            ["plainInt"] = "100",
            ["spacedInt"] = " 100 ",
            ["suffixed"] = "1d",
            ["hexFloat"] = "0x1.8p1",
            ["notANumber"] = "1x",
        });

        IDictionary<string, string> settings = tp.GetSettings();

        // Parsed as an int, rendered without a decimal point.
        ClassicAssert.AreEqual("100", settings["plainInt"]);

        // Integer.parseInt rejects the whitespace, so this is a double in Java.
        ClassicAssert.AreEqual("100.0", settings["spacedInt"]);
        ClassicAssert.AreEqual(100.0d, tp.GetDoubleParameter("spacedInt", -1), 0.001);

        // Double.parseDouble accepts a "d"/"f" type suffix and hex-float notation,
        // neither of which NumberStyles.Float allows.
        ClassicAssert.AreEqual("1.0", settings["suffixed"]);
        ClassicAssert.AreEqual("3.0", settings["hexFloat"]);

        // Anything both parses reject stays a string.
        ClassicAssert.AreEqual("1x", settings["notANumber"]);
    }
#pragma warning restore CS0618 // Type or member is obsolete

    /// <summary>
    /// <see cref="PlainTextFileDataReader.ReadDouble"/> parsed with the current
    /// culture. Under a locale whose decimal separator is ',' the '.' in a model
    /// value is read as a group separator, so "-0.6931471805599453" loaded as
    /// -6931471805599453 rather than throwing: a model read on such a machine
    /// silently produced garbage predictions.
    /// </summary>
    [Test]
    public void TestPlainTextDataReaderIsCultureInvariant()
    {
        RunUnderCulture("de-DE", () =>
        {
            using var input = new MemoryStream(
                Encoding.UTF8.GetBytes("-0.6931471805599453\n1.0E-5\n-42\n"));
            var reader = new PlainTextFileDataReader(input);

            ClassicAssert.AreEqual(-0.6931471805599453d, reader.ReadDouble(), 1e-15);
            ClassicAssert.AreEqual(1.0E-5d, reader.ReadDouble(), 1e-20);
            ClassicAssert.AreEqual(-42, reader.ReadInt32());
        });
    }

    /// <summary>
    /// <see cref="Event.ToString"/> appended its float values with the current
    /// culture and .NET's own format, yielding "0,5" under de-DE and "1E-05"
    /// where Java writes "1.0E-5". <see cref="HashSumEventStream"/> hashes this
    /// string and <see cref="TwoPassDataIndexer"/> compares the hash across its
    /// two passes, so the text has to match Java's exactly.
    /// </summary>
    [Test]
    public void TestEventToStringUsesJavaFloatFormat()
    {
        RunUnderCulture("de-DE", () =>
        {
            var ev = new Event("outcome", ["a", "b", "c"], [0.5f, 1e-5f, 2f]);
            ClassicAssert.AreEqual("outcome [a=0.5 b=1.0E-5 c=2.0]", ev.ToString());
        });
    }

    /// <summary>
    /// The model writers group runs of equal-comparing predicates and write one
    /// entry per run, so the sort feeding that grouping has to be stable the way
    /// Java's <c>Arrays.sort(Object[])</c> is. <see cref="List{T}.Sort()"/> is an
    /// unstable introsort and reorders names within a run, which changes the
    /// bytes of the model file.
    /// </summary>
    [Test]
    public void TestArraysSortIsStable()
    {
        // NOpenNLP: this needs to be well over 16 elements. List<T>.Sort switches
        // to insertion sort for short runs, which happens to be stable, so a small
        // array does not distinguish an unstable introsort from a stable merge.
        // Two outcome patterns interleave so there are long runs of equal
        // elements and the introsort's partitioning actually reorders them.
        const int count = 200;
        var preds = new ComparablePredicate[count];
        for (int i = 0; i < count; i++)
        {
            int[] pattern = i % 2 == 0 ? [0, 1] : [0, 1, 2];
            preds[i] = new ComparablePredicate(
                i.ToString(CultureInfo.InvariantCulture), pattern, [i]);
        }

        // Only the relative order within each pattern group is preserved by a
        // stable sort; the groups themselves move.
        string[] expected = preds
            .OrderBy(p => p.Outcomes.Length) // LINQ OrderBy is documented stable
            .Select(p => p.Name)
            .ToArray();

        Arrays.Sort(preds);

        CollectionAssert.AreEqual(expected, preds.Select(p => p.Name).ToArray());
    }

    /// <summary>
    /// <c>Float.compare(-0.0f, 0.0f)</c> is -1 in Java but
    /// <c>(-0.0f).CompareTo(0.0f)</c> is 0 in .NET, so
    /// <see cref="ComparableEvent.CompareTo"/> has to use J2N rather than the
    /// built-in comparison to order events the way upstream does.
    /// </summary>
    [Test]
    public void TestComparableEventOrdersNegativeZeroLikeJava()
    {
        var negativeZero = new ComparableEvent(0, [1], [-0.0f]);
        var positiveZero = new ComparableEvent(0, [1], [0.0f]);

        ClassicAssert.IsTrue(negativeZero.CompareTo(positiveZero) < 0);
        ClassicAssert.IsTrue(positiveZero.CompareTo(negativeZero) > 0);
    }

    /// <summary>
    /// <see cref="AbstractModelWriter.Persist"/> ends by closing the stream, so a
    /// writer used in a <c>using</c> block closed it a second time on the way out.
    /// Java's <c>OutputStream.close()</c> is a no-op when already closed; .NET
    /// throws <see cref="ObjectDisposedException"/>.
    /// </summary>
    [Test]
    public void TestModelWriterCanBeDisposedAfterPersist()
    {
        var model = BuildModel();

        // NOpenNLP: this needs a stream that rejects use after disposal.
        // MemoryStream tolerates a second Flush, so it would not detect the
        // double close; FileStream throws ObjectDisposedException, which is what
        // a caller writing a model to disk would actually hit.
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        try
        {
            // The using block disposes the writer after Persist has already closed it.
            using (var writer = new BinaryGISModelWriter(model, new FileInfo(path)))
            {
                writer.Persist();
            }

            ClassicAssert.Greater(new FileInfo(path).Length, 0);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// The binary writers must reproduce Java's <c>DataOutputStream</c> layout, so
    /// a model round-trips through the matching reader unchanged.
    /// </summary>
    [Test]
    public void TestBinaryModelRoundTrips()
    {
        var model = BuildModel();
        using var output = new MemoryStream();

        using (var writer = new BinaryGISModelWriter(model, new UncloseableOutputStream(output)))
        {
            writer.Persist();
        }

        output.Position = 0;
        var reloaded = new GISModelReader(new BinaryFileDataReader(output)).Model;

        ClassicAssert.AreEqual(model.NumOutcomes, reloaded.NumOutcomes);
        CollectionAssert.AreEqual(
            model.Eval(["pred_a", "shared_x"]),
            reloaded.Eval(["pred_a", "shared_x"]));
    }

    private static AbstractModel BuildModel()
    {
        string[] predLabels = ["pred_a", "pred_b", "shared_x"];
        string[] outcomeLabels = ["other", "org-start", "org-cont"];
        Context[] parameters =
        [
            new Context([0, 1], [0.5, -0.25]),
            new Context([0, 1, 2], [1.5, 2.5, -3.5]),
            new Context([0, 1], [7.0, 8.0]),
        ];

        return new GISModel(parameters, predLabels, outcomeLabels);
    }

    // NOpenNLP: several fixes here are about culture-sensitive formatting and
    // parsing, which only misbehave when the current culture is not invariant.
    private static void RunUnderCulture(string culture, Action action)
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);
            action();
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
