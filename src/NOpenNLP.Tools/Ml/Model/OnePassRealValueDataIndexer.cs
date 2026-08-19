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

namespace NOpenNLP.Tools.Ml.Model;

/// <summary>
/// An indexer for maxent model data which handles cutoffs for uncommon
/// contextual predicates and provides a unique integer index for each of the
/// predicates and maintains event values.
/// </summary>
public class OnePassRealValueDataIndexer : OnePassDataIndexer
{
    private float[][]? values;

    public OnePassRealValueDataIndexer()
    {
    }

    public override float[][]? Values => values;

    protected override int SortAndMerge(IList<ComparableEvent?> eventsToCompare, bool sort)
    {
        int numUniqueEvents = base.SortAndMerge(eventsToCompare, sort);
        values = new float[numUniqueEvents][];
        int numEvents = eventsToCompare.Count;
        for (int i = 0, j = 0; i < numEvents; i++)
        {
            ComparableEvent? evt = eventsToCompare[i];
            if (null == evt)
            {
                continue; // this was a dupe, skip over it.
            }

            values[j++] = evt.Values!;
        }

        return numUniqueEvents;
    }
}
