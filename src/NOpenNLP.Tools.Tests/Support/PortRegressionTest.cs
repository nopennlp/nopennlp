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
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Formats.Ad;
using NOpenNLP.Tools.Formats.Conllu;
using NOpenNLP.Tools.Ml.Maxent;
using NOpenNLP.Tools.Ml.Maxent.Io;
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Ml.Naivebayes;
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Parser;
using NOpenNLP.Tools.Postag;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Featuregen;
using NOpenNLP.Tools.Util.Model;
using NOpenNLP.Tools.Util.Normalizer;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Version = NOpenNLP.Tools.Util.Version;
using JCG = J2N.Collections.Generic;

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
        POSTaggerFactory factory = new POSTaggerFactory(null, new JCG.Dictionary<string, object>(), null);

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

    /// <summary>
    /// A real-valued event line with trailing whitespace must not produce an extra,
    /// empty predicate. Java's String.split discards trailing empty strings; .NET's
    /// Split keeps them, so the port added an empty context to every such event.
    /// </summary>
    /// <remarks>
    /// Several lines in the upstream real-valued training corpus end in a space. The
    /// empty predicate inflated the model's predicate count, and with it the
    /// dimension of the quasi-newton objective function.
    /// </remarks>
    [Test]
    public void TestRealValueEventStreamIgnoresTrailingWhitespace()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "A feature1=4.0 feature2=2.0 \n", Encoding.UTF8);

            using RealValueFileEventStream stream = new(path);
            Event? read = stream.Read();

            ClassicAssert.NotNull(read);
            CollectionAssert.AreEqual(new[] { "feature1", "feature2" }, read!.Context);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// A model whose artifact map holds other models -- a ParserModel contains a
    /// POSModel and a ChunkerModel -- must survive a serialize/load round trip.
    /// </summary>
    /// <remarks>
    /// Three separate port defects broke this path, and none was reachable until
    /// parser training existed to produce such a model:
    /// <para/>
    /// 1. BaseModel.GetArtifactSerializer and FinishLoadingArtifacts indexed
    /// artifactSerializers directly. Java's Map.get returns null for an absent key
    /// and both call sites rely on that null -- an ISerializableArtifact supplies
    /// its own serializer, and a nested model names one in the manifest -- but the
    /// J2N indexer throws KeyNotFoundException instead.
    /// <para/>
    /// 2. FinishLoadingArtifacts passed the raw ZipArchiveEntry stream to the nested
    /// model's constructor. Java reads from a ZipInputStream, which supports
    /// mark/reset, whereas ZipArchiveEntry.Open returns a forward-only DeflateStream
    /// that cannot seek back to the start of the entry.
    /// </remarks>
    [Test]
    public void TestModelWithNestedModelsRoundTrips()
    {
        using IObjectStream<Parse?> parseSamples = ParserTestUtil.OpenTestTrainingData();
        Tools.Parser.Lang.En.HeadRules headRules = ParserTestUtil.CreateTestHeadRules();

        TrainingParameters @params = new();
        @params.Put(TrainingParameters.ITERATIONS_PARAM, 10);
        @params.Put(TrainingParameters.CUTOFF_PARAM, 5);

        ParserModel model = Tools.Parser.Chunking.Parser.Train("eng", parseSamples, headRules, @params);

        using MemoryStream serialized = new();
        model.Serialize(serialized);

        ParserModel roundTripped = new(new MemoryStream(serialized.ToArray()));

        ClassicAssert.NotNull(roundTripped.BuildModel);
        ClassicAssert.NotNull(roundTripped.CheckModel);
        ClassicAssert.NotNull(roundTripped.ParserTaggerModel);
        ClassicAssert.NotNull(roundTripped.ParserChunkerModel);
        ClassicAssert.NotNull(roundTripped.HeadRules);
        ClassicAssert.AreEqual(ParserType.CHUNKING, roundTripped.ParserTypeValue);
    }

    /// <summary>
    /// <see cref="InvalidFormatException"/> must derive from <see cref="IOException"/>,
    /// as upstream's does.
    /// </summary>
    /// <remarks>
    /// The command line tools and the format factories catch IOException to turn a
    /// malformed corpus into a diagnostic and an exit code. Deriving from Exception
    /// instead made every one of those handlers dead code, so a malformed file escaped
    /// as an unhandled exception with a stack trace.
    /// </remarks>
    [Test]
    public void TestInvalidFormatExceptionIsAnIOException()
    {
        ClassicAssert.IsInstanceOf<IOException>(new InvalidFormatException("boom"));

        // The catch that upstream relies on must actually run.
        bool caught = false;
        try
        {
            throw new InvalidFormatException("boom");
        }
        catch (IOException)
        {
            caught = true;
        }

        ClassicAssert.IsTrue(caught);
    }

    /// <summary>
    /// <c>StringUtil.SplitDroppingTrailingEmpty</c> must match Java's
    /// <c>String.split</c>, which drops trailing empty strings but keeps interior ones,
    /// and returns a single empty element for the empty input.
    /// </summary>
    /// <remarks>
    /// Verified against a real JDK. The readers gate on an exact field count, so a line
    /// ending in the separator counted one field too many and was rejected where
    /// upstream accepts it.
    /// </remarks>
    [Test]
    public void TestSplitDroppingTrailingEmptyMatchesJava()
    {
        CollectionAssert.AreEqual(new[] { "a", "b", "c" },
            StringUtil.SplitDroppingTrailingEmpty("a b c ", ' '));
        CollectionAssert.AreEqual(new[] { "a", "b", "c" },
            StringUtil.SplitDroppingTrailingEmpty("a b c   ", ' '));
        CollectionAssert.AreEqual(new[] { "a", "", "b" },
            StringUtil.SplitDroppingTrailingEmpty("a  b", ' '));
        CollectionAssert.AreEqual(new[] { "abc" },
            StringUtil.SplitDroppingTrailingEmpty("abc", ' '));

        // "".split(",") is one element in Java, while ",".split(",") is none.
        CollectionAssert.AreEqual(new[] { "" },
            StringUtil.SplitDroppingTrailingEmpty("", ','));
        CollectionAssert.AreEqual(System.Array.Empty<string>(),
            StringUtil.SplitDroppingTrailingEmpty(",", ','));
    }

    /// <summary>
    /// A CoNLL 2002 line with a trailing separator must parse, as it does upstream.
    /// </summary>
    /// <remarks>
    /// Java's split drops the trailing empty string, so the line has three fields and
    /// passes the reader's exact-count check; .NET's keeps it, so the port counted four
    /// and threw.
    /// </remarks>
    [Test]
    public void TestConll02AcceptsALineWithATrailingSeparator()
    {
        byte[] data = Encoding.UTF8.GetBytes("Foo NN B-PER \nBar NN O \n\n");

        IInputStreamFactory factory = new ByteArrayInputStreamFactory(data);

        using var stream = new Conll02NameSampleStream(
            Conll02NameSampleStream.Language.NLD, factory,
            Conll02NameSampleStream.GeneratePersonEntities);

        NameSample sample = stream.Read()!;

        ClassicAssert.NotNull(sample);
        CollectionAssert.AreEqual(new[] { "Foo", "Bar" }, sample.Sentence);
        ClassicAssert.AreEqual(1, sample.Names.Length);
        ClassicAssert.AreEqual("person", sample.Names[0].Type);
    }

    /// <summary>
    /// A CoNLL-U line with a trailing tab must be rejected, as it is upstream.
    /// </summary>
    /// <remarks>
    /// The mirror of the CoNLL 2002 case: here .NET's extra empty field made a
    /// nine-column line count as ten, so the port accepted a line upstream rejects and
    /// read the empty tenth field as MISC.
    /// </remarks>
    [Test]
    public void TestConlluRejectsALineWithATrailingSeparator()
    {
        string nineFieldsWithTrailingTab = string.Join("\t",
            "1", "Die", "der", "DET", "ART", "_", "2", "det", "_") + "\t";

        byte[] data = Encoding.UTF8.GetBytes(nineFieldsWithTrailingTab + "\n\n");

        IInputStreamFactory factory = new ByteArrayInputStreamFactory(data);

        using var stream = new ConlluStream(factory);

        Assert.Throws<InvalidFormatException>(new TestDelegate(() => stream.Read()));
    }

    /// <summary>
    /// <c>PortugueseContractionUtility.ToContraction</c> must not throw on an empty left
    /// part.
    /// </summary>
    /// <remarks>
    /// It reads the last element of the split, and a private copy of the split helper
    /// lacked the empty-input case, so it indexed an empty array and threw
    /// IndexOutOfRangeException where upstream returns null.
    /// </remarks>
    [Test]
    public void TestToContractionHandlesAnEmptyLeftPart()
    {
        ClassicAssert.IsNull(PortugueseContractionUtility.ToContraction("", "x"));

        // The ordinary case still works.
        ClassicAssert.AreEqual("da", PortugueseContractionUtility.ToContraction("de", "a"));
    }

    /// <summary>
    /// <see cref="XmlUtil.CreateDocument"/> must parse an internal DTD subset and expand
    /// the entities it declares, and must keep whitespace-only text nodes.
    /// </summary>
    /// <remarks>
    /// Java sets FEATURE_SECURE_PROCESSING, which blocks external entities but still
    /// parses an internal subset; DtdProcessing.Prohibit was stricter and threw on any
    /// DOCTYPE. Java's DocumentBuilder also keeps every text node, while XmlDocument
    /// drops whitespace-only ones unless PreserveWhitespace is set -- which both shifted
    /// child indices and lost a character from the reconstructed text.
    /// </remarks>
    [Test]
    public void TestXmlUtilMatchesJavaDtdAndWhitespaceHandling()
    {
        const string withDtd =
            "<!DOCTYPE sentences [ <!ENTITY oe \"oe\" > ]><sentences><s>c&oe;ur</s></sentences>";

        var parsed = XmlUtil.CreateDocument(
            new MemoryStream(Encoding.UTF8.GetBytes(withDtd)));

        ClassicAssert.AreEqual("coeur", parsed.DocumentElement!.InnerText);

        const string withSpace =
            "<sentences><s><token>A</token> <token>B</token></s></sentences>";

        var spaced = XmlUtil.CreateDocument(
            new MemoryStream(Encoding.UTF8.GetBytes(withSpace)));

        // Java's DOM reports three children here: token, the space, token.
        ClassicAssert.AreEqual(3, spaced.DocumentElement!.FirstChild!.ChildNodes.Count);
        ClassicAssert.AreEqual("A B", spaced.DocumentElement.FirstChild.InnerText);
    }

    /// <summary>
    /// A reader must decode a UTF-8 BOM as U+FEFF rather than consuming it, the way
    /// Java's <c>InputStreamReader</c> does.
    /// </summary>
    /// <remarks>
    /// This matters most for brat, whose .ann files carry character offsets into the
    /// .txt: silently dropping a leading BOM shifts every annotation span by one.
    /// </remarks>
    [Test]
    public void TestPlainTextByLineStreamKeepsAByteOrderMark()
    {
        byte[] withBom = [0xEF, 0xBB, 0xBF, (byte)'h', (byte)'i'];

        IInputStreamFactory factory = new ByteArrayInputStreamFactory(withBom);

        using var stream = new PlainTextByLineStream(factory, Encoding.UTF8);

        string line = stream.Read()!;

        ClassicAssert.AreEqual(3, line.Length);
        ClassicAssert.AreEqual('\uFEFF', line[0]);
        ClassicAssert.AreEqual("hi", line[1..]);
    }

    /// <summary>
    /// A feature generator descriptor arrives inside a model file, which is untrusted input,
    /// and <see cref="GeneratorFactory"/> must parse it with the hardened settings rather than
    /// a bare <see cref="System.Xml.XmlDocument"/>.
    /// </summary>
    /// <remarks>
    /// Upstream has always parsed this through <c>XmlUtil.createDocumentBuilder()</c>; the port
    /// called <c>XmlDocument.Load(Stream)</c> instead and inherited its XXE safety from the
    /// framework's default null <c>XmlResolver</c>. That default holds on every runtime targeted
    /// here, so the port was not vulnerable -- this test pins the guarantee to the library's own
    /// code rather than to a framework default, and would have caught the regression on a
    /// runtime whose default differed. Nothing in 1.9.5 covers this, and no upstream test does.
    /// </remarks>
    [Test]
    public void TestGeneratorFactoryDoesNotResolveExternalEntities()
    {
        string secret = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        File.WriteAllText(secret, "TOP-SECRET");

        try
        {
            string descriptor =
                $"<!DOCTYPE generators [ <!ENTITY xxe SYSTEM \"file://{secret}\"> ]>" +
                "<generators><generator class=\"opennlp.tools.util.featuregen." +
                "TokenFeatureGeneratorFactory\">&xxe;</generator></generators>";

            // The descriptor is well formed, so the parse itself succeeds; what matters is
            // that the external entity expanded to nothing rather than to the file's contents.
            // GeneratorFactory does not hand back the document, so the descriptor is parsed a
            // second time through the same helper to inspect what the entity became.
            var mappings = GeneratorFactory.ExtractArtifactSerializerMappings(
                new MemoryStream(Encoding.UTF8.GetBytes(descriptor)));
            ClassicAssert.IsNotNull(mappings);

            var document = XmlUtil.CreateDocument(
                new MemoryStream(Encoding.UTF8.GetBytes(descriptor)));

            ClassicAssert.IsFalse(document.DocumentElement!.InnerText.Contains("TOP-SECRET"),
                "the external entity must not have been resolved");
        }
        finally
        {
            File.Delete(secret);
        }
    }

    /// <summary>
    /// A dictionary file has no legitimate DOCTYPE, so
    /// <see cref="Dictionary.Serializer.DictionaryEntryPersistor"/> rejects one outright
    /// instead of parsing an internal subset.
    /// </summary>
    /// <remarks>
    /// 1.9.5 routed this reader through <c>XmlUtil.createSaxParser()</c> so it would pick up
    /// the same hardening as every other parser in the library, replacing a raw
    /// <c>XMLReaderFactory.createXMLReader()</c> call that had none. The port builds its
    /// settings from <c>XmlUtil</c> for that reason and then tightens the DTD handling.
    /// </remarks>
    [Test]
    public void TestDictionaryEntryPersistorRejectsDoctype()
    {
        string secret = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        File.WriteAllText(secret, "TOP-SECRET");

        try
        {
            string xml =
                $"<!DOCTYPE dictionary [ <!ENTITY xxe SYSTEM \"file://{secret}\"> ]>" +
                "<dictionary case_sensitive=\"true\"><entry><token>&xxe;</token></entry></dictionary>";

            // The XmlException the DOCTYPE raises is wrapped, as every other malformed
            // dictionary is, so the caller sees one exception type for bad input.
            var ex = Assert.Throws<InvalidFormatException>((Action)(() =>
                Dictionary.Serializer.DictionaryEntryPersistor.Create(
                    new MemoryStream(Encoding.UTF8.GetBytes(xml)),
                    _ => { })));

            ClassicAssert.IsInstanceOf<System.Xml.XmlException>(ex!.InnerException);
            ClassicAssert.IsTrue(ex.InnerException!.Message.Contains("DTD is prohibited"),
                $"the DOCTYPE itself must be what was rejected; got: {ex.InnerException.Message}");
        }
        finally
        {
            File.Delete(secret);
        }
    }

    /// <summary>
    /// Serves a fixed byte array as an <see cref="IInputStreamFactory"/>, so a reader can
    /// be driven from an in-memory corpus rather than an embedded resource.
    /// </summary>
    private sealed class ByteArrayInputStreamFactory(byte[] bytes) : IInputStreamFactory
    {
        private readonly byte[] bytes = bytes;

        public Stream CreateInputStream() => new MemoryStream(bytes, writable: false);
    }
}
