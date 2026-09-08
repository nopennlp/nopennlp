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
using System.IO;
using NOpenNLP.Tools.Support;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Postag;

/// <summary>
/// Tests for the <see cref="POSDictionary"/> class.
/// </summary>
public class POSDictionaryTest
{
    private static POSDictionary LoadDictionary(string name)
        => POSDictionary.Create(TestResources.OpenResource("/opennlp/tools/postag/" + name));

    private static POSDictionary SerializeDeserializeDict(POSDictionary dict)
    {
        using var @out = new MemoryStream();
        dict.Serialize(@out);

        using var @in = new MemoryStream(@out.ToArray());
        return POSDictionary.Create(@in);
    }

    [Test]
    public void TestSerialization()
    {
        var dictionary = new POSDictionary();

        dictionary.Put("a", "1", "2", "3");
        dictionary.Put("b", "4", "5", "6");
        dictionary.Put("c", "7", "8", "9");
        dictionary.Put("Always", "RB", "NNP");

        ClassicAssert.IsTrue(dictionary.Equals(SerializeDeserializeDict(dictionary)));
    }

    [Test]
    public void TestLoadingDictionaryWithoutCaseAttribute()
    {
        POSDictionary dict = LoadDictionary("TagDictionaryWithoutCaseAttribute.xml");

        CollectionAssert.AreEqual(new string[] { "NNP" }, dict.GetTags("McKinsey"));
        ClassicAssert.IsNull(dict.GetTags("Mckinsey"));
    }

    [Test]
    public void TestCaseSensitiveDictionary()
    {
        POSDictionary dict = LoadDictionary("TagDictionaryCaseSensitive.xml");

        CollectionAssert.AreEqual(new string[] { "NNP" }, dict.GetTags("McKinsey"));
        ClassicAssert.IsNull(dict.GetTags("Mckinsey"));

        dict = SerializeDeserializeDict(dict);

        CollectionAssert.AreEqual(new string[] { "NNP" }, dict.GetTags("McKinsey"));
        ClassicAssert.IsNull(dict.GetTags("Mckinsey"));
    }

    [Test]
    public void TestCaseInsensitiveDictionary()
    {
        POSDictionary dict = LoadDictionary("TagDictionaryCaseInsensitive.xml");

        CollectionAssert.AreEqual(new string[] { "NNP" }, dict.GetTags("McKinsey"));
        CollectionAssert.AreEqual(new string[] { "NNP" }, dict.GetTags("Mckinsey"));
        CollectionAssert.AreEqual(new string[] { "NNP" }, dict.GetTags("MCKINSEY"));
        CollectionAssert.AreEqual(new string[] { "NNP" }, dict.GetTags("mckinsey"));

        dict = SerializeDeserializeDict(dict);

        CollectionAssert.AreEqual(new string[] { "NNP" }, dict.GetTags("McKinsey"));
        CollectionAssert.AreEqual(new string[] { "NNP" }, dict.GetTags("Mckinsey"));
    }

    [Test]
    public void TestToString()
    {
        POSDictionary dict = LoadDictionary("TagDictionaryCaseInsensitive.xml");
        ClassicAssert.AreEqual("POSDictionary{size=1, caseSensitive=false}", dict.ToString());
        dict = LoadDictionary("TagDictionaryCaseSensitive.xml");
        ClassicAssert.AreEqual("POSDictionary{size=1, caseSensitive=true}", dict.ToString());
    }

    [Test]
    public void TestEqualsAndHashCode()
    {
        POSDictionary dictA = LoadDictionary("TagDictionaryCaseInsensitive.xml");
        POSDictionary dictB = LoadDictionary("TagDictionaryCaseInsensitive.xml");

        ClassicAssert.AreEqual(dictA, dictB);
        ClassicAssert.AreEqual(dictA.GetHashCode(), dictB.GetHashCode());
    }
}
