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
using NOpenNLP.Tools.Ngram;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;
using NOpenNLP.Tools.Util.Normalizer;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Langdetect;

public class DummyFactory : LanguageDetectorFactory
{
    public DummyFactory()
        : base()
    {
    }

    public override void Init() => base.Init();

    public override ILanguageDetectorContextGenerator GetContextGenerator() =>
        new MyContectGenerator(2, 5, new UpperCaseNormalizer());

    public class UpperCaseNormalizer : ICharSequenceNormalizer
    {
        public string Normalize(string text) => text.ToUpperInvariant();
    }

    public class MyContectGenerator(int min, int max, params ICharSequenceNormalizer[] normalizers)
        : DefaultLanguageDetectorContextGenerator(min, max, normalizers)
    {
        public override string[] GetContext(string document)
        {
            string[] superContext = base.GetContext(document);

            IList<string> context = new JCG.List<string>(superContext);

            document = this.normalizer.Normalize(document);

            SimpleTokenizer tokenizer = SimpleTokenizer.INSTANCE;
            string[] words = tokenizer.Tokenize(document);
            NGramModel tokenNgramModel = new();
            if (words.Length > 0)
            {
                tokenNgramModel.Add(new StringList(words), 1, 3);

                foreach (StringList tokenList in tokenNgramModel)
                {
                    if (tokenList.Count > 0)
                    {
                        context.Add("tg=" + tokenList.ToString());
                    }
                }
            }

            return [.. context];
        }
    }
}
