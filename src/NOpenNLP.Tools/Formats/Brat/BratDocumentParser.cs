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
using J2N.Text;
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Sentdetect;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Brat;

public class BratDocumentParser
{
    private readonly ISentenceDetector sentDetector; // NOpenNLP: made readonly
    private readonly ITokenizer tokenizer; // NOpenNLP: made readonly
    private readonly ISet<string>? nameTypes;

    public BratDocumentParser(ISentenceDetector sentenceDetector, ITokenizer tokenizer,
        ISet<string>? nameTypes = null)
    {
        if (nameTypes is { Count: 0 })
        {
            throw new ArgumentException("nameTypes should be null or have one or more elements", nameof(nameTypes));
        }

        this.sentDetector = sentenceDetector;
        this.tokenizer = tokenizer;
        this.nameTypes = nameTypes;
    }

    public IList<NameSample> Parse(BratDocument sample)
    {
        // Note: Some entities might not match sentence boundaries,
        // to be able to print warning a set of entities id must be maintained
        // to check if all entities have been used up after the matching is done

        JCG.HashSet<string> entityIdSet = [];
        JCG.Dictionary<int, Span> coveredIndexes = [];

        foreach (var ann in sample.Annotations)
        {
            if (IsSpanAnnotation(ann))
            {
                entityIdSet.Add(ann.Id);

                foreach (var span in ((SpanAnnotation)ann).Spans)
                {
                    for (int i = span.Start; i < span.End; i++)
                    {
                        coveredIndexes[i] = span;
                    }
                }
            }
        }

        // Map spans to tokens, and merge fragments based on token

        // Detect sentence and correct sentence spans assuming no split can be inside a name annotation
        JCG.List<Span> sentences = [];
        foreach (var sentence in sentDetector.SentPosDetect(sample.Text))
        {
            // NOpenNLP: upstream reads Map.get, which yields null when the sentence start is
            // not covered by a name; TryGetValue preserves that instead of throwing.
            coveredIndexes.TryGetValue(sentence.Start, out var conflictingName);

            if (sentences.Count > 0 && conflictingName != null &&
                conflictingName.Start < sentence.Start)
            {
                var lastSentence = sentences[^1];
                sentences.RemoveAt(sentences.Count - 1);
                sentences.Add(new Span(lastSentence.Start, sentence.End));

                Console.WriteLine($"Correcting sentence segmentation in document {sample.Id}");
            }
            else
            {
                sentences.Add(sentence);
            }
        }

        // TODO: Token breaks should be enforced on name span boundaries
        // a) Just split tokens
        // b) Implement a custom token split validator which can be injected into the Tokenizer

        // Currently we are missing all

        var samples = new JCG.List<NameSample>(sentences.Count);

        foreach (var sentence in sentences)
        {
            string sentenceText = sentence.GetCoveredText(
                sample.Text.AsCharSequence()).ToString();

            var tokens = tokenizer.TokenizePos(sentenceText);

            // Note:
            // A begin and end token index can be identical, but map to different
            // tokens, to distinguish between between the two begin indexes are
            // stored with a negative sign, and end indexes are stored with a positive sign
            // in the tokenIndexMap.
            // The tokenIndexMap maps to the sentence local token index.

            JCG.Dictionary<int, int> tokenIndexMap = [];

            for (int i = 0; i < tokens.Length; i++)
            {
                tokenIndexMap[-(sentence.Start + tokens[i].Start)] = i;
                tokenIndexMap[sentence.Start + tokens[i].End] = i + 1;
            }

            JCG.List<Span> names = [];

            foreach (var ann in sample.Annotations)
            {
                if (IsSpanAnnotation(ann))
                {
                    var entity = (SpanAnnotation)ann;

                    // NOpenNLP: upstream nulls out merged fragments in place, so the list must
                    // hold nulls until the surviving spans are collected below.
                    JCG.List<Span?> mappedFragments = [];

                    foreach (var span in entity.Spans)
                    {
                        var entitySpan = span;

                        if (sentence.Contains(entitySpan))
                        {
                            entityIdSet.Remove(ann.Id);

                            entitySpan = entitySpan.Trim(sample.Text.AsCharSequence());

                            // NOpenNLP: upstream reads Map.get into an Integer, which is null when
                            // the offset does not fall on a token boundary; TryGetValue preserves
                            // that so the "not matching tokenization" branch still runs.
                            bool hasBeginIndex =
                                tokenIndexMap.TryGetValue(-entitySpan.Start, out int nameBeginIndex);
                            bool hasEndIndex =
                                tokenIndexMap.TryGetValue(entitySpan.End, out int nameEndIndex);

                            if (hasBeginIndex && hasEndIndex)
                            {
                                mappedFragments.Add(new Span(nameBeginIndex, nameEndIndex, entity.Type));
                            }
                            else
                            {
                                Console.Error.WriteLine($"Dropped entity {entity.Id} ({entitySpan.GetCoveredText(sample.Text.AsCharSequence())})  in document {sample.Id}, it is not matching tokenization!");
                            }
                        }
                    }

                    // NOpenNLP: Collections.sort is stable and, at this point, the list holds no
                    // nulls -- they are only introduced by the merge loop below. JCG.List.Sort
                    // is likewise stable, and the comparison unwraps the nullable Span the list
                    // element type carries for that later merge.
                    mappedFragments.Sort((x, y) => x!.CompareTo(y!));

                    for (int i = 1; i < mappedFragments.Count; i++)
                    {
                        if (mappedFragments[i - 1]!.End ==
                            mappedFragments[i]!.Start)
                        {
                            mappedFragments[i] = new Span(mappedFragments[i - 1]!.Start,
                                mappedFragments[i]!.End, mappedFragments[i]!.Type);
                            mappedFragments[i - 1] = null;
                        }
                    }

                    foreach (var span in mappedFragments)
                    {
                        if (span != null)
                        {
                            names.Add(span);
                        }
                    }
                }
            }

            samples.Add(new NameSample(sample.Id, Span.SpansToStrings(tokens, sentenceText.AsCharSequence()),
                [.. names], null, samples.Count == 0));
        }

        foreach (string id in entityIdSet)
        {
            Console.Error.WriteLine($"Dropped entity {id} in document {sample.Id}, is not matching sentence segmentation!");
        }

        return samples;
    }

    private bool IsSpanAnnotation(BratAnnotation ann) =>
        ann is SpanAnnotation && (nameTypes == null || nameTypes.Contains(ann.Type));
}
