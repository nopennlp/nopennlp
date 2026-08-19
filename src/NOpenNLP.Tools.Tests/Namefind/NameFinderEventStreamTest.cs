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
using NOpenNLP.Tools.Util.Featuregen;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Namefind;

/// <summary>
/// This is the test class for <see cref="NameFinderEventStream"/>.
/// </summary>
public class NameFinderEventStreamTest
{
    private static readonly string[] SENTENCE = ["Elise", "Wendel", "appreciated",
        "the", "hint", "and", "enjoyed", "a", "delicious", "traditional", "meal",
        "."];

    private static readonly INameContextGenerator CG =
        new DefaultNameContextGenerator((IAdaptiveFeatureGenerator[]?)null);

    /// <summary>
    /// Tests the correctly generated outcomes for a test sentence.
    /// </summary>
    [Test]
    public void TestOutcomesForSingleTypeSentence()
    {
        NameSample nameSample = new(SENTENCE,
            [new Span(0, 2, "person")], false);

        using IObjectStream<Event?> eventStream = new NameFinderEventStream(
            ObjectStreamUtils.CreateObjectStream(nameSample));

        ClassicAssert.AreEqual("person-" + NameFinderME.START, eventStream.Read()!.Outcome);
        ClassicAssert.AreEqual("person-" + NameFinderME.CONTINUE, eventStream.Read()!.Outcome);

        for (int i = 0; i < 10; i++)
        {
            ClassicAssert.AreEqual(NameFinderME.OTHER, eventStream.Read()!.Outcome);
        }

        ClassicAssert.IsNull(eventStream.Read());
    }

    /// <summary>
    /// Tests the correctly generated outcomes for a test sentence. If the Span
    /// declares its type, passing the type to event stream has no effect
    /// </summary>
    [Test]
    public void TestOutcomesTypeCantOverride()
    {
        string type = "XYZ";

        NameSample nameSample = new(SENTENCE,
            [new Span(0, 2, "person")], false);

        IObjectStream<Event?> eventStream = new NameFinderEventStream(
            ObjectStreamUtils.CreateObjectStream(nameSample), type, CG, null);

        string prefix = type + "-";
        ClassicAssert.AreEqual(prefix + NameFinderME.START, eventStream.Read()!.Outcome);
        ClassicAssert.AreEqual(prefix + NameFinderME.CONTINUE, eventStream.Read()!.Outcome);

        for (int i = 0; i < 10; i++)
        {
            ClassicAssert.AreEqual(NameFinderME.OTHER, eventStream.Read()!.Outcome);
        }

        ClassicAssert.IsNull(eventStream.Read());
        eventStream.Dispose();
    }

    /// <summary>
    /// Tests the correctly generated outcomes for a test sentence. If the Span
    /// does not declare its type and the user passed a type, use the type from
    /// user
    /// </summary>
    [Test]
    public void TestOutcomesWithType()
    {
        string type = "XYZ";

        NameSample nameSample = new(SENTENCE,
            [new Span(0, 2)], false);

        IObjectStream<Event?> eventStream = new NameFinderEventStream(
            ObjectStreamUtils.CreateObjectStream(nameSample), type, CG, null);

        string prefix = type + "-";
        ClassicAssert.AreEqual(prefix + NameFinderME.START, eventStream.Read()!.Outcome);
        ClassicAssert.AreEqual(prefix + NameFinderME.CONTINUE, eventStream.Read()!.Outcome);

        for (int i = 0; i < 10; i++)
        {
            ClassicAssert.AreEqual(NameFinderME.OTHER, eventStream.Read()!.Outcome);
        }

        ClassicAssert.IsNull(eventStream.Read());
        eventStream.Dispose();
    }

    /// <summary>
    /// Tests the correctly generated outcomes for a test sentence. If the Span
    /// does not declare its type and the user did not set a type, it will use
    /// "default".
    /// </summary>
    [Test]
    public void TestOutcomesTypeEmpty()
    {
        NameSample nameSample = new(SENTENCE,
            [new Span(0, 2)], false);

        IObjectStream<Event?> eventStream = new NameFinderEventStream(
            ObjectStreamUtils.CreateObjectStream(nameSample), null, CG, null);

        string prefix = "default-";
        ClassicAssert.AreEqual(prefix + NameFinderME.START, eventStream.Read()!.Outcome);
        ClassicAssert.AreEqual(prefix + NameFinderME.CONTINUE, eventStream.Read()!.Outcome);

        for (int i = 0; i < 10; i++)
        {
            ClassicAssert.AreEqual(NameFinderME.OTHER, eventStream.Read()!.Outcome);
        }

        ClassicAssert.IsNull(eventStream.Read());
        eventStream.Dispose();
    }
}
