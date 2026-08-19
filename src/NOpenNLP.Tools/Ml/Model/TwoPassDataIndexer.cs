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
using System.IO;
using System.Linq;
using System.Numerics;
using NOpenNLP.Tools.Support;
using NOpenNLP.Tools.Util;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Ml.Model;

/// <summary>
/// Collecting event and context counts by making two passes over the events.  The
/// first pass determines which contexts will be used by the model, and the
/// second pass creates the events in memory containing only the contexts which
/// will be used.  This greatly reduces the amount of memory required for storing
/// the events.  During the first pass a temporary event file is created which
/// is read during the second pass.
/// </summary>
public class TwoPassDataIndexer : AbstractDataIndexer
{
    public override void Index(IObjectStream<Event?> eventStream)
    {
        int cutoff = trainingParameters.GetIntParameter(CUTOFF_PARAM, CUTOFF_DEFAULT);
        bool sort = trainingParameters.GetBooleanParameter(SORT_PARAM, SORT_DEFAULT);

        // NOpenNLP: Stopwatch is the .NET equivalent of the two
        // System.currentTimeMillis() calls upstream uses to time the indexing.
        var start = Stopwatch.StartNew();

        Display("Indexing events with TwoPass using cutoff of " + cutoff + "\n\n");

        Display("\tComputing event counts...  ");

        IDictionary<string, int> predicateIndex = new JCG.OrderedDictionary<string, int>();

        // NOpenNLP: upstream calls File.createTempFile plus deleteOnExit; .NET has
        // no deleteOnExit, so the file is deleted in a finally block instead. That
        // also covers the paths where upstream would leak the file on an exception.
        string tmp = Path.GetTempFileName();
        try
        {
            int numEvents;
            var writeEventStream = new HashSumEventStream(eventStream); // do not close.
            using (Stream dos = new FileStream(tmp, FileMode.Create, FileAccess.Write))
            {
                numEvents = ComputeEventCounts(writeEventStream, dos, predicateIndex, cutoff);
            }

            var writeHash = writeEventStream.CalculateHashSum();

            Display("done. " + numEvents + " events\n");

            Display("\tIndexing...  ");

            IList<ComparableEvent?> eventsToCompare;
            BigInteger readHash;
            using (var readStream = new HashSumEventStream(new EventStream(tmp)))
            {
                eventsToCompare = Index(readStream, predicateIndex);
                readHash = readStream.CalculateHashSum();
            }

            if (readHash.CompareTo(writeHash) != 0)
            {
                throw new IOException("Event hash for writing and reading events did not match.");
            }

            Display("done.\n");

            if (sort)
            {
                Display("Sorting and merging events... ");
            }
            else
            {
                Display("Collecting events... ");
            }

            SortAndMerge(eventsToCompare, sort);
            Display(string.Format(CultureInfo.InvariantCulture, "Done indexing in {0:F2} s.\n",
                start.Elapsed.TotalSeconds));
        }
        finally
        {
            try
            {
                File.Delete(tmp);
            }
            catch (IOException)
            {
                // Upstream's deleteOnExit is likewise best-effort.
            }
        }
    }

    /// <summary>
    /// Reads events from <paramref name="eventStream"/> and writes them to
    /// <paramref name="eventStore"/>. The predicates associated with each event are
    /// counted and any which occur at least <paramref name="cutoff"/> times are added
    /// to the <paramref name="predicatesInOut"/> map along with a unique integer index.
    /// <para/>
    /// Protocol:
    /// <list type="number">
    /// <item><description>(utf string) - Event outcome</description></item>
    /// <item><description>(int) - Event context array length</description></item>
    /// <item><description>(utf string) - Event context string</description></item>
    /// <item><description>(int) - Event values array length</description></item>
    /// <item><description>(float) - Event value</description></item>
    /// </list>
    /// </summary>
    private int ComputeEventCounts(IObjectStream<Event?> eventStream, Stream eventStore,
        IDictionary<string, int> predicatesInOut, int cutoff)
    {
        // NOpenNLP: insertion-ordered so the counter does not depend on hash order
        // the way Java's HashMap does.
        var counter = new JCG.OrderedDictionary<string, int>();
        int eventCount = 0;

        while (eventStream.Read() is { } ev)
        {
            eventCount++;

            eventStore.WriteJavaUTF(ev.Outcome);

            eventStore.WriteJavaInt32(ev.Context.Length);
            string[] ec = ev.Context;
            Update(ec, counter);
            foreach (string ctxString in ec)
            {
                eventStore.WriteJavaUTF(ctxString);
            }

            if (ev.Values == null)
            {
                eventStore.WriteJavaInt32(0);
            }
            else
            {
                eventStore.WriteJavaInt32(ev.Values.Length);
                foreach (float value in ev.Values)
                {
                    eventStore.WriteJavaSingle(value);
                }
            }
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

        return eventCount;
    }

    private sealed class EventStream(string file) : ObjectStreamBase<Event?>
    {
        private readonly Stream inputStream = new FileStream(file, FileMode.Open, FileAccess.Read);

        public override Event? Read()
        {
            // NOpenNLP: upstream tests DataInputStream.available() != 0, which on a
            // BufferedInputStream over a local file reports the bytes left. The
            // .NET equivalent for a FileStream is comparing Position to Length.
            if (inputStream.Position != inputStream.Length)
            {
                string outcome = inputStream.ReadJavaUTF();
                int contextLenght = inputStream.ReadJavaInt32();
                string[] context = new string[contextLenght];
                for (int i = 0; i < contextLenght; i++)
                {
                    context[i] = inputStream.ReadJavaUTF();
                }

                int valuesLength = inputStream.ReadJavaInt32();
                float[]? values = null;
                if (valuesLength > 0)
                {
                    values = new float[valuesLength];
                    for (int i = 0; i < valuesLength; i++)
                    {
                        values[i] = inputStream.ReadJavaSingle();
                    }
                }

                return new Event(outcome, context, values);
            }
            else
            {
                return null;
            }
        }

        public override void Reset() => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inputStream.Dispose();
            }
        }
    }
}
