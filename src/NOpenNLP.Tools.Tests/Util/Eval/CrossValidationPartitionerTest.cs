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

namespace NOpenNLP.Tools.Util.Eval;

/// <summary>
/// Test for the <see cref="CrossValidationPartitioner{E}"/> class.
/// </summary>
public class CrossValidationPartitionerTest
{
    [Test]
    public void TestEmptyDataSet()
    {
        ICollection<string> emptyCollection = new HashSet<string>();

        CrossValidationPartitioner<string> partitioner = new(emptyCollection, 2);

        ClassicAssert.IsTrue(partitioner.HasNext);
        ClassicAssert.IsNull(partitioner.Next().Read());

        ClassicAssert.IsTrue(partitioner.HasNext);
        ClassicAssert.IsNull(partitioner.Next().Read());

        ClassicAssert.IsFalse(partitioner.HasNext);

        // NOpenNLP: upstream expects NoSuchElementException, whose closest .NET
        // counterpart for an exhausted sequence is InvalidOperationException.
        Assert.Throws<InvalidOperationException>((Action)(() => partitioner.Next()));
    }

    /// <summary>
    /// Test 3-fold cross validation on a small sample data set.
    /// </summary>
    [Test]
    public void Test3FoldCV()
    {
        LinkedList<string> data = new();
        data.AddLast("01");
        data.AddLast("02");
        data.AddLast("03");
        data.AddLast("04");
        data.AddLast("05");
        data.AddLast("06");
        data.AddLast("07");
        data.AddLast("08");
        data.AddLast("09");
        data.AddLast("10");

        CrossValidationPartitioner<string> partitioner = new(data, 3);

        // first partition
        ClassicAssert.IsTrue(partitioner.HasNext);
        CrossValidationPartitioner<string>.TrainingSampleStream firstTraining = partitioner.Next();

        ClassicAssert.AreEqual("02", firstTraining.Read());
        ClassicAssert.AreEqual("03", firstTraining.Read());
        ClassicAssert.AreEqual("05", firstTraining.Read());
        ClassicAssert.AreEqual("06", firstTraining.Read());
        ClassicAssert.AreEqual("08", firstTraining.Read());
        ClassicAssert.AreEqual("09", firstTraining.Read());
        ClassicAssert.IsNull(firstTraining.Read());

        IObjectStream<string?> firstTest = firstTraining.GetTestSampleStream();

        ClassicAssert.AreEqual("01", firstTest.Read());
        ClassicAssert.AreEqual("04", firstTest.Read());
        ClassicAssert.AreEqual("07", firstTest.Read());
        ClassicAssert.AreEqual("10", firstTest.Read());
        ClassicAssert.IsNull(firstTest.Read());

        // second partition
        ClassicAssert.IsTrue(partitioner.HasNext);
        CrossValidationPartitioner<string>.TrainingSampleStream secondTraining = partitioner.Next();

        ClassicAssert.AreEqual("01", secondTraining.Read());
        ClassicAssert.AreEqual("03", secondTraining.Read());
        ClassicAssert.AreEqual("04", secondTraining.Read());
        ClassicAssert.AreEqual("06", secondTraining.Read());
        ClassicAssert.AreEqual("07", secondTraining.Read());
        ClassicAssert.AreEqual("09", secondTraining.Read());
        ClassicAssert.AreEqual("10", secondTraining.Read());

        ClassicAssert.IsNull(secondTraining.Read());

        IObjectStream<string?> secondTest = secondTraining.GetTestSampleStream();

        ClassicAssert.AreEqual("02", secondTest.Read());
        ClassicAssert.AreEqual("05", secondTest.Read());
        ClassicAssert.AreEqual("08", secondTest.Read());
        ClassicAssert.IsNull(secondTest.Read());

        // third partition
        ClassicAssert.IsTrue(partitioner.HasNext);
        CrossValidationPartitioner<string>.TrainingSampleStream thirdTraining = partitioner.Next();

        ClassicAssert.AreEqual("01", thirdTraining.Read());
        ClassicAssert.AreEqual("02", thirdTraining.Read());
        ClassicAssert.AreEqual("04", thirdTraining.Read());
        ClassicAssert.AreEqual("05", thirdTraining.Read());
        ClassicAssert.AreEqual("07", thirdTraining.Read());
        ClassicAssert.AreEqual("08", thirdTraining.Read());
        ClassicAssert.AreEqual("10", thirdTraining.Read());
        ClassicAssert.IsNull(thirdTraining.Read());

        IObjectStream<string?> thirdTest = thirdTraining.GetTestSampleStream();

        ClassicAssert.AreEqual("03", thirdTest.Read());
        ClassicAssert.AreEqual("06", thirdTest.Read());
        ClassicAssert.AreEqual("09", thirdTest.Read());
        ClassicAssert.IsNull(thirdTest.Read());

        ClassicAssert.IsFalse(partitioner.HasNext);
    }

    [Test]
    public void TestFailSafty()
    {
        LinkedList<string> data = new();
        data.AddLast("01");
        data.AddLast("02");
        data.AddLast("03");
        data.AddLast("04");

        CrossValidationPartitioner<string> partitioner = new(data, 4);

        // Test that iterator from previous partition fails
        // if it is accessed
        CrossValidationPartitioner<string>.TrainingSampleStream firstTraining = partitioner.Next();
        ClassicAssert.AreEqual("02", firstTraining.Read());

        CrossValidationPartitioner<string>.TrainingSampleStream secondTraining = partitioner.Next();

        // NOpenNLP: upstream expects IllegalStateException, which maps onto
        // InvalidOperationException in .NET. The Action cast matches the convention
        // used elsewhere in the ported tests: without it a lambda is ambiguous
        // between NUnit's TestDelegate and Action overloads of Assert.Throws.
        Assert.Throws<InvalidOperationException>((Action)(() => firstTraining.Read()));

        Assert.Throws<InvalidOperationException>((Action)(() => firstTraining.GetTestSampleStream()));

        // Test that training iterator fails if there is a test iterator
        secondTraining.GetTestSampleStream();

        Assert.Throws<InvalidOperationException>((Action)(() => secondTraining.Read()));

        // Test that test iterator from previous partition fails
        // if there is a new partition
        CrossValidationPartitioner<string>.TrainingSampleStream thirdTraining = partitioner.Next();
        IObjectStream<string?> thridTest = thirdTraining.GetTestSampleStream();

        ClassicAssert.IsTrue(partitioner.HasNext);
        partitioner.Next();

        Assert.Throws<InvalidOperationException>((Action)(() => thridTest.Read()));
    }

    [Test]
    public void TestToString()
    {
        ICollection<string> emptyCollection = new HashSet<string>();
        new CrossValidationPartitioner<string>(emptyCollection, 10).ToString();
    }
}
