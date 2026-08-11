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
using System;
using System.Globalization;

namespace NOpenNLP.Tools.Ml.Naivebayes;

// NOpenNLP: non-generic base class
public abstract class Probability
{
    /// <summary>
    /// Gets the probability.
    /// </summary>
    /// <remarks>
    /// NOpenNLP: This was <c>get()</c> in Java.
    /// </remarks>
    public abstract double Value { get; }
}

/// <summary>
/// Class implementing the probability for a label.
/// </summary>
/// <typeparam name="T">the label (category) class</typeparam>
public class Probability<T>(T label) : Probability
{
    protected readonly T label = label; // NOpenNLP: made readonly
    protected double probability = 1;

    /// <summary>
    /// Assigns a probability to a label, discarding any previously assigned probability.
    /// </summary>
    /// <param name="probability">the probability to assign</param>
    public virtual void Set(double probability)
    {
        this.probability = probability;
    }

    /// <summary>
    /// Assigns a probability to a label, discarding any previously assigned probability.
    /// </summary>
    /// <param name="probability">the probability to assign</param>
    public virtual void Set(Probability probability)
    {
        this.probability = probability.Value;
    }

    /// <summary>
    /// Assigns a probability to a label, discarding any previously assigned probability,
    /// if the new probability is greater than the old one.
    /// </summary>
    /// <param name="probability">the probability to assign</param>
    public virtual void SetIfLarger(double probability)
    {
        if (this.probability < probability)
        {
            this.probability = probability;
        }
    }

    /// <summary>
    /// Assigns a probability to a label, discarding any previously assigned probability,
    /// if the new probability is greater than the old one.
    /// </summary>
    /// <param name="probability">the probability to assign</param>
    public virtual void SetIfLarger(Probability probability)
    {
        if (this.probability < probability.Value)
        {
            this.probability = probability.Value;
        }
    }

    /// <summary>
    /// Checks if a probability is greater than the old one.
    /// </summary>
    /// <param name="probability">the probability to assign</param>
    public virtual bool IsLarger(Probability probability)
    {
        return this.probability < probability.Value;
    }

    /// <summary>
    /// Assigns a log probability to a label, discarding any previously assigned probability.
    /// </summary>
    /// <param name="probability">the log probability to assign</param>
    public virtual void SetLog(double probability)
    {
        Set(Math.Exp(probability));
    }

    /// <summary>
    /// Compounds the existing probability mass on the label with the new probability passed in to the method.
    /// </summary>
    /// <param name="probability">the probability weight to add</param>
    public virtual void AddIn(double probability)
    {
        Set(this.probability * probability);
    }

    /// <summary>
    /// Returns the probability associated with a label
    /// </summary>
    /// <returns>the probability associated with the label</returns>
    public override double Value => probability;

    /// <summary>
    /// Returns the log probability associated with a label
    /// </summary>
    /// <returns>the log probability associated with the label</returns>
    public virtual double Log => Math.Log(Value);

    /// <summary>
    /// Returns the probabilities associated with all labels
    /// </summary>
    /// <returns>the HashMap of labels and their probabilities</returns>
    public virtual T Label => label;

    public override string ToString()
        => label == null
            ? probability.ToString(CultureInfo.InvariantCulture)
            : $"{label}:{probability}";
}
