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
using System.IO;
using System.Text;
using NOpenNLP.Tools.Support;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Parser.Lang.En;

public class HeadRulesTest
{
    /// <summary>
    /// Positive: a well-formed head rules line with a small tag count loads without error.
    /// </summary>
    [Test]
    public void TestValidTagCountLoads()
    {
        // "5 NP 1 NN NNS" -- num=5, tags=3 (5-2=3)
        const string rules = "5 NP 1 NN NNS NNP\n";
        _ = new HeadRules(new StringReader(rules));
    }

    /// <summary>
    /// Negative: a head rules line with a huge tag count must throw <see cref="IOException"/>,
    /// not attempt to allocate <see cref="int.MaxValue"/> bytes.
    /// </summary>
    [Test]
    public void TestOversizedTagCountThrows()
    {
        const string rules = "2147483647 NP 1\n";
        Assert.Throws<IOException>((Action)(() => _ = new HeadRules(new StringReader(rules))));
    }

    /// <summary>
    /// Negative: a tag count that would produce a negative array size must throw
    /// <see cref="IOException"/>.
    /// </summary>
    [Test]
    public void TestNegativeTagCountThrows()
    {
        const string rules = "1 NP 1\n";  // 1 - 2 = -1
        Assert.Throws<IOException>((Action)(() => _ = new HeadRules(new StringReader(rules))));
    }

    /// <summary>
    /// Boundary: a value just above <c>MAX_TAGS_PER_RULE</c> (1003 -> numTags = 1001) must
    /// throw <see cref="IOException"/>.
    /// </summary>
    [Test]
    public void TestJustAboveLimitThrows()
    {
        // 1003 declared; 1003 - 2 = 1001 tags, which exceeds MAX_TAGS_PER_RULE (1000)
        const string rules = "1003 NP 1\n";
        Assert.Throws<IOException>((Action)(() => _ = new HeadRules(new StringReader(rules))));
    }

    /// <summary>
    /// Negative: a non-numeric token count must throw <see cref="IOException"/>, not
    /// <see cref="System.FormatException"/>.
    /// </summary>
    [Test]
    public void TestNonNumericTagCountThrows()
    {
        const string rules = "NaN NP 1\n";
        Assert.Throws<IOException>((Action)(() => _ = new HeadRules(new StringReader(rules))));
    }

    // NOpenNLP: upstream's testSerialization round-trips the rules through
    // HeadRules.serialize(Writer) and asserts the re-read rules equal the originals.
    // Serializing head rules out is part of the training half, which this port omits, so that
    // test has no counterpart here. The read half that inference depends on is covered by the
    // port-specific test below.

    /// <summary>
    /// Verifies the head rules parse from the upstream <c>en_head_rules</c> resource,
    /// which is the path inference takes when loading rules from a model.
    /// </summary>
    [Test]
    [NOpenNLPSpecific]
    public void TestReadHeadRules()
    {
        using Stream headRulesIn = TestResources.OpenResource("/opennlp/tools/parser/en_head_rules");
        using StreamReader reader = new(headRulesIn, Encoding.UTF8);

        HeadRules headRules = new(reader);

        ClassicAssert.IsNotNull(headRules.PunctuationTags);
        CollectionAssert.AreEquivalent(new[] { ".", ",", "``", "''" }, headRules.PunctuationTags);

        // A constituent whose type has a rule should resolve to a head, and the head of a
        // single-child constituent is that child's own head.
        Parse np = Parse.ParseParse("(TOP (NP (DT the) (NN dog)))");
        Parse[] children = np.GetChildren()[0].GetChildren();

        ClassicAssert.IsNotNull(headRules.GetHead(children, "NP"));
    }
}
