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

// This file has been modified from the original Apache OpenNLP 1.9.1 source:
// translated from Java to C# and adapted for .NET. See NOTICE.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NOpenNLP.Tools.Lemmatizer;

/// <summary>
/// Simple feature generator for learning statistical lemmatizers.
/// Features based on Grzegorz Chrupała. 2008. Towards a Machine-Learning
/// Architecture for Lexical Functional Grammar Parsing. PhD dissertation,
/// Dublin City University
/// </summary>
/// <remarks>@version2016-02-15</remarks>
public class DefaultLemmatizerContextGenerator : ILemmatizerContextGenerator
{
    private const int PREFIX_LENGTH = 5;
    private const int SUFFIX_LENGTH = 7;

    // NOpenNLP: made readonly
    private static readonly Regex hasCap = new Regex("[A-Z]", RegexOptions.Compiled);
    private static readonly Regex hasNum = new Regex("[0-9]", RegexOptions.Compiled);

    /// <summary>
    /// NOpenNLP: Java renders a null reference as the literal "null" when it is
    /// concatenated into a string, whereas .NET renders it as the empty string.
    /// The feature names built below become model lookup keys, so they must match
    /// what the trained (Java-produced) models expect.
    /// </summary>
    private static string ToJavaString(string value) => value ?? "null";

    protected static string[] GetPrefixes(string lex)
    {
        string[] prefs = new string[PREFIX_LENGTH];
        for (int li = 1; li < PREFIX_LENGTH; li++)
        {
            prefs[li] = lex[..Math.Min(li + 1, lex.Length)];
        }

        return prefs;
    }

    protected static string[] GetSuffixes(string lex)
    {
        string[] suffs = new string[SUFFIX_LENGTH];
        for (int li = 1; li < SUFFIX_LENGTH; li++)
        {
            suffs[li] = lex[Math.Max(lex.Length - li - 1, 0)..];
        }

        return suffs;
    }

    public virtual string[] GetContext(int index, string[] sequence, string[] priorDecisions, object[] additionalContext)
    {
        return GetContext(index, sequence, (string[])additionalContext[0], priorDecisions);
    }

    public virtual string[] GetContext(int index, string[] toks, string[] tags, string[] preds)
    {
        // Word
        string w0;

        // Tag
        string t0;

        // Previous prediction
        string p_1;
        string lex = toks[index];
        if (index < 1)
        {
            p_1 = "p_1=bos";
        }
        else
        {
            p_1 = "p_1=" + ToJavaString(preds[index - 1]);
        }

        w0 = "w0=" + toks[index];
        t0 = "t0=" + tags[index];
        var features = new List<string>
        {
            w0,
            t0,
            p_1,
            p_1 + t0,
            p_1 + w0
        };

        // do some basic suffix analysis
        string[] suffs = GetSuffixes(lex);
        for (int i = 0; i < suffs.Length; i++)
        {
            features.Add("suf=" + ToJavaString(suffs[i]));
        }

        string[] prefs = GetPrefixes(lex);
        for (int i = 0; i < prefs.Length; i++)
        {
            features.Add("pre=" + ToJavaString(prefs[i]));
        }

        // see if the word has any special characters
        if (lex.IndexOf('-') != -1)
        {
            features.Add("h");
        }

        if (hasCap.IsMatch(lex))
        {
            features.Add("c");
        }

        if (hasNum.IsMatch(lex))
        {
            features.Add("d");
        }

        return [..features];
    }
}
