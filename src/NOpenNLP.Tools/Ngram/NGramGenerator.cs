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
using System.Text;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Ngram;

/// <summary>
/// Generates an nGram, with optional separator, and returns the grams as a list
/// of strings.
/// </summary>
public static class NGramGenerator
{
    /// <summary>
    /// Creates an ngram separated by the separator param value
    /// i.e. a,b,c,d with n = 3 and separator = "-" would return a-b-c,b-c-d
    /// </summary>
    /// <param name="input">the input tokens the output ngrams will be derived from</param>
    /// <param name="n">the number of tokens as the sliding window</param>
    /// <param name="separator">each string in each gram will be separated by this value if desired.
    ///     Pass in empty string if no separator is desired</param>
    /// <returns>the generated ngrams</returns>
    public static IList<string> Generate(IList<string> input, int n, string separator)
    {
        var outGrams = new JCG.List<string>();
        for (int i = 0; i < input.Count - (n - 1); i++)
        {
            var sb = new StringBuilder();

            for (int x = i; x < n + i; x++)
            {
                sb.Append(input[x]);
                sb.Append(separator);
            }

            string gram = sb.ToString();
            gram = gram.Substring(0, gram.LastIndexOf(separator, System.StringComparison.Ordinal));
            outGrams.Add(gram);
        }

        return outGrams;
    }

    /// <summary>
    /// Generates an nGram based on a char[] input.
    /// </summary>
    /// <param name="input">the array of chars to convert to nGram</param>
    /// <param name="n">The number of grams (chars) that each output gram will consist of</param>
    /// <param name="separator">each char in each gram will be separated by this value if desired.
    ///     Pass in empty string if no separator is desired</param>
    /// <returns>the generated ngrams</returns>
    public static IList<string> Generate(char[] input, int n, string separator)
    {
        var outGrams = new JCG.List<string>();
        for (int i = 0; i < input.Length - (n - 1); i++)
        {
            var sb = new StringBuilder();

            for (int x = i; x < n + i; x++)
            {
                sb.Append(input[x]);
                sb.Append(separator);
            }

            string gram = sb.ToString();
            gram = gram.Substring(0, gram.LastIndexOf(separator, System.StringComparison.Ordinal));
            outGrams.Add(gram);
        }

        return outGrams;
    }
}
