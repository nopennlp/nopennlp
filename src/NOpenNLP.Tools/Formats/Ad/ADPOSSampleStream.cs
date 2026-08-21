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
using System.Text.RegularExpressions;
using NOpenNLP.Tools.Postag;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Ad;

/// <summary>
/// <b>Note:</b> Do not use this class, internal use only!
/// </summary>
public class ADPOSSampleStream : ObjectStreamBase<POSSample?>
{
    // NOpenNLP: hoisted out of processLeaf(), where upstream calls String.replaceAll and so
    // recompiles the pattern for every leaf.
    private static readonly Regex whitespacePattern = new("\\s+");

    private readonly IObjectStream<ADSentenceStream.Sentence?> adSentenceStream;
    private readonly bool expandME; // NOpenNLP: made readonly
    private readonly bool isIncludeFeatures; // NOpenNLP: made readonly

    /// <summary>
    /// Creates a new <see cref="POSSample"/> stream from a line stream, i.e.
    /// <see cref="IObjectStream{T}"/> of <see cref="string"/>, that could be a
    /// <see cref="PlainTextByLineStream"/> object.
    /// </summary>
    /// <param name="lineStream">a stream of lines as <see cref="string"/></param>
    /// <param name="expandME">
    /// if true will expand the multiword expressions, each word of the expression will have the
    /// POS Tag that was attributed to the expression plus the prefix B- or I- (CONLL convention)
    /// </param>
    /// <param name="includeFeatures">if true will combine the POS Tag with the feature tags</param>
    public ADPOSSampleStream(IObjectStream<string?> lineStream, bool expandME, bool includeFeatures)
    {
        adSentenceStream = new ADSentenceStream(lineStream);
        this.expandME = expandME;
        isIncludeFeatures = includeFeatures;
    }

    /// <summary>
    /// Creates a new <see cref="POSSample"/> stream from a <see cref="Stream"/>.
    /// </summary>
    /// <param name="in">the Corpus <see cref="Stream"/></param>
    /// <param name="charsetName">the charset of the Arvores Deitadas Corpus</param>
    /// <param name="expandME">
    /// if true will expand the multiword expressions, each word of the expression will have the
    /// POS Tag that was attributed to the expression plus the prefix B- or I- (CONLL convention)
    /// </param>
    /// <param name="includeFeatures">if true will combine the POS Tag with the feature tags</param>
    /// <exception cref="IOException">if there is an error during reading</exception>
    // NOpenNLP: upstream wraps this in a catch for UnsupportedEncodingException that its own
    // comment notes can never happen; the wrapper is dropped here.
    public ADPOSSampleStream(IInputStreamFactory @in, string charsetName, bool expandME, bool includeFeatures)
    {
        adSentenceStream = new ADSentenceStream(new PlainTextByLineStream(@in, charsetName));
        this.expandME = expandME;
        isIncludeFeatures = includeFeatures;
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override POSSample? Read()
    {
        while (adSentenceStream.Read() is { } paragraph)
        {
            var root = paragraph.Root;
            JCG.List<string> sentence = [];
            JCG.List<string> tags = [];
            Process(root, sentence, tags);

            return new POSSample(sentence, tags);
        }
        return null;
    }

    private void Process(ADSentenceStream.SentenceParser.Node? node, IList<string> sentence, IList<string> tags)
    {
        if (node != null)
        {
            foreach (var element in node.Elements)
            {
                if (element.IsLeaf)
                {
                    ProcessLeaf((ADSentenceStream.SentenceParser.Leaf)element, sentence, tags);
                }
                else
                {
                    Process((ADSentenceStream.SentenceParser.Node)element, sentence, tags);
                }
            }
        }
    }

    private void ProcessLeaf(ADSentenceStream.SentenceParser.Leaf? leaf, IList<string> sentence, IList<string> tags)
    {
        if (leaf != null)
        {
            string lexeme = leaf.Lexeme!;
            string? tag = leaf.FunctionalTag;

            if (tag == null)
            {
                tag = leaf.Lexeme;
            }

            if (isIncludeFeatures && leaf.MorphologicalTag != null)
            {
                tag += " " + leaf.MorphologicalTag;
            }
            tag = whitespacePattern.Replace(tag!, "=");

            // NOpenNLP: upstream repeats the `tag == null` check here. It is dead code -- the
            // replaceAll above would already have thrown on a null tag -- but it is kept so the
            // structure still matches upstream.
            if (tag == null)
                tag = lexeme;

            if (expandME && lexeme.Contains("_"))
            {
                // NOpenNLP: upstream uses StringTokenizer, which skips empty tokens entirely. A
                // plain Split would instead yield empty entries for runs of underscores or a
                // leading/trailing one, so the empties are removed here to match.
                string[] tokens = lexeme.Split(['_'], StringSplitOptions.RemoveEmptyEntries);

                if (tokens.Length > 0)
                {
                    var toks = new JCG.List<string>(tokens.Length);
                    var tagsWithCont = new JCG.List<string>(tokens.Length);
                    toks.Add(tokens[0]);
                    tagsWithCont.Add("B-" + tag);
                    for (int i = 1; i < tokens.Length; i++)
                    {
                        toks.Add(tokens[i]);
                        tagsWithCont.Add("I-" + tag);
                    }

                    foreach (string tok in toks)
                    {
                        sentence.Add(tok);
                    }
                    foreach (string t in tagsWithCont)
                    {
                        tags.Add(t);
                    }
                }
                else
                {
                    sentence.Add(lexeme);
                    tags.Add(tag);
                }
            }
            else
            {
                sentence.Add(lexeme);
                tags.Add(tag);
            }
        }
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during resetting the stream</exception>
    /// <exception cref="NotSupportedException">if the underlying stream does not support resetting</exception>
    public override void Reset() => adSentenceStream.Reset();

    protected override void Dispose(bool disposing) => adSentenceStream.Dispose();
}
