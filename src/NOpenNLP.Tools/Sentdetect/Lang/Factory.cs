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

using NOpenNLP.Tools.Sentdetect.Lang.Th;
using System.Collections.Generic;

namespace NOpenNLP.Tools.Sentdetect.Lang;

public class Factory
{
    public static readonly char[] ptEosCharacters =
    [
        '.',
        '?',
        '!',
        ';',
        ':',
        '(',
        ')',
        '«',
        '»',
        '\'',
        '"'
    ];

    public static readonly char[] defaultEosCharacters =
    [
        '.',
        '!',
        '?'
    ];

    public static readonly char[] thEosCharacters =
    [
        ' ',
        '\n'
    ];

    public static readonly char[] jpnEosCharacters =
    [
        '。',
        '！',
        '？'
    ];

    public virtual IEndOfSentenceScanner CreateEndOfSentenceScanner(string? languageCode)
        => new DefaultEndOfSentenceScanner(GetEOSCharacters(languageCode));

    public virtual IEndOfSentenceScanner CreateEndOfSentenceScanner(char[] customEOSCharacters)
        => new DefaultEndOfSentenceScanner(customEOSCharacters);

    public virtual ISDContextGenerator CreateSentenceContextGenerator(string? languageCode, ISet<string> abbreviations)
    {
        if ("th".Equals(languageCode) || "tha".Equals(languageCode))
        {
            return new SentenceContextGenerator();
        }
        else if ("pt".Equals(languageCode) || "por".Equals(languageCode))
        {
            return new DefaultSDContextGenerator(abbreviations, ptEosCharacters);
        }

        return new DefaultSDContextGenerator(abbreviations, defaultEosCharacters);
    }

    public virtual ISDContextGenerator CreateSentenceContextGenerator(ISet<string> abbreviations, char[] customEOSCharacters)
        => new DefaultSDContextGenerator(abbreviations, customEOSCharacters);

    public virtual ISDContextGenerator CreateSentenceContextGenerator(string languageCode)
        => CreateSentenceContextGenerator(languageCode, new HashSet<string>());

    public virtual char[] GetEOSCharacters(string? languageCode)
    {
        if ("th".Equals(languageCode) || "tha".Equals(languageCode))
        {
            return thEosCharacters;
        }
        else if ("pt".Equals(languageCode) || "por".Equals(languageCode))
        {
            return ptEosCharacters;
        }
        else if ("jpn".Equals(languageCode))
        {
            return jpnEosCharacters;
        }

        return defaultEosCharacters;
    }
}
