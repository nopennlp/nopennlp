/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License. You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

// This file has been modified from the original Apache OpenNLP source:
// translated from Java to C# and adapted for .NET. See NOTICE.

using System.Text;
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Namefind;

/// <summary>
/// This is the test class for <see cref="NameFinderME"/>.
/// <para/>
/// A proper testing and evaluation of the name finder
/// is only possible with a large corpus which contains
/// a huge amount of test sentences.
/// <para/>
/// The scope of this test is to make sure that the name finder
/// code can be executed. This test can not detect
/// mistakes which lead to incorrect feature generation
/// or other mistakes which decrease the tagging
/// performance of the name finder.
/// <para/>
/// In this test the <see cref="NameFinderME"/> is trained with
/// a small amount of training sentences and then the
/// computed model is used to predict sentences from the
/// training sentences.
/// </summary>
public class NameFinderMETest
{
    private const string TYPE_OVERRIDE = "aType";
    private const string DEFAULT = "default";

    [Test]
    public void TestNameFinder()
    {
        // train the name finder
        // NOpenNLP: upstream builds a MockInputStreamFactory over a classpath File;
        // the ported ResourceAsStreamFactory in Support reads the embedded resource.
        var encoding = Latin1;

        IObjectStream<NameSample?> sampleStream =
            new NameSampleDataStream(
                new PlainTextByLineStream(new ResourceAsStreamFactory(
                    "/opennlp/tools/namefind/AnnotatedSentences.txt"), encoding));

        TrainingParameters params_ = new();
        params_.Put(TrainingParameters.ITERATIONS_PARAM, 70);
        params_.Put(TrainingParameters.CUTOFF_PARAM, 1);

        TokenNameFinderModel nameFinderModel = NameFinderME.Train("eng", null, sampleStream,
            params_, TokenNameFinderFactory.Create(null, null, new JCG.Dictionary<string, object>(), new BioCodec()));

        ITokenNameFinder nameFinder = new NameFinderME(nameFinderModel);

        // now test if it can detect the sample sentences

        string[] sentence = ["Alisa",
            "appreciated",
            "the",
            "hint",
            "and",
            "enjoyed",
            "a",
            "delicious",
            "traditional",
            "meal."];

        Span[] names = nameFinder.Find(sentence);

        ClassicAssert.AreEqual(1, names.Length);
        ClassicAssert.AreEqual(new Span(0, 1, DEFAULT), names[0]);

        sentence =
        [
            "Hi",
            "Mike",
            ",",
            "it's",
            "Stefanie",
            "Schmidt",
            "."
        ];

        names = nameFinder.Find(sentence);

        ClassicAssert.AreEqual(2, names.Length);
        ClassicAssert.AreEqual(new Span(1, 2, DEFAULT), names[0]);
        ClassicAssert.AreEqual(new Span(4, 6, DEFAULT), names[1]);
    }

    /// <summary>
    /// Train NamefinderME using AnnotatedSentencesWithTypes.txt with "person"
    /// nameType and try the model in a sample text.
    /// </summary>
    [Test]
    public void TestNameFinderWithTypes()
    {
        // train the name finder
        var encoding = Latin1;

        IObjectStream<NameSample?> sampleStream = new NameSampleDataStream(
            new PlainTextByLineStream(new ResourceAsStreamFactory(
                "/opennlp/tools/namefind/AnnotatedSentencesWithTypes.txt"), encoding));

        TrainingParameters params_ = new();
        params_.Put(TrainingParameters.ITERATIONS_PARAM, 70);
        params_.Put(TrainingParameters.CUTOFF_PARAM, 1);

        TokenNameFinderModel nameFinderModel = NameFinderME.Train("eng", null, sampleStream,
            params_, TokenNameFinderFactory.Create(null, null, new JCG.Dictionary<string, object>(), new BioCodec()));

        NameFinderME nameFinder = new(nameFinderModel);

        // now test if it can detect the sample sentences

        string[] sentence2 = ["Hi", "Mike", ",", "it's", "Stefanie",
            "Schmidt", "."];

        Span[] names2 = nameFinder.Find(sentence2);

        ClassicAssert.AreEqual(2, names2.Length);
        ClassicAssert.AreEqual(new Span(1, 2, "person"), names2[0]);
        ClassicAssert.AreEqual(new Span(4, 6, "person"), names2[1]);
        ClassicAssert.AreEqual("person", names2[0].Type);
        ClassicAssert.AreEqual("person", names2[1].Type);

        string[] sentence = ["Alisa", "appreciated", "the", "hint", "and",
            "enjoyed", "a", "delicious", "traditional", "meal."];

        Span[] names = nameFinder.Find(sentence);

        ClassicAssert.AreEqual(1, names.Length);
        ClassicAssert.AreEqual(new Span(0, 1, "person"), names[0]);
        ClassicAssert.IsTrue(HasOtherAsOutcome(nameFinderModel));
    }

    /// <summary>
    /// Train NamefinderME using OnlyWithNames.train. The goal is to check if the model
    /// validator accepts it. This is related to the issue OPENNLP-9
    /// </summary>
    [Test]
    public void TestOnlyWithNames()
    {
        // train the name finder
        IObjectStream<NameSample?> sampleStream = new NameSampleDataStream(
            new PlainTextByLineStream(new ResourceAsStreamFactory(
                "/opennlp/tools/namefind/OnlyWithNames.train"), Encoding.UTF8));

        TrainingParameters params_ = new();
        params_.Put(TrainingParameters.ITERATIONS_PARAM, 70);
        params_.Put(TrainingParameters.CUTOFF_PARAM, 1);

        TokenNameFinderModel nameFinderModel = NameFinderME.Train("eng", null, sampleStream,
            params_, TokenNameFinderFactory.Create(null, null, new JCG.Dictionary<string, object>(), new BioCodec()));

        NameFinderME nameFinder = new(nameFinderModel);

        // now test if it can detect the sample sentences

        string[] sentence = SplitOnWhitespace("Neil Abercrombie Anibal Acevedo-Vila Gary Ackerman " +
            "Robert Aderholt Daniel Akaka Todd Akin Lamar Alexander Rodney Alexander");

        Span[] names1 = nameFinder.Find(sentence);

        ClassicAssert.AreEqual(new Span(0, 2, DEFAULT), names1[0]);
        ClassicAssert.AreEqual(new Span(2, 4, DEFAULT), names1[1]);
        ClassicAssert.AreEqual(new Span(4, 6, DEFAULT), names1[2]);
        ClassicAssert.IsFalse(HasOtherAsOutcome(nameFinderModel));
    }

    [Test]
    public void TestOnlyWithNamesTypeOverride()
    {
        // train the name finder
        IObjectStream<NameSample?> sampleStream = new NameSampleDataStream(
            new PlainTextByLineStream(new ResourceAsStreamFactory(
                "/opennlp/tools/namefind/OnlyWithNames.train"), Encoding.UTF8));

        TrainingParameters params_ = new();
        params_.Put(TrainingParameters.ITERATIONS_PARAM, 70);
        params_.Put(TrainingParameters.CUTOFF_PARAM, 1);

        TokenNameFinderModel nameFinderModel = NameFinderME.Train("eng", TYPE_OVERRIDE, sampleStream,
            params_, TokenNameFinderFactory.Create(null, null, new JCG.Dictionary<string, object>(), new BioCodec()));

        NameFinderME nameFinder = new(nameFinderModel);

        // now test if it can detect the sample sentences

        string[] sentence = SplitOnWhitespace("Neil Abercrombie Anibal Acevedo-Vila Gary Ackerman " +
            "Robert Aderholt Daniel Akaka Todd Akin Lamar Alexander Rodney Alexander");

        Span[] names1 = nameFinder.Find(sentence);

        ClassicAssert.AreEqual(new Span(0, 2, TYPE_OVERRIDE), names1[0]);
        ClassicAssert.AreEqual(new Span(2, 4, TYPE_OVERRIDE), names1[1]);
        ClassicAssert.AreEqual(new Span(4, 6, TYPE_OVERRIDE), names1[2]);
        ClassicAssert.IsFalse(HasOtherAsOutcome(nameFinderModel));
    }

    /// <summary>
    /// Train NamefinderME using OnlyWithNamesWithTypes.train.
    /// The goal is to check if the model validator accepts it.
    /// This is related to the issue OPENNLP-9
    /// </summary>
    [Test]
    public void TestOnlyWithNamesWithTypes()
    {
        // train the name finder
        IObjectStream<NameSample?> sampleStream = new NameSampleDataStream(
            new PlainTextByLineStream(new ResourceAsStreamFactory(
                "/opennlp/tools/namefind/OnlyWithNamesWithTypes.train"), Encoding.UTF8));

        TrainingParameters params_ = new();
        params_.Put(TrainingParameters.ITERATIONS_PARAM, 70);
        params_.Put(TrainingParameters.CUTOFF_PARAM, 1);

        TokenNameFinderModel nameFinderModel = NameFinderME.Train("eng", null, sampleStream,
            params_, TokenNameFinderFactory.Create(null, null, new JCG.Dictionary<string, object>(), new BioCodec()));

        NameFinderME nameFinder = new(nameFinderModel);

        // now test if it can detect the sample sentences

        string[] sentence = SplitOnWhitespace("Neil Abercrombie Anibal Acevedo-Vila Gary Ackerman " +
            "Robert Aderholt Daniel Akaka Todd Akin Lamar Alexander Rodney Alexander");

        Span[] names1 = nameFinder.Find(sentence);

        ClassicAssert.AreEqual(new Span(0, 2, "person"), names1[0]);
        ClassicAssert.AreEqual(new Span(2, 4, "person"), names1[1]);
        ClassicAssert.AreEqual(new Span(4, 6, "person"), names1[2]);
        ClassicAssert.AreEqual("person", names1[2].Type);
        ClassicAssert.IsFalse(HasOtherAsOutcome(nameFinderModel));
    }

    /// <summary>
    /// Train NamefinderME using OnlyWithNames.train. The goal is to check if the model
    /// validator accepts it. This is related to the issue OPENNLP-9
    /// </summary>
    [Test]
    public void TestOnlyWithEntitiesWithTypes()
    {
        // train the name finder
        IObjectStream<NameSample?> sampleStream = new NameSampleDataStream(
            new PlainTextByLineStream(new ResourceAsStreamFactory(
                "/opennlp/tools/namefind/OnlyWithEntitiesWithTypes.train"), Encoding.UTF8));

        TrainingParameters params_ = new();
        params_.Put(TrainingParameters.ALGORITHM_PARAM, "MAXENT");
        params_.Put(TrainingParameters.ITERATIONS_PARAM, 70);
        params_.Put(TrainingParameters.CUTOFF_PARAM, 1);

        TokenNameFinderModel nameFinderModel = NameFinderME.Train("eng", null, sampleStream,
            params_, TokenNameFinderFactory.Create(null, null, new JCG.Dictionary<string, object>(), new BioCodec()));

        NameFinderME nameFinder = new(nameFinderModel);

        // now test if it can detect the sample sentences

        string[] sentence = SplitOnWhitespace("NATO United States Barack Obama");

        Span[] names1 = nameFinder.Find(sentence);

        ClassicAssert.AreEqual(new Span(0, 1, "organization"), names1[0]); // NATO
        ClassicAssert.AreEqual(new Span(1, 3, "location"), names1[1]); // United States
        ClassicAssert.AreEqual("person", names1[2].Type);
        ClassicAssert.IsFalse(HasOtherAsOutcome(nameFinderModel));
    }

    private static bool HasOtherAsOutcome(TokenNameFinderModel nameFinderModel)
    {
        // NOpenNLP: NameFinderSequenceModel is nullable on the ported model, but a
        // model that has just been trained always has one.
        ISequenceClassificationModel<string> model = nameFinderModel.NameFinderSequenceModel!;
        string[] outcomes = model.Outcomes;
        foreach (var outcome in outcomes)
        {
            if (outcome.Equals(NameFinderME.OTHER))
            {
                return true;
            }
        }

        return false;
    }

    [Test]
    public void TestDropOverlappingSpans()
    {
        Span[] spans = [new Span(1, 10), new Span(1, 11), new Span(1, 11), new Span(5, 15)];
        Span[] remainingSpan = NameFinderME.DropOverlappingSpans(spans);
        ClassicAssert.AreEqual(new Span(1, 11), remainingSpan[0]);
    }

    /// <summary>
    /// Train NamefinderME using voa1.train with several
    /// nameTypes and try the model in a sample text.
    /// </summary>
    [Test]
    public void TestNameFinderWithMultipleTypes()
    {
        // train the name finder
        IObjectStream<NameSample?> sampleStream = new NameSampleDataStream(
            new PlainTextByLineStream(new ResourceAsStreamFactory(
                "/opennlp/tools/namefind/voa1.train"), Encoding.UTF8));

        TrainingParameters params_ = new();
        params_.Put(TrainingParameters.ITERATIONS_PARAM, 70);
        params_.Put(TrainingParameters.CUTOFF_PARAM, 1);

        TokenNameFinderModel nameFinderModel = NameFinderME.Train("eng", null, sampleStream,
            params_, TokenNameFinderFactory.Create(null, null, new JCG.Dictionary<string, object>(), new BioCodec()));

        NameFinderME nameFinder = new(nameFinderModel);

        // now test if it can detect the sample sentences

        string[] sentence = ["U", ".", "S", ".", "President",
            "Barack", "Obama", "has", "arrived", "in", "South", "Korea", ",",
            "where", "he", "is", "expected", "to", "show", "solidarity", "with",
            "the", "country", "'", "s", "president", "in", "demanding", "North",
            "Korea", "move", "toward", "ending", "its", "nuclear", "weapons",
            "programs", "."];

        Span[] names1 = nameFinder.Find(sentence);

        ClassicAssert.AreEqual(new Span(0, 4, "location"), names1[0]);
        ClassicAssert.AreEqual(new Span(5, 7, "person"), names1[1]);
        ClassicAssert.AreEqual(new Span(10, 12, "location"), names1[2]);
        ClassicAssert.AreEqual(new Span(28, 30, "location"), names1[3]);
        ClassicAssert.AreEqual("location", names1[0].Type);
        ClassicAssert.AreEqual("person", names1[1].Type);
        ClassicAssert.AreEqual("location", names1[2].Type);
        ClassicAssert.AreEqual("location", names1[3].Type);

        sentence = ["Scott", "Snyder", "is", "the", "director", "of",
            "the", "Center", "for", "U", ".", "S", ".", "Korea", "Policy", "."];

        Span[] names2 = nameFinder.Find(sentence);

        ClassicAssert.AreEqual(2, names2.Length);
        ClassicAssert.AreEqual(new Span(0, 2, "person"), names2[0]);
        ClassicAssert.AreEqual(new Span(7, 15, "organization"), names2[1]);
        ClassicAssert.AreEqual("person", names2[0].Type);
        ClassicAssert.AreEqual("organization", names2[1].Type);
    }

    // NOpenNLP: upstream splits on the regex "\\s+". Splitting on a space alone
    // would agree on these inputs but not on a tab or newline, so the character
    // set matches what Java's \\s does.
    private static string[] SplitOnWhitespace(string s) =>
        s.Split([' ', '\t', '\n', '\r', '\f', '\v'], System.StringSplitOptions.RemoveEmptyEntries);

    // NOpenNLP: StandardCharsets.ISO_8859_1 has no named BCL counterpart that is
    // registered on every target, so the code page is used directly.
    private static Encoding Latin1 => Encoding.GetEncoding(28591);
}
