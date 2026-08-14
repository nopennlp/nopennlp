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
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Util;

/// <summary>
/// Tests for the <see cref="Sequence"/> class.
/// </summary>
public class SequenceTest
{
    /// <summary>
    /// Tests the copy constructor <see cref="Sequence(Sequence)"/>.
    /// </summary>
    [Test]
    public void TestCopyConstructor()
    {
        Sequence sequence = new Sequence();
        sequence.Add("a", 10);
        sequence.Add("b", 20);

        Sequence copy = new Sequence(sequence);

        CollectionAssert.AreEqual(sequence.Outcomes, copy.Outcomes);
        // NOpenNLP: upstream uses assertArrayEquals(double[], double[], delta).
        // ClassicAssert.AreEqual's delta overload is scalar-only, so the array
        // comparison goes through the constraint model with a tolerance.
        Assert.That(copy.Probs, Is.EqualTo(sequence.Probs).Within(0.0));
        ClassicAssert.IsTrue(sequence.CompareTo(copy) == 0);
    }

    /// <summary>
    /// Tests <see cref="Sequence.Add(string, double)"/>, also
    /// tests <see cref="Sequence.Outcomes"/> and <see cref="Sequence.Probs"/>.
    /// </summary>
    [Test]
    public void TestAddMethod()
    {
        Sequence sequence = new Sequence();
        sequence.Add("a", 10d);

        // check if insert was successful
        ClassicAssert.AreEqual("a", sequence.Outcomes[0]);
        ClassicAssert.AreEqual(10d, sequence.Probs[0], 0d);
    }

    /// <summary>
    /// Tests <see cref="Sequence.CompareTo(Sequence)"/>.
    /// </summary>
    [Test]
    public void TestCompareTo()
    {
        Sequence lowScore = new Sequence();
        lowScore.Add("A", 1d);
        lowScore.Add("B", 2d);
        lowScore.Add("C", 3d);

        Sequence highScore = new Sequence();
        lowScore.Add("A", 7d);
        lowScore.Add("B", 8d);
        lowScore.Add("C", 9d);

        ClassicAssert.AreEqual(-1, lowScore.CompareTo(highScore));
        ClassicAssert.AreEqual(1, highScore.CompareTo(lowScore));
    }

    /// <summary>
    /// Checks that <see cref="Sequence.ToString()"/> is executable.
    /// </summary>
    [Test]
    public void TestToString()
    {
        new Sequence().ToString();

        Sequence sequence = new Sequence();
        sequence.Add("test", 0.1d);
        sequence.ToString();
    }
}
