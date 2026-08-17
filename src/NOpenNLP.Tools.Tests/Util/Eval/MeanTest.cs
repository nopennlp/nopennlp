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
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Util.Eval;

/// <summary>
/// Tests for the <see cref="Mean"/> class.
/// </summary>
public class MeanTest
{
    [Test]
    public void TestMeanCalculation()
    {
        Mean a = new();
        a.Add(1);
        ClassicAssert.AreEqual(1, a.Count);
        ClassicAssert.AreEqual(1d, a.Value, 0.00001d);

        a.Add(1);
        ClassicAssert.AreEqual(2, a.Count);
        ClassicAssert.AreEqual(1d, a.Value, 0.00001d);
        a.ToString();

        Mean b = new();
        b.Add(0.5);
        ClassicAssert.AreEqual(1, b.Count);
        ClassicAssert.AreEqual(0.5d, b.Value, 0.00001d);

        b.Add(2);
        ClassicAssert.AreEqual(2, b.Count);
        ClassicAssert.AreEqual(1.25d, b.Value, 0.00001d);
        b.ToString();

        Mean c = new();
        ClassicAssert.AreEqual(0, c.Count);
        ClassicAssert.AreEqual(0d, c.Value, 0.00001d);
        c.ToString();
    }
}
