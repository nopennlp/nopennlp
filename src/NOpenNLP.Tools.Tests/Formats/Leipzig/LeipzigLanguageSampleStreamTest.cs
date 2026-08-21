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
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Formats.Leipzig;

/// <summary>
/// Tests for the <see cref="LeipzigLanguageSampleStream"/> class.
/// </summary>
public class LeipzigLanguageSampleStreamTest
{
    // NOpenNLP: upstream resolves a classpath DIRECTORY, getResource(
    // "opennlp/tools/formats/leipzig/samples").getPath(), because
    // LeipzigLanguageSampleStream walks a real folder rather than reading a single
    // resource. Embedded resources have no directory structure to walk, so the
    // fixture is copied to the test output directory instead and located there.
    private static string TestDataPath =>
        Path.Combine(TestContext.CurrentContext.TestDirectory, "Formats", "Leipzig", "samples");

    [Test]
    public void TestReadSentenceFiles()
    {
        int samplesPerLanguage = 2;
        int sentencesPerSample = 1;
        try
        {
            var stream = new LeipzigLanguageSampleStream(
                new DirectoryInfo(TestDataPath), sentencesPerSample, samplesPerLanguage);
            int count = 0;
            while (stream.Read() != null)
                count++;

            ClassicAssert.AreEqual(4, count);
        }
        catch (IOException)
        {
            ClassicAssert.Fail();
        }
    }

    [Test]
    public void TestNotEnoughSentences()
    {
        int samplesPerLanguage = 2;
        int sentencesPerSample = 2;

        // NOpenNLP: the Action cast matches the convention used elsewhere in the ported
        // tests: without it a lambda is ambiguous between NUnit's TestDelegate and Action
        // overloads of Assert.Throws.
        Assert.Throws<InvalidFormatException>((Action)(() =>
        {
            var stream = new LeipzigLanguageSampleStream(
                new DirectoryInfo(TestDataPath), sentencesPerSample, samplesPerLanguage);
            while (stream.Read() != null) ;
        }));
    }
}
