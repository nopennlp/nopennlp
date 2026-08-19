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
using System.Globalization;
using NOpenNLP.Tools.Ml.Model;
using NOpenNLP.Tools.Support;

namespace NOpenNLP.Tools.Ml.Naivebayes;

/// <summary>
/// Abstract parent class for NaiveBayes writers.  It provides the persist method
/// which takes care of the structure of a stored document, and requires an
/// extending class to define precisely how the data should be stored.
/// </summary>
public abstract class NaiveBayesModelWriter : AbstractModelWriter
{
    protected Context[] PARAMS;
    protected string[] OUTCOME_LABELS;
    protected string[] PRED_LABELS;
    internal int numOutcomes;

    protected NaiveBayesModelWriter(AbstractModel model)
    {
        object[] data = model.GetDataStructures();
        this.numOutcomes = model.NumOutcomes;
        PARAMS = (Context[])data[0];

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

        // NOpenNLP: Java's Arrays.sort(Object[]) is stable and the outcome-pattern
        // grouping below relies on it, so an unstable sort would reorder the names
        // within a group and change the model bytes.
        Arrays.Sort(sortPreds);
        return sortPreds;
    }

    protected virtual IList<IList<ComparablePredicate>> CompressOutcomes(ComparablePredicate[] sorted)
    {
        IList<IList<ComparablePredicate>> outcomePatterns = [];
        if (sorted.Length > 0)
        {
            var cp = sorted[0];
            List<ComparablePredicate> newGroup = [];
            foreach (var t in sorted)
            {
                if (cp.CompareTo(t) == 0)
                {
                    newGroup.Add(t);
                }
                else
                {
                    cp = t;
                    outcomePatterns.Add(newGroup);
                    newGroup = [];
                    newGroup.Add(t);
                }
            }

            outcomePatterns.Add(newGroup);
        }

        return outcomePatterns;
    }

    protected virtual IList<IList<ComparablePredicate>> ComputeOutcomePatterns(ComparablePredicate[] sorted)
    {
        var cp = sorted[0];
        IList<IList<ComparablePredicate>> outcomePatterns = [];
        List<ComparablePredicate> newGroup = [];
        foreach (var predicate in sorted)
        {
            if (cp.CompareTo(predicate) == 0)
            {
                newGroup.Add(predicate);
            }
            else
            {
                cp = predicate;
                outcomePatterns.Add(newGroup);
                newGroup = [];
                newGroup.Add(predicate);
            }
        }

        outcomePatterns.Add(newGroup);
        Console.Error.WriteLine(outcomePatterns.Count + " outcome patterns");
        return outcomePatterns;
    }

    /// <summary>
    /// Writes the model to disk, using the <c>WriteX()</c> methods
    /// provided by extending classes.
    /// <para/>
    /// If you wish to create a <see cref="NaiveBayesModelWriter"/> which uses a different
    /// structure, it will be necessary to override the persist method in
    /// addition to implementing the <c>WriteX()</c> methods.
    /// </summary>
    public override void Persist()
    {
        // the type of model (NaiveBayes)
        WriteUTF("NaiveBayes");

        // the mapping from outcomes to their integer indexes
        WriteInt32(OUTCOME_LABELS.Length);

        foreach (string label in OUTCOME_LABELS)
        {
            WriteUTF(label);
        }

        // the mapping from predicates to the outcomes they contributed to.
        // The sorting is done so that we actually can write this out more
        // compactly than as the entire list.
        var sorted = SortValues();
        var compressed = ComputeOutcomePatterns(sorted);

        WriteInt32(compressed.Count);

        foreach (var a in compressed)
        {
            WriteUTF(a.Count.ToString(CultureInfo.InvariantCulture) + a[0].ToString());
        }

        // the mapping from predicate names to their integer indexes
        WriteInt32(sorted.Length);

        foreach (var s in sorted)
        {
            WriteUTF(s.Name);
        }

        // write out the parameters
        foreach (var t in sorted)
        {
            foreach (var t1 in t.Params)
            {
                WriteDouble(t1);
            }
        }

        Close();
    }
}
