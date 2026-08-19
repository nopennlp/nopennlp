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

namespace NOpenNLP.Tools.Ml.Model;

/// <summary>
/// Class used to store parameters or expected values associated with this context which
/// can be updated or assigned.
/// </summary>
public class MutableContext : Context
{
    /// <summary>
    /// Creates a new parameters object with the specified parameters associated with the specified
    /// outcome pattern.
    /// </summary>
    /// <param name="outcomePattern">Array of outcomes for which parameters exists for this context.</param>
    /// <param name="parameters">Parameters for the outcomes specified.</param>
    public MutableContext(int[] outcomePattern, double[] parameters)
        : base(outcomePattern, parameters)
    {
    }

    /// <summary>
    /// Assigns the parameter or expected value at the specified <paramref name="outcomeIndex"/>
    /// the specified value.
    /// </summary>
    /// <param name="outcomeIndex">The index of the parameter or expected value to be updated.</param>
    /// <param name="value">The value to be assigned.</param>
    public virtual void SetParameter(int outcomeIndex, double value)
        => parameters[outcomeIndex] = value;

    /// <summary>
    /// Updated the parameter or expected value at the specified <paramref name="outcomeIndex"/> by
    /// adding the specified value to its current value.
    /// </summary>
    /// <param name="outcomeIndex">The index of the parameter or expected value to be updated.</param>
    /// <param name="value">The value to be added.</param>
    public virtual void UpdateParameter(int outcomeIndex, double value)
        => parameters[outcomeIndex] += value;

    public virtual bool Contains(int outcome) => Array.BinarySearch(outcomes, outcome) >= 0;
}
