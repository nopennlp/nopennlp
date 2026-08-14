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

// This file has been modified from the original Apache OpenNLP 1.9.1 source:
// translated from Java to C# and adapted for .NET. See NOTICE.

using NOpenNLP.Tools.Support;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace NOpenNLP.Tools.Ml.Naivebayes;

/// <summary>
/// Class implementing the probability distribution over labels returned by
/// a classifier as a log of probabilities.
/// This is necessary because floating point precision in Java does not allow for high-accuracy
/// representation of very low probabilities such as would occur in a text categorizer.
/// </summary>
/// <typeparam name="T">the label (category) class</typeparam>
public class LogProbabilities<T> : Probabilities<T>
{
    /// <summary>
    /// Assigns a probability to a label, discarding any previously assigned probability.
    /// </summary>
    /// <param name="t">the label to which the probability is being assigned</param>
    /// <param name="probability">the probability to assign</param>
    public override void Set(T t, double probability)
    {
        isNormalised = false;
        map.Put(t, Log(probability));
    }

    /// <summary>
    /// Assigns a probability to a label, discarding any previously assigned probability.
    /// </summary>
    /// <param name="t">the label to which the probability is being assigned</param>
    /// <param name="probability">the probability to assign</param>
    public override void Set(T t, Probability<T> probability)
    {
        isNormalised = false;
        map.Put(t, probability.Log);
    }

    /// <summary>
    /// Assigns a probability to a label, discarding any previously assigned probability,
    /// if the new probability is greater than the old one.
    /// </summary>
    /// <param name="t">the label to which the probability is being assigned</param>
    /// <param name="probability">the probability to assign</param>
    public override void SetIfLarger(T t, double probability)
    {
        double logProbability = Log(probability);
        // NOpenNLP: Java Map.get() returns null for an absent key.
        map.TryGetValue(t, out double? p);
        if (p == null || logProbability > p)
        {
            isNormalised = false;
            map.Put(t, logProbability);
        }
    }

    /// <summary>
    /// Assigns a log probability to a label, discarding any previously assigned probability.
    /// </summary>
    /// <param name="t">the label to which the log probability is being assigned</param>
    /// <param name="probability">the log probability to assign</param>
    public override void SetLog(T t, double probability)
    {
        isNormalised = false;
        map.Put(t, probability);
    }

    /// <summary>
    /// Compounds the existing probability mass on the label with the new probability passed in to the method.
    /// </summary>
    /// <param name="t">the label whose probability mass is being updated</param>
    /// <param name="probability">the probability weight to add</param>
    /// <param name="count">the amplifying factor for the probability compounding</param>
    public override void AddIn(T t, double probability, int count)
    {
        isNormalised = false;
        // NOpenNLP: Java Map.get() returns null for an absent key.
        map.TryGetValue(t, out double? p);
        p ??= 0;
        probability = Log(probability) * count;
        map.Put(t, p + probability);
    }

    private IDictionary<T?, double?> Normalize()
    {
        if (isNormalised)
            return normalised!; // [!]: isNormalized gates whether normalized is null
        var temp = CreateMapDataStructure();
        double highestLogProbability = double.NegativeInfinity;
        foreach (var entry in map)
        {
            double? p = entry.Value;
            if (p != null && p > highestLogProbability)
            {
                highestLogProbability = p.Value;
            }
        }

        double sum = 0;
        foreach (var entry in map)
        {
            var t = entry.Key;
            double? p = entry.Value;
            if (p != null)
            {
                double temp_p = Math.Exp(p.Value - highestLogProbability);
                if (!double.IsNaN(temp_p))
                {
                    sum += temp_p;
                    temp.Put(t, temp_p);
                }
            }
        }

        foreach (var entry in temp)
        {
            var t = entry.Key;
            double? p = entry.Value;
            if (p != null && sum > double.MinValue)
            {
                temp.Put(t, p / sum);
            }
        }

        normalised = temp;
        isNormalised = true;
        return temp;
    }

    // NOpenNLP TODO: replace with static import of Math.Log
    private static double Log(double prob) // NOpenNLP: made static
    {
        return Math.Log(prob);
    }

    /// <summary>
    /// Returns the probability associated with a label
    /// </summary>
    /// <param name="t">the label whose probability needs to be returned</param>
    /// <returns>the probability associated with the label</returns>
    public override double? Get(T? t)
    {
        double? d = Normalize()[t];
        if (d == null)
            return 0;
        return d;
    }

    /// <summary>
    /// Returns the log probability associated with a label
    /// </summary>
    /// <param name="t">the label whose log probability needs to be returned</param>
    /// <returns>the log probability associated with the label</returns>
    public override double GetLog(T t)
    {
        // NOpenNLP: Java Map.get() returns null for an absent key, which this
        // maps to negative infinity, so the result is never null.
        map.TryGetValue(t, out double? d);
        return d ?? double.NegativeInfinity;
    }

    public override void DiscardCountsBelow(double i)
    {
        i = Math.Log(i);
        List<T> labelsToRemove = [];
        foreach (var entry in map)
        {
            var label = entry.Key;
            double? sum = entry.Value ?? double.NegativeInfinity;
            if (sum < i)
                labelsToRemove.Add(label);
        }

        foreach (var label in labelsToRemove)
        {
            map.Remove(label);
        }
    }

    /// <summary>
    /// Returns the probabilities associated with all labels
    /// </summary>
    /// <returns>the HashMap of labels and their probabilities</returns>
    public override IDictionary<T?, double?> All => Normalize();

    /// <summary>
    /// Returns the most likely label
    /// </summary>
    /// <returns>the label that has the highest associated probability</returns>
    public override T? GetMax()
    {
        double max = double.NegativeInfinity;
        var maxT = default(T);
        foreach (var entry in map)
        {
            var t = entry.Key;
            double? temp = entry.Value;
            if (temp >= max)
            {
                max = temp.Value;
                maxT = t;
            }
        }

        return maxT;
    }
}
