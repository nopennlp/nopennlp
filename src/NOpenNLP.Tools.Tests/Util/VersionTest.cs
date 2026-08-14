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

// This file has been modified from the original Apache OpenNLP 1.9.4 source:
// translated from Java to C# and adapted for .NET. See NOTICE.
using System;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Util;

/// <summary>
/// Tests for the <see cref="Version"/> class.
/// </summary>
public class VersionTest
{
    [Test]
    public void TestParse()
    {
        Version referenceVersion = Version.CurrentVersion();
        ClassicAssert.AreEqual(referenceVersion, Version.Parse(referenceVersion.ToString()));

        ClassicAssert.AreEqual(new Version(1, 5, 2, false),
            Version.Parse("1.5.2-incubating"));
        ClassicAssert.AreEqual(new Version(1, 5, 2, false),
            Version.Parse("1.5.2"));
    }

    [Test]
    public void TestParseSnapshot()
    {
        ClassicAssert.AreEqual(new Version(1, 5, 2, true),
            Version.Parse("1.5.2-incubating-SNAPSHOT"));
        ClassicAssert.AreEqual(new Version(1, 5, 2, true),
            Version.Parse("1.5.2-SNAPSHOT"));
    }

    [Test]
    public void TestParseInvalidVersion()
    {
        // NOpenNLP: upstream catches NumberFormatException; FormatException is the
        // .NET counterpart, thrown here by int.Parse on the empty revision.
        Assert.Throws<FormatException>((Action)(() => Version.Parse("1.5.")));
    }

    [Test]
    public void TestParseInvalidVersion2()
    {
        // NOpenNLP: "1.5" has only one dot, which Parse rejects outright.
        Assert.Throws<FormatException>((Action)(() => Version.Parse("1.5")));
    }
}
