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

namespace NOpenNLP.Tools.Ml.Model;

/// <summary>
/// A maxent predicate representation which we can use to sort based on the
/// outcomes. This allows us to make the mapping of features to their parameters
/// much more compact.
/// </summary>
public class ComparablePredicate(string n, int[] ocs, double[] ps) : IComparable<ComparablePredicate>
{
    public string Name { get; set; } = n;

    public int[] Outcomes { get; set; } = ocs;

    public double[] Params { get; set; } = ps;

    public int CompareTo(ComparablePredicate? cp)
    {
        // NOpenNLP: upstream takes a non-null ComparablePredicate; IComparable<T>
        // in .NET admits null, which sorts first by convention.
        if (cp is null)
        {
            return 1;
        }

        int smallerLength = Math.Min(Outcomes.Length, cp.Outcomes.Length);

        for (int i = 0; i < smallerLength; i++)
        {
            int compareOutcomes = Outcomes[i].CompareTo(cp.Outcomes[i]);
            if (compareOutcomes != 0)
            {
                return compareOutcomes;
            }
        }

        return Outcomes.Length.CompareTo(cp.Outcomes.Length);
    }

    public override int GetHashCode()
        => HashCode.Combine(Name, Arrays.GetHashCode(Outcomes), Arrays.GetHashCode(Params));

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is ComparablePredicate other)
        {
            return Equals(Name, other.Name) &&
                Arrays.Equals(Outcomes, other.Outcomes) &&
                Arrays.Equals(Params, other.Params);
        }

        return false;
    }

    public override string ToString()
    {
        StringBuilder s = new StringBuilder();
        foreach (int outcome in Outcomes)
        {
            // NOpenNLP: this string is written verbatim into the model file, so it
            // is formatted invariantly. StringBuilder.Append(int) uses the current
            // culture, which for a locale such as fa-IR renders a negative number
            // with a different minus sign and a left-to-right mark.
            s.Append(' ').Append(outcome.ToString(CultureInfo.InvariantCulture));
        }

        return s.ToString();
    }
}
