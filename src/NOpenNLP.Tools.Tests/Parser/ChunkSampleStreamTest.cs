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

using NOpenNLP.Tools.Chunker;
using NOpenNLP.Tools.Util;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace NOpenNLP.Tools.Parser;

public class ChunkSampleStreamTest
{
    [Test]
    public void TestConvertParseToPosSample()
    {
        using IObjectStream<ChunkSample?> chunkSampleStream = new ChunkSampleStream(new ParseSampleStream(
            ObjectStreamUtils.CreateObjectStream(ParseTest.PARSE_STRING)));

        ChunkSample sample = chunkSampleStream.Read()!;

        ClassicAssert.AreEqual("She", sample.Sentence[0]);
        ClassicAssert.AreEqual("PRP", sample.Tags[0]);
        ClassicAssert.AreEqual("S-NP", sample.Preds[0]);
        ClassicAssert.AreEqual("was", sample.Sentence[1]);
        ClassicAssert.AreEqual("VBD", sample.Tags[1]);
        ClassicAssert.AreEqual("O", sample.Preds[1]);
        ClassicAssert.AreEqual("just", sample.Sentence[2]);
        ClassicAssert.AreEqual("RB", sample.Tags[2]);
        ClassicAssert.AreEqual("S-ADVP", sample.Preds[2]);
        ClassicAssert.AreEqual("another", sample.Sentence[3]);
        ClassicAssert.AreEqual("DT", sample.Tags[3]);
        ClassicAssert.AreEqual("S-NP", sample.Preds[3]);
        ClassicAssert.AreEqual("freighter", sample.Sentence[4]);
        ClassicAssert.AreEqual("NN", sample.Tags[4]);
        ClassicAssert.AreEqual("C-NP", sample.Preds[4]);
        ClassicAssert.AreEqual("from", sample.Sentence[5]);
        ClassicAssert.AreEqual("IN", sample.Tags[5]);
        ClassicAssert.AreEqual("O", sample.Preds[5]);
        ClassicAssert.AreEqual("the", sample.Sentence[6]);
        ClassicAssert.AreEqual("DT", sample.Tags[6]);
        ClassicAssert.AreEqual("S-NP", sample.Preds[6]);
        ClassicAssert.AreEqual("States", sample.Sentence[7]);
        ClassicAssert.AreEqual("NNPS", sample.Tags[7]);
        ClassicAssert.AreEqual("C-NP", sample.Preds[7]);
        ClassicAssert.AreEqual(",", sample.Sentence[8]);
        ClassicAssert.AreEqual(",", sample.Tags[8]);
        ClassicAssert.AreEqual("O", sample.Preds[8]);
        ClassicAssert.AreEqual("and", sample.Sentence[9]);
        ClassicAssert.AreEqual("CC", sample.Tags[9]);
        ClassicAssert.AreEqual("O", sample.Preds[9]);
        ClassicAssert.AreEqual("she", sample.Sentence[10]);
        ClassicAssert.AreEqual("PRP", sample.Tags[10]);
        ClassicAssert.AreEqual("S-NP", sample.Preds[10]);
        ClassicAssert.AreEqual("seemed", sample.Sentence[11]);
        ClassicAssert.AreEqual("VBD", sample.Tags[11]);
        ClassicAssert.AreEqual("O", sample.Preds[11]);
        ClassicAssert.AreEqual("as", sample.Sentence[12]);
        ClassicAssert.AreEqual("RB", sample.Tags[12]);
        ClassicAssert.AreEqual("S-ADJP", sample.Preds[12]);
        ClassicAssert.AreEqual("commonplace", sample.Sentence[13]);
        ClassicAssert.AreEqual("JJ", sample.Tags[13]);
        ClassicAssert.AreEqual("C-ADJP", sample.Preds[13]);
        ClassicAssert.AreEqual("as", sample.Sentence[14]);
        ClassicAssert.AreEqual("IN", sample.Tags[14]);
        ClassicAssert.AreEqual("O", sample.Preds[14]);
        ClassicAssert.AreEqual("her", sample.Sentence[15]);
        ClassicAssert.AreEqual("PRP$", sample.Tags[15]);
        ClassicAssert.AreEqual("S-NP", sample.Preds[15]);
        ClassicAssert.AreEqual("name", sample.Sentence[16]);
        ClassicAssert.AreEqual("NN", sample.Tags[16]);
        ClassicAssert.AreEqual("C-NP", sample.Preds[16]);
        ClassicAssert.AreEqual(".", sample.Sentence[17]);
        ClassicAssert.AreEqual(".", sample.Tags[17]);
        ClassicAssert.AreEqual("O", sample.Preds[17]);

        ClassicAssert.IsNull(chunkSampleStream.Read());
    }
}
