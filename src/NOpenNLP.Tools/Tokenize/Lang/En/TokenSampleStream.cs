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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Tokenize.Lang.En;

/// <summary>
/// Class which produces an <see cref="IEnumerable{T}"/> of <see cref="TokenSample"/> from a
/// file of space delimited token. This class uses a number of English-specific heuristics
/// to un-separate tokens which are typically found together in text.
/// </summary>
/// <remarks>
/// NOpenNLP: upstream implements <c>Iterator&lt;TokenSample&gt;</c>. Per the port's
/// conventions this exposes <see cref="IEnumerable{T}"/> instead, so callers can
/// <c>foreach</c> over it without driving an enumerator by hand. The reader is consumed
/// once, exactly as upstream's iterator is, so a second enumeration yields nothing.
/// </remarks>
public class TokenSampleStream : IEnumerable<TokenSample>
{
    private readonly TextReader @in; // NOpenNLP: made readonly
    private string? line;
    private readonly Regex alphaNumeric = new Regex("[A-Za-z0-9]"); // NOpenNLP: made readonly
    private bool evenq = true;

    /// <exception cref="IOException">if reading from the stream fails</exception>
    public TokenSampleStream(Stream @is)
    {
        this.@in = new StreamReader(@is);
        line = @in.ReadLine();
    }

    public bool HasNext => line != null;

    public TokenSample Next()
    {
        // NOpenNLP: Java's String.split(regex) is a regex split; Regex.Split is the
        // equivalent, but unlike Java it does not drop a leading empty field, which
        // upstream's "\\s+" pattern produces for a line beginning with whitespace.
        string[] tokens = Regex.Split(line!, "\\s+");
        if (tokens.Length == 0)
        {
            evenq = true;
        }

        StringBuilder sb = new StringBuilder(line!.Length);
        JCG.List<Span> spans = [];
        int length = 0;
        for (int ti = 0; ti < tokens.Length; ti++)
        {
            string token = tokens[ti];
            string lastToken = ti - 1 >= 0 ? tokens[ti - 1] : "";
            switch (token)
            {
                case "-LRB-":
                    token = "(";
                    break;
                case "-LCB-":
                    token = "{";
                    break;
                case "-RRB-":
                    token = ")";
                    break;
                case "-RCB-":
                    token = "}";
                    break;
            }

            if (sb.Length != 0)
            {
                if (!alphaNumeric.IsMatch(token) || token.StartsWith("'", StringComparison.Ordinal)
                    || "n't".Equals(token, StringComparison.OrdinalIgnoreCase))
                {
                    if ((token.Equals("``") || token.Equals("--") || token.Equals("$") ||
                        token.Equals("(") || token.Equals("&") || token.Equals("#") ||
                        (token.Equals("\"") && (evenq && ti != tokens.Length - 1)))
                        && (!lastToken.Equals("(") || !lastToken.Equals("{")))
                    {
                        //System.out.print(" "+token);
                        length++;
                    }
                }
                else
                {
                    if (!lastToken.Equals("``") && (!lastToken.Equals("\"") || evenq) && !lastToken.Equals("(")
                        && !lastToken.Equals("{") && !lastToken.Equals("$") && !lastToken.Equals("#"))
                    {
                        length++;
                    }
                }
            }

            if (token.Equals("\""))
            {
                evenq = ti == tokens.Length - 1 || !evenq;
            }

            if (sb.Length < length)
            {
                sb.Append(" ");
            }

            sb.Append(token);
            spans.Add(new Span(length, length + token.Length));
            length += token.Length;
        }

        try
        {
            line = @in.ReadLine();
        }
        catch (IOException e)
        {
            e.PrintStackTrace();
            line = null;
        }

        return new TokenSample(sb.ToString(), [.. spans]);
    }

    /// <inheritdoc/>
    public IEnumerator<TokenSample> GetEnumerator()
    {
        while (HasNext)
        {
            yield return Next();
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private static void Usage()
    {
        Console.Error.WriteLine("TokenSampleStream [-spans] < in");
        Console.Error.WriteLine("Where in is a space delimited list of tokens.");
    }
}
