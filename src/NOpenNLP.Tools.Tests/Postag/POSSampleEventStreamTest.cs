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

namespace NOpenNLP.Tools.Postag;

/// <summary>
/// Tests for the <see cref="POSSampleEventStream"/> class.
/// </summary>
public class POSSampleEventStreamTest
{
    /// <summary>
    /// Tests that the outcomes for a single sentence match the
    /// expected outcomes.
    /// </summary>
    [Test]
    public void TestOutcomesForSingleSentence()
    {
        string sentence = "That_DT sounds_VBZ good_JJ ._.";

        POSSample sample = POSSample.Parse(sentence);

        using IObjectStream<Event?> eventStream = new POSSampleEventStream(
            ObjectStreamUtils.CreateObjectStream(sample));

        ClassicAssert.AreEqual("DT", eventStream.Read()?.Outcome);
        ClassicAssert.AreEqual("VBZ", eventStream.Read()?.Outcome);
        ClassicAssert.AreEqual("JJ", eventStream.Read()?.Outcome);
        ClassicAssert.AreEqual(".", eventStream.Read()?.Outcome);
        ClassicAssert.IsNull(eventStream.Read());
    }
}
