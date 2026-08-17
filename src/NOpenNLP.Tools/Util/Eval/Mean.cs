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

using System.Globalization;

namespace NOpenNLP.Tools.Util.Eval;


/// <summary>
/// Calculates the arithmetic mean of values
/// added with the <see cref="Add(double)"/> method.
/// </summary>
public class Mean
{
    /// <summary>
    /// The sum of all added values.
    /// </summary>
    private double sum;

    /// <summary>
    /// The number of times a value was added.
    /// </summary>
    private long count;

    /// <summary>
    /// Adds a value to the arithmetic mean.
    /// </summary>
    /// <param name="value">the value which should be added
    /// to the arithmetic mean.</param>
    public void Add(double value) => Add(value, 1);

    /// <summary>
    /// Adds a value count times to the arithmetic mean.
    /// </summary>
    /// <param name="value">the value which should be added
    /// to the arithmetic mean.</param>
    /// <param name="count">number of times the value should be added to
    /// arithmetic mean.</param>
    public void Add(double value, long count)
    {
        sum += value * count;
        this.count += count;
    }

    /// <summary>
    /// Retrieves the mean of all values added with
    /// <see cref="Add(double)"/> or 0 if there are zero added
    /// values.
    /// </summary>
    // NOpenNLP: upstream exposes this as mean(); converted to a property since it
    // reports a value rather than performing an action.
    public double Value => count > 0 ? sum / count : 0;

    /// <summary>
    /// Retrieves the number of times a value
    /// was added to the mean.
    /// </summary>
    public long Count => count;

    /// <inheritdoc/>
    // NOpenNLP: J2N's "J" format reproduces Java's Double.toString, which differs
    // from .NET's "R" -- Java renders integral values as "1.0" and small magnitudes
    // as "1.0E-5" where "R" gives "1" and "1E-05".
    public override string ToString() =>
        J2N.Numerics.Double.ToString(Value, "J", CultureInfo.InvariantCulture);
}
