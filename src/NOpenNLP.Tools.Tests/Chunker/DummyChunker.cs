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
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Chunker;

/// <summary>
/// This dummy chunker implementation reads a file formatted as described at
/// <a href="http://www.cnts.ua.ac.be/conll2000/chunking/output.html/">] to
/// simulate a Chunker. The file has samples of sentences, with target and
/// predicted values.
/// </summary>
public class DummyChunker(DummyChunkSampleStream aSampleStream) : IChunker
{
    private readonly DummyChunkSampleStream mSampleStream = aSampleStream; // NOpenNLP: made readonly

    // NOpenNLP: upstream also declares a List<String>-based chunk() overload,
    // which the ported IChunker does not carry and no test calls.

    public string[] Chunk(string[] toks, string[] tags)
    {
        // NOpenNLP: upstream wraps the IOException in a RuntimeException. .NET does
        // not have checked exceptions, so the IOException simply propagates.
        ChunkSample? predsSample = mSampleStream.Read();

        // checks if the streams are sync
        for (int i = 0; i < toks.Length; i++)
        {
            if (!toks[i].Equals(predsSample!.Sentence[i]) || !tags[i].Equals(predsSample.Tags[i]))
            {
                // NOpenNLP: upstream formats with java.util.Arrays.toString; string.Join
                // inside brackets produces the same "[a, b, c]" shape.
                throw new InvalidOperationException("The streams are not sync!"
                    + "\n expected sentence: [" + string.Join(", ", toks) + "]"
                    + "\n expected tags: [" + string.Join(", ", tags) + "]"
                    + "\n predicted sentence: [" + string.Join(", ", predsSample.Sentence) + "]"
                    + "\n predicted tags: [" + string.Join(", ", predsSample.Tags) + "]");
            }
        }

        return predsSample!.Preds;
    }

    // NOpenNLP: upstream returns null from the four methods below. The ported
    // IChunker declares non-nullable return types, and no test calls them, so
    // they return empty arrays instead.
    public Span[] ChunkAsSpans(string[] toks, string[] tags) => [];

    public Sequence[] TopKSequences(string[] sentence, string[] tags) => [];

    public Sequence[] TopKSequences(string[] sentence, string[] tags, double minSequenceScore) => [];
}
