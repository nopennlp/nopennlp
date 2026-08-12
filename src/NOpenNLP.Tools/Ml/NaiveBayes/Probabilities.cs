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
/// Class implementing the probability distribution over labels returned by a classifier.
/// </summary>
/// <typeparam name="T">the label (category) class</typeparam>
public abstract class Probabilities<T>
{
    protected readonly IDictionary<T, double?> map = new JCG.Dictionary<T, double?>(); // NOpenNLP: made readonly
    protected bool isNormalised /* = false */;
    protected IDictionary<T?, double?>? normalised;
    protected double confidence /* = 0 */;

    /// <summary>
    /// Assigns a probability to a label, discarding any previously assigned probability.
    /// </summary>
    /// <param name="t">the label to which the probability is being assigned</param>
    /// <param name="probability">the probability to assign</param>
    public virtual void Set(T t, double probability)
    {
        isNormalised = false;
        map.Put(t, probability);
    }

    /// <summary>
    /// Assigns a probability to a label, discarding any previously assigned probability.
    /// </summary>
    /// <param name="t">the label to which the probability is being assigned</param>
    /// <param name="probability">the probability to assign</param>
    public virtual void Set(T t, Probability<T> probability)
    {
        isNormalised = false;
        map.Put(t, probability.Value);
    }

    /// <summary>
    /// Assigns a probability to a label, discarding any previously assigned probability,
    /// if the new probability is greater than the old one.
    /// </summary>
    /// <param name="t">the label to which the probability is being assigned</param>
    /// <param name="probability">the probability to assign</param>
    public virtual void SetIfLarger(T t, double probability)
    {
        // NOpenNLP: Java Map.get() returns null for an absent key.
        map.TryGetValue(t, out double? p);
        if (p == null || probability > p)
        {
            isNormalised = false;
            map.Put(t, probability);
        }
    }

    /// <summary>
    /// Assigns a log probability to a label, discarding any previously assigned probability.
    /// </summary>
    /// <param name="t">the label to which the log probability is being assigned</param>
    /// <param name="probability">the log probability to assign</param>
    public virtual void SetLog(T t, double probability)
    {
        Set(t, Math.Exp(probability));
    }

    /// <summary>
    /// Compounds the existing probability mass on the label with the new probability passed in to the method.
    /// </summary>
    /// <param name="t">the label whose probability mass is being updated</param>
    /// <param name="probability">the probability weight to add</param>
    /// <param name="count">the amplifying factor for the probability compounding</param>
    public virtual void AddIn(T t, double probability, int count)
    {
        isNormalised = false;
        // NOpenNLP: Java Map.get() returns null for an absent key.
        map.TryGetValue(t, out double? p);
        p ??= 1;
        probability = Math.Pow(probability, count);
        map.Put(t, p * probability);
    }

    /// <summary>
    /// Returns the probability associated with a label
    /// </summary>
    /// <param name="t">the label whose probability needs to be returned</param>
    /// <returns>the probability associated with the label</returns>
    public virtual double? Get(T? t)
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
    public virtual double GetLog(T t)
    {
        return Math.Log(Get(t) ?? throw new InvalidOperationException("Probability is null"));
    }

    /// <summary>
    /// Returns the probabilities associated with all labels
    /// </summary>
    /// <returns>the <see cref="ISet{T}"/> of labels and their probabilities</returns>
    public virtual ISet<T> Keys => new HashSet<T>(map.Keys);

    /// <summary>
    /// Returns the probabilities associated with all labels
    /// </summary>
    /// <returns>the <see cref="ISet{T}"/> of labels and their probabilities</returns>
    public virtual IDictionary<T?, double?> All => Normalize();

    private IDictionary<T?, double?> Normalize()
    {
        if (isNormalised)
            return normalised!; // [!]: isNormalized gates whether this is null or not
        var temp = CreateMapDataStructure();
        double sum = 0;
        foreach (var entry in map)
        {
            double? p = entry.Value;
            if (p != null)
            {
                sum += p.Value;
            }
        }

        foreach (var entry in temp)
        {
            var t = entry.Key;
            double? p = entry.Value;
            if (p != null)
            {
                temp.Put(t, p / sum);
            }
        }

        normalised = temp;
        isNormalised = true;
        return temp;
    }

    protected virtual IDictionary<T?, double?> CreateMapDataStructure()
    {
        return new JCG.Dictionary<T?, double?>();
    }

    /// <summary>
    /// Returns the most likely label
    /// </summary>
    /// <returns>the label that has the highest associated probability</returns>
    public virtual T? GetMax()
    {
        double max = 0;
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

    /// <summary>
    /// Returns the probability of the most likely label
    /// </summary>
    /// <returns>the highest probability</returns>
    public virtual double? MaxValue => Get(GetMax());

    public virtual void DiscardCountsBelow(double i)
    {
        IList<T> labelsToRemove = new List<T>();
        foreach (var entry in map)
        {
            var label = entry.Key;
            double? sum = entry.Value ?? 0;
            if (sum < i)
                labelsToRemove.Add(label);
        }

        foreach (var label in labelsToRemove)
        {
            map.Remove(label);
        }
    }

    /// <summary>
    /// Returns the best confidence with which this set of probabilities has been calculated.
    /// This is a function of the amount of data that supports the assertion.
    /// It is also a measure of the accuracy of the estimator of the probability.
    /// </summary>
    /// <returns>the best confidence of the probabilities</returns>
    public virtual double GetConfidence()
    {
        return confidence;
    }

    /// <summary>
    /// Sets the best confidence with which this set of probabilities has been calculated.
    /// This is a function of the amount of data that supports the assertion.
    /// It is also a measure of the accuracy of the estimator of the probability.
    /// </summary>
    /// <param name="confidence">the confidence in the probabilities</param>
    public virtual void SetConfidence(double confidence)
    {
        this.confidence = confidence;
    }

    public override string ToString() => All.ToString();
}
