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
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Util;

public class ObjectStreamUtilsTest
{
    [Test]
    public void BuildStreamTest()
    {
        string[] data = ["dog", "cat", "pig", "frog"];

        // make a stream out of the data array...
        IObjectStream<string?> stream = ObjectStreamUtils.CreateObjectStream(data);
        Compare(stream, data);

        // make a stream out of a list...
        // NOpenNLP: upstream calls the createObjectStream(Collection) overload,
        // which the port omits (see ObjectStreamUtils); constructing a
        // CollectionObjectStream directly is the documented equivalent.
        IList<string> dataList = new JCG.List<string>(data);
        stream = new CollectionObjectStream<string>(new JCG.List<string>(data));
        Compare(stream, data);

        // make a stream out of a set...
        // A treeSet will order the set in Alphabetical order, so
        // we can compare it with the sorted Array, but this changes the
        // array.  so it must be checked last.
        Array.Sort(data, StringComparer.Ordinal);
        stream = new CollectionObjectStream<string>(
            new JCG.SortedSet<string>(dataList, StringComparer.Ordinal));
        Compare(stream, data);
    }

    [Test]
    public void ConcatenateStreamTest()
    {
        string[] data1 = ["dog1", "cat1", "pig1", "frog1"];
        string[] data2 = ["dog2", "cat2", "pig2", "frog2"];
        string[] expected = ["dog1", "cat1", "pig1", "frog1", "dog2", "cat2", "pig2", "frog2"];

        // take individual streams and concatenate them as 1 stream.
        // Note: this is much easier than trying to create an array of
        // streams which needs to have annotation to avoid warnings about
        // generics and arrays.
        IObjectStream<string?> stream = ObjectStreamUtils.ConcatenateObjectStream(
            ObjectStreamUtils.CreateObjectStream(data1),
            ObjectStreamUtils.CreateObjectStream(data2));
        Compare(stream, expected);

        // test that collections of streams can be concatenated...
        IList<IObjectStream<string?>> listOfStreams = new JCG.List<IObjectStream<string?>>
        {
            ObjectStreamUtils.CreateObjectStream(data1),
            ObjectStreamUtils.CreateObjectStream(data2),
        };
        // NOpenNLP: upstream calls the concatenateObjectStream(Collection)
        // overload, which the port omits (see ObjectStreamUtils).
        stream = ObjectStreamUtils.ConcatenateObjectStream([.. listOfStreams]);
        Compare(stream, expected);

        // test that sets of streams can be concatenated..
        ISet<IObjectStream<string?>> streamSet = new JCG.HashSet<IObjectStream<string?>>
        {
            ObjectStreamUtils.CreateObjectStream(data1),
            ObjectStreamUtils.CreateObjectStream(data2),
        };
        stream = ObjectStreamUtils.ConcatenateObjectStream([.. streamSet]);
        // The order the of the streams in the set is not know a priori
        // just check that the dog, cat, pig. frog is in the write order...
        CompareUpToLastCharacter(stream, expected);
    }

    private void Compare(IObjectStream<string?> stream, string[] expectedValues)
    {
        string? value;
        int i = 0;
        while ((value = stream.Read()) != null)
        {
            ClassicAssert.IsTrue(i < expectedValues.Length,
                "The stream is longer than expected at index: " + i +
                " expected length: " + expectedValues.Length +
                " expectedValues" + "[" + string.Join(", ", expectedValues) + "]");
            ClassicAssert.AreEqual(expectedValues[i++], value);
        }
    }

    private void CompareUpToLastCharacter(IObjectStream<string?> stream, string[] expectedValues)
    {
        string? value;
        int i = 0;
        while ((value = stream.Read()) != null)
        {
            ClassicAssert.IsTrue(i < expectedValues.Length,
                "The stream is longer than expected at index: " + i +
                " expected length: " + expectedValues.Length +
                " expectedValues" + "[" + string.Join(", ", expectedValues) + "]");
            ClassicAssert.AreEqual(
                expectedValues[i].Substring(0, expectedValues[i].Length - 1),
                value.Substring(0, value.Length - 1));
            i++;
        }
    }
}
