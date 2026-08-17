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
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Parser.Treeinsert;

public class CheckContextGenerator : AbstractContextGenerator
{
    private readonly Parse?[] leftNodes; // NOpenNLP: made readonly

    public CheckContextGenerator(ISet<string> punctSet)
    {
        this.punctSet = punctSet;
        leftNodes = new Parse?[2];
    }

    public virtual string[] GetContext(Parse parent, Parse[] constituents, int index, bool trimFrontier)
    {
        JCG.List<string> features = new(100);
        //default
        features.Add("default");
        Parse[] children = Parser.CollapsePunctuation(parent.GetChildren(), punctSet);
        Parse pstart = children[0];
        Parse pend = children[^1];
        string type = parent.Type;
        Checkcons(pstart, "begin", type, features);
        Checkcons(pend, "last", type, features);
        string production = "p=" + Production(parent, false);
        string punctProduction = "pp=" + Production(parent, true);
        features.Add(production);
        features.Add(punctProduction);

        Parse? p1 = null;
        Parse? p2 = null;
        ICollection<Parse>? p1s = constituents[index].NextPunctuationSet;
        ICollection<Parse>? p2s = null;
        ICollection<Parse>? p_1s = constituents[index].PreviousPunctuationSet;
        ICollection<Parse>? p_2s = null;
        IList<Parse> rf;
        if (index == 0)
        {
            rf = [];
        }
        else
        {
            rf = Parser.GetRightFrontier(constituents[0], punctSet!);
            if (trimFrontier)
            {
                int pi = rf.IndexOf(parent);
                if (pi == -1)
                {
                    // NOpenNLP: upstream throws java.lang.RuntimeException; InvalidOperationException
                    // is the closest unchecked .NET counterpart.
                    throw new InvalidOperationException(
                        "Parent not found in right frontier:" + parent + " rf=" + rf);
                }
                else
                {
                    for (int ri = 0; ri <= pi; ri++)
                    {
                        rf.RemoveAt(0);
                    }
                }
            }
        }

        GetFrontierNodes(rf, leftNodes);
        Parse? p_1 = leftNodes[0];
        Parse? p_2 = leftNodes[1];
        int ps = constituents.Length;
        if (p_1 != null)
        {
            p_2s = p_1.PreviousPunctuationSet;
        }

        if (index + 1 < ps)
        {
            p1 = constituents[index + 1];
            p2s = p1.NextPunctuationSet;
        }

        if (index + 2 < ps)
        {
            p2 = constituents[index + 2];
        }

        Surround(p_1, -1, type, p_1s, features);
        Surround(p_2, -2, type, p_2s, features);
        Surround(p1, 1, type, p1s, features);
        Surround(p2, 2, type, p2s, features);

        return [.. features];
    }
}
