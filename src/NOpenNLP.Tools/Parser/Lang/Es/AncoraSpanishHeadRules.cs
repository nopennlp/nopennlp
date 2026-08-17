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
using System.Text;
using System.Text.RegularExpressions;
using J2N.Text;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util.Model;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Parser.Lang.Es;

/// <summary>
/// Class for storing the Ancora Spanish head rules associated with parsing. In this class
/// headrules for noun phrases are specified. The rest of the rules are
/// in opennlp-tools/lang/es/parser/es-head-rules
/// <para/>
/// NOTE: This class has been adapted from opennlp.tools.parser.lang.en.HeadRules
/// <para/>
/// The main change is the constituents search direction in the first for loop.
/// <para/>
/// Note also the change in the return of the <see cref="GetHead"/> method:
/// In the <c>Lang.En.HeadRules</c> class: <c>return constituents[ci].Head;</c>
/// Now: <c>return constituents[ci];</c>
/// <para/>
/// Other changes include removal of deprecated methods.
/// </summary>
public class AncoraSpanishHeadRules : IHeadRules, IGapLabeler, ISerializableArtifact
{
    public class HeadRulesSerializer : IArtifactSerializer<AncoraSpanishHeadRules>
    {
        public virtual AncoraSpanishHeadRules Create(Stream @in) =>
            // NOpenNLP: Java wraps the stream in an InputStreamReader/BufferedReader; the
            // StreamReader here is left open so the caller keeps ownership of the stream,
            // matching the ArtifactSerializer contract.
            new AncoraSpanishHeadRules(
                new StreamReader(@in, new UTF8Encoding(false), false, 1024, leaveOpen: true));

        // NOpenNLP: serialization is not supported; inference only.
        // public virtual void Serialize(AncoraSpanishHeadRules artifact, Stream @out)
        // {
        //     artifact.Serialize(new StreamWriter(@out, new UTF8Encoding(false), 1024, leaveOpen: true));
        // }

        // NOpenNLP: upstream relies on a default interface implementation to
        // bridge the non-generic IArtifactSerializer; DIMs are unavailable on
        // netstandard2.0/net462, so the bridge is explicit here.
        object IArtifactSerializer.Create(Stream @in) => Create(@in);
    }

    private class HeadRule
    {
        // NOpenNLP: made fields readonly
        public readonly bool leftToRight;
        public readonly string[] tags;

        public HeadRule(bool l2r, string[] tags)
        {
            leftToRight = l2r;

            foreach (string tag in tags)
            {
                if (tag is null)
                {
                    throw new ArgumentNullException(nameof(tags), "tags must not contain null values!");
                }
            }

            this.tags = tags;
        }

        public override int GetHashCode() => HashCode.Combine(leftToRight, Arrays.GetHashCode(tags));

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(obj, this))
            {
                return true;
            }

            if (obj is HeadRule rule)
            {
                return (rule.leftToRight == leftToRight) &&
                    Arrays.Equals(rule.tags, tags);
            }

            return false;
        }
    }

    private JCG.Dictionary<string, HeadRule> headRules = null!;
    private readonly JCG.HashSet<string> punctSet; // NOpenNLP: made readonly

    /// <summary>
    /// Creates a new set of head rules based on the specified reader.
    /// </summary>
    /// <param name="rulesReader">The head rules reader.</param>
    /// <exception cref="IOException">Thrown if the head rules reader can not be read.</exception>
    public AncoraSpanishHeadRules(TextReader rulesReader)
    {
        // NOpenNLP: Java wraps the Reader in a BufferedReader for readLine(); .NET's
        // TextReader already exposes ReadLine, so no wrapper is needed.
        ReadHeadRules(rulesReader);

        punctSet = [];
        punctSet.Add(".");
        punctSet.Add(",");
        punctSet.Add("``");
        punctSet.Add("''");
        //punctSet.Add(":");
    }

    public ISet<string> PunctuationTags => punctSet;

    public Parse? GetHead(Parse[] constituents, string type)
    {
        if (AbstractBottomUpParser.TOK_NODE.Equals(constituents[0].Type))
        {
            return null;
        }

        if (type.Equals("SN") || type.Equals("GRUP.NOM"))
        {
            string[] tags1 =
                ["AQA.*", "AQC.*", "GRUP\\.A", "S\\.A", "NC.*S.*", "NP.*", "NC.*P.*", "GRUP\\.NOM"];

            foreach (Parse constituent in constituents)
            {
                for (int t = tags1.Length - 1; t >= 0; t--)
                {
                    if (Matches(constituent.Type, tags1[t]))
                    {
                        return constituent;
                    }
                }
            }

            for (int ci = 0; ci < constituents.Length; ci++)
            {
                if (constituents[ci].Type.Equals("SN") || constituents[ci].Type.Equals("GRUP.NOM"))
                {
                    return constituents[ci];
                }
            }

            string[] tags2 = ["\\$", "GRUP\\.A", "SA"];
            for (int ci = constituents.Length - 1; ci >= 0; ci--)
            {
                for (int ti = tags2.Length - 1; ti >= 0; ti--)
                {
                    if (Matches(constituents[ci].Type, tags2[ti]))
                    {
                        return constituents[ci];
                    }
                }
            }

            string[] tags3 =
                ["AQ0.*", "AQ[AC].*", "AO.*", "GRUP\\.A", "S\\.A", "RG", "RN", "GRUP\\.NOM"];
            for (int ci = constituents.Length - 1; ci >= 0; ci--)
            {
                for (int ti = tags3.Length - 1; ti >= 0; ti--)
                {
                    if (Matches(constituents[ci].Type, tags3[ti]))
                    {
                        return constituents[ci];
                    }
                }
            }

            return constituents[^1].Head;
        }
        // NOpenNLP: Java's Map.get returns null for an absent key; TryGetValue reproduces
        // the assign-and-test the upstream else-if performs.
        else if (headRules.TryGetValue(type, out var hr))
        {
            string[] tags = hr.tags;
            int cl = constituents.Length;
            int tl = tags.Length;
            if (hr.leftToRight)
            {
                for (int ti = 0; ti < tl; ti++)
                {
                    for (int ci = 0; ci < cl; ci++)
                    {
                        if (Matches(constituents[ci].Type, tags[ti]))
                        {
                            return constituents[ci];
                        }
                    }
                }

                return constituents[0].Head;
            }
            else
            {
                for (int ti = 0; ti < tl; ti++)
                {
                    for (int ci = cl - 1; ci >= 0; ci--)
                    {
                        if (Matches(constituents[ci].Type, tags[ti]))
                        {
                            return constituents[ci];
                        }
                    }
                }

                return constituents[cl - 1].Head;
            }
        }

        return constituents[^1].Head;
    }

    // NOpenNLP-specific: Java's String.matches() anchors the pattern to the whole input,
    // while .NET's Regex.IsMatch searches for a match anywhere. Wrapping the pattern in
    // \A(?:...)\z reproduces the Java semantics; \z (rather than $) is used so a trailing
    // newline in the constituent type cannot produce a spurious match.
    private static bool Matches(string input, string pattern) =>
        Regex.IsMatch(input, "\\A(?:" + pattern + ")\\z");

    private void ReadHeadRules(TextReader str)
    {
        headRules = new JCG.Dictionary<string, HeadRule>(60);
        while (str.ReadLine() is { } line)
        {
            // NOpenNLP: J2N's StringTokenizer exposes only the enumerator API publicly, so
            // MoveNext()/Current stand in for Java's hasMoreTokens()/nextToken(). Java's
            // nextToken() throws NoSuchElementException when the line is short; NextToken
            // here throws InvalidOperationException for the same malformed input.
            using StringTokenizer st = new(line);
            string num = NextToken(st);
            string type = NextToken(st);
            string dir = NextToken(st);
            string[] tags = new string[int.Parse(num) - 2];
            int ti = 0;
            while (st.MoveNext())
            {
                tags[ti] = st.Current;
                ti++;
            }

            headRules[type] = new HeadRule(dir.Equals("1"), tags);
        }
    }

    // NOpenNLP-specific: stands in for Java's StringTokenizer.nextToken(), which J2N does
    // not expose publicly. Throws when no token remains, as the Java method does.
    private static string NextToken(StringTokenizer st) =>
        st.MoveNext() ? st.Current : throw new InvalidOperationException("No tokens remain.");

    public void LabelGaps(IList<Constituent> stack)
    {
        if (stack.Count > 4)
        {
            //Constituent con0 = stack[stack.Count - 1];
            Constituent con1 = stack[^2];
            Constituent con2 = stack[^3];
            Constituent con3 = stack[^4];
            Constituent con4 = stack[^5];

            //subject extraction
            if (con1.Label.Equals("SN")
                && con2.Label.Equals("S") && con3.Label.Equals("GRUP.NOM"))
            {
                con1.Label = con1.Label + "-G";
                con2.Label = con2.Label + "-G";
                con3.Label = con3.Label + "-G";
            }
            //object extraction
            else if (con1.Label.Equals("SN") && con2.Label.Equals("GRUP.VERB")
                && con3.Label.Equals("S") && con4.Label.Equals("GRUP.NOM"))
            {
                con1.Label = con1.Label + "-G";
                con2.Label = con2.Label + "-G";
                con3.Label = con3.Label + "-G";
                con4.Label = con4.Label + "-G";
            }
        }
    }

    // NOpenNLP: writing head rules back out is part of training/serialization, which this
    // inference-only port omits. Reading them (the constructor above) is retained.
    // /// <summary>
    // /// Writes the head rules to the writer in a format suitable for loading
    // /// the head rules again with the constructor. The encoding must be
    // /// taken into account while working with the writer and reader.
    // /// <para/>
    // /// After the entries have been written, the writer is flushed.
    // /// The writer remains open after this method returns.
    // /// </summary>
    // public void Serialize(TextWriter writer)
    // {
    //     foreach (KeyValuePair<string, HeadRule> entry in headRules)
    //     {
    //         string type = entry.Key;
    //         HeadRule headRule = entry.Value;
    //
    //         // write num of tags
    //         writer.Write((headRule.tags.Length + 2).ToString());
    //         writer.Write(' ');
    //
    //         // write type
    //         writer.Write(type);
    //         writer.Write(' ');
    //
    //         // write l2r true == 1
    //         if (headRule.leftToRight)
    //             writer.Write("1");
    //         else
    //             writer.Write("0");
    //
    //         // write tags
    //         foreach (string tag in headRule.tags)
    //         {
    //             writer.Write(' ');
    //             writer.Write(tag);
    //         }
    //
    //         writer.Write('\n');
    //     }
    //
    //     writer.Flush();
    // }

    public override int GetHashCode() => HashCode.Combine(headRules, punctSet);

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(obj, this))
        {
            return true;
        }

        if (obj is AncoraSpanishHeadRules rules)
        {
            // NOpenNLP: J2N's Dictionary and HashSet implement the structural Equals that
            // Java's HashMap/HashSet provide; the BCL types compare by reference.
            return rules.headRules.Equals(headRules)
                && rules.punctSet.Equals(punctSet);
        }

        return false;
    }

    public Type ArtifactSerializerClass => typeof(HeadRulesSerializer);
}
