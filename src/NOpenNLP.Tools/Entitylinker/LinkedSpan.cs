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
using System.Collections.Generic;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Entitylinker;

/// <summary>
/// A "default" extended span that holds additional information about the <see cref="Span"/>.
/// </summary>
/// <typeparam name="T">The type of the linked entries.</typeparam>
public class LinkedSpan<T> : Span
    where T : BaseLink
{
    public LinkedSpan(IList<T> linkedEntries, int s, int e, string? type)
        : base(s, e, type)
    {
        LinkedEntries = linkedEntries;
    }

    public LinkedSpan(IList<T> linkedEntries, int s, int e, string? type, double prob)
        : base(s, e, type, prob)
    {
        LinkedEntries = linkedEntries;
    }

    public LinkedSpan(IList<T> linkedEntries, int s, int e)
        : base(s, e)
    {
        LinkedEntries = linkedEntries;
    }

    public LinkedSpan(IList<T> linkedEntries, Span span, int offset)
        : base(span, offset)
    {
        LinkedEntries = linkedEntries;
    }

    /// <summary>
    /// Gets or sets the n best linked entries from an external data source. For
    /// instance, this will hold gazateer entries for a search into a geonames gazateer.
    /// </summary>
    public IList<T> LinkedEntries { get; set; }

    /// <summary>
    /// Gets or sets the id or index of the sentence from which this span was extracted.
    /// </summary>
    public int SentenceId { get; set; }

    /// <summary>
    /// Gets or sets the search term that was used to link this span to an external data source.
    /// </summary>
    public string? SearchTerm { get; set; }

    // NOpenNLP: Java renders the ArrayList as "[a, b]". Any IList<T> may be assigned here, and
    // the BCL List<T> would print its type name instead, so format the elements explicitly.
    public override string ToString() =>
        $"LinkedSpan\nsentenceid={SentenceId}\nsearchTerm={SearchTerm}\nlinkedEntries=\n[{string.Join(", ", LinkedEntries)}]\n";

    // NOpenNLP: Java hashes the ArrayList structurally. J2N's ListEqualityComparer gives the
    // same element-based hash, so equal spans keep equal hash codes.
    public override int GetHashCode() =>
        HashCode.Combine(
            JCG.ListEqualityComparer<T>.Default.GetHashCode(LinkedEntries),
            SentenceId,
            SearchTerm);

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (obj is LinkedSpan<T> other)
        {
            // NOpenNLP: Java's ArrayList.equals compares element-wise; J2N's ListEqualityComparer
            // preserves that rather than comparing references.
            return JCG.ListEqualityComparer<T>.Default.Equals(LinkedEntries, other.LinkedEntries)
                && SentenceId == other.SentenceId
                && string.Equals(SearchTerm, other.SearchTerm, StringComparison.Ordinal);
        }

        return false;
    }
}
