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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Ad;

/// <summary>
/// Utility class to handle Portuguese contractions.
/// <para/>
/// Some Corpora splits contractions in its parts, for example, "da" &gt; "de" +
/// "a", but according to the fase of language processing, NER for instance, we
/// can't decide if to split a contraction or not, specially because contractions
/// inside names are not separated, but outside are.
/// <para/>
/// <b>Note:</b> Do not use this class, internal use only!
/// </summary>
public class PortugueseContractionUtility
{
    protected static readonly IDictionary<string, string> Contractions;

    static PortugueseContractionUtility()
    {
        var elems = new JCG.Dictionary<string, string>
        {
            // 103 CONTRACTIONS.
            ["a+a"] = "à",
            ["a+as"] = "às",
            ["a+aquele"] = "àquele",
            ["a+aqueles"] = "àqueles",
            ["a+aquela"] = "àquela",
            ["a+aquelas"] = "àquelas",
            ["a+aquilo"] = "àquilo",
            ["a+o"] = "ao",
            ["a+os"] = "aos",
            ["com+mim"] = "comigo",
            ["com+nòs"] = "conosco",
            ["com+si"] = "consigo",
            ["com+ti"] = "contigo",
            ["com+vòs"] = "convosco",
            ["de+aí"] = "daí",
            ["de+alguém"] = "dalguém",
            ["de+algum"] = "dalgum",
            ["de+alguma"] = "dalguma",
            ["de+alguns"] = "dalguns",
            ["de+algumas"] = "dalgumas",
            ["de+ali"] = "dali",
            ["de+aquém"] = "daquém",
            ["de+aquele"] = "daquele",
            ["de+aquela"] = "daquela",
            ["de+aqueles"] = "daqueles",
            ["de+aquelas"] = "daquelas",
            ["de+aqui"] = "daqui",
            ["de+aquilo"] = "daquilo",
            ["de+ele"] = "dele",
            ["de+ela"] = "dela",
            ["de+eles"] = "deles",
            ["de+elas"] = "delas",
            ["de+entre"] = "dentre",
            ["de+esse"] = "desse",
            ["de+essa"] = "dessa",
            ["de+esses"] = "desses",
            ["de+essas"] = "dessas",
            ["de+este"] = "deste",
            ["de+esta"] = "desta",
            ["de+estes"] = "destes",
            ["de+estas"] = "destas",
            ["de+isso"] = "disso",
            ["de+isto"] = "disto",
            ["de+o"] = "do",
            ["de+a"] = "da",
            ["de+os"] = "dos",
            ["de+as"] = "das",
            ["de+outrem"] = "doutrem",
            ["de+outro"] = "doutro",
            ["de+outra"] = "doutra",
            ["de+outros"] = "doutros",
            ["de+outras"] = "doutras",
            ["de+um"] = "dum",
            ["de+uma"] = "duma",
            ["de+uns"] = "duns",
            ["de+umas"] = "dumas",
            ["esse+outro"] = "essoutro",
            ["essa+outra"] = "essoutra",
            ["este+outro"] = "estoutro",
            ["este+outra"] = "estoutra",
            ["ele+o"] = "lho",
            ["ele+a"] = "lha",
            ["ele+os"] = "lhos",
            ["ele+as"] = "lhas",
            ["em+algum"] = "nalgum",
            ["em+alguma"] = "nalguma",
            ["em+alguns"] = "nalguns",
            ["em+algumas"] = "nalgumas",
            ["em+aquele"] = "naquele",
            ["em+aquela"] = "naquela",
            ["em+aqueles"] = "naqueles",
            ["em+aquelas"] = "naquelas",
            ["em+aquilo"] = "naquilo",
            ["em+ele"] = "nele",
            ["em+ela"] = "nela",
            ["em+eles"] = "neles",
            ["em+elas"] = "nelas",
            ["em+esse"] = "nesse",
            ["em+essa"] = "nessa",
            ["em+esses"] = "nesses",
            ["em+essas"] = "nessas",
            ["em+este"] = "neste",
            ["em+esta"] = "nesta",
            ["em+estes"] = "nestes",
            ["em+estas"] = "nestas",
            ["em+isso"] = "nisso",
            ["em+isto"] = "nisto",
            ["em+o"] = "no",
            ["em+a"] = "na",
            ["em+os"] = "nos",
            ["em+as"] = "nas",
            ["em+outro"] = "noutro",
            ["em+outra"] = "noutra",
            ["em+outros"] = "noutros",
            ["em+outras"] = "noutras",
            ["em+um"] = "num",
            ["em+uma"] = "numa",
            ["em+uns"] = "nuns",
            ["em+umas"] = "numas",
            ["por+o"] = "pelo",
            ["por+a"] = "pela",
            ["por+os"] = "pelos",
            ["por+as"] = "pelas",
            ["para+a"] = "pra",
            ["para+o"] = "pro",
            ["para+as"] = "pras",
            ["para+os"] = "pros"
        };
        Contractions = new ReadOnlyDictionary<string, string>(elems);
    }

    /// <summary>
    /// Merges a contraction.
    /// </summary>
    /// <param name="left">the left component</param>
    /// <param name="right">the right component</param>
    /// <returns>the merged contraction, or <c>null</c> if the parts do not form one</returns>
    public static string? ToContraction(string left, string right)
    {
        string key = left + "+" + right;
        // NOpenNLP: upstream pairs containsKey with get; TryGetValue does both in one lookup and
        // avoids the KeyNotFoundException the C# indexer would throw on a miss.
        if (Contractions.TryGetValue(key, out string? contraction))
        {
            return contraction;
        }
        else
        {
            var sb = new StringBuilder();
            // NOpenNLP: Java's String.split(regex) drops trailing empty strings, .NET's
            // string.Split does not. The difference matters because parts[parts.Length - 1] is
            // read below, so a trailing "_" would yield "" here but the last real token in Java.
            string[] parts = SplitDroppingTrailingEmpty(left, '_');
            for (int i = 0; i < parts.Length - 1; i++)
            {
                sb.Append(parts[i]).Append(' ');
            }
            key = parts[^1] + "+" + right;
            if (Contractions.TryGetValue(key, out contraction))
            {
                sb.Append(contraction);
                return sb.ToString();
            }

            if (right.Contains("_"))
            {
                parts = SplitDroppingTrailingEmpty(right, '_');

                key = left + "+" + parts[0];
                if (Contractions.TryGetValue(key, out contraction))
                {
                    sb.Append(contraction).Append(' ');

                    for (int i = 1; i < parts.Length; i++)
                    {
                        sb.Append(parts[i]).Append(' ');
                    }

                    return sb.ToString();
                }
            }

            string leftLower = StringUtil.ToLowerCase(parts[^1]);
            key = leftLower + "+" + right;
            if (Contractions.TryGetValue(key, out contraction))
            {
                string r = contraction;
                string firstChar = r[..1];
                r = StringUtil.ToUpperCase(firstChar) + r[1..];
                sb.Append(r);
                return sb.ToString();
            }
        }

        return null;
    }

    // NOpenNLP-specific: reproduces Java's String.split(regex) trailing-empty-string behavior,
    // which .NET's string.Split does not share. Java removes trailing empty strings but keeps
    // interior ones, and returns a single-element array containing the input when there is no
    // separator. An input that is entirely separators yields an empty array in Java.
    private static string[] SplitDroppingTrailingEmpty(string value, char separator)
    {
        string[] parts = value.Split(separator);

        int length = parts.Length;
        while (length > 0 && parts[length - 1].Length == 0)
        {
            length--;
        }

        if (length == parts.Length)
        {
            return parts;
        }

        string[] trimmed = new string[length];
        System.Array.Copy(parts, trimmed, length);
        return trimmed;
    }
}
