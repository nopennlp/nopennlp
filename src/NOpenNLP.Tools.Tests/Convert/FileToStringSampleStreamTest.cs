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

using System.Collections.Generic;
using System.Text;
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Formats.Convert;
using NOpenNLP.Tools.Support;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Convert;

public class FileToStringSampleStreamTest
{
    // NOpenNLP: upstream uses JUnit's @Rule TemporaryFolder, which NUnit has no direct
    // equivalent for; TempDirectory stands in for it, created per test and removed again.
    private TempDirectory directory = null!;

    [SetUp]
    public void Setup() => directory = new TempDirectory();

    [TearDown]
    public void TearDown() => directory.Dispose();

    [Test]
    public void ReadFileTest()
    {
        const string sentence1 = "This is a sentence.";
        const string sentence2 = "This is another sentence.";

        List<string> sentences = [sentence1, sentence2];

        var directorySampleStream = new DirectorySampleStream(directory.DirectoryInfo, null, false);

        // NOpenNLP: upstream's TemporaryFolder.newFile() names the file itself; the
        // names here serve the same purpose, and FileUtils.writeStringToFile becomes
        // TempDirectory.CreateFile.
        directory.CreateFile("temp1.tmp", sentence1);
        directory.CreateFile("temp2.tmp", sentence2);

        // NOpenNLP: upstream passes Charset.defaultCharset(), which on a modern JVM is
        // UTF-8. Encoding.Default is UTF-8 on .NET Core, but on .NET Framework it is the
        // ANSI code page, so UTF-8 is named explicitly to keep the leg comparable.
        using var stream = new FileToStringSampleStream(directorySampleStream, Encoding.UTF8);

        var read = stream.Read();
        ClassicAssert.IsTrue(sentences.Contains(read!));

        read = stream.Read();
        ClassicAssert.IsTrue(sentences.Contains(read!));
    }
}
