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

namespace NOpenNLP.Tools.Ml.Naivebayes;

/// <summary>
/// Class implementing the probability for a label.
/// </summary>
/// <typeparam name="T">the label (category) class</typeparam>
public class LogProbability<T> : Probability<T>
{
    public LogProbability(T label)
        : base(label)
    {
        Set(1.0);
    }

    /// <summary>
    /// Assigns a probability to a label, discarding any previously assigned probability.
    /// </summary>
    /// <param name="probability">the probability to assign</param>
    public override void Set(double probability)
    {
        this.probability = Math.Log(probability);
    }

    /// <summary>
    /// Assigns a probability to a label, discarding any previously assigned probability.
    /// </summary>
    /// <param name="probability">the probability to assign</param>
    public override void Set(Probability probability)
    {
        this.probability = probability.Log;
    }

    /// <summary>
    /// Assigns a probability to a label, discarding any previously assigned probability,
    /// if the new probability is greater than the old one.
    /// </summary>
    /// <param name="probability">the probability to assign</param>
    public override void SetIfLarger(double probability)
    {
        double logP = Math.Log(probability);
        if (this.probability < logP)
        {
            this.probability = logP;
        }
    }

    /// <summary>
    /// Assigns a probability to a label, discarding any previously assigned probability,
    /// if the new probability is greater than the old one.
    /// </summary>
    /// <param name="probability">the probability to assign</param>
    public override void SetIfLarger(Probability probability)
    {
        if (this.probability < probability.Log)
        {
            this.probability = probability.Log;
        }
    }

    /// <summary>
    /// Checks if a probability is greater than the old one.
    /// </summary>
    /// <param name="probability">the probability to assign</param>
    public override bool IsLarger(Probability probability)
    {
        return this.probability < probability.Log;
    }

    /// <summary>
    /// Assigns a log probability to a label, discarding any previously assigned probability.
    /// </summary>
    /// <param name="probability">the log probability to assign</param>
    public override void SetLog(double probability)
    {
        this.probability = probability;
    }

    /// <summary>
    /// Compounds the existing probability mass on the label with the new
    /// probability passed in to the method.
    /// </summary>
    /// <param name="probability">the probability weight to add</param>
    public override void AddIn(double probability)
    {
        SetLog(this.probability + Math.Log(probability));
    }

    /// <summary>
    /// Returns the probability associated with a label
    /// </summary>
    /// <returns>the probability associated with the label</returns>
    public override double Value => Math.Exp(probability);

    /// <summary>
    /// Returns the log probability associated with a label
    /// </summary>
    /// <returns>the log probability associated with the label</returns>
    public override double Log => probability;

    /// <summary>
    /// Returns the probabilities associated with all labels
    /// </summary>
    /// <returns>the HashMap of labels and their probabilities</returns>
    public override T Label => label;

    // NOpenNLP: upstream re-declares toString() here with a body identical to
    // Probability.toString(); the inherited override already provides it.
}
