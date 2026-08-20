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
using System;
using System.Text;
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Tokenize;

/// <summary>
/// Tests for the <see cref="TokenizerME"/> class.
/// <para/>
/// This test trains the tokenizer with a few sample tokens
/// and then predicts a token. This test checks if the
/// tokenizer code can be executed.
/// </summary>
/// <seealso cref="TokenizerME"/>
public class TokenizerMETest
{
    [Test]
    public void TestTokenizerSimpleModel()
    {
        TokenizerModel model = TokenizerTestUtil.CreateSimpleMaxentTokenModel();

        TokenizerME tokenizer = new TokenizerME(model);

        string[] tokens = tokenizer.Tokenize("test,");

        ClassicAssert.AreEqual(2, tokens.Length);
        ClassicAssert.AreEqual("test", tokens[0]);
        ClassicAssert.AreEqual(",", tokens[1]);
    }

    [Test]
    public void TestTokenizer()
    {
        TokenizerModel model = TokenizerTestUtil.CreateMaxentTokenModel();

        TokenizerME tokenizer = new TokenizerME(model);
        string[] tokens = tokenizer.Tokenize("Sounds like it's not properly thought through!");

        ClassicAssert.AreEqual(9, tokens.Length);
        ClassicAssert.AreEqual("Sounds", tokens[0]);
        ClassicAssert.AreEqual("like", tokens[1]);
        ClassicAssert.AreEqual("it", tokens[2]);
        ClassicAssert.AreEqual("'s", tokens[3]);
        ClassicAssert.AreEqual("not", tokens[4]);
        ClassicAssert.AreEqual("properly", tokens[5]);
        ClassicAssert.AreEqual("thought", tokens[6]);
        ClassicAssert.AreEqual("through", tokens[7]);
        ClassicAssert.AreEqual("!", tokens[8]);
    }

    [Test]
    public void TestInsufficientData()
    {
        IInputStreamFactory trainDataIn = new ResourceAsStreamFactory(
            "/opennlp/tools/tokenize/token-insufficient.train");

        IObjectStream<TokenSample?> samples = new TokenSampleStream(
            new PlainTextByLineStream(trainDataIn, Encoding.UTF8));

        TrainingParameters mlParams = new TrainingParameters();
        mlParams.Put(TrainingParameters.ITERATIONS_PARAM, 100);
        mlParams.Put(TrainingParameters.CUTOFF_PARAM, 5);

        Assert.Throws<InsufficientTrainingDataException>((Action)(() =>
            TokenizerME.Train(samples, TokenizerFactory.Create(null, "eng", null, true, null!)!, mlParams)));
    }
}
