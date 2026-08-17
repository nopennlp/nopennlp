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
using System.Text;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Parser.Chunking;

/// <summary>
/// Class for generating predictive context for deciding when a constituent is complete.
/// </summary>
public class CheckContextGenerator : AbstractContextGenerator
{
    /// <summary>
    /// Creates a new context generator for generating predictive context for deciding
    /// when a constituent is complete.
    /// </summary>
    public CheckContextGenerator()
    {
    }

    /// <summary>
    /// Returns predictive context for deciding whether the specified constituents between the
    /// specified start and end index can be combined to form a new constituent of the specified type.
    /// </summary>
    /// <param name="constituents">The constituents which have yet to be combined into new constituents.</param>
    /// <param name="type">The type of the new constituent proposed.</param>
    /// <param name="start">The first constituent of the proposed constituent.</param>
    /// <param name="end">The last constituent of the proposed constituent.</param>
    /// <returns>The predictive context for deciding whether a new constituent should be created.</returns>
    public virtual string[] GetContext(Parse[] constituents, string type, int start, int end)
    {
        int ps = constituents.Length;
        JCG.List<string> features = new(100);

        //default
        features.Add("default");
        //first constituent label
        // NOpenNLP: Parse.Label may be null. Java's string concatenation renders a null
        // reference as the literal text "null", producing the feature "fl=null"; C# would
        // produce "fl=" instead, so the "null" is spelled out to match upstream exactly.
        features.Add("fl=" + (constituents[0].Label ?? "null"));
        Parse pstart = constituents[start];
        Parse pend = constituents[end];
        Checkcons(pstart, "begin", type, features);
        Checkcons(pend, "last", type, features);
        StringBuilder production = new(20);
        StringBuilder punctProduction = new(20);
        production.Append("p=").Append(type).Append("->");
        punctProduction.Append("pp=").Append(type).Append("->");
        for (int pi = start; pi < end; pi++)
        {
            Parse p = constituents[pi];
            Checkcons(p, pend, type, features);
            production.Append(p.Type).Append(',');
            punctProduction.Append(p.Type).Append(',');
            ICollection<Parse>? nextPunct = p.NextPunctuationSet;
            if (nextPunct != null)
            {
                foreach (Parse punct in nextPunct)
                {
                    punctProduction.Append(punct.Type).Append(',');
                }
            }
        }

        production.Append(pend.Type);
        punctProduction.Append(pend.Type);
        features.Add(production.ToString());
        features.Add(punctProduction.ToString());
        Parse? p_2 = null;
        Parse? p_1 = null;
        Parse? p1 = null;
        Parse? p2 = null;
        ICollection<Parse>? p1s = constituents[end].NextPunctuationSet;
        ICollection<Parse>? p2s = null;
        ICollection<Parse>? p_1s = constituents[start].PreviousPunctuationSet;
        ICollection<Parse>? p_2s = null;
        if (start - 2 >= 0)
        {
            p_2 = constituents[start - 2];
        }

        if (start - 1 >= 0)
        {
            p_1 = constituents[start - 1];
            p_2s = p_1.PreviousPunctuationSet;
        }

        if (end + 1 < ps)
        {
            p1 = constituents[end + 1];
            p2s = p1.NextPunctuationSet;
        }

        if (end + 2 < ps)
        {
            p2 = constituents[end + 2];
        }

        Surround(p_1, -1, type, p_1s, features);
        Surround(p_2, -2, type, p_2s, features);
        Surround(p1, 1, type, p1s, features);
        Surround(p2, 2, type, p2s, features);

        return [.. features];
    }
}
