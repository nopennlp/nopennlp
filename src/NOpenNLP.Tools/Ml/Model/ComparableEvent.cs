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
using System.Globalization;
using System.Text;
using NOpenNLP.Tools.Support;
using JSingle = J2N.Numerics.Single;

namespace NOpenNLP.Tools.Ml.Model;

/// <summary>
/// A maxent event representation which we can use to sort based on the
/// predicates indexes contained in the events.
/// </summary>
public class ComparableEvent(int oc, int[] pids, float[]? values = null) : IComparable<ComparableEvent>
{
    public int Outcome { get; set; } = oc;

    public int[] PredIndexes { get; set; } = pids;

    /// <summary>
    /// The number of times this event has been seen.
    /// </summary>
    public int Seen { get; set; } = 1;

    public float[]? Values { get; set; } = values;

    public int CompareTo(ComparableEvent? ce)
    {
        // NOpenNLP: upstream takes a non-null ComparableEvent; IComparable<T> in
        // .NET admits null, which sorts first by convention.
        if (ce is null)
        {
            return 1;
        }

        int compareOutcome = Outcome.CompareTo(ce.Outcome);
        if (compareOutcome != 0)
        {
            return compareOutcome;
        }

        int smallerLength = Math.Min(PredIndexes.Length, ce.PredIndexes.Length);

        for (int i = 0; i < smallerLength; i++)
        {
            int comparePredIndexes = PredIndexes[i].CompareTo(ce.PredIndexes[i]);
            if (comparePredIndexes != 0)
            {
                return comparePredIndexes;
            }

            // NOpenNLP: upstream compares with Float.compare, boxes the int result
            // back into a Float to test it against 0.0f, then casts to int. The
            // round-trip is a no-op for the -1/0/1 that Float.compare returns, so
            // the comparison is kept but the boxing is not.
            if (Values != null && ce.Values != null)
            {
                int compareValues = JSingle.Compare(Values[i], ce.Values[i]);
                if (compareValues != 0)
                {
                    return compareValues;
                }
            }
            else if (Values != null)
            {
                int compareValues = JSingle.Compare(Values[i], 1.0f);
                if (compareValues != 0)
                {
                    return compareValues;
                }
            }
            else if (ce.Values != null)
            {
                int compareValues = JSingle.Compare(1.0f, ce.Values[i]);
                if (compareValues != 0)
                {
                    return compareValues;
                }
            }
        }

        return PredIndexes.Length.CompareTo(ce.PredIndexes.Length);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is ComparableEvent other)
        {
            return Outcome == other.Outcome &&
                Arrays.Equals(PredIndexes, other.PredIndexes) &&
                Seen == other.Seen &&
                Arrays.Equals(Values, other.Values);
        }

        return false;
    }

    public override int GetHashCode()
        => HashCode.Combine(Outcome, Arrays.GetHashCode(PredIndexes), Seen, Arrays.GetHashCode(Values));

    public override string ToString()
    {
        // NOpenNLP: the integers are formatted invariantly because
        // StringBuilder.Append(int) uses the current culture, which for a locale
        // such as fa-IR renders a negative number with a different minus sign.
        StringBuilder s = new StringBuilder()
            .Append(Outcome.ToString(CultureInfo.InvariantCulture)).Append(':');
        for (int i = 0; i < PredIndexes.Length; i++)
        {
            s.Append(' ').Append(PredIndexes[i].ToString(CultureInfo.InvariantCulture));
            if (Values != null)
            {
                s.Append('=').Append(JSingle.ToString(Values[i], CultureInfo.InvariantCulture));
            }
        }

        return s.ToString();
    }
}
