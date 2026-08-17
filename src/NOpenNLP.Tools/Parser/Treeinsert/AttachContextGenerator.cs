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
using System.Globalization;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Parser.Treeinsert;

public class AttachContextGenerator : AbstractContextGenerator
{
    public AttachContextGenerator(ISet<string> punctSet)
    {
        this.punctSet = punctSet;
    }

    private static bool ContainsPunct(ICollection<Parse>? puncts, string punct)
    {
        if (puncts != null)
        {
            foreach (Parse p in puncts)
            {
                if (p.Type.Equals(punct))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// </summary>
    /// <param name="constituents">The constituents as they have been constructed so far.</param>
    /// <param name="index">The constituent index of the node being attached.</param>
    /// <param name="rightFrontier">The nodes which have been not attach to so far.</param>
    /// <param name="rfi">The index into the right frontier.</param>
    /// <returns>A set of contextual features about this attachment.</returns>
    public virtual string[] GetContext(Parse[] constituents, int index, IList<Parse> rightFrontier, int rfi)
    {
        JCG.List<string> features = new(100);
        Parse fn = rightFrontier[rfi];
        Parse? fp = null;
        if (rfi + 1 < rightFrontier.Count)
        {
            fp = rightFrontier[rfi + 1];
        }

        Parse? p_1 = null;
        if (rightFrontier.Count > 0)
        {
            p_1 = rightFrontier[0];
        }

        Parse p0 = constituents[index];
        Parse? p1 = null;
        if (index + 1 < constituents.Length)
        {
            p1 = constituents[index + 1];
        }

        ICollection<Parse>? punct_1fs = fn.PreviousPunctuationSet;
        ICollection<Parse>? punct_1s = p0.PreviousPunctuationSet;
        ICollection<Parse>? punct1s = p0.NextPunctuationSet;

        string consfp = Cons(fp, -3);
        string consf = Cons(fn, -2);
        string consp_1 = Cons(p_1, -1);
        string consp0 = Cons(p0, 0);
        string consp1 = Cons(p1, 1);

        string consbofp = Consbo(fp, -3);
        string consbof = Consbo(fn, -2);
        string consbop_1 = Consbo(p_1, -1);
        string consbop0 = Consbo(p0, 0);
        string consbop1 = Consbo(p1, 1);

        Cons cfp = new(consfp, consbofp, -3, true);
        Cons cf = new(consf, consbof, -2, true);
        Cons c_1 = new(consp_1, consbop_1, -1, true);
        Cons c0 = new(consp0, consbop0, 0, true);
        Cons c1 = new(consp1, consbop1, 1, true);

        //default
        features.Add("default");

        //unigrams
        features.Add(consfp);
        features.Add(consbofp);
        features.Add(consf);
        features.Add(consbof);
        features.Add(consp_1);
        features.Add(consbop_1);
        features.Add(consp0);
        features.Add(consbop0);
        features.Add(consp1);
        features.Add(consbop1);

        //productions
        string prod = Production(fn, false);
        //String punctProd = production(fn,true,punctSet);
        features.Add("pn=" + prod);
        features.Add("pd=" + prod + "," + p0.Type);
        features.Add("ps=" + fn.Type + "->" + fn.Type + "," + p0.Type);
        if (punct_1s != null)
        {
            StringBuilder punctBuf = new(5);
            foreach (Parse punct in punct_1s)
            {
                punctBuf.Append(punct.Type).Append(',');
            }
            //features.add("ppd="+punctProd+","+punctBuf.toString()+p0.getType());
            //features.add("pps="+fn.getType()+"->"+fn.getType()+","+punctBuf.toString()+p0.getType());
        }

        //bi-grams
        //cons(fn),cons(0)
        Cons2(features, cfp, c0, punct_1s, true);
        Cons2(features, cf, c0, punct_1s, true);
        Cons2(features, c_1, c0, punct_1s, true);
        Cons2(features, c0, c1, punct1s, true);
        Cons3(features, cf, c_1, c0, null, punct_1s, true, true, true);
        Cons3(features, cf, c0, c1, punct_1s, punct1s, true, true, true);
        Cons3(features, cfp, cf, c0, null, punct_1s, true, true, true);
        /*
        for (int ri=0;ri<rfi;ri++) {
          Parse jn = (Parse) rightFrontier.get(ri);
          features.add("jn="+jn.getType());
        }
        */
        int headDistance = p0.HeadIndex - fn.HeadIndex;
        features.Add("hd=" + headDistance.ToString(CultureInfo.InvariantCulture));
        features.Add("nd=" + rfi.ToString(CultureInfo.InvariantCulture));

        features.Add("nd=" + p0.Type + "." + rfi.ToString(CultureInfo.InvariantCulture));
        features.Add("hd=" + p0.Type + "." + headDistance.ToString(CultureInfo.InvariantCulture));
        //features.add("fs="+rightFrontier.size());
        //paired punct features
        if (ContainsPunct(punct_1s, "''"))
        {
            if (ContainsPunct(punct_1fs, "``"))
            {
                features.Add("quotematch"); //? not generating feature correctly
            }
        }

        return [.. features];
    }
}
