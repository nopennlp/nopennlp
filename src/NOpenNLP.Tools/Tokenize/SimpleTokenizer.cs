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
using NOpenNLP.Tools.Util;
using System.Collections.Generic;

namespace NOpenNLP.Tools.Tokenize;

/// <summary>
/// Performs tokenization using character classes.
/// </summary>
public class SimpleTokenizer : AbstractTokenizer
{
    private class CharacterEnum
    {
        internal static readonly CharacterEnum WHITESPACE = new("whitespace");
        internal static readonly CharacterEnum ALPHABETIC = new("alphabetic");
        internal static readonly CharacterEnum NUMERIC = new("numeric");
        internal static readonly CharacterEnum OTHER = new("other");

        private readonly string name; // NOpenNLP: made readonly

        private CharacterEnum(string name)
        {
            this.name = name;
        }

        public override string ToString()
        {
            return name;
        }
    }

    public static readonly SimpleTokenizer INSTANCE;

    static SimpleTokenizer()
    {
        INSTANCE = new SimpleTokenizer();
    }

    /// <summary>
    /// </summary>
    /// <remarks>
    /// Deprecated: Use INSTANCE field instead to obtain an instance, constructor
    ///     will be made private in the future.
    /// </remarks>
    public SimpleTokenizer()
    {
    }

    public override Span[] TokenizePos(string s)
    {
        CharacterEnum charType = CharacterEnum.WHITESPACE;
        CharacterEnum state = charType;
        IList<Span> tokens = new List<Span>();
        int sl = s.Length;
        int start = -1;
        char pc = (char)0;
        for (int ci = 0; ci < sl; ci++)
        {
            char c = s[ci];
            if (StringUtil.IsWhitespace(c))
            {
                charType = CharacterEnum.WHITESPACE;
            }
            else if (char.IsLetter(c))
            {
                charType = CharacterEnum.ALPHABETIC;
            }
            else if (char.IsDigit(c))
            {
                charType = CharacterEnum.NUMERIC;
            }
            else
            {
                charType = CharacterEnum.OTHER;
            }

            if (state == CharacterEnum.WHITESPACE)
            {
                if (charType != CharacterEnum.WHITESPACE)
                {
                    start = ci;
                }
            }
            else
            {
                if (charType != state || charType == CharacterEnum.OTHER && c != pc)
                {
                    tokens.Add(new Span(start, ci));
                    start = ci;
                }
            }

            state = charType;
            pc = c;
        }

        if (charType != CharacterEnum.WHITESPACE)
        {
            tokens.Add(new Span(start, sl));
        }

        return [.. tokens];
    }
}
