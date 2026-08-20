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
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Formats.Conllu;

public class ConlluWordLine
{
    private readonly string id;
    private readonly string form;
    private readonly string lemma;
    private readonly string uPosTag;
    private readonly string xPosTag;
    private readonly string feats;
    private readonly string head;
    private readonly string deprel;
    private readonly string deps;
    private readonly string misc;

    internal ConlluWordLine(string id, string form, string lemma, string uPosTag, string xPosTag,
        string feats, string head, string deprel, string deps, string misc)
    {
        this.id = id;
        this.form = form;
        this.lemma = lemma;
        this.uPosTag = uPosTag;
        this.xPosTag = xPosTag;
        this.feats = feats;
        this.head = head;
        this.deprel = deprel;
        this.deps = deps;
        this.misc = misc;
    }

    /// <exception cref="InvalidFormatException">if the line does not have exactly 10 fields</exception>
    internal ConlluWordLine(string line)
    {
        string[] fields = line.Split('\t');

        if (fields.Length != 10)
        {
            throw new InvalidFormatException("Line must have exactly 10 fields");
        }

        id = fields[0];
        form = fields[1];
        lemma = fields[2];
        uPosTag = fields[3];
        xPosTag = fields[4];
        feats = fields[5];
        head = fields[6];
        deprel = fields[7];
        deps = fields[8];
        misc = fields[9];
    }

    /// <summary>
    /// Gets the word index. An <see cref="int"/> starting at 1 for each new sentence;
    /// may be a range for multiword tokens; may be a decimal number for empty nodes.
    /// </summary>
    public string Id => id;

    /// <summary>
    /// Gets the word form or punctuation symbol.
    /// </summary>
    public string Form => form;

    /// <summary>
    /// Gets the lemma or stem of the word form.
    /// </summary>
    public string Lemma => lemma;

    /// <summary>
    /// Retrieve the Universal part-of-speech tag or the language-specific part-of-speech tag;
    /// underscore if not available.
    /// </summary>
    /// <param name="tagset">the type of tag to retrieve, either universal (U) or language specific (X)</param>
    public string GetPosTag(ConlluTagset tagset)
    {
        switch (tagset)
        {
            case ConlluTagset.U:
                return uPosTag;
            case ConlluTagset.X:
                return xPosTag;
            default:
                // NOpenNLP: upstream throws IllegalStateException; InvalidOperationException
                // is the .NET counterpart.
                throw new InvalidOperationException("Unexpected tagset value: " + tagset);
        }
    }

    /// <summary>
    /// Gets the list of morphological features from the universal feature inventory or from a
    /// defined language-specific extension; underscore if not available.
    /// </summary>
    public string Feats => feats;

    /// <summary>
    /// Gets the head of the current word, which is either a value of ID or zero (0).
    /// </summary>
    public string Head => head;

    /// <summary>
    /// Gets the universal dependency relation to the HEAD (root iff HEAD = 0) or a
    /// defined language-specific subtype of one.
    /// </summary>
    public string Deprel => deprel;

    /// <summary>
    /// Gets the enhanced dependency graph in the form of a list of head-deprel pairs.
    /// </summary>
    public string Deps => deps;

    /// <summary>
    /// Gets any other annotation.
    /// </summary>
    public string Misc => misc;
}
