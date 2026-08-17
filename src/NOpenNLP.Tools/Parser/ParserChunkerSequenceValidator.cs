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
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Parser;

public class ParserChunkerSequenceValidator : ISequenceValidator<TokenTag>
{
    // NOpenNLP: made readonly
    // NOpenNLP: upstream reads CONT/START/OTHER through the chunking Parser subclass;
    // they are declared on AbstractBottomUpParser and are referenced there directly.
    private readonly JCG.Dictionary<string, string> continueStartMap;

    public ParserChunkerSequenceValidator(string[] outcomes)
    {
        continueStartMap = new JCG.Dictionary<string, string>(outcomes.Length);
        foreach (string outcome in outcomes)
        {
            if (outcome.StartsWith(AbstractBottomUpParser.CONT, StringComparison.Ordinal))
            {
                continueStartMap[outcome] = AbstractBottomUpParser.START + outcome[AbstractBottomUpParser.CONT.Length..];
            }
        }
    }

    public virtual bool ValidSequence(int i, string[] inputSequence, string[] tagList, string outcome)
    {
        // NOpenNLP: Java's Map.get() returns null for an absent key, whereas the .NET
        // indexer throws KeyNotFoundException, so the containsKey/get pair upstream
        // performs becomes a single TryGetValue here.
        if (continueStartMap.TryGetValue(outcome, out string? start))
        {
            int lti = tagList.Length - 1;

            if (lti == -1)
            {
                return false;
            }
            else
            {
                string lastTag = tagList[lti];

                if (lastTag.Equals(outcome))
                {
                    return true;
                }

                if (lastTag.Equals(start))
                {
                    return true;
                }

                if (lastTag.Equals(AbstractBottomUpParser.OTHER))
                {
                    return false;
                }

                return false;
            }
        }

        return true;
    }

    public virtual bool ValidSequence(int i, TokenTag[] inputTuples, string[] outcomesSequence, string outcome)
    {
        string[] inputSequence = TokenTag.ExtractTokens(inputTuples);
        return ValidSequence(i, inputSequence, outcomesSequence, outcome);
    }
}
