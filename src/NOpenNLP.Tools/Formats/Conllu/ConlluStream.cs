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
using System.Globalization;
using System.IO;
using System.Text;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Conllu;

/// <summary>
/// The CoNNL-U Format is specified here:
/// http://universaldependencies.org/format.html
/// </summary>
public class ConlluStream : ObjectStreamBase<ConlluSentence?>
{
    private readonly IObjectStream<string?> sentenceStream;

    /// <exception cref="IOException">if there is an error during reading</exception>
    public ConlluStream(IInputStreamFactory @in)
    {
        sentenceStream = new ParagraphStream(new PlainTextByLineStream(@in, Encoding.UTF8));
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">if there is an error during reading</exception>
    public override ConlluSentence? Read()
    {
        string? sentence = sentenceStream.Read();

        if (sentence != null)
        {
            // NOpenNLP: interface-typed because PostProcessContractions returns IList<T>
            IList<ConlluWordLine> wordLines = new JCG.List<ConlluWordLine>();

            var reader = new StringReader(sentence);

            string? sentenceId = null;
            string? text = null;

            while (reader.ReadLine() is { } line)
            {
                // # indicates a comment line and contains additional data
                if (line.Trim().StartsWith("#", StringComparison.Ordinal))
                {
                    string commentLine = line.Trim()[1..];

                    int separator = commentLine.IndexOf('=');

                    if (separator != -1)
                    {
                        string firstPart = commentLine[..separator].Trim();
                        string secondPart = commentLine[(separator + 1)..].Trim();

                        if (secondPart.Length != 0)
                        {
                            switch (firstPart)
                            {
                                case "sent_id":
                                    sentenceId = secondPart;
                                    break;
                                case "text":
                                    text = secondPart;
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    wordLines.Add(new ConlluWordLine(line));
                }
            }

            wordLines = PostProcessContractions(wordLines);

            return new ConlluSentence(wordLines, sentenceId, text);
        }

        return null;
    }

    private static IList<ConlluWordLine> PostProcessContractions(IList<ConlluWordLine> lines)
    {
        // 1. Find contractions
        JCG.Dictionary<string, int> index = [];
        JCG.Dictionary<string, IList<string>> contractions = [];
        JCG.List<string> linesToDelete = [];

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            index[line.Id] = i;
            if (line.Id.Contains("-"))
            {
                JCG.List<string> expandedContractions = [];
                string[] ids = line.Id.Split('-');
                int start = int.Parse(ids[0], CultureInfo.InvariantCulture);
                int end = int.Parse(ids[1], CultureInfo.InvariantCulture);
                for (int j = start; j <= end; j++)
                {
                    string js = j.ToString(CultureInfo.InvariantCulture);
                    expandedContractions.Add(js);
                    linesToDelete.Add(js);
                }

                contractions[line.Id] = expandedContractions;
            }
        }

        // 2. Merge annotation
        foreach (var entry in contractions)
        {
            string contractionId = entry.Key;
            var expandedContractions = entry.Value;
            int contractionIndex = index[contractionId];
            var contraction = lines[contractionIndex];
            JCG.List<ConlluWordLine> expandedParts = [];
            foreach (string id in expandedContractions)
            {
                expandedParts.Add(lines[index[id]]);
            }

            var merged = MergeAnnotation(contraction, expandedParts);
            lines[contractionIndex] = merged;
        }

        // 3. Delete the expanded parts
        for (int i = linesToDelete.Count - 1; i >= 0; i--)
        {
            lines.RemoveAt(index[linesToDelete[i]]);
        }

        return lines;
    }

    /// <summary>
    /// Merges token level annotations.
    /// </summary>
    /// <param name="contraction">the line that receives the annotation</param>
    /// <param name="expandedParts">the lines to get annotation</param>
    /// <returns>the merged line</returns>
    private static ConlluWordLine MergeAnnotation(ConlluWordLine contraction,
        IList<ConlluWordLine> expandedParts)
    {
        string id = contraction.Id;
        string form = contraction.Form;
        string lemma = Join(expandedParts, p => p.Lemma);

        string uPosTag = Join(expandedParts, p => p.GetPosTag(ConlluTagset.U));

        string xPosTag = Join(expandedParts, p => p.GetPosTag(ConlluTagset.X));

        string feats = Join(expandedParts, p => p.Feats);

        string head = contraction.Head;
        string deprel = contraction.Deprel;
        string deps = contraction.Deps;
        string misc = contraction.Misc;

        return new ConlluWordLine(id, form, lemma, uPosTag, xPosTag, feats, head, deprel, deps, misc);
    }

    /// <summary>
    /// Selects a field from each expanded part, skipping the parts where it is unset
    /// (<c>"_"</c>), and joins what remains with <c>'+'</c>.
    /// </summary>
    // NOpenNLP: upstream expresses this as a stream filter/map/joining pipeline
    // repeated for each field; a shared helper avoids repeating it four times.
    private static string Join(IList<ConlluWordLine> expandedParts,
        Func<ConlluWordLine, string> selector)
    {
        var result = new StringBuilder();

        foreach (var part in expandedParts)
        {
            string value = selector(part);

            if ("_".Equals(value, StringComparison.Ordinal))
            {
                continue;
            }

            if (result.Length > 0)
            {
                result.Append('+');
            }

            result.Append(value);
        }

        return result.ToString();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing) => sentenceStream.Dispose();

    /// <inheritdoc/>
    public override void Reset() => sentenceStream.Reset();
}
