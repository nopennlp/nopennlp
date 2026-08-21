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

using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Text;

namespace NOpenNLP.Tools.Sentdetect;

/// <summary>
/// Tests for the <see cref="SentenceDetectorME"/> class.
/// </summary>
public class SentenceDetectorMETest
{
    [Test]
    public void TestSentenceDetector()
    {
        IInputStreamFactory @in = new ResourceAsStreamFactory(
            "/opennlp/tools/sentdetect/Sentences.txt");

        var mlParams = new TrainingParameters();
        mlParams.Put(TrainingParameters.ITERATIONS_PARAM, 100);
        mlParams.Put(TrainingParameters.CUTOFF_PARAM, 0);

        var factory = new SentenceDetectorFactory("eng", true, null, null);

        var sentdetectModel = SentenceDetectorME.Train(
            "eng", new SentenceSampleStream(new PlainTextByLineStream(@in,
                Encoding.UTF8)), factory, mlParams);

        ClassicAssert.AreEqual("eng", sentdetectModel.Language);

        var sentDetect = new SentenceDetectorME(sentdetectModel);

        // Tests sentence detector with sentDetect method
        string sampleSentences1 = "This is a test. There are many tests, this is the second.";
        var sents = sentDetect.SentDetect(sampleSentences1);
        ClassicAssert.AreEqual(sents.Length, 2);
        ClassicAssert.AreEqual(sents[0], "This is a test.");
        ClassicAssert.AreEqual(sents[1], "There are many tests, this is the second.");
        var probs = sentDetect.SentenceProbabilities;
        ClassicAssert.AreEqual(probs.Length, 2);

        string sampleSentences2 = "This is a test. There are many tests, this is the second";
        sents = sentDetect.SentDetect(sampleSentences2);
        ClassicAssert.AreEqual(sents.Length, 2);
        probs = sentDetect.SentenceProbabilities;
        ClassicAssert.AreEqual(probs.Length, 2);
        ClassicAssert.AreEqual(sents[0], "This is a test.");
        ClassicAssert.AreEqual(sents[1], "There are many tests, this is the second");

        string sampleSentences3 = "This is a \"test\". He said \"There are many tests, this is the second.\"";
        sents = sentDetect.SentDetect(sampleSentences3);
        ClassicAssert.AreEqual(sents.Length, 2);
        probs = sentDetect.SentenceProbabilities;
        ClassicAssert.AreEqual(probs.Length, 2);
        ClassicAssert.AreEqual(sents[0], "This is a \"test\".");
        ClassicAssert.AreEqual(sents[1], "He said \"There are many tests, this is the second.\"");

        string sampleSentences4 = "This is a \"test\". I said \"This is a test.\"  Any questions?";
        sents = sentDetect.SentDetect(sampleSentences4);
        ClassicAssert.AreEqual(sents.Length, 3);
        probs = sentDetect.SentenceProbabilities;
        ClassicAssert.AreEqual(probs.Length, 3);
        ClassicAssert.AreEqual(sents[0], "This is a \"test\".");
        ClassicAssert.AreEqual(sents[1], "I said \"This is a test.\"");
        ClassicAssert.AreEqual(sents[2], "Any questions?");

        string sampleSentences5 = "This is a one sentence test space at the end.    ";
        sents = sentDetect.SentDetect(sampleSentences5);
        ClassicAssert.AreEqual(1, sentDetect.SentenceProbabilities.Length);
        ClassicAssert.AreEqual(sents[0], "This is a one sentence test space at the end.");

        string sampleSentences6 = "This is a one sentences test with tab at the end.            ";
        sents = sentDetect.SentDetect(sampleSentences6);
        ClassicAssert.AreEqual(sents[0], "This is a one sentences test with tab at the end.");

        string sampleSentences7 = "This is a test.    With spaces between the two sentences.";
        sents = sentDetect.SentDetect(sampleSentences7);
        ClassicAssert.AreEqual(sents[0], "This is a test.");
        ClassicAssert.AreEqual(sents[1], "With spaces between the two sentences.");

        string sampleSentences9 = "";
        sents = sentDetect.SentDetect(sampleSentences9);
        ClassicAssert.AreEqual(0, sents.Length);

        string sampleSentences10 = "               "; // whitespaces and tabs
        sents = sentDetect.SentDetect(sampleSentences10);
        ClassicAssert.AreEqual(0, sents.Length);

        string sampleSentences11 = "This is test sentence without a dot at the end and spaces          ";
        sents = sentDetect.SentDetect(sampleSentences11);
        ClassicAssert.AreEqual(sents[0], "This is test sentence without a dot at the end and spaces");
        probs = sentDetect.SentenceProbabilities;
        ClassicAssert.AreEqual(1, probs.Length);

        string sampleSentence12 = "    This is a test.";
        sents = sentDetect.SentDetect(sampleSentence12);
        ClassicAssert.AreEqual(sents[0], "This is a test.");

        string sampleSentence13 = " This is a test";
        sents = sentDetect.SentDetect(sampleSentence13);
        ClassicAssert.AreEqual(sents[0], "This is a test");

        // Test that sentPosDetect also works
        var pos = sentDetect.SentPosDetect(sampleSentences2);
        ClassicAssert.AreEqual(pos.Length, 2);
        probs = sentDetect.SentenceProbabilities;
        ClassicAssert.AreEqual(probs.Length, 2);
        ClassicAssert.AreEqual(new Span(0, 15), pos[0]);
        ClassicAssert.AreEqual(new Span(16, 56), pos[1]);
    }

    [Test]
    public void TestInsufficientData()
    {
        IInputStreamFactory @in = new ResourceAsStreamFactory(
            "/opennlp/tools/sentdetect/SentencesInsufficient.txt");

        var mlParams = new TrainingParameters();
        mlParams.Put(TrainingParameters.ITERATIONS_PARAM, 100);
        mlParams.Put(TrainingParameters.CUTOFF_PARAM, 0);

        var factory = new SentenceDetectorFactory("eng", true, null, null);

        Assert.Throws<InsufficientTrainingDataException>((Action)(() => SentenceDetectorME.Train("eng",
            new SentenceSampleStream(
                new PlainTextByLineStream(@in, Encoding.UTF8)), factory, mlParams)));
    }
}
