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

// This file has been modified from the original Apache OpenNLP 1.9.1 source:
// translated from Java to C# and adapted for .NET. See NOTICE.
using System;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Ml.Model;

public class EventTest
{
    [Test]
    public void TestNullOutcome()
    {
        // NOpenNLP: upstream catches NullPointerException from Objects.requireNonNull;
        // ArgumentNullException is the .NET counterpart.
        Assert.Throws<ArgumentNullException>((Action)(() => new Event(null, ["aa", "bb", "cc"])));
    }

    [Test]
    public void TestNullContext()
    {
        // NOpenNLP: see TestNullOutcome regarding the exception type.
        Assert.Throws<ArgumentNullException>((Action)(() => new Event("o1", null)));
    }

    [Test]
    public void TestWithValues()
    {
        Event @event = new Event("o1",
            ["aa", "bb", "cc"]);

        ClassicAssert.AreEqual("o1", @event.Outcome);
        CollectionAssert.AreEqual(new string[] { "aa", "bb", "cc" }, @event.Context);
        ClassicAssert.IsNull(@event.Values);
        ClassicAssert.AreEqual("o1 [aa bb cc]", @event.ToString());
    }

    [Test]
    public void TestWithoutValues()
    {
        Event @event = new Event("o1",
            ["aa", "bb", "cc"],
            [0.2F, 0.4F, 0.4F]);

        ClassicAssert.AreEqual("o1", @event.Outcome);
        CollectionAssert.AreEqual(new string[] { "aa", "bb", "cc" }, @event.Context);
        // NOpenNLP: upstream uses assertArrayEquals(float[], float[], delta); the
        // array comparison goes through the constraint model with a tolerance.
        Assert.That(@event.Values, Is.EqualTo(new float[] { 0.2F, 0.4F, 0.4F }).Within(0.001F));
        ClassicAssert.AreEqual("o1 [aa=0.2 bb=0.4 cc=0.4]", @event.ToString());
    }
}
