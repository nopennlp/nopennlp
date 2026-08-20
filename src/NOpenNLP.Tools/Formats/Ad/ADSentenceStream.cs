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
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Ad;

/// <summary>
/// Stream filter which merges text lines into sentences, following the Arvores
/// Deitadas syntax.
/// <para/>
/// Information about the format:<br/>
/// Susana Afonso.
/// "Árvores deitadas: Descrição do formato e das opções de análise na Floresta Sintáctica"
/// .<br/>
/// 12 de Fevereiro de 2006.
/// http://www.linguateca.pt/documentos/Afonso2006ArvoresDeitadas.pdf
/// <para/>
/// <b>Note:</b> Do not use this class, internal use only!
/// </summary>
public class ADSentenceStream : FilterObjectStream<string?, ADSentenceStream.Sentence?>
{
    public class Sentence
    {
        public const string MetaLabelFinal = "final";

        public string? Text { get; set; }

        public SentenceParser.Node? Root { get; set; }

        public string? Metadata { get; set; }
    }

    /// <summary>
    /// Parses a sample of AD corpus. A sentence in AD corpus is represented by a
    /// Tree. In this class we declare some types to represent that tree. Today we get only
    /// the first alternative (A1).
    /// </summary>
    public class SentenceParser
    {
        // NOpenNLP: upstream declares these as instance fields, recompiling the pattern for
        // every SentenceParser. They are immutable, so they are static readonly here.
        private static readonly Regex nodePattern =
            new Regex("([=-]*)([^:=]+:[^\\(\\s]+)(\\(([^\\)]+)\\))?\\s*(?:(\\((<.+>)\\))*)\\s*$");
        private static readonly Regex leafPattern =
            new Regex("^([=-]*)([^:=]+):([^\\(\\s]+)\\([\"'](.+)[\"']\\s*((?:<.+>)*)\\s*([^\\)]+)?\\)\\s+(.+)");
        private static readonly Regex bizarreLeafPattern =
            new Regex("^([=-]*)([^:=]+=[^\\(\\s]+)\\(([\"'].+[\"'])?\\s*([^\\)]+)?\\)\\s+(.+)");

        // NOpenNLP: RegexOptions.ECMAScript is required here and only here. Java's \w/\W are
        // ASCII-only by default ([a-zA-Z0-9_]), so an accented letter such as 'á' is \W and this
        // pattern matches it as punctuation. .NET's \w/\W are Unicode-aware by default, so 'á'
        // would be \w and the line would NOT be recognized as punctuation. The AD corpus is
        // Portuguese and full of accented characters, so the default would silently change which
        // tokens become punctuation leaves. ECMAScript restores Java's ASCII semantics.
        private static readonly Regex punctuationPattern = new Regex("^(=*)(\\W+)$", RegexOptions.ECMAScript);

        // NOpenNLP: hoisted out of fixPunctuation(), where upstream calls String.replaceAll and so
        // recompiles both patterns on every sentence.
        private static readonly Regex punctuationPeriodPattern = new Regex("\\»\\s+\\.");
        private static readonly Regex punctuationCommaPattern = new Regex("\\»\\s+\\,");

        // NOpenNLP: hoisted out of getElement(), where upstream calls String.matches(). This uses \w
        // and so needs RegexOptions.ECMAScript for the same reason punctuationPattern does: Java's \w
        // is ASCII-only, so a lexeme starting with an accented letter does NOT match upstream and the
        // leaf is kept. Under .NET's default Unicode-aware \w it would match and the leaf would be
        // dropped instead. String.matches() also anchors the whole input, hence the explicit anchors.
        private static readonly Regex bizarreLexemePattern =
            new Regex("^(?:\\w.*?[\\.<>].*)$", RegexOptions.ECMAScript);

        // These stay instance fields: parse() deliberately reuses the previous sentence's text and
        // metadata when it meets the "&&" marker, which signals an alternative parse of the same
        // sentence.
        private string? text;
        private string? meta;

        /// <summary>
        /// Parse the sentence.
        /// </summary>
        public Sentence? Parse(string sentenceString, int para, bool isTitle, bool isBox)
        {
            TextReader reader = new StringReader(sentenceString);
            Sentence sentence = new Sentence();
            Node root = new Node();
            try
            {
                // first line is <s ...>
                string? line = reader.ReadLine();

                bool useSameTextAndMeta = false; // to handle cases where there are diff sug of parse (&&)

                // should find the source source
                // NOpenNLP: upstream dereferences `line` here without a null check and relies on the
                // catch-all below to swallow the resulting NullPointerException when the stream ends
                // before a SOURCE line. The null check is explicit here so the loop exits the same way
                // without depending on an exception.
                while (line != null && !line.StartsWith("SOURCE", StringComparison.Ordinal))
                {
                    if (line.Equals("&&", StringComparison.Ordinal))
                    {
                        // same sentence again!
                        useSameTextAndMeta = true;
                        break;
                    }
                    line = reader.ReadLine();
                    if (line == null)
                    {
                        return null;
                    }
                }
                if (line == null)
                {
                    return null;
                }
                if (!useSameTextAndMeta)
                {
                    // got source, get the metadata
                    string metaFromSource = line.Substring(7);
                    line = reader.ReadLine();
                    if (line == null)
                    {
                        return null;
                    }
                    // we should have the plain sentence
                    // we remove the first token
                    int start = line.IndexOf(" ", StringComparison.Ordinal);
                    text = line.Substring(start + 1).Trim();
                    text = FixPunctuation(text);
                    string titleTag = "";
                    if (isTitle) titleTag = " title";
                    string boxTag = "";
                    if (isBox) boxTag = " box";
                    if (start > 0)
                    {
                        meta = line.Substring(0, start) + " p=" + para + titleTag + boxTag + metaFromSource;
                    }
                }
                sentence.Text = text;
                sentence.Metadata = meta;
                // now we look for the root node

                // skip lines starting with ###
                line = reader.ReadLine();
                while (line != null && line.StartsWith("###", StringComparison.Ordinal))
                {
                    line = reader.ReadLine();
                }

                // got the root. Add it to the stack
                // NOpenNLP: upstream uses java.util.Stack, which extends Vector and therefore also
                // offers bottom-indexed get(int) and firstElement(). System.Collections.Generic.Stack<T>
                // exposes neither, and J2N has no Stack<T>, so a list stands in: Add/peek-last/remove-last
                // reproduce push/peek/pop, and index 0 is firstElement().
                JCG.List<Node> nodeStack = new JCG.List<Node>();

                root.SyntacticTag = "ROOT";
                root.Level = 0;
                nodeStack.Add(root);

                /* now we have to take care of the lastLevel. Every time it raises, we will add the
                leaf to the node at the top. If it decreases, we remove the top. */

                while (line != null && line.Length != 0 && !line.StartsWith("</s>", StringComparison.Ordinal)
                    && !line.Equals("&&", StringComparison.Ordinal))
                {
                    TreeElement? element = GetElement(line);

                    if (element != null)
                    {
                        // The idea here is to keep a stack of nodes that are candidates for
                        // parenting the following elements (nodes and leafs).

                        // 1) When we get a new element, we check its level and remove from
                        // the top of the stack nodes that are brothers or nephews.
                        while (nodeStack.Count > 0 && element.Level > 0
                            && element.Level <= nodeStack[nodeStack.Count - 1].Level)
                        {
                            // NOpenNLP: upstream assigns the popped node to an unused local `nephew`.
                            nodeStack.RemoveAt(nodeStack.Count - 1);
                        }

                        if (element.IsLeaf)
                        {
                            // 2a) If the element is a leaf and there is no parent candidate,
                            // add it as a daughter of the root.
                            if (nodeStack.Count == 0)
                            {
                                root.AddElement(element);
                            }
                            else
                            {
                                // 2b) There are parent candidates.
                                // look for the node with the correct level
                                Node peek = nodeStack[nodeStack.Count - 1];
                                if (element.Level == 0)
                                { // add to the root
                                    nodeStack[0].AddElement(element);
                                }
                                else
                                {
                                    Node? parent = null;
                                    int index = nodeStack.Count - 1;
                                    while (parent == null)
                                    {
                                        if (peek.Level < element.Level)
                                        {
                                            parent = peek;
                                        }
                                        else
                                        {
                                            index--;
                                            if (index > -1)
                                            {
                                                peek = nodeStack[index];
                                            }
                                            else
                                            {
                                                parent = nodeStack[0];
                                            }
                                        }
                                    }
                                    parent.AddElement(element);
                                }
                            }
                        }
                        else
                        {
                            // 3) Check if the element that is at the top of the stack is this
                            // node parent, if yes add it as a son
                            if (nodeStack.Count > 0 && nodeStack[nodeStack.Count - 1].Level < element.Level)
                            {
                                nodeStack[nodeStack.Count - 1].AddElement(element);
                            }
                            else
                            {
                                Console.Error.WriteLine("should not happen!");
                            }
                            // 4) Add it to the stack so it is a parent candidate.
                            nodeStack.Add((Node)element);
                        }
                    }
                    line = reader.ReadLine();
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(sentenceString);
                Console.Error.WriteLine(e.ToString());
                return sentence;
            }
            // second line should be SOURCE
            sentence.Root = root;
            return sentence;
        }

        private static string FixPunctuation(string text)
        {
            text = punctuationPeriodPattern.Replace(text, "».");
            text = punctuationCommaPattern.Replace(text, "»,");
            return text;
        }

        /// <summary>
        /// Parse a tree element from a AD line.
        /// </summary>
        /// <param name="line">the AD line</param>
        /// <returns>the tree element</returns>
        public TreeElement? GetElement(string line)
        {
            // Note: all levels are higher than 1, because 0 is reserved for the root.

            // try node
            // NOpenNLP: Java's Matcher.matches() requires the whole input to match. .NET's
            // Regex.Match does not, so every matches() call site below is anchored explicitly.
            Match nodeMatcher = MatchWholeString(nodePattern, line);
            if (nodeMatcher.Success)
            {
                int level = nodeMatcher.Groups[1].Value.Length + 1;
                string syntacticTag = nodeMatcher.Groups[2].Value;
                Node node = new Node();
                node.Level = level;
                node.SyntacticTag = syntacticTag;
                return node;
            }

            Match leafMatcher = MatchWholeString(leafPattern, line);
            if (leafMatcher.Success)
            {
                int level = leafMatcher.Groups[1].Value.Length + 1;
                string syntacticTag = leafMatcher.Groups[2].Value;
                // NOpenNLP: groups 3, 5 and 6 are optional in the pattern. Java's group(n) returns
                // null when a group did not participate; .NET returns "" with Success == false, so
                // the null is reconstructed from Success to keep the downstream null checks working.
                string funcTag = leafMatcher.Groups[3].Value;
                string lemma = leafMatcher.Groups[4].Value;
                string secondaryTag = leafMatcher.Groups[5].Value;
                string? morphologicalTag = leafMatcher.Groups[6].Success ? leafMatcher.Groups[6].Value : null;
                string lexeme = leafMatcher.Groups[7].Value;
                Leaf leaf = new Leaf();
                leaf.Level = level;
                leaf.SyntacticTag = syntacticTag;
                leaf.FunctionalTag = funcTag;
                leaf.SecondaryTag = secondaryTag;
                leaf.MorphologicalTag = morphologicalTag;
                leaf.Lexeme = lexeme;
                leaf.Lemma = lemma;

                return leaf;
            }

            Match punctuationMatcher = MatchWholeString(punctuationPattern, line);
            if (punctuationMatcher.Success)
            {
                int level = punctuationMatcher.Groups[1].Value.Length + 1;
                string lexeme = punctuationMatcher.Groups[2].Value;
                Leaf leaf = new Leaf();
                leaf.Level = level;
                leaf.Lexeme = lexeme;
                return leaf;
            }

            // process the bizarre cases
            if (line.Equals("_", StringComparison.Ordinal) || line.StartsWith("<lixo", StringComparison.Ordinal)
                || line.StartsWith("pause", StringComparison.Ordinal))
            {
                return null;
            }

            if (line.StartsWith("=", StringComparison.Ordinal))
            {
                Match bizarreLeafMatcher = MatchWholeString(bizarreLeafPattern, line);
                if (bizarreLeafMatcher.Success)
                {
                    int level = bizarreLeafMatcher.Groups[1].Value.Length + 1;
                    string syntacticTag = bizarreLeafMatcher.Groups[2].Value;
                    // NOpenNLP: group 3 is optional and upstream explicitly tests it for null below,
                    // so Success -- not an empty string -- is what distinguishes "did not match".
                    string? lemma = bizarreLeafMatcher.Groups[3].Success ? bizarreLeafMatcher.Groups[3].Value : null;
                    string? morphologicalTag =
                        bizarreLeafMatcher.Groups[4].Success ? bizarreLeafMatcher.Groups[4].Value : null;
                    string lexeme = bizarreLeafMatcher.Groups[5].Value;
                    Leaf leaf = new Leaf();
                    leaf.Level = level;
                    leaf.SyntacticTag = syntacticTag;
                    leaf.MorphologicalTag = morphologicalTag;
                    leaf.Lexeme = lexeme;
                    if (lemma != null)
                    {
                        if (lemma.Length > 2)
                        {
                            lemma = lemma.Substring(1, lemma.Length - 2);
                        }
                        leaf.Lemma = lemma;
                    }

                    return leaf;
                }
                else
                {
                    int level = line.LastIndexOf("=", StringComparison.Ordinal) + 1;
                    string lexeme = line.Substring(level + 1);

                    if (bizarreLexemePattern.IsMatch(lexeme))
                    {
                        return null;
                    }

                    Leaf leaf = new Leaf();
                    leaf.Level = level + 1;
                    leaf.SyntacticTag = "";
                    leaf.MorphologicalTag = "";
                    leaf.Lexeme = lexeme;

                    return leaf;
                }
            }

            Console.Error.WriteLine("Couldn't parse leaf: " + line);
            Leaf newLeaf = new Leaf();
            newLeaf.Level = 1;
            newLeaf.SyntacticTag = "";
            newLeaf.MorphologicalTag = "";
            newLeaf.Lexeme = line;

            return newLeaf;
        }

        // NOpenNLP-specific: Java's Matcher.matches() requires the entire input to match, while
        // .NET's Regex.Match finds a match anywhere. Anchoring the match to the full input length
        // reproduces matches() without having to rewrite each pattern.
        private static Match MatchWholeString(Regex regex, string input)
        {
            Match match = regex.Match(input);
            if (match.Success && match.Index == 0 && match.Length == input.Length)
            {
                return match;
            }
            return Match.Empty;
        }

        /// <summary>
        /// Represents a tree element, Node or Leaf.
        /// </summary>
        public abstract class TreeElement
        {
            public virtual bool IsLeaf => false;

            public string? SyntacticTag { get; set; }

            public int Level { get; set; }

            public string? MorphologicalTag { get; set; }
        }

        /// <summary>
        /// Represents the AD node.
        /// </summary>
        public class Node : TreeElement
        {
            private readonly JCG.List<TreeElement> elems = new JCG.List<TreeElement>(); // NOpenNLP: made readonly

            public void AddElement(TreeElement element) => elems.Add(element);

            public TreeElement[] Elements => elems.ToArray();

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                // print itself and its children
                for (int i = 0; i < Level; i++)
                {
                    sb.Append('=');
                }
                sb.Append(SyntacticTag);
                if (MorphologicalTag != null)
                {
                    sb.Append(MorphologicalTag);
                }
                sb.Append('\n');
                foreach (TreeElement element in elems)
                {
                    sb.Append(element.ToString());
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Represents the AD leaf.
        /// </summary>
        public class Leaf : TreeElement
        {
            public override bool IsLeaf => true;

            public string? FunctionalTag { get; set; }

            public string? SecondaryTag { get; set; }

            /// <summary>
            /// The lexeme. Upstream stores this in a field named <c>word</c> behind
            /// <c>getLexeme()</c>/<c>setLexeme()</c>.
            /// </summary>
            public string? Lexeme { get; set; }

            public string? Lemma { get; set; }

            private static string EmptyOrString(string? value, string prefix, string suffix)
            {
                if (value == null) return "";
                return prefix + value + suffix;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                // print itself and its children
                for (int i = 0; i < Level; i++)
                {
                    sb.Append('=');
                }
                if (SyntacticTag != null)
                {
                    sb.Append(SyntacticTag).Append(':')
                        .Append(FunctionalTag).Append('(')
                        .Append(EmptyOrString(Lemma, "'", "' "))
                        .Append(EmptyOrString(SecondaryTag, "", " "))
                        .Append(MorphologicalTag).Append(") ");
                }
                sb.Append(Lexeme).Append('\n');
                return sb.ToString();
            }
        }
    }

    /// <summary>
    /// The start sentence pattern.
    /// </summary>
    private static readonly Regex sentStart = new Regex("^(?:<s[^>]*>)$");

    /// <summary>
    /// The end sentence pattern.
    /// </summary>
    private static readonly Regex sentEnd = new Regex("^(?:</s>)$");
    private static readonly Regex extEnd = new Regex("^(?:</ext>)$");

    /// <summary>
    /// The start sentence pattern.
    /// </summary>
    private static readonly Regex titleStart = new Regex("^(?:<t[^>]*>)$");

    /// <summary>
    /// The end sentence pattern.
    /// </summary>
    private static readonly Regex titleEnd = new Regex("^(?:</t>)$");

    /// <summary>
    /// The start sentence pattern.
    /// </summary>
    private static readonly Regex boxStart = new Regex("^(?:<caixa[^>]*>)$");

    /// <summary>
    /// The end sentence pattern.
    /// </summary>
    private static readonly Regex boxEnd = new Regex("^(?:</caixa>)$");

    /// <summary>
    /// The start sentence pattern.
    /// </summary>
    private static readonly Regex paraStart = new Regex("^(?:<p[^>]*>)$");

    /// <summary>
    /// The start sentence pattern.
    /// </summary>
    private static readonly Regex textStart = new Regex("^(?:<ext[^>]*>)$");

    private readonly SentenceParser parser; // NOpenNLP: made readonly

    private int paraID = 0;
    private bool isTitle = false;
    private bool isBox = false;

    public ADSentenceStream(IObjectStream<string?> lineStream)
        : base(lineStream)
    {
        parser = new SentenceParser();
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override Sentence? Read()
    {
        StringBuilder sentence = new StringBuilder();
        bool sentenceStarted = false;

        while (true)
        {
            string? line = samples.Read();

            if (line != null)
            {
                // NOpenNLP: every pattern below is used with Java's Matcher.matches(), which anchors
                // the whole input. The .NET patterns carry explicit ^(?:...)$ anchors for that
                // reason, so Regex.IsMatch here is equivalent.
                if (sentenceStarted)
                {
                    if (sentEnd.IsMatch(line) || extEnd.IsMatch(line))
                    {
                        sentenceStarted = false;
                    }
                    else if (!line.StartsWith("A1", StringComparison.Ordinal))
                    {
                        sentence.Append(line).Append('\n');
                    }
                }
                else
                {
                    if (sentStart.IsMatch(line))
                    {
                        sentenceStarted = true;
                    }
                    else if (paraStart.IsMatch(line))
                    {
                        paraID++;
                    }
                    else if (titleStart.IsMatch(line))
                    {
                        isTitle = true;
                    }
                    else if (titleEnd.IsMatch(line))
                    {
                        isTitle = false;
                    }
                    else if (textStart.IsMatch(line))
                    {
                        paraID = 0;
                    }
                    else if (boxStart.IsMatch(line))
                    {
                        isBox = true;
                    }
                    else if (boxEnd.IsMatch(line))
                    {
                        isBox = false;
                    }
                }

                if (!sentenceStarted && sentence.Length > 0)
                {
                    return parser.Parse(sentence.ToString(), paraID, isTitle, isBox);
                }
            }
            else
            {
                // handle end of file
                if (sentenceStarted)
                {
                    if (sentence.Length > 0)
                    {
                        return parser.Parse(sentence.ToString(), paraID, isTitle, isBox);
                    }
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
