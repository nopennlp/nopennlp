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


using System.Collections.Generic;
using System.IO;
using NOpenNLP.Tools.Ngram;
using NOpenNLP.Tools.Support;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Languagemodel;

/// <summary>
/// Tests for <see cref="NGramLanguageModel"/>.
/// </summary>
public class NgramLanguageModelTest
{
    [Test]
    public void TestEmptyVocabularyProbability()
    {
        NGramLanguageModel model = new();
        ClassicAssert.AreEqual(0d, model.CalculateProbability(""), 0d,
            "probability with an empty vocabulary is always 0");
        ClassicAssert.AreEqual(0d, model.CalculateProbability("1", "2", "3"), 0d,
            "probability with an empty vocabulary is always 0");
    }

    [Test]
    public void TestRandomVocabularyAndSentence()
    {
        NGramLanguageModel model = new();
        foreach (var sentence in LanguageModelTestUtils.GenerateRandomVocabulary(10))
        {
            model.Add(sentence);
        }
        double probability = model.CalculateProbability(LanguageModelTestUtils.GenerateRandomSentence());
        ClassicAssert.IsTrue(probability >= 0 && probability <= 1,
            $"a probability measure should be between 0 and 1 [was {probability}]");
    }

    [Test]
    public void TestNgramModel()
    {
        NGramLanguageModel model = new(4);
        model.Add("I", "saw", "the", "fox");
        model.Add("the", "red", "house");
        model.Add("I", "saw", "something", "nice");
        double probability = model.CalculateProbability("I", "saw", "the", "red", "house");
        ClassicAssert.IsTrue(probability >= 0 && probability <= 1,
            $"a probability measure should be between 0 and 1 [was {probability}]");

        string[]? tokens = model.PredictNextTokens("I", "saw");
        ClassicAssert.NotNull(tokens);
        CollectionAssert.AreEqual(new string[] { "the", "fox" }, tokens);
    }

    [Test]
    public void TestBigramProbability()
    {
        NGramLanguageModel model = new(2);
        model.Add("<s>", "I", "am", "Sam", "</s>");
        model.Add("<s>", "Sam", "I", "am", "</s>");
        model.Add("<s>", "I", "do", "not", "like", "green", "eggs", "and", "ham", "</s>");
        double probability = model.CalculateProbability("<s>", "I");
        ClassicAssert.AreEqual(0.666d, probability, 0.001);
        probability = model.CalculateProbability("Sam", "</s>");
        ClassicAssert.AreEqual(0.5d, probability, 0.001);
        probability = model.CalculateProbability("<s>", "Sam");
        ClassicAssert.AreEqual(0.333d, probability, 0.001);
        probability = model.CalculateProbability("am", "Sam");
        ClassicAssert.AreEqual(0.5d, probability, 0.001);
        probability = model.CalculateProbability("I", "am");
        ClassicAssert.AreEqual(0.666d, probability, 0.001);
        probability = model.CalculateProbability("I", "do");
        ClassicAssert.AreEqual(0.333d, probability, 0.001);
        probability = model.CalculateProbability("I", "am", "Sam");
        ClassicAssert.AreEqual(0.333d, probability, 0.001);
    }

    [Test]
    public void TestTrigram()
    {
        NGramLanguageModel model = new(3);
        model.Add("I", "see", "the", "fox");
        model.Add("the", "red", "house");
        model.Add("I", "saw", "something", "nice");
        double probability = model.CalculateProbability("I", "saw", "the", "red", "house");
        ClassicAssert.IsTrue(probability >= 0 && probability <= 1,
            $"a probability measure should be between 0 and 1 [was {probability}]");

        string[]? tokens = model.PredictNextTokens("I", "saw");
        ClassicAssert.NotNull(tokens);
        CollectionAssert.AreEqual(new string[] { "something" }, tokens);
    }

    [Test]
    public void TestBigram()
    {
        NGramLanguageModel model = new(2);
        model.Add("I", "see", "the", "fox");
        model.Add("the", "red", "house");
        model.Add("I", "saw", "something", "nice");
        double probability = model.CalculateProbability("I", "saw", "the", "red", "house");
        ClassicAssert.IsTrue(probability >= 0 && probability <= 1,
            $"a probability measure should be between 0 and 1 [was {probability}]");

        string[]? tokens = model.PredictNextTokens("I", "saw");
        ClassicAssert.NotNull(tokens);
        CollectionAssert.AreEqual(new string[] { "something" }, tokens);
    }

    [Test]
    public void TestSerializedNGramLanguageModel()
    {
        NGramLanguageModel languageModel;
        using (var stream = TestResources.OpenResource("/opennlp/tools/ngram/ngram-model.xml"))
        {
            languageModel = new NGramLanguageModel(stream, 3);
        }

        double probability = languageModel.CalculateProbability("The", "brown", "fox", "jumped");
        ClassicAssert.IsTrue(probability >= 0 && probability <= 1,
            $"a probability measure should be between 0 and 1 [was {probability}]");
        string[]? tokens = languageModel.PredictNextTokens("the", "brown", "fox");
        ClassicAssert.NotNull(tokens);
        CollectionAssert.AreEqual(new string[] { "jumped" }, tokens);
    }

    [Test]
    public void TestTrigramLanguageModelCreationFromText()
    {
        const int ngramSize = 3;
        NGramLanguageModel languageModel = new(ngramSize);
        using (var stream = TestResources.OpenResource("/opennlp/tools/languagemodel/sentences.txt"))
        using (StreamReader reader = new(stream))
        {
            while (reader.ReadLine() is { } line)
            {
                IList<string> split = line.Split(' ');
                IList<string> generatedStrings = NGramGenerator.Generate(split, ngramSize, " ");
                foreach (var generatedString in generatedStrings)
                {
                    string[] tokens = generatedString.Split(' ');
                    if (tokens.Length > 0)
                    {
                        languageModel.Add(tokens);
                    }
                }
            }
        }

        string[]? tokens2 = languageModel.PredictNextTokens("neural", "network", "language");
        ClassicAssert.NotNull(tokens2);
        CollectionAssert.AreEqual(new string[] { "models" }, tokens2);
        double p1 = languageModel.CalculateProbability("neural", "network", "language", "models");
        double p2 = languageModel.CalculateProbability("neural", "network", "language", "model");
        ClassicAssert.IsTrue(p1 > p2);
    }
}
