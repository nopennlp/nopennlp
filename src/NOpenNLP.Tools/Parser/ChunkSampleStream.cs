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
using System.IO;
using NOpenNLP.Tools.Chunker;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Parser;

public class ChunkSampleStream : FilterObjectStream<Parse?, ChunkSample?>
{
    public ChunkSampleStream(IObjectStream<Parse?> @in)
        : base(@in)
    {
    }

    private static void GetInitialChunks(Parse p, IList<Parse> ichunks)
    {
        if (p.IsPosTag)
        {
            ichunks.Add(p);
        }
        else
        {
            Parse[] kids = p.GetChildren();
            bool allKidsAreTags = true;
            foreach (Parse kid in kids)
            {
                if (!kid.IsPosTag)
                {
                    allKidsAreTags = false;
                    break;
                }
            }

            if (allKidsAreTags)
            {
                ichunks.Add(p);
            }
            else
            {
                foreach (Parse kid in kids)
                {
                    GetInitialChunks(kid, ichunks);
                }
            }
        }
    }

    public static Parse[] GetInitialChunks(Parse p)
    {
        IList<Parse> chunks = new JCG.List<Parse>();
        GetInitialChunks(p, chunks);
        return [.. chunks];
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override ChunkSample? Read()
    {
        Parse? parse = samples.Read();

        if (parse != null)
        {
            Parse[] chunks = GetInitialChunks(parse);
            IList<string> toks = new JCG.List<string>();
            IList<string> tags = new JCG.List<string>();
            IList<string> preds = new JCG.List<string>();
            foreach (Parse c in chunks)
            {
                if (c.IsPosTag)
                {
                    toks.Add(c.CoveredText);
                    tags.Add(c.Type);
                    preds.Add(Chunking.Parser.OTHER);
                }
                else
                {
                    bool start = true;
                    string ctype = c.Type;
                    Parse[] kids = c.GetChildren();
                    foreach (Parse tok in kids)
                    {
                        toks.Add(tok.CoveredText);
                        tags.Add(tok.Type);
                        if (start)
                        {
                            preds.Add(Chunking.Parser.START + ctype);
                            start = false;
                        }
                        else
                        {
                            preds.Add(Chunking.Parser.CONT + ctype);
                        }
                    }
                }
            }

            return new ChunkSample([.. toks], [.. tags], [.. preds]);
        }
        else
        {
            return null;
        }
    }
}
