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

// This file has been modified from the original Apache OpenNLP 1.9.4 source:
// translated from Java to C# and adapted for .NET. See NOTICE.
using System.Collections.Generic;
using NOpenNLP.Tools.Support;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Lemmatizer;

public class DictionaryLemmatizerMultiTest
{
    private static DictionaryLemmatizer dictionaryLemmatizer;

    [OneTimeSetUp]
    public void LoadDictionary()
    {
        dictionaryLemmatizer = new DictionaryLemmatizer(
            TestResources.OpenResource("/opennlp/tools/lemmatizer/smalldictionarymulti.dict")
        );
    }

    [Test]
    public void TestForNullPointerException()
    {
        IList<string> sentence = ["The", "dogs", "were", "running", "and", "barking",
            "down", "the", "street"];
        IList<string> sentencePOS = ["DT", "NNS", "VBD", "VBG", "CC", "VBG", "RP", "DT", "NN"];
        List<IList<string>> expectedLemmas =
        [
            new List<string> { "the" },
            new List<string> { "dog" },
            new List<string> { "is" },
            new List<string> { "run,run" },
            new List<string> { "and" },
            new List<string> { "bark,bark" },
            new List<string> { "down" },
            new List<string> { "the" },
            new List<string> { "street" },
        ];

        IList<IList<string>> actualLemmas = dictionaryLemmatizer.Lemmatize(sentence, sentencePOS);

        for (int i = 0; i < sentence.Count; i++)
        {
            // don't compare cases where the word is not in the dictionary...
            if (!actualLemmas[0][0].Equals("O"))
            {
                CollectionAssert.AreEqual(expectedLemmas[i], actualLemmas[i]);
            }
        }
    }
}
