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
/// Parses the conll 2000 shared task shallow parser training data.
/// <para/>
/// Data format is specified on the conll page:<br/>
/// <a href="http://www.cnts.ua.ac.be/conll2000/chunking/">
/// http://www.cnts.ua.ac.be/conll2000/chunking/</a>
/// </summary>
public class ChunkSampleStream : FilterObjectStream<string?, ChunkSample?>
{
    /// <summary>
    /// Initializes the current instance.
    /// </summary>
    /// <param name="samples">a plain text line stream</param>
    public ChunkSampleStream(IObjectStream<string?> samples)
        : base(samples)
    {
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override ChunkSample? Read()
    {
        IList<string> toks = new JCG.List<string>();
        IList<string> tags = new JCG.List<string>();
        IList<string> preds = new JCG.List<string>();

        for (string? line = samples.Read(); line != null && !line.Equals(""); line = samples.Read())
        {
            string[] parts = line.Split(' ');
            if (parts.Length != 3)
            {
                Console.Error.WriteLine("Skipping corrupt line: " + line);
            }
            else
            {
                toks.Add(parts[0]);
                tags.Add(parts[1]);
                preds.Add(parts[2]);
            }
        }

        if (toks.Count > 0)
        {
            return new ChunkSample([.. toks], [.. tags], [.. preds]);
        }

        return null;
    }
}
