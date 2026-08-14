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
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Ml;

public class ArrayMathTest
{
    [Test]
    public void TestInnerProductDoubleNaN()
    {
        ClassicAssert.IsTrue(double.IsNaN(ArrayMath.InnerProduct(null!, [0])));
        ClassicAssert.IsTrue(double.IsNaN(ArrayMath.InnerProduct([0], null!)));
        ClassicAssert.IsTrue(double.IsNaN(ArrayMath.InnerProduct([0, 1, 2], [0, 1, 2, 3])));
    }

    [Test]
    public void TestInnerProduct()
    {
        ClassicAssert.AreEqual(0, ArrayMath.InnerProduct([], []), 0);
        ClassicAssert.AreEqual(-1, ArrayMath.InnerProduct([1], [-1]), 0);
        ClassicAssert.AreEqual(14, ArrayMath.InnerProduct([1, 2, 3], [1, 2, 3]), 0);
    }

    [Test]
    public void TestL1Norm()
    {
        ClassicAssert.AreEqual(0, ArrayMath.L1norm([]), 0);
        ClassicAssert.AreEqual(0, ArrayMath.L1norm([0]), 0);
        ClassicAssert.AreEqual(2, ArrayMath.L1norm([1, -1]), 0);
        ClassicAssert.AreEqual(55, ArrayMath.L1norm([1, -2, 3, -4, 5, -6, 7, -8, 9, -10]), 0);
    }

    [Test]
    public void TestL2Norm()
    {
        ClassicAssert.AreEqual(0, ArrayMath.L2norm([]), 0);
        ClassicAssert.AreEqual(0, ArrayMath.L2norm([0]), 0);
        ClassicAssert.AreEqual(1.41421, ArrayMath.L2norm([1, -1]), 0.001);
        ClassicAssert.AreEqual(0.54772, ArrayMath.L2norm([0.1, -0.2, 0.3, -0.4]), 0.001);
    }

    [Test]
    public void TestInvL2Norm()
    {
        ClassicAssert.AreEqual(0.70711, ArrayMath.InvL2norm([1, -1]), 0.001);
        ClassicAssert.AreEqual(1.82575, ArrayMath.InvL2norm([0.1, -0.2, 0.3, -0.4]), 0.001);
    }

    [Test]
    public void TestLogSumOfExps()
    {
        ClassicAssert.AreEqual(0, ArrayMath.LogSumOfExps([0]), 0);
        ClassicAssert.AreEqual(1, ArrayMath.LogSumOfExps([1]), 0);
        ClassicAssert.AreEqual(2.048587, ArrayMath.LogSumOfExps([-1, 2]), 0.001);
        ClassicAssert.AreEqual(1.472216, ArrayMath.LogSumOfExps([-0.1, 0.2, -0.3, 0.4]), 0.001);
    }

    [Test]
    public void TestMax()
    {
        ClassicAssert.AreEqual(0, ArrayMath.Max([0]), 0);
        ClassicAssert.AreEqual(0, ArrayMath.Max([0, 0, 0]), 0);
        ClassicAssert.AreEqual(2, ArrayMath.Max([0, 1, 2]), 0);
        ClassicAssert.AreEqual(200, ArrayMath.Max([100, 200, 2]), 0);
        ClassicAssert.AreEqual(300, ArrayMath.Max([100, 200, 300, -10, -20]), 0);
    }

    [Test]
    public void TestArgmaxException1()
    {
        // NOpenNLP: upstream declares @Test(expected = IllegalArgumentException.class);
        // ArgumentException is the .NET counterpart.
        Assert.Throws<ArgumentException>((Action)(() => ArrayMath.Argmax(null!)));
    }

    [Test]
    public void TestArgmaxException2()
    {
        // NOpenNLP: see TestArgmaxException1 regarding the exception type.
        Assert.Throws<ArgumentException>((Action)(() => ArrayMath.Argmax([])));
    }

    [Test]
    public void TestArgmax()
    {
        ClassicAssert.AreEqual(0, ArrayMath.Argmax([0]));
        ClassicAssert.AreEqual(0, ArrayMath.Argmax([0, 0, 0]));
        ClassicAssert.AreEqual(2, ArrayMath.Argmax([0, 1, 2]));
        ClassicAssert.AreEqual(1, ArrayMath.Argmax([100, 200, 2]));
        ClassicAssert.AreEqual(2, ArrayMath.Argmax([100, 200, 300, -10, -20]));
    }

    [Test]
    public void TestToDoubleArray()
    {
        // NOpenNLP: upstream passes Collections.EMPTY_LIST and Arrays.asList(...);
        // the .NET counterparts are an empty List<double> and a collection expression.
        ClassicAssert.AreEqual(0, ArrayMath.ToDoubleArray(new List<double>()).Length);
        CollectionAssert.AreEqual(new double[] { 0 }, ArrayMath.ToDoubleArray([0D]));
        CollectionAssert.AreEqual(new double[] { 0, 1, -2.5, -0.3, 4 },
            ArrayMath.ToDoubleArray([0D, 1D, -2.5D, -0.3D, 4D]));
    }

    [Test]
    public void TestToInt32Array()
    {
        ClassicAssert.AreEqual(0, ArrayMath.ToIntArray(new List<int>()).Length);
        CollectionAssert.AreEqual(new int[] { 0 }, ArrayMath.ToIntArray([0]));
        CollectionAssert.AreEqual(new int[] { 0, 1, -2, -3, 4},
            ArrayMath.ToIntArray([0, 1, -2, -3, 4]));
    }
}
