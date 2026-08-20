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
using NOpenNLP.Tools.Namefind;
using NOpenNLP.Tools.Tokenize;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Formats.Muc;

public class MucNameContentHandler : SgmlParser.ContentHandler
{
    private const string EntityElementName = "ENAMEX";
    private const string TimeElementName = "TIMEX";
    private const string NumElementName = "NUMEX";

    // NOpenNLP: upstream builds both sets in a static initializer and wraps them in
    // Collections.unmodifiableSet; a read-only collection initializer says the same thing.
    private static readonly ISet<string> ExpectedTypes = new JCG.HashSet<string>
    {
        "PERSON",
        "ORGANIZATION",
        "LOCATION",
        "DATE",
        "TIME",
        "MONEY",
        "PERCENT"
    }.AsReadOnly();

    private static readonly ISet<string> NameElementNames = new JCG.HashSet<string>
    {
        EntityElementName,
        TimeElementName,
        NumElementName
    }.AsReadOnly();

    private readonly ITokenizer tokenizer;
    private readonly IList<NameSample> storedSamples;

    private bool isInsideContentElement = false;
    private readonly IList<string> text = new JCG.List<string>();
    private bool isClearAdaptiveData = false;

    // NOpenNLP: J2N has no Stack<T>; the BCL one matches java.util.Stack for the
    // push/pop operations used here. Upstream calls Stack.add(..), which java.util.Stack
    // inherits from Vector and which appends to the end -- the same position pop() reads
    // from -- so Push is the equivalent.
    private readonly Stack<Span> incompleteNames = new Stack<Span>();

    private readonly IList<Span> names = new JCG.List<Span>(); // NOpenNLP: made readonly

    public MucNameContentHandler(ITokenizer tokenizer, IList<NameSample> storedSamples)
    {
        this.tokenizer = tokenizer;
        this.storedSamples = storedSamples;
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidFormatException">if an unknown name type is encountered</exception>
    public override void StartElement(string name, IDictionary<string, string> attributes)
    {
        if (MucElementNames.DocElement.Equals(name))
        {
            isClearAdaptiveData = true;
        }

        if (MucElementNames.ContentElements.Contains(name))
        {
            isInsideContentElement = true;
        }

        if (NameElementNames.Contains(name))
        {
            // NOpenNLP: Map.get returns null for an absent key, which upstream then reports
            // in the message below -- Java renders that null as the text "null". TryGetValue
            // preserves both the absent case and how it prints.
            if (!attributes.TryGetValue("TYPE", out string? nameType))
            {
                nameType = null;
            }

            if (nameType is null || !ExpectedTypes.Contains(nameType))
            {
                throw new InvalidFormatException("Unknown timex, numex or namex type: "
                    + (nameType ?? "null") + ", expected one of " + FormatExpectedTypes());
            }

            incompleteNames.Push(new Span(text.Count, text.Count, StringUtil.ToLowerCase(nameType)));
        }
    }

    // NOpenNLP: Java renders a Set in a string concatenation as "[a, b, c]" via
    // AbstractCollection.toString(); .NET would print the type name instead.
    private static string FormatExpectedTypes() => "[" + string.Join(", ", ExpectedTypes) + "]";

    /// <inheritdoc/>
    public override void Characters(string chars)
    {
        if (isInsideContentElement)
        {
            string[] tokens = tokenizer.Tokenize(chars);
            foreach (string token in tokens)
            {
                text.Add(token);
            }
        }
    }

    /// <inheritdoc/>
    public override void EndElement(string name)
    {
        if (NameElementNames.Contains(name))
        {
            Span nameSpan = incompleteNames.Pop();
            nameSpan = new Span(nameSpan.Start, text.Count, nameSpan.Type);
            names.Add(nameSpan);
        }

        if (MucElementNames.ContentElements.Contains(name))
        {
            storedSamples.Add(new NameSample([.. text], [.. names], isClearAdaptiveData));

            if (isClearAdaptiveData)
            {
                isClearAdaptiveData = false;
            }

            text.Clear();
            names.Clear();
            isInsideContentElement = false;
        }
    }
}
