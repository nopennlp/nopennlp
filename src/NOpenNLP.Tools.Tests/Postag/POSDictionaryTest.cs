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
using NOpenNLP.Tools.Support;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Postag;

/// <summary>
/// Tests for the <see cref="POSDictionary"/> class.
/// </summary>
/// <remarks>
/// NOpenNLP: upstream round-trips several of these dictionaries through
/// POSDictionary.serialize(...). Serialize is not ported yet (it is commented out
/// in POSDictionary), so the serialize half of those tests is omitted and noted on
/// each one. TestSerialization, which tests nothing else, is omitted entirely.
/// Restore all of them when Serialize is ported.
/// </remarks>
public class POSDictionaryTest
{
    private static POSDictionary LoadDictionary(string name)
        => POSDictionary.Create(TestResources.OpenResource("/opennlp/tools/postag/" + name));

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
        // NOpenNLP: upstream repeats these assertions after a serialize/deserialize
        // round trip. See the remarks on this class.
        POSDictionary dict = LoadDictionary("TagDictionaryCaseSensitive.xml");

        CollectionAssert.AreEqual(new string[] { "NNP" }, dict.GetTags("McKinsey"));
        ClassicAssert.IsNull(dict.GetTags("Mckinsey"));
    }

    [Test]
    public void TestCaseInsensitiveDictionary()
    {
        // NOpenNLP: upstream repeats these assertions after a serialize/deserialize
        // round trip. See the remarks on this class.
        POSDictionary dict = LoadDictionary("TagDictionaryCaseInsensitive.xml");

        CollectionAssert.AreEqual(new string[] { "NNP" }, dict.GetTags("McKinsey"));
        CollectionAssert.AreEqual(new string[] { "NNP" }, dict.GetTags("Mckinsey"));
        CollectionAssert.AreEqual(new string[] { "NNP" }, dict.GetTags("MCKINSEY"));
        CollectionAssert.AreEqual(new string[] { "NNP" }, dict.GetTags("mckinsey"));
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
