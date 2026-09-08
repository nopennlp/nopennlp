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
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Languagemodel;

/// <summary>
/// Tests for evaluating accuracy of language models.
/// </summary>
public class LanguageModelEvaluationTest
{
    [Test]
    public void TestPerplexityComparison()
    {
        ICollection<string[]> trainingVocabulary =
            LanguageModelTestUtils.GenerateRandomVocabulary(1100000);
        ICollection<string[]> testVocabulary =
            LanguageModelTestUtils.GenerateRandomVocabulary(100);

        NGramLanguageModel unigramLM = new(1);
        foreach (var sentence in trainingVocabulary)
        {
            unigramLM.Add(new StringList(sentence), 1, 1);
        }
        double unigramPerplexity =
            LanguageModelTestUtils.GetPerplexity(unigramLM, testVocabulary, 1);

        NGramLanguageModel bigramLM = new(2);
        foreach (var sentence in trainingVocabulary)
        {
            bigramLM.Add(new StringList(sentence), 1, 2);
        }
        double bigramPerplexity =
            LanguageModelTestUtils.GetPerplexity(bigramLM, testVocabulary, 2);
        ClassicAssert.IsTrue(unigramPerplexity >= bigramPerplexity);

        NGramLanguageModel trigramLM = new(3);
        foreach (var sentence in trainingVocabulary)
        {
            trigramLM.Add(new StringList(sentence), 1, 3);
        }
        double trigramPerplexity =
            LanguageModelTestUtils.GetPerplexity(trigramLM, testVocabulary, 3);
        ClassicAssert.IsTrue(bigramPerplexity >= trigramPerplexity);
    }
}
