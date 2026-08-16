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
using System.Text;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Tokenize;

/// <summary>
/// A rule based detokenizer. Simple rules which indicate in which direction a token should be
/// moved are looked up in a <see cref="DetokenizationDictionary"/> object.
/// </summary>
/// <remarks>
/// <seealso cref="IDetokenizer"/>
/// <seealso cref="DetokenizationDictionary"/>
/// </remarks>
public class DictionaryDetokenizer(DetokenizationDictionary dict) : IDetokenizer
{
    public virtual DetokenizationOperation[] Detokenize(string[] tokens)
    {
        DetokenizationOperation[] operations = new DetokenizationOperation[tokens.Length];

        JCG.HashSet<string> matchingTokens = [];

        for (int i = 0; i < tokens.Length; i++)
        {
            DetokenizationOperationType? dictOperation = dict.GetOperation(tokens[i]);

            if (dictOperation is null)
            {
                operations[i] = DetokenizationOperation.NoOperation;
            }
            else if (dictOperation == DetokenizationOperationType.MoveLeft)
            {
                operations[i] = DetokenizationOperation.MergeToLeft;
            }
            else if (dictOperation == DetokenizationOperationType.MoveRight)
            {
                operations[i] = DetokenizationOperation.MergeToRight;
            }
            else if (dictOperation == DetokenizationOperationType.MoveBoth)
            {
                operations[i] = DetokenizationOperation.MergeBoth;
            }
            else if (dictOperation == DetokenizationOperationType.RightLeftMatching)
            {
                if (matchingTokens.Contains(tokens[i]))
                {
                    // The token already occurred once, move it to the left
                    // and clear the occurrence flag
                    operations[i] = DetokenizationOperation.MergeToLeft;
                    matchingTokens.Remove(tokens[i]);
                }
                else
                {
                    // First time this token is seen, move it to the right
                    // and remember it
                    operations[i] = DetokenizationOperation.MergeToRight;
                    matchingTokens.Add(tokens[i]);
                }
            }
            else
            {
                throw new InvalidOperationException("Unknown operation: " + dictOperation);
            }
        }

        return operations;
    }

    public virtual string Detokenize(string[] tokens, string? splitMarker)
    {
        DetokenizationOperation[] operations = Detokenize(tokens);

        if (tokens.Length != operations.Length)
        {
            throw new ArgumentException("tokens and operations array must have same length: tokens=" +
                tokens.Length + ", operations=" + operations.Length + "!");
        }

        StringBuilder untokenizedString = new StringBuilder();

        for (int i = 0; i < tokens.Length; i++)
        {
            // attach token to string buffer
            untokenizedString.Append(tokens[i]);

            bool isAppendSpace;
            bool isAppendSplitMarker;

            // if this token is the last token do not attach a space
            if (i + 1 == operations.Length)
            {
                isAppendSpace = false;
                isAppendSplitMarker = false;
            }
            // if next token move left, no space after this token,
            // its safe to access next token
            else if (operations[i + 1] == DetokenizationOperation.MergeToLeft
                || operations[i + 1] == DetokenizationOperation.MergeBoth)
            {
                isAppendSpace = false;
                isAppendSplitMarker = true;
            }
            // if this token is move right, no space
            else if (operations[i] == DetokenizationOperation.MergeToRight
                || operations[i] == DetokenizationOperation.MergeBoth)
            {
                isAppendSpace = false;
                isAppendSplitMarker = true;
            }
            else
            {
                isAppendSpace = true;
                isAppendSplitMarker = false;
            }

            if (isAppendSpace)
            {
                untokenizedString.Append(' ');
            }

            if (isAppendSplitMarker && splitMarker != null)
            {
                untokenizedString.Append(splitMarker);
            }
        }

        return untokenizedString.ToString();
    }
}
