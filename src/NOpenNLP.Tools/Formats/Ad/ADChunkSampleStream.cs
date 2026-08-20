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
using NOpenNLP.Tools.Chunker;
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Ad;

/// <summary>
/// Parser for Floresta Sita(c)tica Arvores Deitadas corpus, output to for the
/// Portuguese Chunker training.
/// <para/>
/// The heuristic to extract chunks where based o paper 'A Machine Learning
/// Approach to Portuguese Clause Identification', (Eraldo Fernandes, Cicero
/// Santos and Ruy Milidiú).<br/>
/// <para/>
/// Data can be found on this web site:<br/>
/// http://www.linguateca.pt/floresta/corpus.html
/// <para/>
/// Information about the format:<br/>
/// Susana Afonso.
/// "Árvores deitadas: Descrição do formato e das opções de análise na Floresta Sintáctica"
/// .<br/>
/// 12 de Fevereiro de 2006.
/// http://www.linguateca.pt/documentos/Afonso2006ArvoresDeitadas.pdf
/// <para/>
/// Detailed info about the NER tagset:
/// http://beta.visl.sdu.dk/visl/pt/info/portsymbol.html#semtags_names
/// <para/>
/// <b>Note:</b> Do not use this class, internal use only!
/// </summary>
public class ADChunkSampleStream : ObjectStreamBase<ChunkSample?>
{
    protected readonly IObjectStream<ADSentenceStream.Sentence?> adSentenceStream;

    private int start = -1;
    private int end = -1;

    private int index = 0;

    public const string Other = "O";

    /// <summary>
    /// Creates a new <see cref="NameSample"/> stream from a line stream, i.e.
    /// <see cref="IObjectStream{T}"/> of <see cref="string"/>, that could be a
    /// <see cref="PlainTextByLineStream"/> object.
    /// </summary>
    /// <param name="lineStream">a stream of lines as <see cref="string"/></param>
    public ADChunkSampleStream(IObjectStream<string?> lineStream)
    {
        adSentenceStream = new ADSentenceStream(lineStream);
    }

    /// <exception cref="IOException">if there is an error during reading</exception>
    // NOpenNLP: upstream wraps this in a catch for UnsupportedEncodingException that its own
    // comment notes can never happen; the wrapper is dropped here.
    public ADChunkSampleStream(IInputStreamFactory @in, string charsetName)
    {
        adSentenceStream = new ADSentenceStream(new PlainTextByLineStream(@in, charsetName));
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override ChunkSample? Read()
    {
        ADSentenceStream.Sentence? paragraph;
        while ((paragraph = adSentenceStream.Read()) != null)
        {
            if (end > -1 && index >= end)
            {
                // leave
                return null;
            }

            if (start > -1 && index < start)
            {
                index++;
                // skip this one
            }
            else
            {
                ADSentenceStream.SentenceParser.Node? root = paragraph.Root;
                IList<string> sentence = new JCG.List<string>();
                IList<string> tags = new JCG.List<string>();
                IList<string> target = new JCG.List<string>();

                ProcessRoot(root, sentence, tags, target);

                if (sentence.Count > 0)
                {
                    index++;
                    return new ChunkSample(sentence, tags, target);
                }
            }
        }
        return null;
    }

    protected virtual void ProcessRoot(ADSentenceStream.SentenceParser.Node? root, IList<string> sentence,
        IList<string> tags, IList<string> target)
    {
        if (root != null)
        {
            ADSentenceStream.SentenceParser.TreeElement[] elements = root.Elements;
            foreach (ADSentenceStream.SentenceParser.TreeElement element in elements)
            {
                if (element.IsLeaf)
                {
                    ProcessLeaf((ADSentenceStream.SentenceParser.Leaf)element, false, Other, sentence, tags, target);
                }
                else
                {
                    ProcessNode((ADSentenceStream.SentenceParser.Node)element, sentence, tags, target, null);
                }
            }
        }
    }

    private void ProcessNode(ADSentenceStream.SentenceParser.Node node, IList<string> sentence,
        IList<string> tags, IList<string> target, string? inheritedTag)
    {
        string phraseTag = GetChunkTag(node);

        bool inherited = false;
        if (phraseTag.Equals(Other, StringComparison.Ordinal) && inheritedTag != null)
        {
            phraseTag = inheritedTag;
            inherited = true;
        }

        ADSentenceStream.SentenceParser.TreeElement[] elements = node.Elements;
        for (int i = 0; i < elements.Length; i++)
        {
            if (elements[i].IsLeaf)
            {
                bool isIntermediate = false;
                string tag = phraseTag;
                ADSentenceStream.SentenceParser.Leaf leaf = (ADSentenceStream.SentenceParser.Leaf)elements[i];

                string? localChunk = GetChunkTag(leaf);
                if (localChunk != null && !tag.Equals(localChunk, StringComparison.Ordinal))
                {
                    tag = localChunk;
                }

                if (IsIntermediate(tags, target, tag) && (inherited || i > 0))
                {
                    isIntermediate = true;
                }
                if (!IsIncludePunctuations && leaf.FunctionalTag == null &&
                    (
                        !(i + 1 < elements.Length && elements[i + 1].IsLeaf) ||
                        !(i > 0 && elements[i - 1].IsLeaf)
                    )
                )
                {
                    isIntermediate = false;
                    tag = Other;
                }
                ProcessLeaf(leaf, isIntermediate, tag, sentence, tags, target);
            }
            else
            {
                int before = target.Count;
                ProcessNode((ADSentenceStream.SentenceParser.Node)elements[i], sentence, tags, target, phraseTag);

                // if the child node was of a different type we should break the chunk sequence
                for (int j = target.Count - 1; j >= before; j--)
                {
                    if (!target[j].EndsWith("-" + phraseTag, StringComparison.Ordinal))
                    {
                        phraseTag = Other;
                        break;
                    }
                }
            }
        }
    }

    protected virtual void ProcessLeaf(ADSentenceStream.SentenceParser.Leaf leaf, bool isIntermediate, string phraseTag,
        IList<string> sentence, IList<string> tags, IList<string> target)
    {
        string chunkTag;

        if (leaf.FunctionalTag != null && phraseTag.Equals(Other, StringComparison.Ordinal))
        {
            phraseTag = GetPhraseTagFromPosTag(leaf.FunctionalTag);
        }

        if (!phraseTag.Equals(Other, StringComparison.Ordinal))
        {
            if (isIntermediate)
            {
                chunkTag = "I-" + phraseTag;
            }
            else
            {
                chunkTag = "B-" + phraseTag;
            }
        }
        else
        {
            chunkTag = phraseTag;
        }

        sentence.Add(leaf.Lexeme!);
        if (leaf.SyntacticTag == null)
        {
            tags.Add(leaf.Lexeme!);
        }
        else
        {
            // NOpenNLP: ConvertFuncTag returns its argument unchanged when useCGTags is false, so
            // this can be null when the leaf has no functional tag. Upstream adds that null to the
            // tag list; ChunkSample would reject it, but this path is only reached when the
            // syntactic tag is non-null, in which case the AD format always supplies a functional
            // tag as well.
            tags.Add(ConvertFuncTag(leaf.FunctionalTag, false)!);
        }
        target.Add(chunkTag);
    }

    protected virtual string GetPhraseTagFromPosTag(string functionalTag)
    {
        if (functionalTag.Equals("v-fin", StringComparison.Ordinal))
        {
            return "VP";
        }
        else if (functionalTag.Equals("n", StringComparison.Ordinal))
        {
            return "NP";
        }
        return Other;
    }

    public static string? ConvertFuncTag(string? t, bool useCGTags)
    {
        if (useCGTags)
        {
            if ("art".Equals(t, StringComparison.Ordinal) || "pron-det".Equals(t, StringComparison.Ordinal)
                || "pron-indef".Equals(t, StringComparison.Ordinal))
            {
                t = "det";
            }
        }
        return t;
    }

    protected virtual string? GetChunkTag(ADSentenceStream.SentenceParser.Leaf leaf)
    {
        string? tag = leaf.SyntacticTag;
        if ("P".Equals(tag, StringComparison.Ordinal))
        {
            return "VP";
        }
        return null;
    }

    protected virtual string GetChunkTag(ADSentenceStream.SentenceParser.Node node)
    {
        string tag = node.SyntacticTag!;

        string phraseTag = tag.Substring(tag.LastIndexOf(":", StringComparison.Ordinal) + 1);

        while (phraseTag.EndsWith("-", StringComparison.Ordinal))
        {
            phraseTag = phraseTag.Substring(0, phraseTag.Length - 1);
        }

        // maybe we should use only np, vp and pp, but will keep ap and advp.
        if (phraseTag.Equals("np", StringComparison.Ordinal) || phraseTag.Equals("vp", StringComparison.Ordinal)
            || phraseTag.Equals("pp", StringComparison.Ordinal) || phraseTag.Equals("ap", StringComparison.Ordinal)
            || phraseTag.Equals("advp", StringComparison.Ordinal)
            || phraseTag.Equals("adjp", StringComparison.Ordinal))
        {
            phraseTag = StringUtil.ToUpperCase(phraseTag);
        }
        else
        {
            phraseTag = Other;
        }
        return phraseTag;
    }

    public int Start
    {
        set => start = value;
    }

    public int End
    {
        set => end = value;
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during resetting the stream</exception>
    /// <exception cref="NotSupportedException">if the underlying stream does not support resetting</exception>
    public override void Reset() => adSentenceStream.Reset();

    protected override void Dispose(bool disposing) => adSentenceStream.Dispose();

    protected virtual bool IsIncludePunctuations => false;

    protected virtual bool IsIntermediate(IList<string> tags, IList<string> target, string phraseTag) =>
        target.Count > 0 && target[target.Count - 1].EndsWith("-" + phraseTag, StringComparison.Ordinal);
}
