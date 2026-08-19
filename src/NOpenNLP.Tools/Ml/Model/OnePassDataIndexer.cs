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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Ml.Model;

/// <summary>
/// An indexer for maxent model data which handles cutoffs for uncommon
/// contextual predicates and provides a unique integer index for each of the
/// predicates.
/// </summary>
public class OnePassDataIndexer : AbstractDataIndexer
{
    public override void Index(IObjectStream<Event?> eventStream)
    {
        int cutoff = trainingParameters.GetIntParameter(CUTOFF_PARAM, CUTOFF_DEFAULT);
        bool sort = trainingParameters.GetBooleanParameter(SORT_PARAM, SORT_DEFAULT);

        // NOpenNLP: Stopwatch is the .NET equivalent of the two
        // System.currentTimeMillis() calls upstream uses to time the indexing.
        var start = Stopwatch.StartNew();

        Display("Indexing events with OnePass using cutoff of " + cutoff + "\n\n");

        Display("\tComputing event counts...  ");
        var predicateIndex = new JCG.OrderedDictionary<string, int>();
        var events = ComputeEventCounts(eventStream, predicateIndex, cutoff);
        Display("done. " + events.Count + " events\n");

        Display("\tIndexing...  ");
        // NOpenNLP: upstream calls ObjectStreamUtils.createObjectStream(Collection);
        // the port omits that overload because C# would bind the collection to the
        // varargs one, so CollectionObjectStream is constructed directly.
        var eventsToCompare = Index(new CollectionObjectStream<Event>(events), predicateIndex);

        Display("done.\n");

        Display("Sorting and merging events... ");
        SortAndMerge(eventsToCompare, sort);
        Display(string.Format(CultureInfo.InvariantCulture, "Done indexing in {0:F2} s.\n",
            start.Elapsed.TotalSeconds));
    }

    /// <summary>
    /// Reads events from <paramref name="eventStream"/> into a list. The predicates
    /// associated with each event are counted and any which occur at least
    /// <paramref name="cutoff"/> times are added to the <paramref name="predicatesInOut"/>
    /// map along with a unique integer index.
    /// </summary>
    private JCG.List<Event> ComputeEventCounts(IObjectStream<Event?> eventStream,
        JCG.OrderedDictionary<string, int> predicatesInOut, int cutoff)
    {
        // NOpenNLP: insertion-ordered so the counter does not depend on hash order
        // the way Java's HashMap does. The predicate set below is sorted anyway,
        // but the counts are read back through it.
        var counter = new JCG.OrderedDictionary<string, int>();
        JCG.List<Event> events = [];
        while (eventStream.Read() is { } ev)
        {
            events.Add(ev);
            Update(ev.Context, counter);
        }

        // NOpenNLP: Java sorts with String.compareTo, which compares UTF-16 code
        // units. StringComparer.Ordinal is the .NET equivalent; the default string
        // comparer here would be culture-sensitive and order predicates differently.
        string[] predicateSet = counter
            .Where(entry => entry.Value >= cutoff)
            .Select(entry => entry.Key)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        predCounts = new int[predicateSet.Length];
        for (int i = 0; i < predicateSet.Length; i++)
        {
            predCounts[i] = counter[predicateSet[i]];
            predicatesInOut[predicateSet[i]] = i;
        }

        return events;
    }
}
