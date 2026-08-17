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
using NOpenNLP.Tools.Chunker;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Parser;

/// <summary>
/// Creates predivtive context for the pre-chunking phases of parsing.
/// </summary>
public class ChunkContextGenerator : IChunkerContextGenerator
{
    private const string EOS = "eos";
    private readonly Cache<string, string[]>? contextsCache; // NOpenNLP: made readonly
    private object? wordsKey;

    public ChunkContextGenerator(int cacheSize = 0)
    {
        if (cacheSize > 0)
        {
            contextsCache = new Cache<string, string[]>(cacheSize);
        }
    }

    public virtual string[] GetContext(int i, string[] words, string[] tags, string[] preds)
    {
        JCG.List<string> features = new(19);
        int x_2 = i - 2;
        int x_1 = i - 1;
        int x2 = i + 2;
        int x1 = i + 1;

        string w_2, w_1, w0, w1, w2;
        string t_2, t_1, t0, t1, t2;
        string p_2, p_1;

        // chunkandpostag(-2)
        if (x_2 >= 0)
        {
            t_2 = tags[x_2];
            p_2 = preds[x_2];
            w_2 = words[x_2];
        }
        else
        {
            t_2 = EOS;
            p_2 = EOS;
            w_2 = EOS;
        }

        // chunkandpostag(-1)
        if (x_1 >= 0)
        {
            t_1 = tags[x_1];
            p_1 = preds[x_1];
            w_1 = words[x_1];
        }
        else
        {
            t_1 = EOS;
            p_1 = EOS;
            w_1 = EOS;
        }

        // chunkandpostag(0)
        t0 = tags[i];
        w0 = words[i];

        // chunkandpostag(1)
        if (x1 < tags.Length)
        {
            t1 = tags[x1];
            w1 = words[x1];
        }
        else
        {
            t1 = EOS;
            w1 = EOS;
        }

        // chunkandpostag(2)
        if (x2 < tags.Length)
        {
            t2 = tags[x2];
            w2 = words[x2];
        }
        else
        {
            t2 = EOS;
            w2 = EOS;
        }

        string cacheKey = i + t_2 + t1 + t0 + t1 + t2 + p_2 + p_1;
        if (contextsCache != null)
        {
            if (ReferenceEquals(wordsKey, words))
            {
                // NOpenNLP: renamed from upstream's "contexts" because C# forbids an inner
                // local that shadows one declared later in the enclosing method body.
                string[]? cachedContexts = contextsCache[cacheKey];
                if (cachedContexts != null)
                {
                    return cachedContexts;
                }
            }
            else
            {
                contextsCache.Clear();
                wordsKey = words;
            }
        }

        string ct_2 = Chunkandpostag(-2, w_2, t_2, p_2);
        string ctbo_2 = Chunkandpostagbo(-2, t_2, p_2);
        string ct_1 = Chunkandpostag(-1, w_1, t_1, p_1);
        string ctbo_1 = Chunkandpostagbo(-1, t_1, p_1);
        string ct0 = Chunkandpostag(0, w0, t0, null);
        string ctbo0 = Chunkandpostagbo(0, t0, null);
        string ct1 = Chunkandpostag(1, w1, t1, null);
        string ctbo1 = Chunkandpostagbo(1, t1, null);
        string ct2 = Chunkandpostag(2, w2, t2, null);
        string ctbo2 = Chunkandpostagbo(2, t2, null);

        features.Add("default");
        features.Add(ct_2);
        features.Add(ctbo_2);
        features.Add(ct_1);
        features.Add(ctbo_1);
        features.Add(ct0);
        features.Add(ctbo0);
        features.Add(ct1);
        features.Add(ctbo1);
        features.Add(ct2);
        features.Add(ctbo2);

        //chunkandpostag(-1,0)
        features.Add(ct_1 + "," + ct0);
        features.Add(ctbo_1 + "," + ct0);
        features.Add(ct_1 + "," + ctbo0);
        features.Add(ctbo_1 + "," + ctbo0);

        //chunkandpostag(0,1)
        features.Add(ct0 + "," + ct1);
        features.Add(ctbo0 + "," + ct1);
        features.Add(ct0 + "," + ctbo1);
        features.Add(ctbo0 + "," + ctbo1);
        string[] contexts = [.. features];
        contextsCache?.Put(cacheKey, contexts);

        return contexts;
    }

    private static string Chunkandpostag(int i, string tok, string tag, string? chunk)
    {
        StringBuilder feat = new(20);
        feat.Append(i).Append('=').Append(tok).Append('|').Append(tag);
        if (i < 0)
        {
            // NOpenNLP: Java renders a null String as the literal text "null" when appended to a
            // StringBuilder; C# appends nothing. The "null" is explicit so features stay identical.
            feat.Append('|').Append(chunk ?? "null");
        }

        return feat.ToString();
    }

    private static string Chunkandpostagbo(int i, string tag, string? chunk)
    {
        StringBuilder feat = new(20);
        feat.Append(i).Append("*=").Append(tag);
        if (i < 0)
        {
            // NOpenNLP: see Chunkandpostag above; a null chunk must render as the text "null".
            feat.Append('|').Append(chunk ?? "null");
        }

        return feat.ToString();
    }

    public virtual string[] GetContext(int index, TokenTag[] sequence, string[] priorDecisions,
        object[] additionalContext)
    {
        string[] token = TokenTag.ExtractTokens(sequence);
        string[] tags = TokenTag.ExtractTags(sequence);

        return GetContext(index, token, tags, priorDecisions);
    }
}
