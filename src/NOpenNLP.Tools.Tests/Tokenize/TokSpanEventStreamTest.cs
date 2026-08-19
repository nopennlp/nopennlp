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
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Tokenize;

/// <summary>
/// Tests for the <see cref="TokSpanEventStream"/> class.
/// </summary>
public class TokSpanEventStreamTest
{
    /// <summary>
    /// Tests the event stream for correctly generated outcomes.
    /// </summary>
    [Test]
    public void TestEventOutcomes()
    {
        IObjectStream<string?> sentenceStream =
            ObjectStreamUtils.CreateObjectStream("\"<SPLIT>out<SPLIT>.<SPLIT>\"");

        IObjectStream<TokenSample?> tokenSampleStream = new TokenSampleStream(sentenceStream);

        using (IObjectStream<Event?> eventStream = new TokSpanEventStream(tokenSampleStream, false))
        {
            ClassicAssert.AreEqual(TokenizerME.SPLIT, eventStream.Read()!.Outcome);
            ClassicAssert.AreEqual(TokenizerME.NO_SPLIT, eventStream.Read()!.Outcome);
            ClassicAssert.AreEqual(TokenizerME.NO_SPLIT, eventStream.Read()!.Outcome);
            ClassicAssert.AreEqual(TokenizerME.SPLIT, eventStream.Read()!.Outcome);
            ClassicAssert.AreEqual(TokenizerME.SPLIT, eventStream.Read()!.Outcome);

            ClassicAssert.IsNull(eventStream.Read());
            ClassicAssert.IsNull(eventStream.Read());
        }
    }
}
