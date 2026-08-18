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
using System.Linq;
using System.Text;
using NOpenNLP.Tools.Dictionary.Serializer;
using NOpenNLP.Tools.Ngram;
using NOpenNLP.Tools.Postag;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Featuregen;
using NOpenNLP.Tools.Util.Model;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Support;

/// <summary>
/// Round-trips each artifact type that has a serializer through write and read,
/// asserting the result equals what went in.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP; not part of the Apache OpenNLP source. Upstream covers
/// the write path only indirectly, through training tests that persist a model and
/// load it again. Those depend on the trainers, which are not ported yet, so the
/// write path restored here would otherwise ship with no coverage at all.
/// </remarks>
[NOpenNLPSpecific]
public class ArtifactRoundTripTest
{
    [Test]
    public void TestPOSDictionaryRoundTrip()
    {
        POSDictionary dictionary = new POSDictionary(caseSensitive: true);
        dictionary.Put("McKinsey", "NNP");
        dictionary.Put("set", "VB", "VBD", "VBN", "NN");

        POSDictionary read = WriteAndRead(dictionary.Serialize, POSDictionary.Create);

        ClassicAssert.AreEqual(dictionary, read);
        CollectionAssert.AreEqual(new[] { "NNP" }, read.GetTags("McKinsey"));
        CollectionAssert.AreEqual(new[] { "VB", "VBD", "VBN", "NN" }, read.GetTags("set"));
        ClassicAssert.IsTrue(read.IsCaseSensitive);
    }

    /// <summary>
    /// The case_sensitive attribute decides how lookups are keyed, so it has to
    /// survive the round-trip in both states.
    /// </summary>
    [Test]
    public void TestPOSDictionaryCaseInsensitiveRoundTrip()
    {
        POSDictionary dictionary = new POSDictionary(caseSensitive: false);
        dictionary.Put("McKinsey", "NNP");

        POSDictionary read = WriteAndRead(dictionary.Serialize, POSDictionary.Create);

        ClassicAssert.IsFalse(read.IsCaseSensitive);
        CollectionAssert.AreEqual(new[] { "NNP" }, read.GetTags("MCKINSEY"));
    }

    [Test]
    public void TestDictionaryRoundTrip()
    {
        Dictionary.Dictionary dictionary = new Dictionary.Dictionary(caseSensitive: true);
        dictionary.Put(new StringList("a"));
        dictionary.Put(new StringList("b", "c"));

        Dictionary.Dictionary read = WriteAndRead(
            dictionary.Serialize, @in => new Dictionary.Dictionary(@in));

        ClassicAssert.AreEqual(dictionary, read);
        ClassicAssert.AreEqual(2, read.Count);
        ClassicAssert.IsTrue(read.Contains(new StringList("b", "c")));
    }

    /// <summary>
    /// Dictionary.Equals compares only the entry set, faithfully to upstream, so a
    /// round-trip that lost case_sensitive would still compare equal. This checks the
    /// flag by its observable effect on lookups instead.
    /// </summary>
    [Test]
    public void TestDictionaryRoundTripPreservesCaseSensitivity()
    {
        Dictionary.Dictionary sensitive = new Dictionary.Dictionary(caseSensitive: true);
        sensitive.Put(new StringList("McKinsey"));

        Dictionary.Dictionary readSensitive = WriteAndRead(
            sensitive.Serialize, @in => new Dictionary.Dictionary(@in));

        ClassicAssert.IsTrue(readSensitive.Contains(new StringList("McKinsey")));
        ClassicAssert.IsFalse(readSensitive.Contains(new StringList("mckinsey")));

        Dictionary.Dictionary insensitive = new Dictionary.Dictionary(caseSensitive: false);
        insensitive.Put(new StringList("McKinsey"));

        Dictionary.Dictionary readInsensitive = WriteAndRead(
            insensitive.Serialize, @in => new Dictionary.Dictionary(@in));

        ClassicAssert.IsTrue(readInsensitive.Contains(new StringList("McKinsey")));
        ClassicAssert.IsTrue(readInsensitive.Contains(new StringList("mckinsey")));
    }

    /// <summary>
    /// Same check through AsStringSet, whose upstream counterpart is an inner class
    /// building its lookup wrappers against the live Dictionary.
    /// </summary>
    [Test]
    public void TestDictionaryAsStringSetHonoursCaseSensitivity()
    {
        Dictionary.Dictionary sensitive = new Dictionary.Dictionary(caseSensitive: true);
        sensitive.Put(new StringList("McKinsey"));

        Dictionary.Dictionary read = WriteAndRead(
            sensitive.Serialize, @in => new Dictionary.Dictionary(@in));

        ClassicAssert.IsTrue(read.AsStringSet().Contains("McKinsey"));
        ClassicAssert.IsFalse(read.AsStringSet().Contains("mckinsey"));
    }

    [Test]
    public void TestNGramModelRoundTrip()
    {
        NGramModel model = new NGramModel();
        model.Add(new StringList("the", "quick"));
        model.Add(new StringList("the", "quick"));
        model.Add(new StringList("brown", "fox"));

        NGramModel read = WriteAndRead(model.Serialize, @in => new NGramModel(@in));

        ClassicAssert.AreEqual(model, read);
        ClassicAssert.AreEqual(2, read.Count);
        // The count attribute is the whole point of the ngram format; a round-trip
        // that dropped it would still produce an equal-sized model.
        ClassicAssert.AreEqual(2, read.GetCount(new StringList("the", "quick")));
        ClassicAssert.AreEqual(1, read.GetCount(new StringList("brown", "fox")));
    }

    [Test]
    public void TestDetokenizationDictionaryRoundTrip()
    {
        DetokenizationDictionary dictionary = new DetokenizationDictionary(
            ["(", ")", "\"", "."],
            [
                DetokenizationOperationType.MoveRight,
                DetokenizationOperationType.MoveLeft,
                DetokenizationOperationType.RightLeftMatching,
                DetokenizationOperationType.MoveLeft
            ]);

        DetokenizationDictionary read = WriteAndRead(
            dictionary.Serialize, @in => new DetokenizationDictionary(@in));

        ClassicAssert.AreEqual(DetokenizationOperationType.MoveRight, read.GetOperation("("));
        ClassicAssert.AreEqual(DetokenizationOperationType.MoveLeft, read.GetOperation(")"));
        ClassicAssert.AreEqual(DetokenizationOperationType.RightLeftMatching, read.GetOperation("\""));
        ClassicAssert.AreEqual(DetokenizationOperationType.MoveLeft, read.GetOperation("."));
    }

    /// <summary>
    /// The operation is persisted as the upstream Java constant name, not the C#
    /// member name, so a dictionary written here stays readable by Apache OpenNLP.
    /// </summary>
    [Test]
    public void TestDetokenizationDictionaryWritesUpstreamOperationNames()
    {
        DetokenizationDictionary dictionary = new DetokenizationDictionary(
            ["("], [DetokenizationOperationType.MoveRight]);

        using var @out = new MemoryStream();
        dictionary.Serialize(@out);

        string xml = Encoding.UTF8.GetString(@out.ToArray());

        StringAssert.Contains("MOVE_RIGHT", xml);
        StringAssert.DoesNotContain("MoveRight", xml);
    }

    /// <summary>
    /// Java writes String.valueOf(boolean), which is lowercase. C#'s
    /// bool.ToString() yields "True", which upstream's SAX reader would not parse
    /// as true, so a dictionary written that way would silently come back
    /// case-insensitive in Apache OpenNLP.
    /// </summary>
    [Test]
    public void TestDictionaryWritesLowercaseCaseSensitiveAttribute()
    {
        Dictionary.Dictionary dictionary = new Dictionary.Dictionary(caseSensitive: true);
        dictionary.Put(new StringList("a"));

        using var @out = new MemoryStream();
        dictionary.Serialize(@out);

        string xml = Encoding.UTF8.GetString(@out.ToArray());

        StringAssert.Contains("case_sensitive=\"true\"", xml);
    }

    [Test]
    public void TestWordClusterDictionaryRoundTrip()
    {
        // The reader splits on a space, so the writer has to emit one.
        using var source = ToStream("dog 0\ncat 1\n");
        WordClusterDictionary dictionary = new WordClusterDictionary(source);

        WordClusterDictionary read = WriteAndRead(
            dictionary.Serialize, @in => new WordClusterDictionary(@in));

        ClassicAssert.AreEqual("0", read.LookupToken("dog"));
        ClassicAssert.AreEqual("1", read.LookupToken("cat"));
    }

    [Test]
    public void TestBrownClusterRoundTrip()
    {
        // Brown cluster files are tab-separated, cluster first, then token.
        using var source = ToStream("0\tdog\t100\n1\tcat\t50\n");
        BrownCluster dictionary = new BrownCluster(source);

        BrownCluster read = WriteAndRead(dictionary.Serialize, @in => new BrownCluster(@in));

        ClassicAssert.AreEqual("0", read.LookupToken("dog"));
        ClassicAssert.AreEqual("1", read.LookupToken("cat"));
    }

    [Test]
    public void TestHeadRulesRoundTrip()
    {
        using var source = TestResources.OpenResource("/opennlp/tools/parser/en_head_rules");
        Parser.Lang.En.HeadRules headRules = new Parser.Lang.En.HeadRules(
            new StreamReader(source, Encoding.UTF8));

        using var @out = new MemoryStream();
        using (var writer = new StreamWriter(@out, new UTF8Encoding(false), 1024, leaveOpen: true))
        {
            headRules.Serialize(writer);
        }

        @out.Position = 0;
        Parser.Lang.En.HeadRules read = new Parser.Lang.En.HeadRules(
            new StreamReader(@out, Encoding.UTF8));

        ClassicAssert.AreEqual(headRules, read);
    }

    /// <summary>
    /// Every serializer registered by BaseModel writes through the non-generic
    /// IArtifactSerializer.Serialize bridge, which casts. A bridge wired to the
    /// wrong type would only fail when a model is written, not when it is read.
    /// </summary>
    [Test]
    public void TestNonGenericSerializerBridgeWrites()
    {
        Dictionary.Dictionary dictionary = new Dictionary.Dictionary(caseSensitive: true);
        dictionary.Put(new StringList("a"));

        IArtifactSerializer serializer = new DictionarySerializer();

        using var @out = new MemoryStream();
        serializer.Serialize(dictionary, @out);

        @out.Position = 0;
        object? read = serializer.Create(@out);

        ClassicAssert.AreEqual(dictionary, read);
    }

    /// <summary>
    /// Java's Properties.store leaves the stream open and so does DictionaryEntryPersistor,
    /// which BaseModel.Serialize relies on: it writes several artifacts into one zip
    /// stream in sequence, and a serializer that closed its entry stream early would
    /// truncate everything after it.
    /// </summary>
    [Test]
    public void TestSerializeLeavesStreamOpen()
    {
        Dictionary.Dictionary dictionary = new Dictionary.Dictionary(caseSensitive: true);
        dictionary.Put(new StringList("a"));

        using var @out = new MemoryStream();
        dictionary.Serialize(@out);

        // Would throw ObjectDisposedException if Serialize had closed the stream.
        @out.WriteByte((byte)'x');

        ClassicAssert.IsTrue(@out.Length > 1);
    }

    /// <summary>
    /// Tokens are written as XML text, so anything the format reserves has to be
    /// escaped on the way out and unescaped on the way back in. Upstream's SAX
    /// serializer does this; a hand-rolled writer that concatenated strings would
    /// produce a file that either fails to parse or silently loses characters.
    /// </summary>
    [Test]
    public void TestDictionaryEscapesXmlSpecialCharacters()
    {
        POSDictionary dictionary = new POSDictionary(caseSensitive: true);
        dictionary.Put("a<b&c\"d'e>f", "NN");

        POSDictionary read = WriteAndRead(dictionary.Serialize, POSDictionary.Create);

        CollectionAssert.AreEqual(new[] { "NN" }, read.GetTags("a<b&c\"d'e>f"));
    }

    /// <summary>
    /// The writer fixes UTF-8, so non-ASCII tokens have to survive unchanged rather
    /// than depending on the machine's default encoding, as Java's charset-less
    /// OutputStreamWriter would.
    /// </summary>
    [Test]
    public void TestDictionaryRoundTripsNonAsciiTokens()
    {
        POSDictionary dictionary = new POSDictionary(caseSensitive: true);
        dictionary.Put("café", "NN");
        dictionary.Put("naïve", "JJ");

        POSDictionary read = WriteAndRead(dictionary.Serialize, POSDictionary.Create);

        CollectionAssert.AreEqual(new[] { "NN" }, read.GetTags("café"));
        CollectionAssert.AreEqual(new[] { "JJ" }, read.GetTags("naïve"));
    }

    /// <summary>
    /// An empty dictionary still has to produce a well-formed document with the
    /// root element, not an empty file.
    /// </summary>
    [Test]
    public void TestEmptyDictionaryRoundTrip()
    {
        POSDictionary dictionary = new POSDictionary(caseSensitive: true);

        POSDictionary read = WriteAndRead(dictionary.Serialize, POSDictionary.Create);

        ClassicAssert.AreEqual(dictionary, read);
    }

    /// <summary>
    /// The whole-model path: BaseModel.Serialize writes every artifact into one zip
    /// stream, then loads it back. Java writes through a single ZipOutputStream with
    /// putNextEntry/closeEntry, while .NET takes a separate stream per entry from
    /// ZipArchive.CreateEntry, so the translation needs covering end to end. This also
    /// exercises Properties.Store leaving the zip's entry stream usable for the
    /// artifacts written after the manifest.
    /// </summary>
    [Test]
    public void TestBaseModelRoundTrip()
    {
        var manifestInfoEntries = new System.Collections.Generic.Dictionary<string, string>
        {
            { "Custom-Entry", "hello" }
        };

        RoundTripModel model = new RoundTripModel("en", manifestInfoEntries);

        using var @out = new MemoryStream();
        model.Serialize(@out);

        @out.Position = 0;
        RoundTripModel read = new RoundTripModel(@out);

        ClassicAssert.AreEqual("en", read.Language);
        ClassicAssert.AreEqual("hello", read.GetManifestProperty("Custom-Entry"));

        var dictionary = read.GetArtifact<Dictionary.Dictionary>(RoundTripModel.DictionaryEntryName);
        ClassicAssert.IsNotNull(dictionary);
        ClassicAssert.AreEqual(2, dictionary!.Count);
        ClassicAssert.IsTrue(dictionary.Contains(new StringList("beta", "gamma")));

        // Set for every ISerializableArtifact before the zip is written, and read back
        // by ExtensionLoader to pick the serializer on load.
        ClassicAssert.AreEqual(typeof(DictionarySerializer).FullName,
            read.GetManifestProperty("serializer-class-" + RoundTripModel.DictionaryEntryName));
    }

    /// <summary>
    /// Every artifact must land in its own zip entry, named by its key. Writing them
    /// all to one stream would still produce a readable zip, but the artifacts would
    /// be concatenated into a single entry.
    /// </summary>
    [Test]
    public void TestBaseModelWritesOneZipEntryPerArtifact()
    {
        RoundTripModel model = new RoundTripModel("en",
            new System.Collections.Generic.Dictionary<string, string>());

        using var @out = new MemoryStream();
        model.Serialize(@out);

        @out.Position = 0;
        using var zip = new System.IO.Compression.ZipArchive(
            @out, System.IO.Compression.ZipArchiveMode.Read);

        CollectionAssert.AreEquivalent(
            new[] { "manifest.properties", RoundTripModel.DictionaryEntryName },
            zip.Entries.Select(e => e.FullName).ToList());

        foreach (var entry in zip.Entries)
        {
            ClassicAssert.Greater(entry.Length, 0, $"entry '{entry.FullName}' is empty");
        }
    }

    /// <summary>
    /// Serialize(FileInfo) must truncate, as Java's FileOutputStream does.
    /// FileInfo.OpenWrite uses FileMode.OpenOrCreate and does not, so writing a model
    /// over a larger existing one left the old file's tail in place. That does not
    /// fail loudly: a zip reader scans backwards for the end-of-central-directory
    /// record, finds the old model's, and loads the OLD artifacts. Saving a retrained
    /// model would silently read back the previous one.
    /// </summary>
    [Test]
    public void TestSerializeToFileTruncatesExistingFile()
    {
        RoundTripModel small = new RoundTripModel("en",
            new System.Collections.Generic.Dictionary<string, string>());

        // A model with more, larger artifacts, so its file is comfortably bigger.
        var padding = new System.Collections.Generic.Dictionary<string, string>();
        for (int i = 0; i < 40; i++)
        {
            padding["Pad-Entry-" + i] = new string('x', 200);
        }

        RoundTripModel large = new RoundTripModel("de", padding);

        string path = Path.Combine(Path.GetTempPath(),
            "nopennlp-truncate-" + System.Guid.NewGuid().ToString("N") + ".bin");
        try
        {
            large.Serialize(new FileInfo(path));
            long largeLength = new FileInfo(path).Length;

            small.Serialize(new FileInfo(path));

            ClassicAssert.Less(new FileInfo(path).Length, largeLength,
                "the file still holds the larger model's bytes, so it was not truncated");

            using var read = File.OpenRead(path);
            RoundTripModel reloaded = new RoundTripModel(read);

            // Without truncation this reads back "de" -- the model that was replaced.
            ClassicAssert.AreEqual("en", reloaded.Language);
            ClassicAssert.IsNull(reloaded.GetManifestProperty("Pad-Entry-0"));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    /// <summary>
    /// A BaseModel subclass holding only artifacts whose serializers are ported.
    /// The generic "model" artifact is deliberately absent: it serializes through
    /// GenericModelWriter, which is part of the ml.model write path and not ported yet.
    /// </summary>
    private sealed class RoundTripModel : BaseModel
    {
        internal const string DictionaryEntryName = "stuff.dictionary";

        public RoundTripModel(string languageCode,
            System.Collections.Generic.IDictionary<string, string> manifestInfoEntries)
            : base("RoundTrip", languageCode, manifestInfoEntries, null)
        {
            Dictionary.Dictionary dictionary = new Dictionary.Dictionary(caseSensitive: true);
            dictionary.Put(new StringList("alpha"));
            dictionary.Put(new StringList("beta", "gamma"));

            artifactMap[DictionaryEntryName] = dictionary;
            CheckArtifactMap();
        }

        public RoundTripModel(Stream @in)
            : base("RoundTrip", @in)
        {
        }
    }

    /// <summary>
    /// A model records its artifact serializers under serializer-class- keys, and
    /// ExtensionLoader has to resolve those names on load. Java's Class.getName()
    /// separates a nested class from its outer class with '$' where .NET reflection
    /// uses '+', and several of OpenNLP's serializers are nested, so without that
    /// substitution a Java-trained model carrying such an entry fails to load with
    /// "Extension class 'null'".
    /// </summary>
    // ParserModel.HeadRulesSerializer is private, matching upstream's package-private
    // class, so these compare on the resolved type's name rather than a typeof().
    [TestCase("opennlp.tools.util.model.DictionarySerializer",
        "NOpenNLP.Tools.Util.Model.DictionarySerializer")]
    [TestCase("opennlp.tools.postag.POSTaggerFactory$POSDictionarySerializer",
        "NOpenNLP.Tools.Postag.POSTaggerFactory+POSDictionarySerializer")]
    [TestCase("opennlp.tools.parser.ParserModel$HeadRulesSerializer",
        "NOpenNLP.Tools.Parser.ParserModel+HeadRulesSerializer")]
    [TestCase("opennlp.tools.util.featuregen.BrownCluster$BrownClusterSerializer",
        "NOpenNLP.Tools.Util.Featuregen.BrownCluster+BrownClusterSerializer")]
    public void TestResolvesJavaSerializerClassNames(string javaName, string expected)
    {
        IArtifactSerializer serializer =
            Util.Ext.ExtensionLoader.InstantiateExtension<IArtifactSerializer>(javaName);

        ClassicAssert.AreEqual(expected, serializer.GetType().FullName);
    }

    /// <summary>
    /// A dictionary written here has to be loadable by Apache OpenNLP itself, so the
    /// bytes real Java produced for the same content are pinned as a fixture and read
    /// back through the ported reader. Verified against opennlp-tools 1.9.4 on a JVM:
    /// Java loads the file this port writes and reports it equal to its own, and the
    /// port loads the file below, which Java wrote.
    /// </summary>
    /// <remarks>
    /// Java's SAX serializer differs cosmetically -- encoding="UTF-8" rather than
    /// "utf-8", no newline after the declaration, and a four-space indent -- none of
    /// which is significant to an XML parser. Entry order differs too, because Java
    /// iterates a HashMap.
    /// </remarks>
    [Test]
    public void TestReadsDictionaryWrittenByApacheOpenNlp()
    {
        const string javaWritten =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?><dictionary case_sensitive=\"true\">\n" +
            "    <entry tags=\"VB VBD VBN NN\">\n" +
            "        <token>set</token>\n" +
            "    </entry>\n" +
            "    <entry tags=\"NNP\">\n" +
            "        <token>McKinsey</token>\n" +
            "    </entry>\n" +
            "</dictionary>\n";

        using var @in = ToStream(javaWritten);
        POSDictionary read = POSDictionary.Create(@in);

        ClassicAssert.IsTrue(read.IsCaseSensitive);
        CollectionAssert.AreEqual(new[] { "NNP" }, read.GetTags("McKinsey"));
        CollectionAssert.AreEqual(new[] { "VB", "VBD", "VBN", "NN" }, read.GetTags("set"));
    }

    private static Stream ToStream(string content) =>
        new MemoryStream(Encoding.UTF8.GetBytes(content));

    private static T WriteAndRead<T>(System.Action<Stream> serialize, System.Func<Stream, T> create)
    {
        using var @out = new MemoryStream();
        serialize(@out);

        @out.Position = 0;
        return create(@out);
    }
}
