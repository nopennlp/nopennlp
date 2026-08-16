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

using NOpenNLP.Tools.Util;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Namefind;

public class BilouCodec : ISequenceCodec<string>
{
    public const string START = "start";
    public const string CONTINUE = "cont";
    public const string LAST = "last";
    public const string UNIT = "unit";
    public const string OTHER = "other";

    public virtual Span[] Decode(IList<string> c)
    {
        int start = -1;
        int end = -1;
        IList<Span> spans = new JCG.List<Span>(c.Count);
        for (int li = 0; li < c.Count; li++)
        {
            string chunkTag = c[li];
            if (chunkTag.EndsWith(START, StringComparison.Ordinal))
            {
                start = li;
                end = li + 1;
            }
            else if (chunkTag.EndsWith(CONTINUE, StringComparison.Ordinal))
            {
                end = li + 1;
            }
            else if (chunkTag.EndsWith(LAST, StringComparison.Ordinal))
            {
                if (start != -1)
                {
                    spans.Add(new Span(start, end + 1, BioCodec.ExtractNameType(c[li - 1])));
                    start = -1;
                    end = -1;
                }
            }
            else if (chunkTag.EndsWith(UNIT, StringComparison.Ordinal))
            {
                spans.Add(new Span(li, li + 1, BioCodec.ExtractNameType(c[li])));
            }
        }

        return [.. spans];
    }

    public virtual string[] Encode(Span[] names, int length)
    {
        string[] outcomes = new string[length];
        // NOpenNLP: upstream uses Arrays.fill; Array.Fill is not in netstandard2.0,
        // so the span overload stands in for it.
        outcomes.AsSpan().Fill(OTHER);

        foreach (var name in names)
        {
            if (name.Length > 1)
            {
                if (name.Type == null)
                {
                    outcomes[name.Start] = "default" + "-" + START;
                }
                else
                {
                    outcomes[name.Start] = name.Type + "-" + START;
                }

                // now iterate from begin + 1 till end
                for (int i = name.Start + 1; i < name.End - 1; i++)
                {
                    if (name.Type == null)
                    {
                        outcomes[i] = "default" + "-" + CONTINUE;
                    }
                    else
                    {
                        outcomes[i] = name.Type + "-" + CONTINUE;
                    }
                }

                if (name.Type == null)
                {
                    outcomes[name.End - 1] = "default" + "-" + LAST;
                }
                else
                {
                    outcomes[name.End - 1] = name.Type + "-" + LAST;
                }
            }
            else
            {
                if (name.Type == null)
                {
                    outcomes[name.End - 1] = "default" + "-" + UNIT;
                }
                else
                {
                    outcomes[name.End - 1] = name.Type + "-" + UNIT;
                }
            }
        }

        return outcomes;
    }

    public virtual ISequenceValidator<string> CreateSequenceValidator() => new BilouNameFinderSequenceValidator();

    /// <summary>
    /// B requires CL or L
    /// <para/>
    /// C requires BL
    /// <para/>
    /// L requires B
    /// <para/>
    /// O requires any valid combo/unit
    /// <para/>
    /// U requires none
    /// </summary>
    /// <param name="outcomes">all possible model outcomes</param>
    /// <returns><c>true</c>, if model outcomes are compatible</returns>
    public virtual bool AreOutcomesCompatible(string[] outcomes)
    {
        ISet<string> start = new JCG.HashSet<string>();
        ISet<string> cont = new JCG.HashSet<string>();
        ISet<string> last = new JCG.HashSet<string>();
        ISet<string> unit = new JCG.HashSet<string>();

        foreach (string outcome in outcomes)
        {
            if (outcome.EndsWith(START, StringComparison.Ordinal))
            {
                start.Add(outcome[..^START.Length]);
            }
            else if (outcome.EndsWith(CONTINUE, StringComparison.Ordinal))
            {
                cont.Add(outcome[..^CONTINUE.Length]);
            }
            else if (outcome.EndsWith(LAST, StringComparison.Ordinal))
            {
                last.Add(outcome[..^LAST.Length]);
            }
            else if (outcome.EndsWith(UNIT, StringComparison.Ordinal))
            {
                unit.Add(outcome[..^UNIT.Length]);
            }
            else if (!outcome.Equals(OTHER))
            {
                return false;
            }
        }

        if (start.Count == 0 && unit.Count == 0)
        {
            return false;
        }
        else
        {
            // Start, must have matching Last
            foreach (string startPrefix in start)
            {
                if (!last.Contains(startPrefix))
                {
                    return false;
                }
            }
            // Cont, must have matching Start and Last
            foreach (string contPrefix in cont)
            {
                if (!start.Contains(contPrefix) && !last.Contains(contPrefix))
                {
                    return false;
                }
            }
            // Last, must have matching Start
            foreach (string lastPrefix in last)
            {
                if (!start.Contains(lastPrefix))
                {
                    return false;
                }
            }
        }

        return true;
    }
}
