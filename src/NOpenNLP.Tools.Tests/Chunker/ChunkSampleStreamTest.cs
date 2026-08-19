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

using System.Text;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Chunker;

public class ChunkSampleStreamTest
{
    [Test]
    public void TestReadingEvents()
    {
        string sample = "word11 tag11 pred11" +
            '\n' +
            "word12 tag12 pred12" +
            '\n' +
            "word13 tag13 pred13" +
            '\n' +
            '\n' +
            "word21 tag21 pred21" +
            '\n' +
            "word22 tag22 pred22" +
            '\n' +
            "word23 tag23 pred23" +
            '\n';

        // First sample sentence

        // Start next sample sentence

        // Second sample sentence

        IObjectStream<string?> stringStream = new PlainTextByLineStream(
            new MockInputStreamFactory(sample), Encoding.UTF8);

        IObjectStream<ChunkSample?> chunkStream = new ChunkSampleStream(stringStream);

        // read first sample
        ChunkSample? firstSample = chunkStream.Read();
        ClassicAssert.AreEqual("word11", firstSample!.Sentence[0]);
        ClassicAssert.AreEqual("tag11", firstSample.Tags[0]);
        ClassicAssert.AreEqual("pred11", firstSample.Preds[0]);
        ClassicAssert.AreEqual("word12", firstSample.Sentence[1]);
        ClassicAssert.AreEqual("tag12", firstSample.Tags[1]);
        ClassicAssert.AreEqual("pred12", firstSample.Preds[1]);
        ClassicAssert.AreEqual("word13", firstSample.Sentence[2]);
        ClassicAssert.AreEqual("tag13", firstSample.Tags[2]);
        ClassicAssert.AreEqual("pred13", firstSample.Preds[2]);

        // read second sample
        ChunkSample? secondSample = chunkStream.Read();
        ClassicAssert.AreEqual("word21", secondSample!.Sentence[0]);
        ClassicAssert.AreEqual("tag21", secondSample.Tags[0]);
        ClassicAssert.AreEqual("pred21", secondSample.Preds[0]);
        ClassicAssert.AreEqual("word22", secondSample.Sentence[1]);
        ClassicAssert.AreEqual("tag22", secondSample.Tags[1]);
        ClassicAssert.AreEqual("pred22", secondSample.Preds[1]);
        ClassicAssert.AreEqual("word23", secondSample.Sentence[2]);
        ClassicAssert.AreEqual("tag23", secondSample.Tags[2]);
        ClassicAssert.AreEqual("pred23", secondSample.Preds[2]);

        ClassicAssert.IsNull(chunkStream.Read());

        // NOpenNLP: upstream calls close(); the ported streams are IDisposable.
        chunkStream.Dispose();
    }
}
