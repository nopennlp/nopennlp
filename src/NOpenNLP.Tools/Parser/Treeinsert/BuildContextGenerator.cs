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
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Parser.Treeinsert;

/// <summary>
/// Creates the features or contexts for the building phase of parsing.
/// This phase builds constituents from the left-most node of these
/// constituents.
/// </summary>
public class BuildContextGenerator : AbstractContextGenerator
{
    private readonly Parse?[] leftNodes; // NOpenNLP: made readonly

    public BuildContextGenerator()
    {
        leftNodes = new Parse?[2];
    }

    /// <summary>
    /// Returns the contexts/features for the decision to build a new constituent for the specified parse
    /// at the specified index.
    /// </summary>
    /// <param name="constituents">The constituents of the parse so far.</param>
    /// <param name="index">The index of the constituent where a build decision is being made.</param>
    /// <returns>the contexts/features for the decision to build a new constituent.</returns>
    public virtual string[] GetContext(Parse[] constituents, int index)
    {
        int ps = constituents.Length;

        Parse p0 = constituents[index];

        Parse? p1 = null;
        if (index + 1 < ps)
        {
            p1 = constituents[index + 1];
        }

        Parse? p2 = null;
        if (index + 2 < ps)
        {
            p2 = constituents[index + 2];
        }

        ICollection<Parse>? punct_1s = p0.PreviousPunctuationSet;
        ICollection<Parse>? punct1s = p0.NextPunctuationSet;

        ICollection<Parse>? punct2s = null;
        if (p1 != null)
        {
            punct2s = p1.NextPunctuationSet;
        }

        IList<Parse> rf;
        if (index == 0)
        {
            rf = [];
        }
        else
        {
            //this isn't a root node so, punctSet won't be used and can be passed as empty.
            ISet<string> emptyPunctSet = new JCG.HashSet<string>();
            rf = Parser.GetRightFrontier(constituents[0], emptyPunctSet);
        }

        GetFrontierNodes(rf, leftNodes);
        Parse? p_1 = leftNodes[0];
        Parse? p_2 = leftNodes[1];

        ICollection<Parse>? punct_2s = null;
        if (p_1 != null)
        {
            punct_2s = p_1.PreviousPunctuationSet;
        }

        string consp_2 = Cons(p_2, -2);
        string consp_1 = Cons(p_1, -1);
        string consp0 = Cons(p0, 0);
        string consp1 = Cons(p1, 1);
        string consp2 = Cons(p2, 2);

        string consbop_2 = Consbo(p_2, -2);
        string consbop_1 = Consbo(p_1, -1);
        string consbop0 = Consbo(p0, 0);
        string consbop1 = Consbo(p1, 1);
        string consbop2 = Consbo(p2, 2);

        Cons c_2 = new(consp_2, consbop_2, -2, true);
        Cons c_1 = new(consp_1, consbop_1, -1, true);
        Cons c0 = new(consp0, consbop0, 0, true);
        Cons c1 = new(consp1, consbop1, 1, true);
        Cons c2 = new(consp2, consbop2, 2, true);

        JCG.List<string> features = [];
        features.Add("default");

        //unigrams
        features.Add(consp_2);
        features.Add(consbop_2);
        features.Add(consp_1);
        features.Add(consbop_1);
        features.Add(consp0);
        features.Add(consbop0);
        features.Add(consp1);
        features.Add(consbop1);
        features.Add(consp2);
        features.Add(consbop2);

        //cons(0),cons(1)
        Cons2(features, c0, c1, punct1s, true);
        //cons(-1),cons(0)
        Cons2(features, c_1, c0, punct_1s, true);
        //features.add("stage=cons(0),cons(1),cons(2)");
        Cons3(features, c0, c1, c2, punct1s, punct2s, true, true, true);
        Cons3(features, c_2, c_1, c0, punct_2s, punct_1s, true, true, true);
        Cons3(features, c_1, c0, c1, punct_1s, punct_1s, true, true, true);

        if (rf.Count == 0)
        {
            features.Add(EOS + "," + consp0);
            features.Add(EOS + "," + consbop0);
        }

        return [.. features];
    }
}
