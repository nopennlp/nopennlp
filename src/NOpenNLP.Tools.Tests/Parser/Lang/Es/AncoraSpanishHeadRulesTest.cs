/*
 * Copyright 2026 NOpenNLP Contributors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.IO;
using NOpenNLP.Tools.Support;
using NUnit.Framework;

namespace NOpenNLP.Tools.Parser.Lang.Es;

/// <summary>
/// The tag-count bounds OPENNLP-1826 added to <see cref="AncoraSpanishHeadRules"/> are the
/// same ones it added to the English <c>HeadRules</c>, but upstream only wrote tests for the
/// English class. Both readers parse the same untrusted count field out of a model artifact,
/// so the Spanish one is covered here to the same depth.
/// </summary>
[NOpenNLPSpecific]
public class AncoraSpanishHeadRulesTest
{
    /// <summary>
    /// Positive: a well-formed head rules line with a small tag count loads without error.
    /// </summary>
    [Test]
    public void TestValidTagCountLoads()
    {
        // num=5, so 5-2=3 tags follow
        const string rules = "5 grup.nom 1 nc np aq\n";
        _ = new AncoraSpanishHeadRules(new StringReader(rules));
    }

    /// <summary>
    /// Negative: a head rules line with a huge tag count must throw <see cref="IOException"/>,
    /// not attempt to allocate <see cref="int.MaxValue"/> entries.
    /// </summary>
    [Test]
    public void TestOversizedTagCountThrows()
    {
        const string rules = "2147483647 grup.nom 1\n";
        Assert.Throws<IOException>((Action)(() => _ = new AncoraSpanishHeadRules(new StringReader(rules))));
    }

    /// <summary>
    /// Negative: a tag count that would produce a negative array size must throw
    /// <see cref="IOException"/>.
    /// </summary>
    [Test]
    public void TestNegativeTagCountThrows()
    {
        const string rules = "1 grup.nom 1\n";  // 1 - 2 = -1
        Assert.Throws<IOException>((Action)(() => _ = new AncoraSpanishHeadRules(new StringReader(rules))));
    }

    /// <summary>
    /// Boundary: a value just above <c>MAX_TAGS_PER_RULE</c> (1003 -> numTags = 1001) must
    /// throw <see cref="IOException"/>.
    /// </summary>
    [Test]
    public void TestJustAboveLimitThrows()
    {
        const string rules = "1003 grup.nom 1\n";
        Assert.Throws<IOException>((Action)(() => _ = new AncoraSpanishHeadRules(new StringReader(rules))));
    }

    /// <summary>
    /// Negative: a non-numeric token count must throw <see cref="IOException"/>, not
    /// <see cref="System.FormatException"/>.
    /// </summary>
    [Test]
    public void TestNonNumericTagCountThrows()
    {
        const string rules = "NaN grup.nom 1\n";
        Assert.Throws<IOException>((Action)(() => _ = new AncoraSpanishHeadRules(new StringReader(rules))));
    }
}
