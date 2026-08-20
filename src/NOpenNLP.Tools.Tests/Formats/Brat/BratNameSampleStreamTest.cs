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
using System.IO;
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Sentdetect;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Formats.Brat;

public class BratNameSampleStreamTest
{
    // NOpenNLP: upstream resolves the brat corpus directory with
    // getResource("/opennlp/tools/formats/brat/").getFile(), which works because the
    // Java build copies test resources onto the classpath as real files.
    // BratDocumentStream walks a directory on disk, and the .NET counterparts of those
    // resources are embedded in the assembly, so they are materialized into a temporary
    // directory for the duration of the fixture instead.
    private static readonly string[] BratResources =
    [
        "opennlp-1193.ann",
        "opennlp-1193.txt",
        "voa-with-entities.ann",
        "voa-with-entities.txt",
        "voa-with-entities-overlapping.ann",
        "voa-with-entities-overlapping.txt",
        "voa-with-relations.ann",
        "voa-with-relations.txt",
    ];

    private TempDirectory bratDirectory = null!;

    [SetUp]
    public void Setup()
    {
        bratDirectory = new TempDirectory("nopennlp-brat");

        foreach (string name in BratResources)
        {
            bratDirectory.CopyResource("/opennlp/tools/formats/brat/" + name, name);
        }
    }

    [TearDown]
    public void TearDown() => bratDirectory.Dispose();

    private BratNameSampleStream CreateNameSampleWith(string nameContainsFilter,
        ISet<string>? nameTypes)
    {
        IDictionary<string, string> typeToClassMap = new Dictionary<string, string>();
        BratAnnotationStreamTest.AddEntityTypes(typeToClassMap);
        AnnotationConfiguration config = new(typeToClassMap);

        DirectoryInfo dir = bratDirectory.DirectoryInfo;
        Func<FileSystemInfo, bool> fileFilter =
            pathname => pathname.Name.Contains(nameContainsFilter);

        IObjectStream<BratDocument?> bratDocumentStream = new BratDocumentStream(config, dir,
            false, fileFilter);

        return new BratNameSampleStream(new NewlineSentenceDetector(),
            WhitespaceTokenizer.INSTANCE, bratDocumentStream, nameTypes);
    }

    [Test]
    public void ReadNoOverlap()
    {
        BratNameSampleStream stream = CreateNameSampleWith("-entities.",
            null);
        int count = 0;
        NameSample? sample = stream.Read();
        while (sample != null)
        {
            count++;
            sample = stream.Read();
        }

        ClassicAssert.AreEqual(8, count);
    }

    [Test]
    public void ReadOverlapFail()
    {
        BratNameSampleStream stream = CreateNameSampleWith("overlapping",
            null);

        // NOpenNLP: upstream expects RuntimeException, which NameSample throws for
        // overlapping name spans. The port throws InvalidOperationException there, the
        // closest .NET counterpart for an unchecked programming-error throw.
        Assert.Throws<InvalidOperationException>((Action)(() =>
        {
            NameSample? sample = stream.Read();
            while (sample != null)
            {
                sample = stream.Read();
            }
        }));
    }

    [Test]
    public void EmptySample()
    {
        // NOpenNLP: upstream expects IllegalArgumentException, whose .NET counterpart is
        // ArgumentException.
        Assert.Throws<ArgumentException>((Action)(() => CreateNameSampleWith("overlapping",
            new HashSet<string>())));
    }

    [Test]
    public void ReadOverlapFilter()
    {
        BratNameSampleStream stream = CreateNameSampleWith("overlapping",
            new HashSet<string> { "Person" });
        int count = 0;
        NameSample? sample = stream.Read();
        while (sample != null)
        {
            count++;
            sample = stream.Read();
        }

        ClassicAssert.AreEqual(8, count);
    }
}
