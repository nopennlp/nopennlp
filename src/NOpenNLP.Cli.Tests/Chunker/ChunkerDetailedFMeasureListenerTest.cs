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

using System.Globalization;
using System.IO;
using System.Text;
using NOpenNLP.Tools.Chunker;
using NOpenNLP.Tools.Formats;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Cmdline.Chunker;

/// <remarks>
/// NOpenNLP: upstream keeps this test in opennlp-tools, beside the chunker it evaluates,
/// because the cmdline package ships there too. This port splits cmdline out into
/// NOpenNLP.Cli, which NOpenNLP.Tools.Tests does not reference, so the test lives here
/// instead of at its upstream-mirrored path.
/// </remarks>
public class ChunkerDetailedFMeasureListenerTest
{
    [Test]
    public void TestEvaluator()
    {
        ResourceAsStreamFactory inPredicted = new("/opennlp/tools/chunker/output.txt");
        ResourceAsStreamFactory inExpected = new("/opennlp/tools/chunker/output.txt");
        ResourceAsStreamFactory detailedOutputStream = new("/opennlp/tools/chunker/detailedOutput.txt");

        DummyChunkSampleStream predictedSample = new(
            new PlainTextByLineStream(inPredicted, Encoding.UTF8), true);

        DummyChunkSampleStream expectedSample = new(
            new PlainTextByLineStream(inExpected, Encoding.UTF8), false);

        IChunker dummyChunker = new DummyChunker(predictedSample);

        ChunkerDetailedFMeasureListener listener = new();
        ChunkerEvaluator evaluator = new(dummyChunker, listener);

        evaluator.Evaluate(expectedSample);

        StringBuilder expected = new();
        using StreamReader reader = new(detailedOutputStream.CreateInputStream(), Encoding.UTF8);
        string? line = reader.ReadLine();

        while (line != null)
        {
            expected.Append(line);
            expected.Append('\n');
            line = reader.ReadLine();
        }

        // NOpenNLP: upstream passes Locale.ENGLISH; the port's DetailedFMeasureListener
        // takes a CultureInfo instead.
        ClassicAssert.AreEqual(expected.ToString().Trim(),
            listener.CreateReport(CultureInfo.GetCultureInfo("en")).Trim());
    }
}
