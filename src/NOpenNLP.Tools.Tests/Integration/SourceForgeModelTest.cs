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
using NOpenNLP.Tools.Chunker;
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Postag;
using NOpenNLP.Tools.Sentdetect;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Integration;

/// <summary>
/// Runs each ported tool against the pre-trained SourceForge model it was built
/// for, and checks the output is what the model is known to produce.
/// </summary>
/// <remarks>
/// Authored for NOpenNLP, adapted from Apache OpenNLP's SourceForgeModelEval.
/// <para/>
/// Upstream hashes the output of each model over the 300K sentence Leipzig news
/// corpus and compares the digest to a constant. That needs a 63 MB corpus
/// download and the ObjectStream sample-stream stack, neither of which is
/// available here, so this checks the same models against fixed sentences whose
/// expected analysis is stated inline. It is a weaker guarantee than upstream's
/// hash over 300K sentences, but it exercises the same code paths, and the
/// expected values are readable enough to tell a real regression from a change
/// in behaviour.
/// <para/>
/// The parser model is excluded: it is 34 MB and the parser is not ported.
/// <para/>
/// The models are fetched by build/download-test-models.ps1. Without them these
/// tests report inconclusive rather than failing; see <see cref="TestData"/>.
/// </remarks>
[Category("Integration")]
[NOpenNLPSpecific]
public class SourceForgeModelTest
{
    /// <summary>
    /// The opening of the Penn Treebank Wall Street Journal sample, which is the
    /// kind of newswire text these models were trained on.
    /// </summary>
    private const string SampleText =
        "Pierre Vinken, 61 years old, will join the board as a nonexecutive director Nov. 29. " +
        "Mr. Vinken is chairman of Elsevier N.V., the Dutch publishing group. " +
        "Rudolph Agnew, 55 years old and former chairman of Consolidated Gold Fields PLC, " +
        "was named a director of this British industrial conglomerate.";

    /// <summary>
    /// One sentence carrying an entity of every type the seven name finder
    /// models are trained to recognize.
    /// </summary>
    private static readonly string[] EntityTokens =
    [
        "John", "Smith", "paid", "$", "25.5", "million", "to", "Acme", "Corp.",
        "in", "Chicago", "yesterday", "afternoon", ",", "a", "15", "%", "increase", "."
    ];

    private static readonly string[] FirstSentenceTokens =
    [
        "Pierre", "Vinken", ",", "61", "years", "old", ",", "will", "join", "the",
        "board", "as", "a", "nonexecutive", "director", "Nov.", "29", "."
    ];

    private static readonly string[] FirstSentenceTags =
    [
        "NNP", "NNP", ",", "CD", "NNS", "JJ", ",", "MD", "VB", "DT", "NN", "IN",
        "DT", "JJ", "NN", "NNP", "CD", "."
    ];

    private static FileInfo Model(string name)
        => new FileInfo(TestData.RequireFile("models-sf/" + name));

    /// <summary>
    /// Sentence boundaries, including the two abbreviations that make this hard:
    /// "Nov. 29." ends a sentence but "Nov." does not, and "N.V.," is mid-sentence.
    /// </summary>
    [Test]
    public void EvalSentenceModel()
    {
        SentenceDetectorME sentenceDetector =
            new SentenceDetectorME(new SentenceModel(Model("en-sent.bin")));

        string[] sentences = sentenceDetector.SentDetect(SampleText);

        ClassicAssert.AreEqual(3, sentences.Length);
        ClassicAssert.AreEqual(
            "Pierre Vinken, 61 years old, will join the board as a nonexecutive director Nov. 29.",
            sentences[0]);
        ClassicAssert.AreEqual(
            "Mr. Vinken is chairman of Elsevier N.V., the Dutch publishing group.",
            sentences[1]);
        ClassicAssert.IsTrue(sentences[2].StartsWith("Rudolph Agnew,"));
    }

    /// <summary>
    /// Tokenization, which has to split the commas and the final stop off their
    /// words while leaving the abbreviation "Nov." intact.
    /// </summary>
    [Test]
    public void EvalTokenModel()
    {
        TokenizerME tokenizer = new TokenizerME(new TokenizerModel(Model("en-token.bin")));

        string[] tokens = tokenizer.Tokenize(
            "Pierre Vinken, 61 years old, will join the board as a nonexecutive director Nov. 29.");

        CollectionAssert.AreEqual(FirstSentenceTokens, tokens);
    }

    [Test]
    public void EvalPosModelMaxent()
    {
        POSTaggerME tagger = new POSTaggerME(new POSModel(Model("en-pos-maxent.bin")));

        CollectionAssert.AreEqual(FirstSentenceTags, tagger.Tag(FirstSentenceTokens));
    }

    /// <summary>
    /// The perceptron model is a different trainer, and so a different code path
    /// through the model reader, but agrees with maxent on this sentence.
    /// </summary>
    [Test]
    public void EvalPosModelPerceptron()
    {
        POSTaggerME tagger = new POSTaggerME(new POSModel(Model("en-pos-perceptron.bin")));

        CollectionAssert.AreEqual(FirstSentenceTags, tagger.Tag(FirstSentenceTokens));
    }

    [Test]
    public void EvalChunkerModel()
    {
        ChunkerME chunker = new ChunkerME(new ChunkerModel(Model("en-chunker.bin")));

        string[] chunks = chunker.Chunk(FirstSentenceTokens, FirstSentenceTags);

        // "Pierre Vinken" / "61 years" / "old" / "will join" / "the board" /
        // "as" / "a nonexecutive director" / "Nov. 29"
        string[] expected =
        [
            "B-NP", "I-NP", "O", "B-NP", "I-NP", "B-ADJP", "O", "B-VP", "I-VP",
            "B-NP", "I-NP", "B-PP", "B-NP", "I-NP", "I-NP", "B-NP", "I-NP", "O"
        ];
        CollectionAssert.AreEqual(expected, chunks);
    }

    [TestCase("person", 0, 2, TestName = "EvalNameFinderModel(person)")]
    [TestCase("money", 3, 6, TestName = "EvalNameFinderModel(money)")]
    [TestCase("organization", 7, 9, TestName = "EvalNameFinderModel(organization)")]
    [TestCase("location", 10, 11, TestName = "EvalNameFinderModel(location)")]
    [TestCase("date", 11, 12, TestName = "EvalNameFinderModel(date)")]
    [TestCase("time", 12, 13, TestName = "EvalNameFinderModel(time)")]
    [TestCase("percentage", 15, 17, TestName = "EvalNameFinderModel(percentage)")]
    public void EvalNameFinderModel(string type, int start, int end)
    {
        NameFinderME nameFinder =
            new NameFinderME(new TokenNameFinderModel(Model($"en-ner-{type}.bin")));

        Span[] names = nameFinder.Find(EntityTokens);

        // "John Smith" / "$ 25.5 million" / "Acme Corp." / "Chicago" /
        // "yesterday" / "afternoon" / "15 %"
        ClassicAssert.AreEqual(1, names.Length);
        ClassicAssert.AreEqual(new Span(start, end, type), names[0]);
    }
}
