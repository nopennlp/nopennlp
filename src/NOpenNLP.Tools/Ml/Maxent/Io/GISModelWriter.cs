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
using System.Globalization;
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Support;

namespace NOpenNLP.Tools.Ml.Maxent.Io;

/// <summary>
/// Abstract parent class for GISModel writers.  It provides the persist method
/// which takes care of the structure of a stored document, and requires an
/// extending class to define precisely how the data should be stored.
/// </summary>
public abstract class GISModelWriter : AbstractModelWriter
{
    protected Context[] PARAMS;
    protected string[] OUTCOME_LABELS;
    protected string[] PRED_LABELS;

    protected GISModelWriter(AbstractModel model)
    {
        object[] data = model.GetDataStructures();

        IDictionary<string, Context> pmap = (IDictionary<string, Context>)data[1];

        OUTCOME_LABELS = (string[])data[2];
        PARAMS = new Context[pmap.Count];
        PRED_LABELS = new string[pmap.Count];

        int i = 0;
        foreach (KeyValuePair<string, Context> pred in pmap)
        {
            PRED_LABELS[i] = pred.Key;
            PARAMS[i] = pred.Value;
            i++;
        }
    }

    /// <summary>
    /// Writes the model to disk, using the <c>WriteX()</c> methods provided
    /// by extending classes.
    /// <para/>
    /// If you wish to create a <see cref="GISModelWriter"/> which uses a different
    /// structure, it will be necessary to override the persist method in addition to
    /// implementing the <c>WriteX()</c> methods.
    /// </summary>
    public override void Persist()
    {
        // the type of model (GIS)
        WriteUTF("GIS");

        // the value of the correction constant (not used anymore)
        WriteInt32(1);

        // the value of the correction params (not used anymore)
        WriteDouble(1);

        // the mapping from outcomes to their integer indexes
        WriteInt32(OUTCOME_LABELS.Length);

        foreach (string OUTCOME_LABEL in OUTCOME_LABELS)
        {
            WriteUTF(OUTCOME_LABEL);
        }

        // the mapping from predicates to the outcomes they contributed to.
        // The sorting is done so that we actually can write this out more
        // compactly than as the entire list.
        ComparablePredicate[] sorted = SortValues();
        IList<IList<ComparablePredicate>> compressed = CompressOutcomes(sorted);

        WriteInt32(compressed.Count);

        foreach (IList<ComparablePredicate> aCompressed in compressed)
        {
            WriteUTF(aCompressed.Count.ToString(CultureInfo.InvariantCulture) + aCompressed[0].ToString());
        }

        // the mapping from predicate names to their integer indexes
        WriteInt32(PARAMS.Length);

        foreach (ComparablePredicate aSorted in sorted)
        {
            WriteUTF(aSorted.Name);
        }

        // write out the parameters
        foreach (ComparablePredicate aSorted in sorted)
        {
            for (int j = 0; j < aSorted.Params.Length; j++)
            {
                WriteDouble(aSorted.Params[j]);
            }
        }

        Close();
    }

    protected virtual ComparablePredicate[] SortValues()
    {
        ComparablePredicate[] sortPreds = new ComparablePredicate[PARAMS.Length];

        int numParams = 0;
        for (int pid = 0; pid < PARAMS.Length; pid++)
        {
            int[] predkeys = PARAMS[pid].Outcomes;
            // Array.Sort(predkeys);
            int numActive = predkeys.Length;
            double[] activeParams = PARAMS[pid].Parameters;

            numParams += numActive;

            sortPreds[pid] = new ComparablePredicate(PRED_LABELS[pid], predkeys, activeParams);
        }

        // NOpenNLP: Java's Arrays.sort(Object[]) is stable and CompressOutcomes
        // below groups adjacent equal-comparing predicates, so an unstable sort
        // would reorder the names within a group and change the model bytes.
        Arrays.Sort(sortPreds);
        return sortPreds;
    }

    protected virtual IList<IList<ComparablePredicate>> CompressOutcomes(ComparablePredicate[] sorted)
    {
        IList<IList<ComparablePredicate>> outcomePatterns = [];
        if (sorted.Length > 0)
        {
            ComparablePredicate cp = sorted[0];
            List<ComparablePredicate> newGroup = [];
            for (int i = 0; i < sorted.Length; i++)
            {
                if (cp.CompareTo(sorted[i]) == 0)
                {
                    newGroup.Add(sorted[i]);
                }
                else
                {
                    cp = sorted[i];
                    outcomePatterns.Add(newGroup);
                    newGroup = [];
                    newGroup.Add(sorted[i]);
                }
            }

            outcomePatterns.Add(newGroup);
        }

        return outcomePatterns;
    }
}
