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
using System.Text;
using System.Text.RegularExpressions;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Parser;

/// <summary>
/// Data structure for holding parse constituents.
/// </summary>
public class Parse : ICloneable, IComparable<Parse>
{
    public const string BRACKET_LRB = "(";
    public const string BRACKET_RRB = ")";
    public const string BRACKET_LCB = "{";
    public const string BRACKET_RCB = "}";
    public const string BRACKET_LSB = "[";
    public const string BRACKET_RSB = "]";

    /// <summary>
    /// The text string on which this parse is based.
    /// This object is shared among all parses for the same sentence.
    /// </summary>
    private readonly string text; // NOpenNLP: made readonly

    /// <summary>
    /// The character offsets into the text for this constituent.
    /// </summary>
    private Span span;

    /// <summary>
    /// The syntactic type of this parse.
    /// </summary>
    private string type;

    /// <summary>
    /// The sub-constituents of this parse.
    /// </summary>
    // NOpenNLP: pruneParse assigns null here to detach a pruned node, so this is nullable.
    private JCG.List<Parse>? parts;

    /// <summary>
    /// The head parse of this parse. A parse can be its own head.
    /// </summary>
    private Parse head;

    /// <summary>
    /// A string used during parse construction to specify which
    /// stage of parsing has been performed on this node.
    /// </summary>
    private string? label;

    /// <summary>
    /// Index in the sentence of the head of this constituent.
    /// </summary>
    private int headIndex;

    /// <summary>
    /// The parent parse of this parse.
    /// </summary>
    private Parse? parent;

    /// <summary>
    /// The probability associated with the syntactic type
    /// assigned to this parse.
    /// </summary>
    private double prob;

    /// <summary>
    /// The string buffer used to track the derivation of this parse.
    /// </summary>
    private StringBuilder? derivation;

    /// <summary>
    /// Specifies whether this constituent was built during the chunking phase.
    /// </summary>
    private bool isChunk;

    /// <summary>
    /// The pattern used to find the base constituent label of a
    /// Penn Treebank labeled constituent.
    /// </summary>
    private static readonly Regex typePattern = new("^([^ =-]+)", RegexOptions.Compiled);

    /// <summary>
    /// The pattern used to find the function tags.
    /// </summary>
    private static readonly Regex funTypePattern = new("^[^ =-]+-([^ =-]+)", RegexOptions.Compiled);

    /// <summary>
    /// The pattern used to identify tokens in Penn Treebank labeled constituents.
    /// </summary>
    // NOpenNLP: Java's \s is ASCII-only by default; ECMAScript keeps .NET from also
    // matching Unicode whitespace here.
    private static readonly Regex tokenPattern =
        new("^[^ ()]+ ([^ ()]+)\\s*\\)", RegexOptions.Compiled | RegexOptions.ECMAScript);

    /// <summary>
    /// The set of punctuation parses which are between this parse and the previous parse.
    /// </summary>
    private ICollection<Parse>? prevPunctSet;

    /// <summary>
    /// The set of punctuation parses which are between this parse and
    /// the subsequent parse.
    /// </summary>
    private ICollection<Parse>? nextPunctSet;

    /// <summary>
    /// Specifies whether constituent labels should include parts specified
    /// after minus character.
    /// </summary>
    private static bool useFunctionTags;

    /// <summary>
    /// Creates a new parse node for this specified text and span of the specified type
    /// with the specified probability and the specified head index.
    /// </summary>
    /// <param name="text">The text of the sentence for which this node is a part of.</param>
    /// <param name="span">The character offsets for this node within the specified text.</param>
    /// <param name="type">The constituent label of this node.</param>
    /// <param name="p">The probability of this parse.</param>
    /// <param name="index">The token index of the head of this parse.</param>
    public Parse(string text, Span span, string type, double p, int index)
    {
        this.text = text;
        this.span = span;
        this.type = type;
        this.prob = p;
        this.head = this;
        this.headIndex = index;
        this.parts = [];
        this.label = null;
        this.parent = null;
    }

    /// <summary>
    /// Creates a new parse node for this specified text and span of the specified type with
    /// the specified probability and the specified head and head index.
    /// </summary>
    /// <param name="text">The text of the sentence for which this node is a part of.</param>
    /// <param name="span">The character offsets for this node within the specified text.</param>
    /// <param name="type">The constituent label of this node.</param>
    /// <param name="p">The probability of this parse.</param>
    /// <param name="h">The head token of this parse.</param>
    public Parse(string text, Span span, string type, double p, Parse? h)
        : this(text, span, type, p, 0)
    {
        if (h != null)
        {
            this.head = h;
            this.headIndex = h.headIndex;
        }
    }

    /// <summary>
    /// The sub-constituents of this parse. Detached nodes have no parts.
    /// </summary>
    private JCG.List<Parse> Parts => parts!;

    public object Clone()
    {
        Parse p = new(this.text, this.span, this.type, this.prob, this.head);
        p.parts = [.. this.Parts];

        if (derivation != null)
        {
            p.derivation = new StringBuilder(100);
            p.derivation.Append(this.derivation);
        }

        p.label = this.label;
        return p;
    }

    /// <summary>
    /// Clones the right frontier of parse up to the specified node.
    /// </summary>
    /// <param name="node">The last node in the right frontier of the parse tree which should be cloned.</param>
    /// <returns>A clone of this parse and its right frontier up to and including the specified node.</returns>
    public Parse Clone(Parse node)
    {
        if (this == node)
        {
            return (Parse)this.Clone();
        }
        else
        {
            Parse c = (Parse)this.Clone();
            Parse lc = c.Parts[^1];
            c.Parts[^1] = lc.Clone(node);
            return c;
        }
    }

    /// <summary>
    /// Clones the right frontier of this root parse up to and including the specified node.
    /// </summary>
    /// <param name="node">The last node in the right frontier of the parse tree which should be cloned.</param>
    /// <param name="parseIndex">The child index of the parse for this root node.</param>
    /// <returns>
    /// A clone of this root parse and its right frontier up to and including the specified node.
    /// </returns>
    public Parse CloneRoot(Parse node, int parseIndex)
    {
        Parse c = (Parse)this.Clone();
        Parse fc = c.Parts[parseIndex];
        c.Parts[parseIndex] = fc.Clone(node);
        return c;
    }

    /// <summary>
    /// Specifies whether function tags should be included as part of the constituent type.
    /// </summary>
    /// <param name="uft">true is they should be included; false otherwise.</param>
    public static void UseFunctionTags(bool uft) => useFunctionTags = uft;

    /// <summary>
    /// Gets or sets the constituent label for this node of the parse.
    /// </summary>
    public string Type
    {
        get => type;
        set => type = value;
    }

    /// <summary>
    /// Returns the set of punctuation parses that occur immediately before this parse.
    /// </summary>
    public ICollection<Parse>? PreviousPunctuationSet => prevPunctSet;

    /// <summary>
    /// Designates that the specified punctuation should is prior to this parse.
    /// </summary>
    /// <param name="punct">The punctuation.</param>
    public void AddPreviousPunctuation(Parse punct)
    {
        // NOpenNLP: upstream uses a TreeSet, which orders by Parse.compareTo (descending
        // probability) and drops entries that compare equal. J2N's SortedSet with the
        // same comparer preserves both behaviors.
        this.prevPunctSet ??= new JCG.SortedSet<Parse>();
        prevPunctSet.Add(punct);
    }

    /// <summary>
    /// Returns the set of punctuation parses that occur immediately after this parse.
    /// </summary>
    public ICollection<Parse>? NextPunctuationSet => nextPunctSet;

    /// <summary>
    /// Designates that the specified punctuation follows this parse.
    /// </summary>
    /// <param name="punct">The punctuation set.</param>
    public void AddNextPunctuation(Parse punct)
    {
        this.nextPunctSet ??= new JCG.SortedSet<Parse>();
        nextPunctSet.Add(punct);
    }

    /// <summary>
    /// Sets the set of punctuation tags which follow this parse.
    /// </summary>
    /// <param name="punctSet">The set of punctuation tags which follow this parse.</param>
    public void SetNextPunctuation(ICollection<Parse>? punctSet) => this.nextPunctSet = punctSet;

    /// <summary>
    /// Sets the set of punctuation tags which preceed this parse.
    /// </summary>
    /// <param name="punctSet">The set of punctuation tags which preceed this parse.</param>
    public void SetPrevPunctuation(ICollection<Parse>? punctSet) => this.prevPunctSet = punctSet;

    /// <summary>
    /// Inserts the specified constituent into this parse based on its text span. This
    /// method assumes that the specified constituent can be inserted into this parse.
    /// </summary>
    /// <param name="constituent">The constituent to be inserted.</param>
    public void Insert(Parse constituent)
    {
        Span ic = constituent.span;
        if (span.Contains(ic))
        {
            int pi = 0;
            int pn = Parts.Count;
            for (; pi < pn; pi++)
            {
                Parse subPart = Parts[pi];
                Span sp = subPart.span;
                if (sp.Start >= ic.End)
                {
                    break;
                }
                // constituent contains subPart
                else if (ic.Contains(sp))
                {
                    Parts.RemoveAt(pi);
                    pi--;
                    constituent.Parts.Add(subPart);
                    subPart.Parent = constituent;
                    pn = Parts.Count;
                }
                else if (sp.Contains(ic))
                {
                    subPart.Insert(constituent);
                    return;
                }
            }

            Parts.Insert(pi, constituent);
            constituent.Parent = this;
        }
        else
        {
            throw new ArgumentException("Inserting constituent not contained in the sentence!");
        }
    }

    /// <summary>
    /// Appends the specified string buffer with a string representation of this parse.
    /// </summary>
    /// <param name="sb">A string buffer into which the parse string can be appended.</param>
    public void Show(StringBuilder sb)
    {
        int start;
        start = span.Start;
        if (!type.Equals(AbstractBottomUpParser.TOK_NODE))
        {
            sb.Append('(');
            sb.Append(type).Append(' ');
        }

        foreach (Parse c in Parts)
        {
            Span s = c.span;
            if (start < s.Start)
            {
                sb.Append(EncodeToken(text.Substring(start, s.Start - start)));
            }

            c.Show(sb);
            start = s.End;
        }

        if (start < span.End)
        {
            sb.Append(EncodeToken(text.Substring(start, span.End - start)));
        }

        if (!type.Equals(AbstractBottomUpParser.TOK_NODE))
        {
            sb.Append(')');
        }
    }

    /// <summary>
    /// Displays this parse using Penn Treebank-style formatting.
    /// </summary>
    public void Show()
    {
        StringBuilder sb = new(text.Length * 4);
        Show(sb);
        Console.WriteLine(sb);
    }

    /// <summary>
    /// Returns the probability associated with the pos-tag sequence assigned to this parse.
    /// </summary>
    public double TagSequenceProb
    {
        get
        {
            if (Parts.Count == 1 && Parts[0].type.Equals(AbstractBottomUpParser.TOK_NODE))
            {
                return Math.Log(prob);
            }
            else if (Parts.Count == 0)
            {
                Console.Error.WriteLine("Parse.getTagSequenceProb: Wrong base case!");
                return 0.0;
            }
            else
            {
                double sum = 0.0;
                foreach (Parse part in Parts)
                {
                    sum += part.TagSequenceProb;
                }

                return sum;
            }
        }
    }

    /// <summary>
    /// Returns whether this parse is complete.
    /// </summary>
    /// <returns>Returns true if the parse contains a single top-most node.</returns>
    public bool Complete() => Parts.Count == 1;

    public string CoveredText => text.Substring(span.Start, span.End - span.Start);

    /// <summary>
    /// Represents this parse in a human readable way.
    /// </summary>
    public override string ToString() => CoveredText;

    /// <summary>
    /// Returns the text of the sentence over which this parse was formed.
    /// </summary>
    public string Text => text;

    /// <summary>
    /// Returns the character offsets for this constituent.
    /// </summary>
    public Span Span => span;

    /// <summary>
    /// Returns the log of the product of the probability associated with all the
    /// decisions which formed this constituent.
    /// </summary>
    public double Prob => prob;

    /// <summary>
    /// Adds the specified probability log to this current log for this parse.
    /// </summary>
    /// <param name="logProb">The probability of an action performed on this parse.</param>
    public void AddProb(double logProb) => this.prob += logProb;

    /// <summary>
    /// Returns the child constituents of this constituent.
    /// </summary>
    public Parse[] GetChildren() => [.. Parts];

    /// <summary>
    /// Replaces the child at the specified index with a new child with the specified label.
    /// </summary>
    /// <param name="index">The index of the child to be replaced.</param>
    /// <param name="label">The label to be assigned to the new child.</param>
    public void SetChild(int index, string label)
    {
        Parse newChild = (Parse)Parts[index].Clone();
        newChild.Label = label;
        Parts[index] = newChild;
    }

    public void Add(Parse daughter, IHeadRules rules)
    {
        if (daughter.prevPunctSet != null)
        {
            Parts.AddRange(daughter.prevPunctSet);
        }

        Parts.Add(daughter);
        this.span = new Span(span.Start, daughter.Span.End);
        this.head = rules.GetHead(GetChildren(), type)!;
        this.headIndex = head.headIndex;
    }

    public void Remove(int index)
    {
        Parts.RemoveAt(index);
        if (Parts.Count > 0)
        {
            if (index == 0 || index == Parts.Count)
            {
                // size is orig last element
                span = new Span(Parts[0].span.Start, Parts[^1].span.End);
            }
        }
    }

    public Parse AdjoinRoot(Parse node, IHeadRules rules, int parseIndex)
    {
        Parse lastChild = Parts[parseIndex];
        Parse adjNode = new(this.text,
            new Span(lastChild.Span.Start, node.Span.End), lastChild.Type, 1,
            rules.GetHead([lastChild, node], lastChild.Type));
        adjNode.Parts.Add(lastChild);
        if (node.prevPunctSet != null)
        {
            adjNode.Parts.AddRange(node.prevPunctSet);
        }

        adjNode.Parts.Add(node);
        Parts[parseIndex] = adjNode;
        return adjNode;
    }

    /// <summary>
    /// Sister adjoins this node's last child and the specified sister node and returns their
    /// new parent node. The new parent node replace this nodes last child.
    /// </summary>
    /// <param name="sister">The node to be adjoined.</param>
    /// <param name="rules">The head rules for the parser.</param>
    /// <returns>The new parent node of this node and the specified sister node.</returns>
    public Parse Adjoin(Parse sister, IHeadRules rules)
    {
        Parse lastChild = Parts[^1];
        Parse adjNode = new(this.text,
            new Span(lastChild.Span.Start, sister.Span.End), lastChild.Type, 1,
            rules.GetHead([lastChild, sister], lastChild.Type));
        adjNode.Parts.Add(lastChild);
        if (sister.prevPunctSet != null)
        {
            adjNode.Parts.AddRange(sister.prevPunctSet);
        }

        adjNode.Parts.Add(sister);
        Parts[^1] = adjNode;
        this.span = new Span(span.Start, sister.Span.End);
        this.head = rules.GetHead(GetChildren(), type)!;
        this.headIndex = head.headIndex;
        return adjNode;
    }

    public void ExpandTopNode(Parse root)
    {
        bool beforeRoot = true;
        for (int pi = 0, ai = 0; pi < Parts.Count; pi++, ai++)
        {
            Parse node = Parts[pi];
            if (node == root)
            {
                beforeRoot = false;
            }
            else if (beforeRoot)
            {
                root.Parts.Insert(ai, node);
                Parts.RemoveAt(pi);
                pi--;
            }
            else
            {
                root.Parts.Add(node);
                Parts.RemoveAt(pi);
                pi--;
            }
        }

        root.UpdateSpan();
    }

    /// <summary>
    /// Returns the number of children for this parse node.
    /// </summary>
    public int ChildCount => Parts.Count;

    /// <summary>
    /// Returns the index of this specified child.
    /// </summary>
    /// <param name="child">A child of this parse.</param>
    /// <returns>
    /// the index of this specified child or -1 if the specified child is not a child of this parse.
    /// </returns>
    public int IndexOf(Parse child) => Parts.IndexOf(child);

    /// <summary>
    /// Returns the head constituent associated with this constituent.
    /// </summary>
    public Parse Head => head;

    /// <summary>
    /// Returns the index within a sentence of the head token for this parse.
    /// </summary>
    public int HeadIndex => headIndex;

    /// <summary>
    /// Gets or sets the label assigned to this parse node during parsing
    /// which specifies how this node will be formed into a constituent.
    /// </summary>
    public string? Label
    {
        get => label;
        set => label = value;
    }

    private static string? GetType(string rest)
    {
        if (rest.StartsWith("-LCB-", StringComparison.Ordinal))
        {
            return "-LCB-";
        }
        else if (rest.StartsWith("-RCB-", StringComparison.Ordinal))
        {
            return "-RCB-";
        }
        else if (rest.StartsWith("-LRB-", StringComparison.Ordinal))
        {
            return "-LRB-";
        }
        else if (rest.StartsWith("-RRB-", StringComparison.Ordinal))
        {
            return "-RRB-";
        }
        else if (rest.StartsWith("-RSB-", StringComparison.Ordinal))
        {
            return "-RSB-";
        }
        else if (rest.StartsWith("-LSB-", StringComparison.Ordinal))
        {
            return "-LSB-";
        }
        else if (rest.StartsWith("-NONE-", StringComparison.Ordinal))
        {
            return "-NONE-";
        }
        else
        {
            Match typeMatcher = typePattern.Match(rest);
            if (typeMatcher.Success)
            {
                string type = typeMatcher.Groups[1].Value;
                if (useFunctionTags)
                {
                    Match funMatcher = funTypePattern.Match(rest);
                    if (funMatcher.Success)
                    {
                        string ftag = funMatcher.Groups[1].Value;
                        type = type + "-" + ftag;
                    }
                }

                return type;
            }
        }

        return null;
    }

    private static string EncodeToken(string token)
    {
        if (BRACKET_LRB.Equals(token))
        {
            return "-LRB-";
        }
        else if (BRACKET_RRB.Equals(token))
        {
            return "-RRB-";
        }
        else if (BRACKET_LCB.Equals(token))
        {
            return "-LCB-";
        }
        else if (BRACKET_RCB.Equals(token))
        {
            return "-RCB-";
        }
        else if (BRACKET_LSB.Equals(token))
        {
            return "-LSB-";
        }
        else if (BRACKET_RSB.Equals(token))
        {
            return "-RSB-";
        }

        return token;
    }

    private static string DecodeToken(string token)
    {
        if ("-LRB-".Equals(token))
        {
            return BRACKET_LRB;
        }
        else if ("-RRB-".Equals(token))
        {
            return BRACKET_RRB;
        }
        else if ("-LCB-".Equals(token))
        {
            return BRACKET_LCB;
        }
        else if ("-RCB-".Equals(token))
        {
            return BRACKET_RCB;
        }
        else if ("-LSB-".Equals(token))
        {
            return BRACKET_LSB;
        }
        else if ("-RSB-".Equals(token))
        {
            return BRACKET_RSB;
        }

        return token;
    }

    /// <summary>
    /// Returns the string containing the token for the specified portion of the parse string or
    /// null if the portion of the parse string does not represent a token.
    /// </summary>
    /// <param name="rest">The portion of the parse string remaining to be processed.</param>
    /// <returns>
    /// The string containing the token for the specified portion of the parse string or
    /// null if the portion of the parse string does not represent a token.
    /// </returns>
    private static string? GetToken(string rest)
    {
        Match tokenMatcher = tokenPattern.Match(rest);
        if (tokenMatcher.Success)
        {
            return DecodeToken(tokenMatcher.Groups[1].Value);
        }

        return null;
    }

    /// <summary>
    /// Computes the head parses for this parse and its sub-parses and stores this information
    /// in the parse data structure.
    /// </summary>
    /// <param name="rules">The head rules which determine how the head of the parse is computed.</param>
    public void UpdateHeads(IHeadRules rules)
    {
        if (parts != null && parts.Count != 0)
        {
            foreach (Parse c in parts)
            {
                c.UpdateHeads(rules);
            }

            this.head = rules.GetHead([.. parts], type)!;
            if (head == null)
            {
                head = this;
            }
            else
            {
                this.headIndex = head.headIndex;
            }
        }
        else
        {
            this.head = this;
        }
    }

    public void UpdateSpan() =>
        span = new Span(Parts[0].span.Start, Parts[^1].span.End);

    /// <summary>
    /// Prune the specified sentence parse of vacuous productions.
    /// </summary>
    public static void PruneParse(Parse parse)
    {
        JCG.List<Parse> nodes = [parse];
        while (nodes.Count != 0)
        {
            Parse node = nodes[0];
            nodes.RemoveAt(0);
            Parse[] children = node.GetChildren();
            if (children.Length == 1 && node.Type.Equals(children[0].Type))
            {
                int index = node.Parent!.Parts.IndexOf(node);
                children[0].Parent = node.Parent;
                node.Parent!.Parts[index] = children[0];
                node.parent = null;
                node.parts = null;
            }

            nodes.AddRange(children);
        }
    }

    public static void FixPossesives(Parse parse)
    {
        Parse[] tags = parse.GetTagNodes();
        for (int ti = 0; ti < tags.Length; ti++)
        {
            if (tags[ti].Type.Equals("POS"))
            {
                if (ti + 1 < tags.Length && tags[ti + 1].Parent == tags[ti].Parent!.Parent)
                {
                    int start = tags[ti + 1].Span.Start;
                    int end = tags[ti + 1].Span.End;
                    for (int npi = ti + 2; npi < tags.Length; npi++)
                    {
                        if (tags[npi].Parent == tags[npi - 1].Parent)
                        {
                            end = tags[npi].Span.End;
                        }
                        else
                        {
                            break;
                        }
                    }

                    Parse npPos = new(parse.Text, new Span(start, end), "NP", 1, tags[ti + 1]);
                    parse.Insert(npPos);
                }
            }
        }
    }

    /// <summary>
    /// Parses the specified tree-bank style parse string and return a Parse structure for that string.
    /// </summary>
    /// <param name="parse">A tree-bank style parse string.</param>
    /// <returns>a Parse structure for the specified tree-bank style parse string.</returns>
    public static Parse ParseParse(string parse) => ParseParse(parse, null);

    /// <summary>
    /// Parses the specified tree-bank style parse string and return a Parse structure
    /// for that string.
    /// </summary>
    /// <param name="parse">A tree-bank style parse string.</param>
    /// <param name="gl">The gap labeler.</param>
    /// <returns>a Parse structure for the specified tree-bank style parse string.</returns>
    public static Parse ParseParse(string parse, IGapLabeler? gl)
    {
        StringBuilder text = new();
        int offset = 0;
        // NOpenNLP: upstream uses java.util.Stack, which extends Vector; IGapLabeler.LabelGaps
        // indexes into it, so a list stands in for it here. See IGapLabeler.
        JCG.List<Constituent> stack = [];
        JCG.List<Constituent> cons = [];
        for (int ci = 0, cl = parse.Length; ci < cl; ci++)
        {
            char c = parse[ci];
            if (c == '(')
            {
                string rest = parse.Substring(ci + 1);
                string? type = GetType(rest);
                if (type == null)
                {
                    Console.Error.WriteLine("null type for: " + rest);
                }

                string? token = GetToken(rest);
                stack.Add(new Constituent(type!, new Span(offset, offset)));
                if (token != null)
                {
                    if (Equals(type, "-NONE-") && gl != null)
                    {
                        gl.LabelGaps(stack);
                    }
                    else
                    {
                        cons.Add(new Constituent(AbstractBottomUpParser.TOK_NODE,
                            new Span(offset, offset + token.Length)));
                        text.Append(token).Append(' ');
                        offset += token.Length + 1;
                    }
                }
            }
            else if (c == ')')
            {
                Constituent con = stack[^1];
                stack.RemoveAt(stack.Count - 1);
                int start = con.Span.Start;
                if (start < offset)
                {
                    cons.Add(new Constituent(con.Label, new Span(start, offset - 1)));
                }
            }
        }

        string txt = text.ToString();
        int tokenIndex = -1;
        Parse p = new(txt, new Span(0, txt.Length), AbstractBottomUpParser.TOP_NODE, 1, 0);
        foreach (Constituent con in cons)
        {
            string type = con.Label;
            if (!type.Equals(AbstractBottomUpParser.TOP_NODE))
            {
                if (AbstractBottomUpParser.TOK_NODE.Equals(type))
                {
                    tokenIndex++;
                }

                Parse c = new(txt, con.Span, type, 1, tokenIndex);
                p.Insert(c);
            }
        }

        return p;
    }

    /// <summary>
    /// Gets or sets the parent parse node of this constituent.
    /// </summary>
    public Parse? Parent
    {
        get => parent;
        set => parent = value;
    }

    /// <summary>
    /// Indicates whether this parse node is a pos-tag.
    /// </summary>
    public bool IsPosTag =>
        Parts.Count == 1 && Parts[0].Type.Equals(AbstractBottomUpParser.TOK_NODE);

    /// <summary>
    /// Returns true if this constituent contains no sub-constituents.
    /// </summary>
    public bool IsFlat
    {
        get
        {
            bool flat = true;
            foreach (Parse part in Parts)
            {
                flat &= part.IsPosTag;
            }

            return flat;
        }
    }

    /// <summary>
    /// Gets or sets whether this constituent was built during the chunking phase.
    /// </summary>
    public bool IsChunk
    {
        get => isChunk;
        set => isChunk = value;
    }

    /// <summary>
    /// Returns the parse nodes which are children of this node and which are pos tags.
    /// </summary>
    public Parse[] GetTagNodes()
    {
        JCG.List<Parse> tags = [];
        JCG.List<Parse> nodes = [.. this.Parts];
        while (nodes.Count != 0)
        {
            Parse p = nodes[0];
            nodes.RemoveAt(0);
            if (p.IsPosTag)
            {
                tags.Add(p);
            }
            else
            {
                nodes.InsertRange(0, p.Parts);
            }
        }

        return [.. tags];
    }

    public Parse[] GetTokenNodes()
    {
        JCG.List<Parse> tokens = [];
        JCG.List<Parse> nodes = [.. this.Parts];
        while (nodes.Count != 0)
        {
            Parse p = nodes[0];
            nodes.RemoveAt(0);
            if (p.Type.Equals(AbstractBottomUpParser.TOK_NODE))
            {
                tokens.Add(p);
            }
            else
            {
                nodes.InsertRange(0, p.Parts);
            }
        }

        return [.. tokens];
    }

    /// <summary>
    /// Returns the deepest shared parent of this node and the specified node.
    /// If the nodes are identical then their parent is returned.
    /// If one node is the parent of the other then the parent node is returned.
    /// </summary>
    /// <param name="node">The node from which parents are compared to this node's parents.</param>
    /// <returns>the deepest shared parent of this node and the specified node.</returns>
    public Parse? GetCommonParent(Parse? node)
    {
        if (this == node)
        {
            return parent;
        }

        JCG.HashSet<Parse> parents = [];
        Parse? cparent = this;
        while (cparent != null)
        {
            parents.Add(cparent);
            cparent = cparent.Parent;
        }

        while (node != null)
        {
            if (parents.Contains(node))
            {
                return node;
            }

            node = node.Parent;
        }

        return null;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(obj, this))
        {
            return true;
        }

        if (obj is Parse p)
        {
            return Equals(label, p.label) && span.Equals(p.span)
                && text.Equals(p.text) && JCG.ListEqualityComparer<Parse>.Default.Equals(parts, p.parts);
        }

        return false;
    }

    // Note: label is missing here!
    public override int GetHashCode() => HashCode.Combine(span, text);

    public int CompareTo(Parse? p) => p!.Prob.CompareTo(this.Prob);

    /// <summary>
    /// Gets or sets the derivation string for this parse if one has been created.
    /// </summary>
    public StringBuilder? Derivation
    {
        get => derivation;
        set => derivation = value;
    }

    private void CodeTree(Parse p, int[] levels)
    {
        Parse[] kids = p.GetChildren();
        StringBuilder levelsBuff = new();
        levelsBuff.Append('[');
        int[] nlevels = new int[levels.Length + 1];
        for (int li = 0; li < levels.Length; li++)
        {
            nlevels[li] = levels[li];
            levelsBuff.Append(levels[li]).Append('.');
        }

        for (int ki = 0; ki < kids.Length; ki++)
        {
            nlevels[levels.Length] = ki;
            Console.WriteLine(levelsBuff.ToString() + ki + "] " + kids[ki].Type +
                " " + kids[ki].GetHashCode() + " -> " + kids[ki].Parent!.GetHashCode() +
                " " + kids[ki].Parent!.Type + " " + kids[ki].CoveredText);
            CodeTree(kids[ki], nlevels);
        }
    }

    /// <summary>
    /// Prints to standard out a representation of the specified parse which
    /// contains hash codes so that parent/child relationships can be explicitly seen.
    /// </summary>
    public void ShowCodeTree() => CodeTree(this, []);

    /// <summary>
    /// Utility method to inserts named entities.
    /// </summary>
    public static void AddNames(string tag, Span[] names, Parse[] tokens)
    {
        foreach (Span nameTokenSpan in names)
        {
            Parse startToken = tokens[nameTokenSpan.Start];
            Parse endToken = tokens[nameTokenSpan.End - 1];
            Parse? commonParent = startToken.GetCommonParent(endToken);
            if (commonParent != null)
            {
                Span nameSpan = new(startToken.Span.Start, endToken.Span.End);
                if (nameSpan.Equals(commonParent.Span))
                {
                    commonParent.Insert(new Parse(commonParent.Text, nameSpan, tag, 1.0,
                        endToken.HeadIndex));
                }
                else
                {
                    Parse[] kids = commonParent.GetChildren();
                    bool crossingKids = false;
                    foreach (Parse kid in kids)
                    {
                        if (nameSpan.Crosses(kid.Span))
                        {
                            crossingKids = true;
                        }
                    }

                    if (!crossingKids)
                    {
                        commonParent.Insert(new Parse(commonParent.Text, nameSpan,
                            tag, 1.0, endToken.HeadIndex));
                    }
                    else
                    {
                        if (commonParent.Type.Equals("NP"))
                        {
                            Parse[] grandKids = kids[0].GetChildren();
                            if (grandKids.Length > 1
                                && nameSpan.Contains(grandKids[^1].Span))
                            {
                                commonParent.Insert(new Parse(commonParent.Text, commonParent.Span,
                                    tag, 1.0, commonParent.HeadIndex));
                            }
                        }
                    }
                }
            }
        }
    }
}
