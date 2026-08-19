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

namespace NOpenNLP.Tools.Ml.Perceptron;

/// <summary>
/// Abstract parent class for Perceptron writers.  It provides the persist method
/// which takes care of the structure of a stored document, and requires an
/// extending class to define precisely how the data should be stored.
/// </summary>
public abstract class PerceptronModelWriter : AbstractModelWriter
{
    protected Context[] PARAMS;
    protected string[] OUTCOME_LABELS;
    protected string[] PRED_LABELS;
    private readonly int numOutcomes; // NOpenNLP: made readonly

    protected PerceptronModelWriter(AbstractModel model)
    {
        object[] data = model.GetDataStructures();
        this.numOutcomes = model.NumOutcomes;
        PARAMS = (Context[])data[0];

        var pmap = (IDictionary<string, Context>)data[1];

        OUTCOME_LABELS = (string[])data[2];
        PARAMS = new Context[pmap.Count];
        PRED_LABELS = new string[pmap.Count];

        int i = 0;
        foreach (var pred in pmap)
        {
            PRED_LABELS[i] = pred.Key;
            PARAMS[i] = pred.Value;
            i++;
        }
    }

    protected virtual ComparablePredicate[] SortValues()
    {
        ComparablePredicate[] sortPreds;
        var tmpPreds = new ComparablePredicate[PARAMS.Length];
        int[] tmpOutcomes = new int[numOutcomes];
        double[] tmpParams = new double[numOutcomes];
        int numPreds = 0;

        // remove parameters with 0 weight and predicates with no parameters
        for (int pid = 0; pid < PARAMS.Length; pid++)
        {
            int numParams = 0;
            double[] predParams = PARAMS[pid].Parameters;
            int[] outcomePattern = PARAMS[pid].Outcomes;
            for (int pi = 0; pi < predParams.Length; pi++)
            {
                if (predParams[pi] != 0d)
                {
                    tmpOutcomes[numParams] = outcomePattern[pi];
                    tmpParams[numParams] = predParams[pi];
                    numParams++;
                }
            }

            int[] activeOutcomes = new int[numParams];
            double[] activeParams = new double[numParams];

            for (int pi = 0; pi < numParams; pi++)
            {
                activeOutcomes[pi] = tmpOutcomes[pi];
                activeParams[pi] = tmpParams[pi];
            }

            if (numParams != 0)
            {
                tmpPreds[numPreds] = new ComparablePredicate(PRED_LABELS[pid], activeOutcomes, activeParams);
                numPreds++;
            }
        }

        Console.Error.WriteLine("Compressed " + PARAMS.Length + " parameters to " + numPreds);
        sortPreds = new ComparablePredicate[numPreds];
        Array.Copy(tmpPreds, 0, sortPreds, 0, numPreds);

        // NOpenNLP: Java's Arrays.sort(Object[]) is stable and ComputeOutcomePatterns
        // below groups adjacent equal-comparing predicates, so an unstable sort
        // would reorder the names within a group and change the model bytes.
        Arrays.Sort(sortPreds);
        return sortPreds;
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
    /// If you wish to create a <see cref="PerceptronModelWriter"/> which uses a different
    /// structure, it will be necessary to override the persist method in
    /// addition to implementing the <c>WriteX()</c> methods.
    /// </summary>
    public override void Persist()
    {
        // the type of model (Perceptron)
        WriteUTF("Perceptron");

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
