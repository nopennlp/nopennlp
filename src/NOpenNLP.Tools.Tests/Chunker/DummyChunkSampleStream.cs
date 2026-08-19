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
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Chunker;

/// <summary>
/// This dummy chunk sample stream reads a file formatted as described at
/// <a href="http://www.cnts.ua.ac.be/conll2000/chunking/output.html/">] and
/// can be used together with DummyChunker simulate a chunker.
/// </summary>
public class DummyChunkSampleStream : FilterObjectStream<string?, ChunkSample?>
{
    private readonly bool mIsPredicted; // NOpenNLP: made readonly
    private int count = 0;

    // the predicted flag sets if the stream will contain the expected or the
    // predicted tags.
    public DummyChunkSampleStream(IObjectStream<string?> samples, bool isPredicted)
        : base(samples)
        => mIsPredicted = isPredicted;

    /// <summary>
    /// Returns a pair representing the expected and the predicted at 0: the
    /// chunk tag according to the corpus at 1: the chunk tag predicted
    /// </summary>
    /// <seealso cref="IObjectStream{T}.Read"/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override ChunkSample? Read()
    {
        IList<string> toks = new JCG.List<string>();
        IList<string> posTags = new JCG.List<string>();
        IList<string> chunkTags = new JCG.List<string>();
        IList<string> predictedChunkTags = new JCG.List<string>();

        for (string? line = samples.Read(); line != null && !line.Equals(""); line = samples.Read())
        {
            string[] parts = line.Split(' ');
            if (parts.Length != 4)
            {
                Console.Error.WriteLine("Skipping corrupt line " + count + ": " + line);
            }
            else
            {
                toks.Add(parts[0]);
                posTags.Add(parts[1]);
                chunkTags.Add(parts[2]);
                predictedChunkTags.Add(parts[3]);
            }

            count++;
        }

        if (toks.Count > 0)
        {
            if (mIsPredicted)
            {
                return new ChunkSample([.. toks], [.. posTags], [.. predictedChunkTags]);
            }
            else
            {
                return new ChunkSample([.. toks], [.. posTags], [.. chunkTags]);
            }
        }
        else
        {
            return null;
        }
    }
}
