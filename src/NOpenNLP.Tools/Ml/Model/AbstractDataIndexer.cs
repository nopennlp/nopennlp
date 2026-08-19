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
using System.Linq;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Ml.Model;

/// <summary>
/// Abstract class for collecting event and context counts used in training.
/// </summary>
public abstract class AbstractDataIndexer : IDataIndexer
{
    public const string CUTOFF_PARAM = AbstractTrainer.CUTOFF_PARAM;
    public const int CUTOFF_DEFAULT = AbstractTrainer.CUTOFF_DEFAULT;

    public const string SORT_PARAM = "sort";
    public const bool SORT_DEFAULT = true;

    protected TrainingParameters trainingParameters = null!;
    protected IDictionary<string, string>? reportMap;

    protected bool printMessages;

    public virtual void Init(TrainingParameters indexingParameters, IDictionary<string, string>? reportMap)
    {
        this.reportMap = reportMap;
        // NOpenNLP: upstream assigns to the parameter rather than the field here,
        // so the field stays null. Kept as-is; nothing in this class reads it.
        if (this.reportMap == null) reportMap = new JCG.Dictionary<string, string>();
        trainingParameters = indexingParameters;

        printMessages = trainingParameters.GetBooleanParameter(AbstractTrainer.VERBOSE_PARAM,
            AbstractTrainer.VERBOSE_DEFAULT);
    }

    private int numEvents;

    /// <summary>The integer contexts associated with each unique event.</summary>
    protected int[][] contexts = null!;

    /// <summary>The integer outcome associated with each unique event.</summary>
    protected int[] outcomeList = null!;

    /// <summary>The number of times an event occured in the training data.</summary>
    protected int[] numTimesEventsSeen = null!;

    /// <summary>The predicate/context names.</summary>
    protected string[] predLabels = null!;

    /// <summary>The names of the outcomes.</summary>
    protected string[] outcomeLabels = null!;

    /// <summary>The number of times each predicate occured.</summary>
    protected int[] predCounts = null!;

    public virtual int[][] Contexts => contexts;

    public virtual int[] NumTimesEventsSeen => numTimesEventsSeen;

    public virtual int[] OutcomeList => outcomeList;

    public virtual string[] PredLabels => predLabels;

    public virtual string[] OutcomeLabels => outcomeLabels;

    public virtual int[] PredCounts => predCounts;

    public abstract void Index(IObjectStream<Event> eventStream);

    /// <summary>
    /// Sorts and uniques the array of comparable events and return the number of unique events.
    /// This method will alter the eventsToCompare array -- it does an in place
    /// sort, followed by an in place edit to remove duplicates.
    /// </summary>
    /// <param name="eventsToCompare">a <see cref="ComparableEvent"/> list</param>
    /// <param name="sort">whether to sort and merge duplicates</param>
    /// <returns>The number of unique events in the specified list.</returns>
    /// <exception cref="InsufficientTrainingDataException">if not enough events are provided</exception>
    protected virtual int SortAndMerge(IList<ComparableEvent?> eventsToCompare, bool sort)
    {
        int numUniqueEvents = 1;
        numEvents = eventsToCompare.Count;
        if (sort && eventsToCompare.Count > 0)
        {
            // NOpenNLP: Collections.sort is stable and this loop depends on it, so
            // the stable sort in ListExtensions is used rather than List<T>.Sort.
            eventsToCompare.Sort();

            ComparableEvent ce = eventsToCompare[0]!;
            for (int i = 1; i < numEvents; i++)
            {
                ComparableEvent ce2 = eventsToCompare[i]!;

                if (ce.CompareTo(ce2) == 0)
                {
                    ce.Seen++; // increment the seen count
                    eventsToCompare[i] = null; // kill the duplicate
                }
                else
                {
                    ce = ce2; // a new champion emerges...
                    numUniqueEvents++; // increment the # of unique events
                }
            }
        }
        else
        {
            numUniqueEvents = eventsToCompare.Count;
        }

        if (numUniqueEvents == 0)
        {
            throw new InsufficientTrainingDataException("Insufficient training data to create model.");
        }

        if (sort) Display("done. Reduced " + numEvents + " events to " + numUniqueEvents + ".\n");

        contexts = new int[numUniqueEvents][];
        outcomeList = new int[numUniqueEvents];
        numTimesEventsSeen = new int[numUniqueEvents];

        for (int i = 0, j = 0; i < numEvents; i++)
        {
            ComparableEvent? evt = eventsToCompare[i];
            if (null == evt)
            {
                continue; // this was a dupe, skip over it.
            }

            numTimesEventsSeen[j] = evt.Seen;
            outcomeList[j] = evt.Outcome;
            contexts[j] = evt.PredIndexes;
            ++j;
        }

        return numUniqueEvents;
    }

    protected virtual IList<ComparableEvent?> Index(IObjectStream<Event?> events,
        IDictionary<string, int> predicateIndex)
    {
        // NOpenNLP: insertion-ordered so the outcome indexes, and therefore the
        // model, do not depend on hash order the way Java's HashMap does.
        IDictionary<string, int> omap = new JCG.OrderedDictionary<string, int>();

        List<ComparableEvent?> eventsToCompare = [];

        while (events.Read() is { } ev)
        {
            if (!omap.ContainsKey(ev.Outcome))
            {
                omap[ev.Outcome] = omap.Count;
            }

            // NOpenNLP: Java's Map.get returns null for an absent predicate and the
            // stream drops it; TryGetValue is the equivalent that does not throw.
            int[] cons = ev.Context
                .Select(pred => predicateIndex.TryGetValue(pred, out int idx) ? (int?)idx : null)
                .Where(i => i.HasValue)
                .Select(i => i!.Value)
                .ToArray();

            // drop events with no active features
            if (cons.Length > 0)
            {
                int ocID = omap[ev.Outcome];
                eventsToCompare.Add(new ComparableEvent(ocID, cons, ev.Values));
            }
            else
            {
                Display("Dropped event " + ev.Outcome + ":"
                    + "[" + string.Join(", ", ev.Context) + "]" + "\n");
            }
        }

        outcomeLabels = ToIndexedStringArray(omap);
        predLabels = ToIndexedStringArray(predicateIndex);
        return eventsToCompare;
    }

    public virtual int NumEvents => numEvents;

    // NOpenNLP: upstream also has a deprecated four-argument update(String[],
    // Set<String>, Map<String,Integer>, int), annotated "will be removed after
    // 1.8.1 release" and called from nowhere in 1.9.4. It is not ported.

    /// <summary>
    /// Updates the set of predicates and counter with the specified event contexts.
    /// </summary>
    /// <param name="ec">The contexts/features which occur in a event.</param>
    /// <param name="counter">The predicate counters.</param>
    protected static void Update(string[] ec, IDictionary<string, int> counter)
    {
        foreach (string s in ec)
        {
            counter.TryGetValue(s, out int value);
            counter[s] = value + 1;
        }
    }

    /// <summary>
    /// Utility method for creating a <see cref="string"/> array from a map whose
    /// keys are labels to be stored in the array and whose values are the indices
    /// at which the corresponding labels should be inserted.
    /// </summary>
    protected static string[] ToIndexedStringArray(IDictionary<string, int> labelToIndexMap)
        => labelToIndexMap.OrderBy(e => e.Value).Select(e => e.Key).ToArray();

    public virtual float[][]? Values => null;

    protected virtual void Display(string s)
    {
        if (printMessages)
        {
            Console.Write(s);
        }
    }
}
