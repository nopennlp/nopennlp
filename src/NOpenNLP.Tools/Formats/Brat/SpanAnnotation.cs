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
using NOpenNLP.Tools.Util;

namespace NOpenNLP.Tools.Formats.Brat;

public class SpanAnnotation : BratAnnotation
{
    private readonly Span[] spans;
    private readonly string coveredText;

    internal SpanAnnotation(string id, string type, Span[] spans, string coveredText)
        : base(id, type)
    {
        this.spans = new Span[spans.Length];
        Array.Copy(spans, this.spans, spans.Length);
        Array.Sort(this.spans);
        this.coveredText = coveredText;
    }

    public Span[] Spans => spans;

    public string CoveredText => coveredText;

    public override string ToString()
    {
        // NOpenNLP: stands in for Java's Arrays.toString(Object[]), which renders
        // the elements comma-separated inside square brackets.
        var spanText = new StringBuilder("[");

        for (int i = 0; i < spans.Length; i++)
        {
            if (i > 0)
            {
                spanText.Append(", ");
            }

            spanText.Append(spans[i]);
        }

        spanText.Append(']');

        return $"{base.ToString()} {spanText} {CoveredText}";
    }
}
